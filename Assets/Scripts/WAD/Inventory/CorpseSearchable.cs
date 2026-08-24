using System.Collections.Generic;
using UnityEngine;
using WAD.Procedural;
using WAD.Weapons;

namespace WAD.Inventory
{
    /// <summary>
    /// Wird von EnemyController.Die() automatisch hinzugefuegt. Rollt beim Tod
    /// einmalig Loot aus einer LootTableSO + optional eine Waffe. Der Spieler
    /// durchsucht per Interaktion (siehe PlayerInteraction) - alles landet
    /// gesammelt im Inventar bzw. die Waffe wird direkt ausgeruestet.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class CorpseSearchable : MonoBehaviour, IInteractable
    {
        public LootTableSO lootTable;
        public WeaponSO droppedWeapon;
        [Range(0f, 1f)] public float droppedWeaponChance = 0.5f;

        [Tooltip("Falls du ein fertiges Waffen-Weltmodell-Prefab hast (mit WeaponController drauf)")]
        public GameObject droppedWeaponWorldPrefab;

        private readonly List<(IInventoryItem item, int quantity)> rolledLoot = new List<(IInventoryItem, int)>();
        private bool weaponRolled;
        private bool hasBeenSearched;

        public bool HasWeapon => weaponRolled;
        public string InteractionPrompt => "F - Durchsuchen";

        public void Interact(PlayerInteraction interactor)
        {
            Search(interactor.inventory, interactor.weaponHolder);
        }

        /// <summary> Wird einmalig beim Tod aufgerufen (siehe EnemyController.SpawnLootableCorpse). </summary>
        public void RollLoot()
        {
            if (lootTable != null)
            {
                // Bis zu 3 Item-Rolls pro Leiche, wie kleine "Taschen"
                var rng = new System.Random(GetInstanceID());
                int rolls = rng.Next(1, 4);

                for (int i = 0; i < rolls; i++)
                {
                    var entry = lootTable.RollWeighted(rng);
                    if (entry?.itemAsset is IInventoryItem item)
                    {
                        int qty = rng.Next(entry.minQuantity, entry.maxQuantity + 1);
                        rolledLoot.Add((item, qty));
                    }
                }
            }

            if (droppedWeapon != null && Random.value <= droppedWeaponChance)
            {
                weaponRolled = true;
            }
        }

        /// <summary> Vom PlayerInteraction aufgerufen, wenn der Spieler 'F' auf der Leiche drueckt. </summary>
        public void Search(InventoryManager inventory, WAD.Weapons.PlayerWeaponHolder weaponHolder)
        {
            if (hasBeenSearched) return;
            hasBeenSearched = true;

            foreach (var (item, quantity) in rolledLoot)
            {
                inventory.AddItem(item, quantity);
            }

            if (weaponRolled && droppedWeapon != null && weaponHolder != null)
            {
                GameObject weaponInstance = droppedWeaponWorldPrefab != null
                    ? Instantiate(droppedWeaponWorldPrefab)
                    : new GameObject($"Weapon_{droppedWeapon.weaponId}");

                var controller = weaponInstance.GetComponent<WeaponController>();
                if (controller == null) controller = weaponInstance.AddComponent<WeaponController>();
                controller.weaponData = droppedWeapon;

                weaponHolder.PickUpWeapon(controller);
            }

            Debug.Log($"[CorpseSearchable] Durchsucht: {rolledLoot.Count} Item-Stacks{(weaponRolled ? " + Waffe" : "")}.");

            Destroy(gameObject);
        }
    }
}