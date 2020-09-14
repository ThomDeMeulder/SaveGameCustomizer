using HarmonyLib;
using SaveGameCustomizer.Config;
using System.Collections.Generic;

namespace SaveGameCustomizer.Patches
{
    [HarmonyPatch(typeof(Language), "LoadLanguageFile")]
    internal static class Language_LoadLanguageFile_Patch
    {
        private static void Postfix(ref Dictionary<string, string> ___strings)
        {
            ___strings.Add(SaveGameConfig.EditButtonControllerText.Item1, SaveGameConfig.EditButtonControllerText.Item2);
            ___strings.Add(SaveGameConfig.ColourButtonControllerText.Item1, SaveGameConfig.ColourButtonControllerText.Item2);
        }
    }
}
