using UnityEngine;
using WAD.Inventory;

namespace WAD.Weapons
{
    /// <summary>
    /// Munitionstyp-Definition (Kaliber). Erstelle im Editor via:
    /// Assets > Create > WAD > Ammo Type
    ///
    /// Implementiert IInteractable... nein, IInventoryItem: lose Patronen
    /// werden als stapelbares Item im Inventar gefuehrt.
    /// </summary>
    [CreateAssetMenu(fileName = "Ammo_", menuName = "WAD/Ammo Type")]
    public class AmmoTypeSO : ScriptableObject, IInventoryItem
    {
        public string ammoId;
        public string displayName;

        [Header("Ballistik")]
        public float baseDamage = 35f;
        public float penetrationFactor = 1f;
        public float muzzleVelocity = 400f;

        [Header("Gewicht")]
        public float weightPerRoundGrams = 12f;

        [Header("Visuals")]
        public GameObject tracerPrefab;
        public GameObject impactVFXPrefab;
        [Tooltip("Physische Huelse, die nach jedem Schuss ausgeworfen wird (Punkt: Huelsenauswurf-System) - je Kaliber ein eigenes Modell/Groesse")]
        public GameObject casingPrefab;
        public Sprite icon;

        // --- IInventoryItem (lose Patronen, stapelbar) ---
        public string ItemId => ammoId;
        public string DisplayName => displayName;
        public float WeightKgPerUnit => weightPerRoundGrams / 1000f;
        public bool Stackable => true;
        public int MaxStackSize => 60;
        public Sprite Icon => icon;
    }
}