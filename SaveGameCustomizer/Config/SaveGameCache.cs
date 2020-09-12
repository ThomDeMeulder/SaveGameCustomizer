using System.Collections.Generic;

namespace SaveGameCustomizer.Config
{
    internal static class SaveGameCache
    {
        private static Dictionary<string, SaveGameConfig> cache = new Dictionary<string, SaveGameConfig>();

        internal static bool GetSaveGameConfigDataFromSlotName(string slot, out SaveGameConfig data)
        {
            return cache.TryGetValue(slot, out data);
        }

        internal static void AddSaveGameConfigDataBySlot(string slot, SaveGameConfig data)
        {
            cache.Add(slot, data);
        }

        internal static void ClearAll()
        {
            cache.Clear();
        }
    }
}
