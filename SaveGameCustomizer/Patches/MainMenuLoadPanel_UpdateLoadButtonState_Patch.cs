using HarmonyLib;
#if BELOWZERO
using Newtonsoft.Json;
#elif SUBNAUTICA
using Oculus.Newtonsoft.Json;
#endif
using QModManager.API;
using SaveGameCustomizer.Behaviours;
using SaveGameCustomizer.Config;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Reflection.Emit;
#if BELOWZERO
using TMPro;
#endif
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static UnityEngine.UI.Button;

namespace SaveGameCustomizer.Patches
{
    [HarmonyPatch(typeof(MainMenuLoadPanel), "UpdateLoadButtonState")]
    internal static class MainMenuLoadPanel_UpdateLoadButtonState_Patch
    {
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> ChangeColourOnError(IEnumerable<CodeInstruction> instructions)
        {
            foreach (CodeInstruction instruction in instructions)
            {
#if BELOWZERO
                if (instruction.opcode.Equals(OpCodes.Ldstr) && instruction.operand.Equals("<color=#ff0000ff>"))
                {
                    yield return new CodeInstruction(OpCodes.Ldstr, "<color=#ffffffff>");
                    continue;
                }
#elif SUBNAUTICA
                if (instruction.opcode.Equals(OpCodes.Ldstr) && instruction.operand.Equals("<color=#ff0000ff>Changeset {0} is newer than the current version!</color>"))
                {
                    yield return new CodeInstruction(OpCodes.Ldstr, "<color=#ffffffff>CS {0} is a save from the future!</color>");
                    continue;
                }
#endif
                yield return instruction;
            }
        }

