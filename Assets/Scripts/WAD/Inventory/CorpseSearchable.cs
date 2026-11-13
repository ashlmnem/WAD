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
    public class CorpseSearchable : MonoBehaviour, IInteractable
    {
        public LootTableSO lootTable;
        public WeaponSO droppedWeapon;
        [Range(0f, 1f)] public float droppedWeaponChance = 0.5f;
        [Tooltip("Existing enemy weapon instance. Preserves its randomized attachments and magazine state.")]
        public WeaponController droppedWeaponInstance;

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
                        // Random.Next(min, maxExclusive) wirft eine
                        // ArgumentOutOfRangeException, wenn im LootTable
                        // versehentlich minQuantity > maxQuantity steht.
                        int lo = Mathf.Min(entry.minQuantity, entry.maxQuantity);
                        int hi = Mathf.Max(entry.minQuantity, entry.maxQuantity);
                        int qty = rng.Next(lo, hi + 1);
                        rolledLoot.Add((item, qty));
                    }
                }
            }

            if ((droppedWeaponInstance != null || droppedWeapon != null) && Random.value <= droppedWeaponChance)
            {
                weaponRolled = true;
            }
        }

        /// <summary> Vom PlayerInteraction aufgerufen, wenn der Spieler 'F' auf der Leiche drueckt. </summary>
        public void Search(InventoryManager inventory, WAD.Weapons.PlayerWeaponHolder weaponHolder)
        {
            if (hasBeenSearched) return;
            hasBeenSearched = true;

            if (inventory != null)
            {
                foreach (var (item, quantity) in rolledLoot)
                {
                    inventory.AddItem(item, quantity);
                }
            }

            if (weaponRolled && droppedWeapon != null && weaponHolder != null)
            {
                WeaponController controller = droppedWeaponInstance;
                if (controller == null)
                {
                    GameObject prefab = droppedWeaponWorldPrefab != null
                        ? droppedWeaponWorldPrefab
                        : droppedWeapon.worldModelPrefab;
                    if (prefab != null)
                    {
                        GameObject weaponObject = Instantiate(prefab, transform.position, transform.rotation);
                        controller = weaponObject.GetComponentInChildren<WeaponController>(true);
                    }
                }

                if (controller != null)
                {
                    controller.weaponData = droppedWeapon;
                    EnsureUsableMagazine(controller);
                    weaponHolder.PickUpWeapon(controller);
                }
                else
                {
                    Debug.LogWarning($"[CorpseSearchable:{name}] Weapon '{droppedWeapon.displayName}' has no usable world prefab.", this);
                    hasBeenSearched = false;
                    return;
                }
            }

            Debug.Log($"[CorpseSearchable] Durchsucht: {rolledLoot.Count} Item-Stacks{(weaponRolled ? " + Waffe" : "")}.");

            Destroy(gameObject);
        }

        private static void EnsureUsableMagazine(WeaponController controller)
        {
            if (controller.weaponData == null) return;

            MagazineTypeSO defaultMagazine = controller.weaponData.DefaultMagazineType;
            if (controller.loadedMagazine == null || controller.loadedMagazine.magazineType == null ||
                controller.loadedMagazine.ammoType == null)
            {
                controller.loadedMagazine = defaultMagazine != null ? new Magazine(defaultMagazine) : null;
            }

            if (controller.reserveMagazines == null) controller.reserveMagazines = new List<Magazine>();
            controller.reserveMagazines.RemoveAll(magazine => magazine == null);

            bool hasReserve = false;
            foreach (Magazine magazine in controller.reserveMagazines)
            {
                if (magazine != null && !magazine.IsEmpty && controller.weaponData.AcceptsMagazine(magazine.magazineType))
                {
                    hasReserve = true;
                    break;
                }
            }

            if (!hasReserve && defaultMagazine != null) controller.reserveMagazines.Add(new Magazine(defaultMagazine));
        }
    }
}