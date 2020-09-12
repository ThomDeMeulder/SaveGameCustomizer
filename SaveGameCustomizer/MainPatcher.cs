using HarmonyLib;
using QModManager.API;
using QModManager.API.ModLoading;
using System;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SaveGameCustomizer
{
    [QModCore]
    public static class MainPatcher
    {
        internal static Sprite SettingIcon { get; private set; }

        [QModPatch]
        public static void Patch()
        {
            var assetBundle = AssetBundle.LoadFromFile(Path.Combine("./QMods/SaveGameCustomizer/assets/", "subnautica_csg_assetbundle"));
            if (assetBundle == null)
            {
                QModServices.Main.AddCriticalMessage("Asset Bundle failed to load, download may be corrupt!");
                return;
            }

            SettingIcon = assetBundle.LoadAsset<Sprite>("setting_icon");
            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), "com.github.thom.save_game_customizer");
        }

        internal static void Log(string message)
        {
            Console.WriteLine($"SaveGameCustomizer: {message}");
        }

        internal static void ChangeButtonPosition(Transform button, float x, float y)
        {
            Vector3 buttonLocalPosition = button.localPosition;
            buttonLocalPosition.x = x;
            buttonLocalPosition.y = y;
            button.localPosition = buttonLocalPosition;
        }

        internal static void ChangeEvenTriggers(EventTrigger trigger, Color lightColour, Color darkColour)
        {
            trigger.triggers.Clear();
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
    }
}