        [HarmonyPostfix]
        private static void ChangeSaveGameData(MainMenuLoadButton lb)
        {
            try
            {
                SaveLoadManager.GameInfo gameInfo = SaveLoadManager.main.GetGameInfo(lb.saveGame);
                if (gameInfo == null)
                {
                    // Something went wrong at UWE's part or a user messed up their save game files.
                    return;
                }

                if (!SaveGameCache.GetSaveGameConfigDataFromSlotName(lb.saveGame, out SaveGameConfig config))
                {
                    // Something went wrong with loading our data. This should NOT have happened in any case!
                    QModServices.Main.AddCriticalMessage("Something went wrong while loading your save data!");
                    return;
                }

                // Change the delete button position
                Transform deleteButtonTransform = lb.load.FindChild("DeleteButton").transform;
                MainPatcher.ChangeButtonPosition(deleteButtonTransform, 150, 18);

                bool shouldChangeFontSize = gameInfo.IsValid();
#if BELOWZERO
                if (gameInfo.storyVersion != 2)
                {
                    shouldChangeFontSize = false;
                }
#endif

                string saveName = config.Name;
                if (!shouldChangeFontSize)
                {
                    saveName = $"<color=#ffffffff>{saveName}</color>";
                }
                ChangeSaveName(gameInfo, lb, saveName, shouldChangeFontSize);

                if (!gameInfo.IsValid())
                {
                    // This means the ChangeSet is higher than the current version which means the game...
                    // can't load that particular save. I can respect that.
                    return;
                }

#if BELOWZERO
                if (gameInfo.storyVersion != 2)
                {
                    // Incompatible story save game for Below Zero, I can also respect this.
                    return;
                }
#endif

                // Change the background colour
                Image saveBackground = lb.load.GetComponent<Image>();
                saveBackground.color = SaveGameConfig.AllColours[config.ColourIndex];

                // Move the last played date to the play time text and change the scale a little
                Vector3 NewTextScale = new Vector3(0.8f, 0.8f, 0.8f);

                DateTime time = new DateTime(gameInfo.dateTicks);
                string month = CultureInfo.GetCultureInfo("en-GB").DateTimeFormat.GetMonthName(time.Month);
                string playTime = Utils.PrettifyTime(gameInfo.gameTime);
#if BELOWZERO
                lb.saveGameLengthText.text = $"{time.Day} {month} - {playTime}";
                lb.saveGameLengthText.gameObject.transform.localScale = NewTextScale;
                MainPatcher.ChangeButtonPosition(lb.saveGameLengthText.gameObject.transform, -63.09f, 8.0f);
#elif SUBNAUTICA
                GameObject saveGameTextGameObject = lb.load.FindChild("SaveGameLength");
                MainPatcher.ChangeButtonPosition(saveGameTextGameObject.transform, -63.09f, 8.0f);
                saveGameTextGameObject.GetComponent<Text>().text = $"{time.Day} {month} - {playTime}";
                saveGameTextGameObject.transform.localScale = NewTextScale;
#endif

                // Get triggers on buttons
                EventTrigger deleteButtonEventTrigger = deleteButtonTransform.GetComponent<EventTrigger>();
                EventTrigger loadButtonEventTrigger = lb.transform.Find("Load/LoadButton").GetComponent<EventTrigger>();

#if SUBNAUTICA
                // Fix the button showing up for controller
                mGUI_Change_Legend_On_Select controllerSelectComponent = lb.GetComponent<mGUI_Change_Legend_On_Select>();
                controllerSelectComponent.legendButtonConfiguration = controllerSelectComponent.legendButtonConfiguration.AddToArray(new LegendButtonData
                {
                    legendButtonIdx = 3,
                    button = GameInput.Button.Jump,
                    buttonDescription = SaveGameConfig.EditButtonControllerText.Item1
                });
#endif

                MainMenuLoadMenu menu = lb.transform.parent.parent.parent.GetComponent<MainMenuLoadMenu>();

                // Get the colour
                Color lightColour = SaveGameConfig.AllColours[config.ColourIndex];

                if (lb.gameObject.GetComponent<SelectedColours>() == null)
                {
                    // Add the live checker for controller support
                    lb.gameObject.AddComponent<MainMenuEditButtonChanger>();

                    // Add the SelectedColours component to the save for controller support
                    SelectedColours colourComponent = lb.gameObject.AddComponent<SelectedColours>();
                    colourComponent.SelectedColour = lightColour;
                    colourComponent.ColourIndex = config.ColourIndex;

                    // Add the edit button
                    GameObject editButton = UnityEngine.Object.Instantiate(deleteButtonTransform.gameObject, deleteButtonTransform.parent);
                    editButton.name = "EditButton";

                    // Remove old trigger
                    editButton.GetComponent<Button>().onClick = new ButtonClickedEvent();

                    // Fix scale and position
                    MainPatcher.ChangeButtonPosition(editButton.transform, 150, -18);
                    const float Scale = 0.8f;
                    editButton.transform.localScale = new Vector3(Scale, Scale, Scale);

                    // Set the new icon
                    Image editButtonImage = editButton.GetComponent<Image>();
                    editButtonImage.sprite = MainPatcher.SettingIcon;

                    // Add the edit menu
                    GameObject editMenu = UnityEngine.Object.Instantiate(lb.delete, lb.transform);
                    editMenu.name = "Edit";

                    // Remove / add components from the menu
                    UnityEngine.Object.Destroy(editMenu.GetComponent<MainMenuDeleteGame>());
                    UnityEngine.Object.Destroy(editMenu.GetComponent<TranslationLiveUpdate>());
                    editMenu.AddComponent<MainMenuCustomizeGame>();

                    // Make sure we can open the edit menu
                    MethodInfo shiftAlphaMethod = AccessTools.Method(typeof(MainMenuLoadButton), "ShiftAlpha", new Type[] { typeof(CanvasGroup), typeof(float), typeof(float), typeof(float), typeof(bool), typeof(Selectable) });
                    EventTrigger editButtonTriggerComponent = editButton.GetComponent<EventTrigger>();
                    editButtonTriggerComponent.triggers.Clear();

                    // Delete all the child objects to make room for our custom ones
                    for (int i = 0; i < editMenu.transform.childCount; i++)
                    {
                        UnityEngine.Object.Destroy(editMenu.transform.GetChild(i).gameObject);
                    }

                    // Add the save button to the edit menu now that the edit menu children are cleared
                    GameObject saveButtonGameObject = UnityEngine.Object.Instantiate(lb.transform.Find("Delete/DeleteCancelButton").gameObject, editMenu.transform);
                    MainPatcher.ChangeButtonPosition(saveButtonGameObject.transform, 130.0f, 0.0f);
                    saveButtonGameObject.name = "EditMenuSaveButton";

                    // Destroy unneeded component
                    UnityEngine.Object.DestroyImmediate(saveButtonGameObject.GetComponent<TranslationLiveUpdate>());

                    Button saveButton = saveButtonGameObject.GetComponent<Button>();
                    saveButtonGameObject.GetComponent<Image>().color = Color.green; ;

                    // Edit the save button text
                    GameObject saveButtonGameObjectText = saveButtonGameObject.transform.GetChild(0).gameObject;
                    saveButtonGameObjectText.name = "EditMenuSaveButtonText";
#if BELOWZERO
                    TextMeshProUGUI saveButtonText = saveButtonGameObjectText.GetComponent<TextMeshProUGUI>();
                    saveButtonText.text = "Save";
                    saveButtonText.color = Color.white;
#elif SUBNAUTICA
                    Text saveButtonText = saveButtonGameObjectText.GetComponent<Text>();
                    saveButtonText.text = "Save";
                    saveButtonText.color = Color.white;
#endif

                    // Add the colour buttons
                    GameObject leftColourButton = UnityEngine.Object.Instantiate(saveButtonGameObject, editMenu.transform);

                    // Resize the clickable area
                    RectTransform leftColourButtonRectTransform = leftColourButton.GetComponent<RectTransform>();
                    Vector2 offsetMaxLeftColourButton = leftColourButtonRectTransform.offsetMax;
                    offsetMaxLeftColourButton.x = -20;
                    leftColourButtonRectTransform.offsetMax = offsetMaxLeftColourButton;
                    Vector2 sizeDeltaLeftColourButton = leftColourButtonRectTransform.sizeDelta;
                    sizeDeltaLeftColourButton.x = 52;
                    leftColourButtonRectTransform.sizeDelta = sizeDeltaLeftColourButton;

                    MainPatcher.ChangeButtonPosition(leftColourButton.transform, -110.0f, 0.0f);
                    leftColourButton.name = "EditMenuLeftColourButton";

                    // Destroy unneeded child
                    UnityEngine.Object.DestroyImmediate(leftColourButton.transform.GetChild(0).gameObject);

                    // Change the sprite
                    Image leftColourButtonImage = leftColourButton.GetComponent<Image>();
                    leftColourButtonImage.sprite = MainPatcher.ArrowIcon;
                    leftColourButtonImage.canvasRenderer.SetColor(Color.white);

                    // Remove the old input event
                    leftColourButton.GetComponent<Button>().onClick = new ButtonClickedEvent();

                    // Initialise the right colour button
                    GameObject rightColourButton = UnityEngine.Object.Instantiate(leftColourButton, editMenu.transform);
                    rightColourButton.name = "EditMenuRightColourButton";
                    Vector3 rightColourButtonEuler = rightColourButton.transform.eulerAngles;
                    rightColourButtonEuler.z = 180.0f;
                    rightColourButton.transform.eulerAngles = rightColourButtonEuler;

                    MainPatcher.ChangeButtonPosition(rightColourButton.transform, 50.0f, 0.0f);
                    Image rightColourButtonImage = rightColourButton.GetComponent<Image>();
                    rightColourButtonImage.canvasRenderer.SetColor(Color.white);

                    // Add the input menu
                    GameObject inputMenuGameObject = UnityEngine.Object.Instantiate(saveButtonGameObject, editMenu.transform);
                    inputMenuGameObject.name = "EditMenuInputMenu";

                    // Destroy all unneeded components / children
                    UnityEngine.Object.Destroy(inputMenuGameObject.GetComponent<EventTrigger>());
                    UnityEngine.Object.DestroyImmediate(inputMenuGameObject.GetComponent<Button>());

                    // Change the background image color
                    inputMenuGameObject.GetComponent<Image>().color = Color.white;

#if SUBNAUTICA
                    // Add input field component
                    InputField inputFieldComponent = inputMenuGameObject.AddComponent<InputField>();
                    inputFieldComponent.text = config.Name;
                    inputFieldComponent.textComponent = inputMenuGameObject.transform.GetChild(0).GetComponent<Text>();
                    inputFieldComponent.textComponent.color = Color.white;
                    inputFieldComponent.characterLimit = 21;
#elif BELOWZERO
                    TMP_InputField tmpInputField = inputMenuGameObject.AddComponent<TMP_InputField>();
                    tmpInputField.textComponent = inputMenuGameObject.transform.GetChild(0).GetComponent<TextMeshProUGUI>();
                    tmpInputField.textComponent.fontSize = 10;
                    tmpInputField.text = config.Name;
                    inputMenuGameObject.SetActive(false);
                    inputMenuGameObject.SetActive(true);
                    tmpInputField.characterLimit = 21;
#endif

                    // Set the offset for the rect transform
                    RectTransform inputMenuRectTransform = inputMenuGameObject.GetComponent<RectTransform>();
                    Vector2 offsetMin = inputMenuRectTransform.offsetMin;
                    offsetMin.x = -40;
                    inputMenuRectTransform.offsetMin = offsetMin;

                    Vector2 offsetMax = inputMenuRectTransform.offsetMax;
                    offsetMax.x = 90;
                    inputMenuRectTransform.offsetMax = offsetMax;

                    MainPatcher.ChangeButtonPosition(inputMenuGameObject.transform, -30.0f, 0.0f);

                    // Save the data when clicking the save button
                    saveButton.onClick = new ButtonClickedEvent();
                    saveButton.onClick.AddListener(() =>
                    {
                        MainPatcher.ChangeToSavesMenu(editMenu, lb);

                        // Update all needed data
#if SUBNAUTICA
                        ChangeSaveName(gameInfo, lb, inputFieldComponent.text, true);
#elif BELOWZERO
                        ChangeSaveName(gameInfo, lb, tmpInputField.text, true);
#endif
                        ChangeSlotColourTriggers(menu, saveBackground, deleteButtonEventTrigger, loadButtonEventTrigger, editButtonTriggerComponent, SaveGameConfig.AllColours[colourComponent.ColourIndex]);

                        // Notify where needed
                        MainPatcher.RaiseSlotDataChangedEvent(new Events.SlotChangedData
                        {
                            NewColourIndex = colourComponent.ColourIndex,
                            Object = lb.gameObject
                        });

                        // Save to the needed file
#if SUBNAUTICA
                        config.Name = inputFieldComponent.text;
#elif BELOWZERO
                        config.Name = tmpInputField.text;
#endif
                        config.ColourIndex = colourComponent.ColourIndex;

                        try
                        {
                            MainPatcher.Log($"Saving settings for slot: {lb.saveGame}");
                            byte[] data = SaveGameConfig.SerializeConfig(config);
                            PlatformUtils.main.GetUserStorage().SaveFilesAsync(lb.saveGame, new Dictionary<string, byte[]>
                            {
                                {
                                    SaveGameConfig.CustomSaveGameFileName,
                                    data
                                }
                            });
                        }
                        catch (JsonException serializeException)
                        {
                            MainPatcher.Log($"Exception while saving for slot: {lb.saveGame}");
                            MainPatcher.Log(serializeException.Message);
                        }
                    });

                    // Change all colours and change/add/remove triggers
                    EventTrigger.Entry entry = new EventTrigger.Entry();
                    entry.eventID = EventTriggerType.PointerClick;
                    entry.callback.AddListener((data) =>
                    {
                        MainPatcher.ChangeToEditMenu(editMenu, lb);
                    });
                    editButtonTriggerComponent.triggers.Add(entry);

                    ChangeSlotColourTriggers(menu, saveBackground, deleteButtonEventTrigger, loadButtonEventTrigger, editButtonTriggerComponent, lightColour);
                    leftColourButtonImage.color = Color.white;
                    rightColourButtonImage.color = Color.white;
#if SUBNAUTICA
                    MainPatcher.UpdateDisplayColoursOnClick(colourComponent, inputFieldComponent);
                    ChangeEventTriggerForColourButton(leftColourButton.GetComponent<EventTrigger>(), colourComponent, -1, inputFieldComponent);
                    ChangeEventTriggerForColourButton(rightColourButton.GetComponent<EventTrigger>(), colourComponent, 1, inputFieldComponent);
#elif BELOWZERO
                    MainPatcher.UpdateDisplayColoursOnClick(colourComponent, tmpInputField);
                    ChangeEventTriggerForColourButton(leftColourButton.GetComponent<EventTrigger>(), colourComponent, -1, tmpInputField);
                    ChangeEventTriggerForColourButton(rightColourButton.GetComponent<EventTrigger>(), colourComponent, 1, tmpInputField);
#endif
                }

                // Change the texture sprite to be the custom one so we don't get weird colour issues.
                if (MainPatcher.Background == null)
                {
                    Texture2D texture2D = DuplicateTexture(menu.selectedSprite.texture);
                    for (int x = 0; x < texture2D.width; x++)
                    {
                        for (int y = 0; y < texture2D.height; y++)
                        {
                            if (Mathf.Abs(texture2D.GetPixel(x, y).a) <= 0.001f) continue;
                            texture2D.SetPixel(x, y, Color.white);
                        }
                    }

                    texture2D.anisoLevel = 9;
                    texture2D.Apply();
                    MainPatcher.Background = Sprite.Create(texture2D, menu.selectedSprite.rect, menu.selectedSprite.pivot, menu.selectedSprite.pixelsPerUnit, 0, SpriteMeshType.FullRect, menu.selectedSprite.border);
                    MainPatcher.Background.name = "SGC_RuntimeBackgroundSprite";
                }

                saveBackground.sprite = MainPatcher.Background;
            }
            catch (Exception exception)
            {
                const string ErrorMessageText = "Error while loading a save. Check the logs for more information!";
#if BELOWZERO
                ErrorMessage.AddError(ErrorMessageText);
#elif SUBNAUTICA
                // Breaks in Below Zero, so we'll have to do it another way.
                QModServices.Main.AddCriticalMessage(ErrorMessageText);
#endif
                MainPatcher.Log("EXCEPTION WHILE LOADING SAVE FILE!");
                MainPatcher.Log(exception.Message);
                MainPatcher.Log(exception.StackTrace);
            }
        }

