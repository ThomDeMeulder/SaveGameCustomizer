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

            // Change the delete button position
            Transform deleteButtonTransform = lb.load.FindChild("DeleteButton").transform;
            MainPatcher.ChangeButtonPosition(deleteButtonTransform, 150, 18);

            DateTime time = new DateTime(gameInfo.dateTicks);
            lb.load.FindChild("SaveGameTime").GetComponent<Text>().text = $"{config.Name} - {time.Day} {CultureInfo.GetCultureInfo("en-GB").DateTimeFormat.GetMonthName(time.Month)}";

            if (!gameInfo.IsValid())
            {
                // This means the ChangeSet is higher than the current version which means the game...
                // can't load that particular save. I can respect that.
                return;
            }

            // Get the colours for this save
            Color lightColour = SaveGameConfig.AllColours[config.ColourIndex].Item1;
            Color darkColour = SaveGameConfig.AllColours[config.ColourIndex].Item2;


            if (MainMenuLoadPanel_Start_Patch.IsStart)
            {
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
                editButton.GetComponent<Image>().sprite = MainPatcher.SettingIcon;

                // Add the edit menu
                GameObject editMenu = UnityEngine.Object.Instantiate(lb.delete, lb.transform);
                editMenu.name = "Edit";

                // Remove / add components from the menu
                UnityEngine.Object.Destroy(editMenu.GetComponent<MainMenuDeleteGame>());
                editMenu.AddComponent<MainMenuCustomizeGame>();

                // Make sure we can open the edit menu
                MethodInfo shiftAlphaMethod = AccessTools.Method(typeof(MainMenuLoadButton), "ShiftAlpha", new Type[] { typeof(CanvasGroup), typeof(float), typeof(float), typeof(float), typeof(bool), typeof(Selectable) });
                EventTrigger editButtonTriggerComponent = editButton.GetComponent<EventTrigger>();
                MainPatcher.ChangeEvenTriggers(editButtonTriggerComponent, lightColour, darkColour);

                EventTrigger.Entry entry = new EventTrigger.Entry();
                entry.eventID = EventTriggerType.PointerClick;
                entry.callback.AddListener((data) =>
                {
                    uGUI_MainMenu.main.OnRightSideOpened(editMenu);
                    uGUI_LegendBar.ClearButtons(); // REMOVES LEGEND FOR CONTROLLER!
                    CoroutineHost.StartCoroutine((IEnumerator)shiftAlphaMethod.Invoke(lb, new object[] { lb.load.GetComponent<CanvasGroup>(), 0f, lb.animTime, lb.alphaPower, false, null }));
                    CoroutineHost.StartCoroutine((IEnumerator)shiftAlphaMethod.Invoke(lb, new object[] { editMenu.GetComponent<CanvasGroup>(), 1f, lb.animTime, lb.alphaPower, true, null })); // TODO Make the last parameter a Selectable for controller support!
                });
                editButtonTriggerComponent.triggers.Add(entry);

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
                saveButton.onClick = new ButtonClickedEvent();
                saveButton.onClick.AddListener(() =>
                {
                    MainMenuRightSide.main.OpenGroup("SavedGames");
                    CoroutineHost.StartCoroutine((IEnumerator)shiftAlphaMethod.Invoke(lb, new object[] { lb.load.GetComponent<CanvasGroup>(), 1f, lb.animTime, lb.alphaPower, true, null }));
                    CoroutineHost.StartCoroutine((IEnumerator)shiftAlphaMethod.Invoke(lb, new object[] { editMenu.GetComponent<CanvasGroup>(), 0f, lb.animTime, lb.alphaPower, false, null })); // TODO Make the last parameter a Selectable for controller support!
                    // TODO save all variables modified (colour, name)!
                });

                // Edit the save button text
                GameObject saveButtonGameObjectText = saveButtonGameObject.transform.GetChild(0).gameObject;
                saveButtonGameObjectText.name = "EditMenuSaveButtonText";
                Text saveButtonText = saveButtonGameObjectText.GetComponent<Text>();
                saveButtonText.text = "Save";
                saveButtonText.color = Color.white;

                // Add the input menu
                GameObject inputMenuGameObject = UnityEngine.Object.Instantiate(saveButtonGameObject, editMenu.transform);
                MainPatcher.ChangeButtonPosition(inputMenuGameObject.transform, 80.0f, 0.0f);
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
                inputFieldComponent.textComponent.color = Color.black;

                // Set the offset for the rect transform
                RectTransform inputMenuRectTransform = inputMenuGameObject.GetComponent<RectTransform>();
                Vector2 offsetMin = inputMenuRectTransform.offsetMin;
                offsetMin.x = -40;
                inputMenuRectTransform.offsetMin = offsetMin;

                Vector2 offsetMax = inputMenuRectTransform.offsetMax;
                offsetMax.x = 90;
                inputMenuRectTransform.offsetMax = offsetMax;
            }


            // Change the background colour
            Image saveBackground = lb.load.GetComponent<Image>();
            saveBackground.color = SaveGameConfig.AllColours[config.ColourIndex].Item1;

            // Change the texture sprite to be the highlighted one, this is so we don't get dark / weird colours
            MainMenuLoadMenu menu = lb.transform.parent.parent.parent.GetComponent<MainMenuLoadMenu>();
            saveBackground.sprite = menu.selectedSprite;

            // Remove the UI trigger from all needed buttons and add our own triggers to it.
            MainPatcher.ChangeEvenTriggers(lb.transform.Find("Load/LoadButton").GetComponent<EventTrigger>(), lightColour, darkColour);
            MainPatcher.ChangeEvenTriggers(lb.transform.Find("Load/DeleteButton").GetComponent<EventTrigger>(), lightColour, darkColour);
        }
    }
}
