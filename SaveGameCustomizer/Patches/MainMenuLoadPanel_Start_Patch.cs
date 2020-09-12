using HarmonyLib;

namespace SaveGameCustomizer.Patches
{
    [HarmonyPatch(typeof(MainMenuLoadPanel), "Start")]
    internal static class MainMenuLoadPanel_Start_Patch
    {
        internal static bool IsStart { get; private set; }

        [HarmonyPrefix]
        private static void Prefix()
        {
            IsStart = true;
        }

        [HarmonyPostfix]
        private static void Postfix()
        {
            IsStart = false;
        }
    }
}
