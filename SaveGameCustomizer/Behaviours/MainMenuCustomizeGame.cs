using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace SaveGameCustomizer.Behaviours
{
    public class MainMenuCustomizeGame : MonoBehaviour, uGUI_INavigableIconGrid, uGUI_IButtonReceiver
    {
        private GameObject saveButton;
        private GameObject inputMenu;
        private GameObject selectedItem;
        private LegendButtonData[] defaultLegendButtonItems;
        private mGUI_Change_Legend_On_Select legend;

        private void Start()
        {
            // Get references
            defaultLegendButtonItems = transform.parent.GetComponent<mGUI_Change_Legend_On_Select>().legendButtonConfiguration;
            saveButton = transform.Find("EditMenuSaveButton").gameObject;
            inputMenu = transform.Find("EditMenuInputMenu").gameObject;

            // Add legend for controller
            mGUI_Change_Legend_On_Select saveButtonLegend = saveButton.AddComponent<mGUI_Change_Legend_On_Select>();
            saveButtonLegend.legendButtonConfiguration = new LegendButtonData[] { defaultLegendButtonItems[0], defaultLegendButtonItems[1] };
            legend = saveButtonLegend;
        }

        public void DeselectItem()
        {
            if (selectedItem == null)
            {
                return;
            }

            selectedItem = null;
        }

        public uGUI_INavigableIconGrid GetNavigableGridInDirection(int dirX, int dirY)
        {
            return null;
        }

        public Graphic GetSelectedIcon()
        {
            return null;
        }

        public object GetSelectedItem()
        {
            return selectedItem;
        }

        public bool OnButtonDown(GameInput.Button button)
        {
            if (button == GameInput.Button.UICancel)
            {
                MainPatcher.ChangeToSavesMenu(gameObject, transform.parent.GetComponent<MainMenuLoadButton>());
                return true;
            } else if (button == GameInput.Button.UISubmit)
            {
                if (ReferenceEquals(selectedItem, saveButton))
                {
                    saveButton.GetComponent<Button>().onClick?.Invoke();
                }
                return true;
            }
            return false;
        }

        public bool SelectFirstItem()
        {
            SelectItem(saveButton);
            return true;
        }

        public void SelectItem(object item)
        {
            DeselectItem();
            selectedItem = item as GameObject;

            selectedItem.GetComponent<Selectable>().Select(); // TODO check if this isn't the "Swap" problem when selecting with controller

            // TODO: someday make a virtual keyboard for controllers because UWE doesn't provide them 'out of the box' for PC. :(

            legend.SyncLegendBarToGUISelection();
        }

        public bool SelectItemClosestToPosition(Vector3 worldPos)
        {
            return false;
        }

        public bool SelectItemInDirection(int dirX, int dirY)
        {
            if (selectedItem == null)
            {
                return SelectFirstItem();
            }

            if (Mathf.Abs(dirX) <= 0.001f)
            {
                return false;
            }

            if (ReferenceEquals(saveButton, selectedItem) && dirX < 0)
            {
                SelectItem(inputMenu);
            }
            else if (ReferenceEquals(inputMenu, selectedItem) && dirX > 0)
            {
                SelectItem(saveButton);
            }
            return true;
        }
    }
}
