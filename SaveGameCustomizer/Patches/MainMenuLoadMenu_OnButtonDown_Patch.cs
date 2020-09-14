using HarmonyLib;
using UnityEngine;

namespace SaveGameCustomizer.Patches
{
    [HarmonyPatch(typeof(MainMenuLoadMenu), "OnButtonDown")]
    internal static class MainMenuLoadMenu_OnButtonDown_Patch
    {
        private static bool Prefix(ref bool __result, GameInput.Button button, GameObject ___selectedItem)
        {
            if (button == GameInput.Button.Jump)
            {
                // Change to edit menu if needed
                if (___selectedItem != null && ___selectedItem.name != "NewGame")
                {
                    // Change to edit menu
                    MainPatcher.ChangeToEditMenu(___selectedItem.transform.Find("Edit").gameObject, ___selectedItem.GetComponent<MainMenuLoadButton>());

                    // Return needed results
                    __result = true;
                    return false;
                }
            }

            return true;
        }
    }
}
