#if BELOWZERO
using Newtonsoft.Json;
#else
using Oculus.Newtonsoft.Json;
#endif
using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace SaveGameCustomizer.Config
{
    internal class SaveGameConfig
    {
        // Key / Translation
        public static Tuple<string, string> EditButtonControllerText { get; } = new Tuple<string, string>("subnautica.sgc_mod.edit_button_controller_text", "Edit");
        public static Tuple<string, string> ColourButtonControllerText { get; } = new Tuple<string, string>("subnautica.sgc_mod.colour_button_controller_text", "Swap Colour");

        public const string CustomSaveGameFileName = "sgc_config.json";
        private static readonly System.Random random = new System.Random();
        private static readonly string[] randomSaveGameNames =
        {
            "Mango",
            "Lemon",
            "Coconut",
            "Strawberry",
            "Apple",
            "Banana",
            "Pineapple",
            "Avocado",
            "Peach",
            "Papaya",
            "Watermelon",
            "Kiwi",
            "Blackberry",
            "Cherry"
        };

        public static readonly Color[] AllColours =
        {
            CreateColourFromRGB(18.0f, 224.0f, 25.0f), // Light Green
            CreateColourFromRGB(15.0f, 189.0f, 21.0f), // Green
            CreateColourFromRGB(11.0f, 133.0f, 15.0f), // Dark Green
            CreateColourFromRGB(255.0f, 153.0f, 0.0f), // Orange
            CreateColourFromRGB(194.0f, 116.0f, 0.0f), // Dark Orange / Brown
            CreateColourFromRGB(242.0f, 109.0f, 242.0f), // Magenta / Pink (or something like that)
            CreateColourFromRGB(235.0f, 23.0f, 235.0f), // Purple
            CreateColourFromRGB(143.0f, 16.0f, 143.0f), // Dark Purple
            CreateColourFromRGB(207.0f, 23.0f, 23.0f), // Red
            CreateColourFromRGB(150.0f, 18.0f, 18.0f), // Dark Red
            CreateColourFromRGB(11.0f, 109.0f, 214.0f), // Blue
            CreateColourFromRGB(2.0f, 73.0f, 150.0f), // Dark Blue
            CreateColourFromRGB(60.0f, 60.0f, 60.0f), // Gray
            CreateColourFromRGB(0.0f, 0.0f, 0.0f), // Black
        };

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("colour_index")]
        public int ColourIndex { get; set; }

        internal static void CreateSaveGameConfigOnAbsence(string path)
        {
            if (!path.EndsWith(CustomSaveGameFileName) || !path.Contains("slot") || File.Exists(path)) return;

            using (FileStream stream = File.Create(path))
            {
                string randomName = randomSaveGameNames[random.Next(randomSaveGameNames.Length)];
                int colourIndex = random.Next(AllColours.Length);

                SaveGameConfig config = new SaveGameConfig
                {
                    Name = randomName,
                    ColourIndex = colourIndex
                };

                byte[] data = SerializeConfig(config);
                stream.Write(data, 0, data.Length);
            }
        }

        internal static byte[] SerializeConfig(SaveGameConfig config)
        {
            string json = JsonConvert.SerializeObject(config, Formatting.Indented);
            return Encoding.UTF8.GetBytes(json);
        }

        internal static bool GetConfigFromBytes(byte[] data, out SaveGameConfig config)
        {
            try
            {
                string json = Encoding.UTF8.GetString(data);
                SaveGameConfig newConfig = JsonConvert.DeserializeObject<SaveGameConfig>(json);
                config = newConfig;
                return true;
            }
            catch (Exception exception)
            {
                MainPatcher.Log("Exception while loading config for save file!");
                MainPatcher.Log(exception.Message);
                config = null;
                return false;
            }
        }

        private static Color CreateColourFromRGB(float red, float green, float blue)
        {
            return new Color(red / 255.0f, green / 255.0f, blue / 255.0f);
        }
    }
}
