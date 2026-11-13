using UnityEngine;
using WAD.Inventory;
using WAD.Weapons;

namespace WAD.Core
{
    /// <summary>
    /// Ein Item + Menge - gemeinsam genutzt von Taskmaster-Aufgaben und der
    /// Kampfjet-Reparatur, um doppelte Strukturen zu vermeiden.
    /// </summary>
    [System.Serializable]
    public class ItemRequirement
    {
        [Tooltip("Inventar-Item (EntitySO/AmmoTypeSO/ItemSO) oder MagazineTypeSO")]
        public Object itemAsset;
        public int quantity = 1;

        public IInventoryItem Item => itemAsset as IInventoryItem;
        public MagazineTypeSO MagazineType => itemAsset as MagazineTypeSO;
    }
}
