using UnityEngine;
using WAD.Inventory;

namespace WAD.Core
{
    public enum ItemCategory
    {
        Food,
        Medical,
        Equipment,   // z.B. NVGs
        Misc
    }

    /// <summary>
    /// Generisches Loot-Item. Erstelle im Editor via: Assets > Create > WAD > Item
    /// z.B. "Ration Pack", "NVG", "Bandage"
    /// </summary>
    [CreateAssetMenu(fileName = "Item_", menuName = "WAD/Item")]
    public class ItemSO : ScriptableObject, IInventoryItem
    {
        public string itemId;
        public string displayName;
        [TextArea(2, 5)] public string description;
        public ItemCategory category;

        public float weightKg = 0.5f;
        public bool stackable = true;
        public int maxStackSize = 10;
        public Sprite icon;

        [Header("Verbrauchbar (z.B. Essen, Medkit)")]
        public bool isConsumable;
        public float hungerRestored;
        public float healthRestored;

        public string ItemId => itemId;
        public string DisplayName => displayName;
        public float WeightKgPerUnit => weightKg;
        public bool Stackable => stackable;
        public int MaxStackSize => maxStackSize;
        public Sprite Icon => icon;
    }
}