        // https://stackoverflow.com/questions/44733841/how-to-make-texture2d-readable-via-script
        private static Texture2D DuplicateTexture(Texture2D source)
        {
            RenderTexture renderTex = RenderTexture.GetTemporary(
                        source.width,
                        source.height,
                        0,
                        RenderTextureFormat.Default,
                        RenderTextureReadWrite.Linear);

            Graphics.Blit(source, renderTex);
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = renderTex;
            Texture2D readableText = new Texture2D(source.width, source.height);
            readableText.ReadPixels(new Rect(0, 0, renderTex.width, renderTex.height), 0, 0);
            readableText.Apply();
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(renderTex);
            return readableText;
        }

        private static void ChangeSlotColourTriggers(MainMenuLoadMenu menu, Image slotBackGroundImage, EventTrigger deleteButton, EventTrigger loadButton, EventTrigger editButton, Color lightColour)
        {
            slotBackGroundImage.color = lightColour;
            ChangeEvenTriggers(menu, loadButton, lightColour);
            ChangeEvenTriggers(menu, deleteButton, lightColour);
            ChangeEvenTriggers(menu, editButton, lightColour, true);
        }

#if SUBNAUTICA
        private static void ChangeEventTriggerForColourButton(EventTrigger eventTrigger, SelectedColours coloursComponent, int addAmount, InputField inputFieldComponent)
#elif BELOWZERO
        private static void ChangeEventTriggerForColourButton(EventTrigger eventTrigger, SelectedColours coloursComponent, int addAmount, TMP_InputField inputFieldComponent)
#endif
        {
            eventTrigger.gameObject.GetComponent<Button>().transition = Selectable.Transition.None;
            eventTrigger.triggers.Clear();
            eventTrigger.triggers.Add(CreateEntry(coloursComponent, addAmount, inputFieldComponent));
        }

#if SUBNAUTICA
        private static EventTrigger.Entry CreateEntry(SelectedColours coloursComponent, int addAmount, InputField inputFieldComponent)
#elif BELOWZERO
        private static EventTrigger.Entry CreateEntry(SelectedColours coloursComponent, int addAmount, TMP_InputField inputFieldComponent)
#endif
        {
            EventTrigger.Entry entry = new EventTrigger.Entry();
            entry.eventID = EventTriggerType.PointerClick;
            entry.callback.AddListener(data => 
            {
                MainPatcher.UpdateColourIndex(coloursComponent.ColourIndex, addAmount, coloursComponent, inputFieldComponent);
            });
            return entry;
        }

