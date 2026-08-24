using UnityEngine;

namespace WAD.UI
{
    /// <summary>
    /// Generisches Ein-/Ausblenden eines Panels. Auf ein beliebiges Objekt
    /// (z.B. den Button selbst, oder ein Menu-Root) ziehen, "Target Panel"
    /// zuweisen, und "TogglePanel()" per OnClick() aufrufen.
    /// </summary>
    public class PanelToggle : MonoBehaviour
    {
        public GameObject targetPanel;
        public bool startHidden = true;

        private void Start()
        {
            if (targetPanel != null && startHidden)
            {
                targetPanel.SetActive(false);
            }
        }

        public void TogglePanel()
        {
            if (targetPanel == null) return;
            targetPanel.SetActive(!targetPanel.activeSelf);
        }

        public void ShowPanel()
        {
            if (targetPanel != null) targetPanel.SetActive(true);
        }

        public void HidePanel()
        {
            if (targetPanel != null) targetPanel.SetActive(false);
        }
    }
}
