using UnityEngine;

namespace SaveGameCustomizer.Behaviours
{
    public class MainMenuCustomizeGame : uGUI_NavigableControlGrid, uGUI_IButtonReceiver
    {
        private void Update()
        {
            Canvas.GetDefaultCanvasMaterial().color = Color.white;
        }

        public bool OnButtonDown(GameInput.Button button)
        {
            // TODO IMPLEMENT FOR CONTROLLERS.
            return false;
        }
    }
}
