using System.Collections.Generic;
using UnityEngine;

namespace WAD.Procedural
{
    [System.Serializable]
    public class LootTableEntry
    {
        [Tooltip("EntitySO, AmmoTypeSO oder ItemSO - alle implementieren IInventoryItem")]
        public UnityEngine.Object itemAsset;
        [Tooltip("Relatives Gewicht - hoeher = haeufiger")]
        public float weight = 1f;
        public int minQuantity = 1;
        public int maxQuantity = 1;
    }

    /// <summary>
    /// Gewichtete Loot-Tabelle fuer ein Level. Erstelle im Editor via:
    /// Assets > Create > WAD > Loot Table
    /// z.B. "Level1_StormLoot" mit Munition, Rationen, seltenen Entities
    /// </summary>
    [CreateAssetMenu(fileName = "LootTable_", menuName = "WAD/Loot Table")]
    public class LootTableSO : ScriptableObject
    {
        public List<LootTableEntry> entries = new List<LootTableEntry>();

        public LootTableEntry RollWeighted(System.Random rng)
        {
            if (entries.Count == 0) return null;

            float totalWeight = 0f;
            foreach (var e in entries) totalWeight += e.weight;

            double roll = rng.NextDouble() * totalWeight;
            float cumulative = 0f;
            foreach (var e in entries)
            {
                cumulative += e.weight;
                if (roll <= cumulative) return e;
            }
            return entries[entries.Count - 1];
        }
    }
}
