using UnityEngine;

namespace SaveGameCustomizer.Behaviours
{
    public class MainMenuEditButtonChanger : MonoBehaviour
    {
        private GameObject editButton;
        private GameObject rightColourArrow, leftColourArrow;

        private void Start()
        {
            editButton = transform.Find("Load/EditButton").gameObject;
            rightColourArrow = transform.Find("Edit/EditMenuRightColourButton").gameObject;
            leftColourArrow = transform.Find("Edit/EditMenuLeftColourButton").gameObject;

            GameInput.OnPrimaryDeviceChanged += OnPrimaryDeviceChanged;
            if (GameInput.IsPrimaryDeviceGamepad())
            {
                editButton.SetActive(false);
                rightColourArrow.SetActive(false);
                leftColourArrow.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            GameInput.OnPrimaryDeviceChanged -= OnPrimaryDeviceChanged;
        }

        private void OnPrimaryDeviceChanged()
        {
            bool active = !GameInput.IsPrimaryDeviceGamepad();
            editButton.SetActive(active);
            rightColourArrow.SetActive(active);
            leftColourArrow.SetActive(active);
        }
    }
}
