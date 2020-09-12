using HarmonyLib;
using SaveGameCustomizer.Config;
using System.Collections.Generic;

namespace SaveGameCustomizer.Patches
{
    [HarmonyPatch(typeof(UserStoragePC), "LoadSlotsAsync")]
    internal static class UserStoragePC_LoadSlotsAsync_Patch
    {
        private static void Prefix(ref List<string> fileNames)
        {
            SaveGameCache.ClearAll();
            fileNames.Add(SaveGameConfig.CustomSaveGameFileName);
        }
    }
}
