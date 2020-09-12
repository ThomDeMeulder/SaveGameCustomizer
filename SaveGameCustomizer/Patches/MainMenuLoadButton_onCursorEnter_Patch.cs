using HarmonyLib;

namespace SaveGameCustomizer.Patches
{
    [HarmonyPatch(typeof(MainMenuLoadButton), "onCursorEnter")]
    internal static class MainMenuLoadButton_onCursorEnter_Patch
    {
        private static bool Prefix()
        {
            return false;
        }
    }
}
