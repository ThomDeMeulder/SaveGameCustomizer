using HarmonyLib;
using QModManager.API;
using SaveGameCustomizer.Behaviours;
using SaveGameCustomizer.Config;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UWE;
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
                if (instruction.opcode.Equals(OpCodes.Ldstr) && instruction.operand.Equals("<color=#ff0000ff>Changeset {0} is newer than the current version!</color>"))
                {
                    yield return new CodeInstruction(OpCodes.Ldstr, "<color=#ffffffff>CS {0} is a save from the future!</color>");
                    continue;
                }
                yield return instruction;
            }
        }

        [HarmonyPostfix]
        private static void ChangeSaveGameData(MainMenuLoadButton lb)
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
                QModServices.Main.AddCriticalMessage("Something went wrong while loading your save data. Please report this to the developer with logs!");
                return;
            }

            // Change the background colour
            Image saveBackground = lb.load.GetComponent<Image>();
            saveBackground.color = SaveGameConfig.AllColours[config.ColourIndex].Item1;

            // Change the delete button position
            Transform deleteButtonTransform = lb.load.FindChild("DeleteButton").transform;
            MainPatcher.ChangeButtonPosition(deleteButtonTransform, 150, 18);

            ChangeSaveName(gameInfo, lb, config.Name);

            if (!gameInfo.IsValid())
            {
                // This means the ChangeSet is higher than the current version which means the game...
                // can't load that particular save. I can respect that.
                return;
            }

            // Get triggers on buttons
            EventTrigger deleteButtonEventTrigger = deleteButtonTransform.GetComponent<EventTrigger>();
            EventTrigger loadButtonEventTrigger = lb.transform.Find("Load/LoadButton").GetComponent<EventTrigger>();

            // Fix the button showing up for controller
            mGUI_Change_Legend_On_Select controllerSelectComponent = lb.GetComponent<mGUI_Change_Legend_On_Select>();
            controllerSelectComponent.legendButtonConfiguration = controllerSelectComponent.legendButtonConfiguration.AddToArray(new LegendButtonData
            {
                legendButtonIdx = 3,
                button = GameInput.Button.Jump,
                buttonDescription = SaveGameConfig.EditButtonControllerText.Item1
            });

            // Get the colours
            Color lightColour = SaveGameConfig.AllColours[config.ColourIndex].Item1;
            Color darkerColour = SaveGameConfig.AllColours[config.ColourIndex].Item2;

            if (lb.gameObject.GetComponent<SelectedColours>() == null)
            {
                // Add the live checker for controller support
                lb.gameObject.AddComponent<MainMenuEditButtonChanger>();

                // Add the SelectedColours component to the save for controller support
                SelectedColours colourComponent = lb.gameObject.AddComponent<SelectedColours>();
                colourComponent.SelectedColour = lightColour;
                colourComponent.DarkerColour = darkerColour;

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
                saveButtonGameObject.GetComponent<Image>().color = Color.green;

                // Edit the save button text
                GameObject saveButtonGameObjectText = saveButtonGameObject.transform.GetChild(0).gameObject;
                saveButtonGameObjectText.name = "EditMenuSaveButtonText";
                Text saveButtonText = saveButtonGameObjectText.GetComponent<Text>();
                saveButtonText.text = "Save";
                saveButtonText.color = Color.white;

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

                // Add input field component
                InputField inputFieldComponent = inputMenuGameObject.AddComponent<InputField>();
                inputFieldComponent.text = config.Name;
                inputFieldComponent.textComponent = inputMenuGameObject.transform.GetChild(0).GetComponent<Text>();
                inputFieldComponent.textComponent.color = Color.white;
                inputFieldComponent.characterLimit = 15;

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
                    MainMenuRightSide.main.OpenGroup("SavedGames");
                    CoroutineHost.StartCoroutine((IEnumerator)shiftAlphaMethod.Invoke(lb, new object[] { lb.load.GetComponent<CanvasGroup>(), 1f, lb.animTime, lb.alphaPower, true, null }));
                    CoroutineHost.StartCoroutine((IEnumerator)shiftAlphaMethod.Invoke(lb, new object[] { editMenu.GetComponent<CanvasGroup>(), 0f, lb.animTime, lb.alphaPower, false, null })); // TODO Make the last parameter a Selectable for controller support!

                    // Update all needed data
                    ChangeSaveName(gameInfo, lb, inputFieldComponent.text);
                    config.Name = inputFieldComponent.text;
                    ChangeSlotColourTriggers(saveBackground, deleteButtonEventTrigger, loadButtonEventTrigger, editButtonTriggerComponent, SaveGameConfig.AllColours[config.ColourIndex].Item1, SaveGameConfig.AllColours[config.ColourIndex].Item2);

                    // Notify where needed
                    MainPatcher.RaiseColourEvent(new Events.SlotChangedData
                    {
                        NewColourIndex = config.ColourIndex,
                        Object = lb.gameObject
                    });

                    // TODO Save to file.
                });

                // Change all colours and change/add/remove triggers
                EventTrigger.Entry entry = new EventTrigger.Entry();
                entry.eventID = EventTriggerType.PointerClick;
                entry.callback.AddListener((data) =>
                {
                    MainPatcher.ChangeToEditMenu(editMenu, lb, inputFieldComponent);
                });
                editButtonTriggerComponent.triggers.Add(entry);

                ChangeSlotColourTriggers(saveBackground, deleteButtonEventTrigger, loadButtonEventTrigger, editButtonTriggerComponent, lightColour, darkerColour);
                leftColourButtonImage.color = Color.white;
                rightColourButtonImage.color = Color.white;
                UpdateDisplayColoursOnClick(config, inputFieldComponent);
                ChangeEventTriggerForColourButton(leftColourButton.GetComponent<EventTrigger>(), config, -1, inputFieldComponent);
                ChangeEventTriggerForColourButton(rightColourButton.GetComponent<EventTrigger>(), config, 1, inputFieldComponent);
            }

            // Change the texture sprite to be the highlighted one, this is so we don't get dark / weird colours
            MainMenuLoadMenu menu = lb.transform.parent.parent.parent.GetComponent<MainMenuLoadMenu>();
            saveBackground.sprite = menu.selectedSprite;
        }

        private static void ChangeSlotColourTriggers(Image slotBackGroundImage, EventTrigger deleteButton, EventTrigger loadButton, EventTrigger editButton, Color lightColour, Color darkColour)
        {
            slotBackGroundImage.color = lightColour;
            ChangeEvenTriggers(loadButton, lightColour, darkColour);
            ChangeEvenTriggers(deleteButton, lightColour, darkColour);
            ChangeEvenTriggers(editButton, lightColour, darkColour, true);
        }

        private static void UpdateDisplayColoursOnClick(SaveGameConfig config, InputField inputFieldComponent)
        {
            inputFieldComponent.image.color = SaveGameConfig.ProperColours[config.ColourIndex];
        }

        private static void ChangeEventTriggerForColourButton(EventTrigger eventTrigger, SaveGameConfig config, int addAmount, InputField inputFieldComponent)
        {
            eventTrigger.gameObject.GetComponent<Button>().transition = Selectable.Transition.None;
            eventTrigger.triggers.Clear();
            eventTrigger.triggers.Add(CreateEntry(config, addAmount, inputFieldComponent));
        }

        private static EventTrigger.Entry CreateEntry(SaveGameConfig config, int addAmount, InputField inputFieldComponent)
        {
            EventTrigger.Entry entry = new EventTrigger.Entry();
            entry.eventID = EventTriggerType.PointerClick;
            entry.callback.AddListener(data => 
            {
                if (config.ColourIndex + addAmount >= SaveGameConfig.AllColours.Length)
                {
                    config.ColourIndex = 0;
                } 
                else if (config.ColourIndex + addAmount < 0)
                {
                    config.ColourIndex = SaveGameConfig.AllColours.Length - 1;
                }
                else
                {
                    config.ColourIndex += addAmount;
                }
                UpdateDisplayColoursOnClick(config, inputFieldComponent);
            });
            return entry;
        }

        private static void ChangeEvenTriggers(EventTrigger trigger, Color lightColour, Color darkColour, bool skipClear = false)
        {
            if (!skipClear) trigger.triggers.Clear();
            AddNewTriggers(trigger, lightColour, darkColour);
        }

        private static void AddNewTriggers(EventTrigger eventTrigger, Color lightColour, Color darkColour)
        {
            Image image = eventTrigger.gameObject.transform.parent.GetComponent<Image>();

            eventTrigger.triggers.Add(CreateEntry(image, darkColour, EventTriggerType.PointerEnter));
            eventTrigger.triggers.Add(CreateEntry(image, lightColour, EventTriggerType.PointerExit));
        }

        private static EventTrigger.Entry CreateEntry(Image image, Color newImageColour, EventTriggerType triggerType)
        {
            EventTrigger.Entry entry = new EventTrigger.Entry();
            entry.eventID = triggerType;
            entry.callback.AddListener((data) => image.color = newImageColour);
            return entry;
        }

        private static void ChangeSaveName(SaveLoadManager.GameInfo gameInfo, MainMenuLoadButton lb, string newName)
        {
            DateTime time = new DateTime(gameInfo.dateTicks);
            lb.load.FindChild("SaveGameTime").GetComponent<Text>().text = $"{newName} - {time.Day} {CultureInfo.GetCultureInfo("en-GB").DateTimeFormat.GetMonthName(time.Month)}";
        }
    }
}
