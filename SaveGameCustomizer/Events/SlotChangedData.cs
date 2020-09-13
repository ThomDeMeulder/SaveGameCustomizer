using UnityEngine;

namespace SaveGameCustomizer.Events
{
    internal struct SlotChangedData
    {
        public GameObject Object { get; set; }
        public int NewColourIndex { get; set; }
    }
}
