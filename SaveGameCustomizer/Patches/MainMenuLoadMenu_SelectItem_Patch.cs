using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace SaveGameCustomizer.Patches
{
    [HarmonyPatch(typeof(MainMenuLoadMenu), "SelectItem")]
    internal static class MainMenuLoadMenu_SelectItem_Patch
    {
        private static void Postfix(GameObject ___selectedItem)
        {
            MainPatcher.HandleChangeColour(___selectedItem, true);
        }
    }
}
