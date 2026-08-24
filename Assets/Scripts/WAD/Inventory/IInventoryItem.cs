using UnityEngine;

namespace WAD.Inventory
{
    /// <summary>
    /// Gemeinsame Schnittstelle fuer alles, was als Stack im Inventar liegen kann:
    /// EntitySO, AmmoTypeSO (lose Patronen) und generisches ItemSO (Essen, Medkits,
    /// NVGs etc.) implementieren das alle.
    ///
    /// Waffen selbst (WeaponController-Instanzen) laufen NICHT hierueber, sondern
    /// weiterhin ueber PlayerWeaponHolder - die sind keine stapelbaren Items.
    /// </summary>
    public interface IInventoryItem
    {
        string ItemId { get; }
        string DisplayName { get; }
        float WeightKgPerUnit { get; }
        bool Stackable { get; }
        int MaxStackSize { get; }
        Sprite Icon { get; }
    }
}