using HarmonyLib;
using SaveGameCustomizer.Config;
using UnityEngine;
using UnityEngine.UI;

namespace SaveGameCustomizer.Behaviours
{
    public class MainMenuCustomizeGame : MonoBehaviour, uGUI_INavigableIconGrid, uGUI_IButtonReceiver
    {
        private SelectedColours coloursComponent;
        private GameObject saveButton;
        private GameObject inputMenu;
        private InputField inputField;
        private GameObject selectedItem;
        private LegendButtonData[] defaultLegendButtonItems;
        private Image saveButtonImage;

        private void Start()
        {
            // Get references
            coloursComponent = transform.parent.GetComponent<SelectedColours>();
            defaultLegendButtonItems = transform.parent.GetComponent<mGUI_Change_Legend_On_Select>().legendButtonConfiguration;
            saveButton = transform.Find("EditMenuSaveButton").gameObject;
            inputMenu = transform.Find("EditMenuInputMenu").gameObject;
            inputField = inputMenu.GetComponent<InputField>();
            saveButtonImage = saveButton.GetComponent<Image>();
            saveButton.GetComponent<CanvasRenderer>().SetColor(Color.white);

            // Add legend for controller
            mGUI_Change_Legend_On_Select saveButtonLegend = saveButton.AddComponent<mGUI_Change_Legend_On_Select>();
            saveButtonLegend.legendButtonConfiguration = new LegendButtonData[] { defaultLegendButtonItems[0], defaultLegendButtonItems[1] };

            mGUI_Change_Legend_On_Select inputMenuLegend = inputMenu.AddComponent<mGUI_Change_Legend_On_Select>();
            inputMenuLegend.legendButtonConfiguration = new LegendButtonData[] { defaultLegendButtonItems[0],
                new LegendButtonData
                {
                    legendButtonIdx = 1,
                    button = GameInput.Button.MoveDown,
                    buttonDescription = SaveGameConfig.ColourButtonControllerText.Item1
                },
                new LegendButtonData
                {
                    legendButtonIdx = 2,
                    button = GameInput.Button.MoveUp,
                    buttonDescription = SaveGameConfig.ColourButtonControllerText.Item1
                }
            };
        }

        public void DeselectItem()
        {
            if (selectedItem == null)
            {
                return;
            }

            if (ReferenceEquals(selectedItem, saveButton))
            {
                saveButtonImage.color = Color.green;
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
            } 
            else if (button == GameInput.Button.UISubmit)
            {
                if (ReferenceEquals(selectedItem, saveButton))
                {
                    saveButton.GetComponent<Button>().onClick?.Invoke();
                }
                return true;
            } 
            else if (ReferenceEquals(selectedItem, inputMenu))
            {
                if (button == GameInput.Button.MoveDown)
                {
                    MainPatcher.UpdateColourIndex(coloursComponent.ColourIndex, 1, coloursComponent, inputField);
                    return true;
                }
                else if (button == GameInput.Button.MoveUp)
                {
                    MainPatcher.UpdateColourIndex(coloursComponent.ColourIndex, -1, coloursComponent, inputField);
                    return true;
                }
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

            if (ReferenceEquals(selectedItem, saveButton))
            {
                saveButtonImage.color = Color.yellow;
            }

            selectedItem.GetComponent<Selectable>().Select();

            // TODO: someday make a virtual keyboard for controllers because UWE doesn't provide them 'out of the box' for PC. :(

            mGUI_Change_Legend_On_Select legendComponent = selectedItem.GetComponent<mGUI_Change_Legend_On_Select>();
            if (legendComponent != null)
            {
                legendComponent.SyncLegendBarToGUISelection();
            }
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
