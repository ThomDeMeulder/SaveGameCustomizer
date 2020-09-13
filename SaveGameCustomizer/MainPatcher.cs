using HarmonyLib;
using QModManager.API;
using QModManager.API.ModLoading;
using SaveGameCustomizer.Behaviours;
using SaveGameCustomizer.Events;
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
        internal static Sprite ArrowIcon { get; private set; }

        internal static event Action<SlotChangedData> OnSlotDataChanged;

        [QModPatch]
        public static void Patch()
        {
            var assetBundle = AssetBundle.LoadFromFile(Path.Combine("./QMods/SaveGameCustomizer/assets/", "subnautica_sgc_bundle"));
            if (assetBundle == null)
            {
                QModServices.Main.AddCriticalMessage("Asset Bundle failed to load, download may be corrupt!");
                return;
            }

            SettingIcon = assetBundle.LoadAsset<Sprite>("setting_icon");
            ArrowIcon = assetBundle.LoadAsset<Sprite>("arrow_icon");
            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), "com.github.thom.save_game_customizer");
        }

        internal static void RaiseColourEvent(SlotChangedData data)
        {
            OnSlotDataChanged?.Invoke(data);
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

        internal static void HandleChangeColour(GameObject ___selectedItem, bool highlight)
        {
            SelectedColours component = ___selectedItem.GetComponent<SelectedColours>();
            if (component == null)
            {
                return;
            }

            Image selectedImageComponent = ___selectedItem.transform.GetChild(0).GetComponent<Image>();
            selectedImageComponent.color = highlight ? component.DarkerColour : component.SelectedColour;
        }
    }
}
