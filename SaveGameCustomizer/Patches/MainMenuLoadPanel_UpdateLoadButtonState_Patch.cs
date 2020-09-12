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
            ChangeButtonPosition(deleteButtonTransform, true);

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

            // Add the edit button
            GameObject editButton = UnityEngine.Object.Instantiate(deleteButtonTransform.gameObject, deleteButtonTransform.parent);
            editButton.name = "EditButton";

            // Fix scale and position
            ChangeButtonPosition(editButton.transform, false);
            const float Scale = 0.8f;
            editButton.transform.localScale = new Vector3(Scale, Scale, Scale);

            // Set the new icon and fix triggers
            editButton.GetComponent<Image>().sprite = MainPatcher.SettingIcon;
            ChangeEvenTriggers(editButton.GetComponent<EventTrigger>(), lightColour, darkColour);

            // Doesn't work apparently? UnityEngine.Object.Destroy(editButton.GetComponent<EventTrigger>());
            //Button editButtonComponent = editButton.GetComponent<Button>();

            // Change the background colour
            Image saveBackground = lb.load.GetComponent<Image>();
            saveBackground.color = SaveGameConfig.AllColours[config.ColourIndex].Item1;

            // Change the texture sprite to be the highlighted one, this is so we don't get dark / weird colours
            MainMenuLoadMenu menu = lb.transform.parent.parent.parent.GetComponent<MainMenuLoadMenu>();
            saveBackground.sprite = menu.selectedSprite;

            // Remove the UI trigger from the LoadButton and DeleteButton so it doesn't mess up the colours,
            // then we can add our own triggers to it.
            ChangeEvenTriggers(lb.transform.Find("Load/LoadButton").GetComponent<EventTrigger>(), lightColour, darkColour);
            ChangeEvenTriggers(lb.transform.Find("Load/DeleteButton").GetComponent<EventTrigger>(), lightColour, darkColour);
        }

        private static void ChangeEvenTriggers(EventTrigger trigger, Color lightColour, Color darkColour)
        {
            trigger.triggers.Clear();
            AddNewTriggers(trigger, lightColour, darkColour);
        }

        private static void ChangeButtonPosition(Transform button, bool positive)
        {
            Vector3 buttonLocalPosition = button.localPosition;
            buttonLocalPosition.x = 150;
            buttonLocalPosition.y = 18 * (positive ? 1 : -1);
            button.localPosition = buttonLocalPosition;
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
    }
}
