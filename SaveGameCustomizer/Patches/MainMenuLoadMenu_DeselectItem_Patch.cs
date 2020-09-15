using HarmonyLib;
using SaveGameCustomizer.Behaviours;
using UnityEngine;
using UnityEngine.UI;

namespace SaveGameCustomizer.Patches
{
    [HarmonyPatch(typeof(MainMenuLoadMenu), "DeselectItem")]
    internal static class MainMenuLoadMenu_DeselectItem_Patch
    {
        private static void Prefix(GameObject ___selectedItem, out GameObject __state)
        {
            __state = ___selectedItem;
            if (___selectedItem == null)
            {
                return;
            }

            MainPatcher.HandleChangeColour(___selectedItem, false);
        }

        private static void Postfix(GameObject __state)
        {
            if (__state == null || __state.GetComponent<SelectedColours>() == null)
            {
                return;
            }

            __state.transform.GetChild(0).GetComponent<Image>().sprite = MainPatcher.Background;
        }
    }
}
