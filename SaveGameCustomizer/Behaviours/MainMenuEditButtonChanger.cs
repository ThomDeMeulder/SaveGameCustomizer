using UnityEngine;

namespace SaveGameCustomizer.Behaviours
{
    public class MainMenuEditButtonChanger : MonoBehaviour
    {
        private GameObject editButton;

        private void Start()
        {
            editButton = transform.Find("Load/EditButton").gameObject;

            GameInput.OnPrimaryDeviceChanged += OnPrimaryDeviceChanged;
            if (GameInput.IsPrimaryDeviceGamepad())
            {
                editButton.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            GameInput.OnPrimaryDeviceChanged -= OnPrimaryDeviceChanged;
        }

        private void OnPrimaryDeviceChanged()
        {
            editButton.SetActive(!GameInput.IsPrimaryDeviceGamepad());
        }
    }
}
