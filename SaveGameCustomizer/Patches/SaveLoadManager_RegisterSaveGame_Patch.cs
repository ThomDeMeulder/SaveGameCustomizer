using HarmonyLib;
using SaveGameCustomizer.Config;

namespace SaveGameCustomizer.Patches
{
    [HarmonyPatch(typeof(SaveLoadManager), "RegisterSaveGame")]
    internal static class SaveLoadManager_RegisterSaveGame_Patch
    {
        private static void Postfix(string slotName, UserStorageUtils.LoadOperation loadOperation)
        {
            if (loadOperation.GetSuccessful() && 
                loadOperation.files.TryGetValue(SaveGameConfig.CustomSaveGameFileName, out byte[] value) &&
                SaveGameConfig.GetConfigFromBytes(value, out SaveGameConfig config))
            {
                SaveGameCache.AddSaveGameConfigDataBySlot(slotName, config);
            }
        }
    }
}
