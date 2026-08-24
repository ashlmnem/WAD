using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace WAD.Inventory.UI
{
    /// <summary>
    /// NEUBAU (v2). Zeigt InventoryManager.stacks als Liste an. Toggle per Tab.
    /// </summary>
    public class InventoryUIController : MonoBehaviour
    {
        [Header("Referenzen")]
        public InventoryManager inventory;
        public GameObject panelRoot;
        public Transform contentParent;
        public GameObject slotPrefab;

        [Header("Info-Anzeige")]
        public Text weightText;

        [Header("Steuerung")]
        public KeyCode toggleKey = KeyCode.Tab;

        private readonly List<GameObject> spawnedSlots = new List<GameObject>();
        private bool isOpen;

        private void Start()
        {
            if (inventory != null) inventory.OnInventoryChanged += RebuildSlots;
            SetOpen(false);
            RebuildSlots();
        }

        private void OnDestroy()
        {
            if (inventory != null) inventory.OnInventoryChanged -= RebuildSlots;
        }

        private void Update()
        {
            if (Input.GetKeyDown(toggleKey))
            {
                SetOpen(!isOpen);
            }
        }

        private void SetOpen(bool open)
        {
            isOpen = open;
            if (panelRoot != null) panelRoot.SetActive(open);

            Cursor.lockState = open ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = open;

            if (open) RebuildSlots();
        }

        private void HandleSlotClicked(string itemId)
        {
            inventory.UseItem(itemId);
        }

        private void RebuildSlots()
        {
            foreach (var slot in spawnedSlots) Destroy(slot);
            spawnedSlots.Clear();

            if (inventory == null || contentParent == null || slotPrefab == null) return;

            float totalWeight = 0f;

            foreach (var stack in inventory.stacks)
            {
                if (stack.Item == null) continue;

                GameObject slotObj = Instantiate(slotPrefab, contentParent);
                var slotUI = slotObj.GetComponent<InventorySlotUI>();
                slotUI?.Setup(stack.Item, stack.quantity, HandleSlotClicked);

                spawnedSlots.Add(slotObj);
                totalWeight += stack.Item.WeightKgPerUnit * stack.quantity;
            }

            if (weightText != null)
            {
                weightText.text = $"{totalWeight:F1} / {inventory.backpackCapacityKg:F0} kg";
            }
        }
    }
}