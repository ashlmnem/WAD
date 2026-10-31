using UnityEngine;

namespace WAD.Inventory
{
    /// <summary>
    /// Liegt auf jedem aufsammelbaren Loot-Objekt in der Welt (Essen, Munition,
    /// Entities). Referenziert das zugehoerige SO (EntitySO / AmmoTypeSO / ItemSO)
    /// ueber das gemeinsame IInventoryItem-Interface.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class WorldItemPickup : MonoBehaviour, IInteractable
    {
        [Tooltip("EntitySO, AmmoTypeSO oder ItemSO - alle implementieren IInventoryItem")]
        public UnityEngine.Object itemAsset;
        public int quantity = 1;

        /// <summary> Wird kurz vor dem Zerstoeren ausgeloest, wenn der Spieler das Item aufgesammelt hat (z.B. damit Spawner sich das merken koennen). </summary>
        public event System.Action<WorldItemPickup> PickedUp;

        public IInventoryItem Item => itemAsset as IInventoryItem;

        public string InteractionPrompt => $"F - {Item?.DisplayName} aufheben";

        public void Interact(PlayerInteraction interactor)
        {
            TryPickUp(interactor.inventory);
        }

        public bool TryPickUp(InventoryManager inventory)
        {
            if (Item == null)
            {
                Debug.LogWarning($"[WorldItemPickup] {name}: itemAsset implementiert kein IInventoryItem.");
                return false;
            }

            if (inventory == null) return false;

            bool added = inventory.AddItem(Item, quantity);
            if (added)
            {
                PickedUp?.Invoke(this);
                Destroy(gameObject);
            }
            return added;
        }
    }
}