using HarmonyLib;
using SaveGameCustomizer.Behaviours;

namespace SaveGameCustomizer.Patches
{
    [HarmonyPatch(typeof(MainMenuLoadPanel), "Start")]
    internal static class MainMenuLoadPanel_Start_Patch
    {
        private static void Postfix(MainMenuLoadPanel __instance)
        {
            __instance.gameObject.AddComponent<MainMenuSaveGameUpdater>();
        }
    }
}
