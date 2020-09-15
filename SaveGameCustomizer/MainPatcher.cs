using HarmonyLib;
using QModManager.API;
using QModManager.API.ModLoading;
using SaveGameCustomizer.Behaviours;
using SaveGameCustomizer.Config;
using SaveGameCustomizer.Events;
using System;
using System.Collections;
using System.IO;
using System.Reflection;
#if BELOWZERO
using TMPro;
#endif
using UnityEngine;
using UnityEngine.UI;
using UWE;

namespace SaveGameCustomizer
{
    [QModCore]
    public static class MainPatcher
    {
        internal static Sprite SettingIcon { get; private set; }
        internal static Sprite ArrowIcon { get; private set; }
        internal static Sprite Background { get; set; }

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

        internal static void RaiseSlotDataChangedEvent(SlotChangedData data)
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

        internal static void ChangeToEditMenu(GameObject editMenu, MainMenuLoadButton lb)
        {
            MethodInfo shiftAlphaMethod = AccessTools.Method(typeof(MainMenuLoadButton), "ShiftAlpha", new Type[] { typeof(CanvasGroup), typeof(float), typeof(float), typeof(float), typeof(bool), typeof(Selectable) });

            uGUI_MainMenu.main.OnRightSideOpened(editMenu);
            uGUI_LegendBar.ClearButtons(); // Removes the legend, controller support.
            CoroutineHost.StartCoroutine((IEnumerator)shiftAlphaMethod.Invoke(lb, new object[] { lb.load.GetComponent<CanvasGroup>(), 0f, lb.animTime, lb.alphaPower, false, null }));
            CoroutineHost.StartCoroutine((IEnumerator)shiftAlphaMethod.Invoke(lb, new object[] { editMenu.GetComponent<CanvasGroup>(), 1f, lb.animTime, lb.alphaPower, true, null }));
        }

        internal static void ChangeToSavesMenu(GameObject editMenu, MainMenuLoadButton lb)
        {
            MethodInfo shiftAlphaMethod = AccessTools.Method(typeof(MainMenuLoadButton), "ShiftAlpha", new Type[] { typeof(CanvasGroup), typeof(float), typeof(float), typeof(float), typeof(bool), typeof(Selectable) });

            MainMenuRightSide.main.OpenGroup("SavedGames");
            CoroutineHost.StartCoroutine((IEnumerator)shiftAlphaMethod.Invoke(lb, new object[] { lb.load.GetComponent<CanvasGroup>(), 1f, lb.animTime, lb.alphaPower, true, null }));
            CoroutineHost.StartCoroutine((IEnumerator)shiftAlphaMethod.Invoke(lb, new object[] { editMenu.GetComponent<CanvasGroup>(), 0f, lb.animTime, lb.alphaPower, false, null }));
        }

#if SUBNAUTICA
        internal static void UpdateColourIndex(int currentIndex, int addAmount, SelectedColours coloursComponent, InputField inputFieldComponent)
#elif BELOWZERO
        internal static void UpdateColourIndex(int currentIndex, int addAmount, SelectedColours coloursComponent, TMP_InputField inputFieldComponent)
#endif
        {
            if (currentIndex + addAmount >= SaveGameConfig.AllColours.Length)
            {
                coloursComponent.ColourIndex = 0;
            }
            else if (currentIndex + addAmount < 0)
            {
                coloursComponent.ColourIndex = SaveGameConfig.AllColours.Length - 1;
            }
            else
            {
                coloursComponent.ColourIndex += addAmount;
            }
            UpdateDisplayColoursOnClick(coloursComponent, inputFieldComponent);
        }

#if SUBNAUTICA
        internal static void UpdateDisplayColoursOnClick(SelectedColours coloursComponent, InputField inputFieldComponent)
#elif BELOWZERO
        internal static void UpdateDisplayColoursOnClick(SelectedColours coloursComponent, TMP_InputField inputFieldComponent)
#endif
        {
            inputFieldComponent.image.color = SaveGameConfig.AllColours[coloursComponent.ColourIndex].Item1;
        }
    }
}
