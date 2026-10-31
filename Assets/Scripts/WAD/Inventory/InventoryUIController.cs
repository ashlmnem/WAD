using System;
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

        /// <summary>
        /// True, solange irgendein Inventar-Panel offen ist. WeaponController
        /// blockiert damit Schiessen/Zielen/Nachladen, waehrend der Spieler im
        /// Inventar klickt - vorher feuerte ein Klick auf einen Slot einen Schuss.
        /// </summary>
        public static bool IsOpen { get; private set; }

        private void Start()
        {
            ResolveMissingReferences();

            if (inventory != null)
            {
                inventory.OnInventoryChanged += RebuildSlots;
            }
            else
            {
                Debug.LogWarning($"[InventoryUI:{name}] Kein InventoryManager gefunden - die Inventar-Anzeige bleibt leer.");
            }

            SetOpen(false);
            RebuildSlots();
        }

        /// <summary>
        /// Die UI liegt im Canvas-Prefab, der InventoryManager aber auf dem Spieler.
        /// Diese Referenz kann deshalb nicht im Prefab gespeichert werden; ohne
        /// diesen Fallback blieb 'inventory' null und es wurde nie ein Slot erzeugt.
        /// </summary>
        private void ResolveMissingReferences()
        {
            if (inventory == null) inventory = FindFirstObjectByType<InventoryManager>();

            if (panelRoot == null)
            {
                Debug.LogWarning($"[InventoryUI:{name}] 'Panel Root' ist nicht zugewiesen - Tab kann kein Panel ein-/ausblenden.");
            }

            if (contentParent == null)
            {
                Debug.LogWarning($"[InventoryUI:{name}] 'Content Parent' ist nicht zugewiesen - es koennen keine Slots erzeugt werden.");
            }

            if (slotPrefab == null)
            {
                Debug.LogWarning($"[InventoryUI:{name}] 'Slot Prefab' ist nicht zugewiesen - es koennen keine Slots erzeugt werden.");
            }
        }

        private void OnDestroy()
        {
            if (inventory != null) inventory.OnInventoryChanged -= RebuildSlots;

            // Nicht "IsOpen = false" pauschal, sondern nur den eigenen Beitrag
            // zuruecknehmen (es koennte mehr als ein Panel geben).
            if (isOpen)
            {
                isOpen = false;
                IsOpen = AnyOtherOpen();
            }
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

            IsOpen = open || AnyOtherOpen();

            if (open) RebuildSlots();
        }

        private bool AnyOtherOpen()
        {
            foreach (var other in FindObjectsByType<InventoryUIController>(FindObjectsSortMode.None))
            {
                if (other != this && other.isOpen) return true;
            }
            return false;
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