using System.Collections.Generic;
using UnityEngine;
using WAD.Weapons;

namespace WAD.Procedural
{
    [System.Serializable]
    public class MagazineLootEntry
    {
        [Tooltip("Der Magazin-TYP, der hier spawnen kann (z.B. 'M27 Standard', 'M27 Trommel') - bestimmt Munition, Kapazitaet UND Bodenmodell")]
        public MagazineTypeSO magazineType;
        [Tooltip("Relatives Gewicht - hoeher = haeufiger")]
        public float weight = 1f;
        [Range(0f, 1f)] public float minFillPercent = 0.3f;
        [Range(0f, 1f)] public float maxFillPercent = 1f;
    }

    /// <summary>
    /// Gewichtete Magazin-Loot-Tabelle. Erstelle im Editor via:
    /// Assets > Create > WAD > Magazine Table
    /// </summary>
    [CreateAssetMenu(fileName = "MagazineTable_", menuName = "WAD/Magazine Table")]
    public class MagazineTableSO : ScriptableObject
    {
        public List<MagazineLootEntry> entries = new List<MagazineLootEntry>();

        public MagazineLootEntry RollWeighted(System.Random rng)
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