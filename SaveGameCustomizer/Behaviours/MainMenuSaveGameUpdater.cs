using SaveGameCustomizer.Config;
using UnityEngine;
using UnityEngine.UI;

namespace SaveGameCustomizer.Behaviours
{
    public class MainMenuSaveGameUpdater : MonoBehaviour
    {
        private void Start()
        {
            MainPatcher.OnSlotDataChanged += MainPatcher_OnColourChanged;
        }

        private void OnDestroy()
        {
            MainPatcher.OnSlotDataChanged -= MainPatcher_OnColourChanged;
        }

        private void MainPatcher_OnColourChanged(Events.SlotChangedData data)
        {
            Color lightColour = SaveGameConfig.AllColours[data.NewColourIndex].Item1;

            Transform editTransform = data.Object.transform.Find("Load");
            editTransform.GetComponent<Image>().color = lightColour;

            SelectedColours selectedColoursComponent = data.Object.GetComponent<SelectedColours>();
            selectedColoursComponent.SelectedColour = lightColour;
            selectedColoursComponent.DarkerColour = SaveGameConfig.AllColours[data.NewColourIndex].Item2;
        }
    }
}
