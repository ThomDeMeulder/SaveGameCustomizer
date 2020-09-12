using HarmonyLib;
using QModManager.API;
using SaveGameCustomizer.Config;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection.Emit;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

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
            MainPatcher.ChangeButtonPosition(deleteButtonTransform, true);

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

                // Fix scale and position
                MainPatcher.ChangeButtonPosition(editButton.transform, false);
                const float Scale = 0.8f;
                editButton.transform.localScale = new Vector3(Scale, Scale, Scale);

                // Set the new icon
                editButton.GetComponent<Image>().sprite = MainPatcher.SettingIcon;

                // Add the edit menu when clicked
                GameObject editMenu = UnityEngine.Object.Instantiate(lb.delete, lb.transform);
                editMenu.name = "Edit";

                // Input menu - TODO
                // Delete all the child objects to make room for our custom ones
                /*for (int i = 0; i < editMenu.transform.childCount; i++)
                {
                    UnityEngine.Object.Destroy(editMenu.transform.GetChild(i).gameObject);
                } TODO!!!! */

                // Make sure we can open the menu
                /*MethodInfo shiftAlphaMethod = AccessTools.Method(typeof(MainMenuLoadButton), "ShiftAlpha", new Type[] { typeof(CanvasGroup), typeof(float), typeof(float), typeof(float), typeof(bool), typeof(Selectable) });
                Button editButtonComponent = editButton.GetComponent<Button>();
                ButtonClickedEvent editButtonClickEvent = editButtonComponent.onClick = new ButtonClickedEvent();
                editButtonClickEvent.AddListener(() =>
                {
                    uGUI_MainMenu.main.OnRightSideOpened(editMenu);
                    uGUI_LegendBar.ClearButtons(); // REMOVES LEGEND FROM CONTROLLER!
                    CoroutineHost.StartCoroutine((IEnumerator)shiftAlphaMethod.Invoke(lb, new object[] { lb.load.GetComponent<CanvasGroup>(), 0f, lb.animTime, lb.alphaPower, false, null }));
                    CoroutineHost.StartCoroutine((IEnumerator)shiftAlphaMethod.Invoke(lb, new object[] { editMenu.GetComponent<CanvasGroup>(), 1f, lb.animTime, lb.alphaPower, false, null })); // TODO Make the last parameter a Selectable for controller support!
                });*/
                MainPatcher.ChangeEvenTriggers(editButton.GetComponent<EventTrigger>(), lightColour, darkColour);
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
