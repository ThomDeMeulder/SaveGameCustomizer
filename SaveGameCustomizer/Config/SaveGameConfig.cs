using Oculus.Newtonsoft.Json;
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

        // Light then dark.
        public static readonly Tuple<Color,Color>[] AllColours =
        {
            CreateColoursFromRGB(66.0f, 143.0f, 56.0f, 45.0f, 97.0f, 39.0f), // Dark Green
            CreateColoursFromRGB(41.0f, 232.0f, 19.0f, 36.0f, 196.0f, 18.0f), // Light Green
            CreateColoursFromRGB(235.0f, 164.0f, 12.0f, 184.0f, 129.0f, 11.0f), // Orange
            CreateColoursFromRGB(224.0f, 7.0f, 170.0f, 179.0f, 5.0f, 135.0f, 0.6f, 0.6f), // Purple
            CreateColoursFromRGB(252.0f, 189.0f, 250.0f, 212.0f, 159.0f, 210.0f, 0.6f, 0.6f), // Pink
            CreateColoursFromRGB(0.0f, 34.0f, 255.0f, 3.0f, 23.0f, 150.0f, 0.7f, 0.8f), // Dark Blue
            CreateColoursFromRGB(214.0f, 0.0f, 0.0f, 160.0f, 0.0f, 0.0f), // Dark Red
            CreateColoursFromRGB(0.0f, 0.0f, 0.0f, 40.0f, 40.0f, 40.0f) // Black / gray
        };

        // Used for white materials
        public static readonly Color[] ProperColours =
        {
            CreateColourFromRGB(64.0f, 128.0f, 11.0f),
            CreateColourFromRGB(107.0f, 212.0f, 21.0f),
            CreateColourFromRGB(232.0f, 142.0f, 32.0f),
            CreateColourFromRGB(187.0f, 102.0f, 189.0f),
            CreateColourFromRGB(232.0f, 176.0f, 232.0f),
            CreateColourFromRGB(11.0f, 97.0f, 184.0f),
            CreateColourFromRGB(212.0f, 6.0f, 6.0f),
            CreateColourFromRGB(0.0f, 0.0f, 0.0f)
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

                string json = JsonConvert.SerializeObject(config, Formatting.Indented);
                byte[] data = Encoding.UTF8.GetBytes(json);

                stream.Write(data, 0, data.Length);
            }
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

        private static Tuple<Color, Color> CreateColoursFromRGB(float lightRed, float lightGreen, float lightBlue, float darkRed, float darkGreen, float darkBlue, float lightAlpha = 1.0f, float darkAlpha = 1.0f)
        {
            return new Tuple<Color, Color>(new Color(lightRed / 255.0f, lightGreen / 255.0f, lightBlue / 255.0f, lightAlpha), new Color(darkRed / 255.0f, darkGreen / 255.0f, darkBlue / 255.0f, darkAlpha));
        }

        private static Color CreateColourFromRGB(float red, float green, float blue)
        {
            return new Color(red / 255.0f, green / 255.0f, blue / 255.0f);
        }
    }
}
