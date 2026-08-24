using UnityEngine;
using WAD.Inventory;

namespace WAD.Core
{
    /// <summary>
    /// Ein Item + Menge - gemeinsam genutzt von Taskmaster-Aufgaben und der
    /// Kampfjet-Reparatur, um doppelte Strukturen zu vermeiden.
    /// </summary>
    [System.Serializable]
    public class ItemRequirement
    {
        [Tooltip("EntitySO, AmmoTypeSO oder ItemSO - alle implementieren IInventoryItem")]
        public Object itemAsset;
        public int quantity = 1;

        public IInventoryItem Item => itemAsset as IInventoryItem;
    }
}