        private static void ChangeEvenTriggers(MainMenuLoadMenu menu, EventTrigger trigger, Color lightColour, bool skipClear = false)
        {
            if (!skipClear) trigger.triggers.Clear();
            AddNewTriggers(menu, trigger, lightColour);
        }

        private static void AddNewTriggers(MainMenuLoadMenu menu, EventTrigger eventTrigger, Color lightColour)
        {
            Image image = eventTrigger.gameObject.transform.parent.GetComponent<Image>();

            eventTrigger.triggers.Add(CreateEntry(menu, image, Color.white, EventTriggerType.PointerEnter));
            eventTrigger.triggers.Add(CreateEntry(menu, image, lightColour, EventTriggerType.PointerExit));
        }

        private static EventTrigger.Entry CreateEntry(MainMenuLoadMenu menu, Image image, Color newImageColour, EventTriggerType triggerType)
        {
            EventTrigger.Entry entry = new EventTrigger.Entry();
            entry.eventID = triggerType;
            entry.callback.AddListener(data => 
            {
                if (triggerType == EventTriggerType.PointerEnter)
                    image.sprite = menu.selectedSprite;
                else if (triggerType == EventTriggerType.PointerExit)
                    image.sprite = MainPatcher.Background;

                image.color = newImageColour;
            });
            return entry;
        }

        private static void ChangeSaveName(SaveLoadManager.GameInfo gameInfo, MainMenuLoadButton lb, string newName, bool shouldChangeFontSize)
        {
#if BELOWZERO
            lb.saveGameTimeText.text = newName;
            if (shouldChangeFontSize)
            {
                lb.saveGameTimeText.transform.localScale = new Vector3(1.05f, 1.05f, 1.05f);
                lb.saveGameTimeText.fontSize = 13;
            }
#elif SUBNAUTICA
            Text textComponent = lb.load.FindChild("SaveGameTime").GetComponent<Text>();
            textComponent.text = newName;
            if (shouldChangeFontSize) textComponent.fontSize = 14;
#endif
        }
    }
}
