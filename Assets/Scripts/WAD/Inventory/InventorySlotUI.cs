using UnityEngine;
using UnityEngine.UI;

namespace WAD.Inventory.UI
{
    /// <summary>
    /// NEUBAU (v2). Liegt auf dem Slot-Prefab. Braucht als Geschwister-
    /// Komponente einen Button (fuer Klicks) und referenziert Icon/Name/Menge
    /// als Kind-Objekte.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class InventorySlotUI : MonoBehaviour
    {
        public Image iconImage;
        public Text nameText;
        public Text quantityText;

        private string boundItemId;
        private System.Action<string> onClickCallback;
        private Button button;

        private void Awake()
        {
            button = GetComponent<Button>();
            button.onClick.AddListener(HandleClick);
        }

        private void HandleClick()
        {
            onClickCallback?.Invoke(boundItemId);
        }

        public void Setup(IInventoryItem item, int quantity, System.Action<string> onClick)
        {
            boundItemId = item.ItemId;
            onClickCallback = onClick;

            if (iconImage != null)
            {
                iconImage.sprite = item.Icon;
                iconImage.enabled = item.Icon != null;
            }

            if (nameText != null)
            {
                nameText.text = item.DisplayName;
            }

            if (quantityText != null)
            {
                quantityText.text = item.Stackable && quantity > 1 ? $"x{quantity}" : "";
            }
        }
    }
}