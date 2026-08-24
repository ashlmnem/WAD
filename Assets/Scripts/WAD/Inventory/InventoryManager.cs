using System.Collections.Generic;
using UnityEngine;
using WAD.Player;
using WAD.Weapons;
using WAD.Core;

namespace WAD.Inventory
{
    [System.Serializable]
    public class InventoryStack
    {
        public UnityEngine.Object itemAsset; // EntitySO / AmmoTypeSO / ItemSO
        public int quantity;

        public IInventoryItem Item => itemAsset as IInventoryItem;
    }

    /// <summary>
    /// NEUBAU (v2). Zentrales Inventar: Rucksack-Items (Entities, lose Munition,
    /// generisches Loot). Einzige Quelle fuer das Nicht-Waffen-Gewicht.
    /// </summary>
    public class InventoryManager : MonoBehaviour
    {
        [Header("Referenzen")]
        public TarkovMovementController movementController;
        public PlayerWeaponHolder weaponHolder;
        public PlayerHealth playerHealth;

        [Header("Kapazität")]
        public float backpackCapacityKg = 25f;

        [Header("Aktueller Inhalt")]
        public List<InventoryStack> stacks = new List<InventoryStack>();

        public event System.Action OnInventoryChanged;

        private void Update()
        {
            PushWeightToMovementController();
        }

        public bool AddItem(IInventoryItem item, int amount = 1)
        {
            if (item == null || amount <= 0) return false;

            if (item.Stackable)
            {
                InventoryStack existing = stacks.Find(s => s.Item != null && s.Item.ItemId == item.ItemId
                    && s.quantity < item.MaxStackSize);

                if (existing != null)
                {
                    int spaceLeft = item.MaxStackSize - existing.quantity;
                    int toAdd = Mathf.Min(spaceLeft, amount);
                    existing.quantity += toAdd;
                    amount -= toAdd;

                    if (amount <= 0)
                    {
                        OnInventoryChanged?.Invoke();
                        return true;
                    }
                }
            }

            while (amount > 0)
            {
                int stackAmount = item.Stackable ? Mathf.Min(amount, item.MaxStackSize) : 1;
                stacks.Add(new InventoryStack { itemAsset = item as Object, quantity = stackAmount });
                amount -= stackAmount;
            }

            OnInventoryChanged?.Invoke();
            return true;
        }

        public bool RemoveItem(string itemId, int amount = 1)
        {
            int remaining = amount;

            for (int i = stacks.Count - 1; i >= 0 && remaining > 0; i--)
            {
                var stack = stacks[i];
                if (stack.Item == null || stack.Item.ItemId != itemId) continue;

                int removeFromThis = Mathf.Min(stack.quantity, remaining);
                stack.quantity -= removeFromThis;
                remaining -= removeFromThis;

                if (stack.quantity <= 0) stacks.RemoveAt(i);
            }

            bool success = remaining == 0;
            if (success) OnInventoryChanged?.Invoke();
            return success;
        }

        public int GetQuantity(string itemId)
        {
            int total = 0;
            foreach (var stack in stacks)
            {
                if (stack.Item != null && stack.Item.ItemId == itemId) total += stack.quantity;
            }
            return total;
        }

        public float GetTotalWeightKg()
        {
            float total = 0f;
            foreach (var stack in stacks)
            {
                if (stack.Item != null) total += stack.Item.WeightKgPerUnit * stack.quantity;
            }
            return total;
        }

        /// <summary>
        /// Benutzt ein Item (z.B. Klick im UI). Consumables wenden ihren Effekt an
        /// und werden verbraucht. Weapon-Entities (z.B. Entity-001srp) werden
        /// stattdessen ausgeruestet - dafuer wird eine Instanz von
        /// EntitySO.equipablePrefab erzeugt und ueber PlayerWeaponHolder in die
        /// Hand genommen, das Inventar-Item verschwindet dabei aus dem Rucksack.
        /// </summary>
        public bool UseItem(string itemId)
        {
            InventoryStack stack = stacks.Find(s => s.Item != null && s.Item.ItemId == itemId);
            if (stack == null) return false;

            if (stack.itemAsset is ItemSO itemSO && itemSO.isConsumable)
            {
                if (itemSO.healthRestored > 0f && playerHealth != null)
                {
                    playerHealth.Heal(itemSO.healthRestored);
                }

                RemoveItem(itemId, 1);
                return true;
            }

            if (stack.itemAsset is EntitySO entitySO && entitySO.category == EntitySO.EntityCategory.Weapon)
            {
                if (entitySO.equipablePrefab == null || weaponHolder == null)
                {
                    Debug.LogWarning($"[InventoryManager] '{entitySO.displayName}' hat kein 'Equipable Prefab' zugewiesen oder es fehlt eine WeaponHolder-Referenz - kann nicht ausgeruestet werden.");
                    return false;
                }

                GameObject instance = Object.Instantiate(entitySO.equipablePrefab);
                var controller = instance.GetComponent<WeaponController>();
                if (controller == null)
                {
                    Debug.LogWarning($"[InventoryManager] 'Equipable Prefab' von '{entitySO.displayName}' hat kein WeaponController-Skript drauf.");
                    Object.Destroy(instance);
                    return false;
                }

                weaponHolder.PickUpWeapon(controller);
                RemoveItem(itemId, 1);
                return true;
            }

            return false;
        }

        private void PushWeightToMovementController()
        {
            if (movementController == null) return;

            float weaponsWeight = weaponHolder != null ? weaponHolder.GetTotalEquippedWeaponsWeightKg() : 0f;
            movementController.currentWeight = GetTotalWeightKg() + weaponsWeight;
        }

        public void ClearOnDeath()
        {
            stacks.Clear();
            OnInventoryChanged?.Invoke();
        }
    }
}