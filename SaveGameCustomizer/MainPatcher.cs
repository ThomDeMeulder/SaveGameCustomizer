using HarmonyLib;
using QModManager.API;
using QModManager.API.ModLoading;
using System;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace SaveGameCustomizer
{
    [QModCore]
    public static class MainPatcher
    {
        internal static Sprite SettingIcon { get; private set; }

        [QModPatch]
        public static void Patch()
        {
            var assetBundle = AssetBundle.LoadFromFile(Path.Combine("./QMods/SaveGameCustomizer/assets/", "subnautica_csg_assetbundle"));
            if (assetBundle == null)
            {
                QModServices.Main.AddCriticalMessage("Asset Bundle failed to load, download may be corrupt!");
                return;
            }

            SettingIcon = assetBundle.LoadAsset<Sprite>("setting_icon");
            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), "com.github.thom.save_game_customizer");
        }

        internal static void Log(string message)
        {
            Console.WriteLine($"SaveGameCustomizer: {message}");
        }
    }
}
