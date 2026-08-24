using UnityEngine;
using WAD.Inventory;

namespace WAD.Core
{
    /// <summary>
    /// Basis-Datenobjekt fuer eine Entity (z.B. Entity-001srp, Entity-003mn).
    /// Erstelle im Editor via: Assets > Create > WAD > Entity
    ///
    /// Die eigentliche Sonderlogik (z.B. "verwandelt sich in M27 nach erstem Schuss",
    /// "ruft Helikopter") kommt NICHT hierher, sondern in eigene MonoBehaviour-
    /// Komponenten auf dem World-Object bzw. Waffen-Prefab. Dieses SO ist nur
    /// die Dateninstanz zur Identifikation + Inventar-Anzeige.
    ///
    /// Implementiert IInventoryItem, damit Entities direkt im generischen
    /// Inventarsystem (siehe InventoryManager) landen koennen.
    /// </summary>
    [CreateAssetMenu(fileName = "Entity_", menuName = "WAD/Entity")]
    public class EntitySO : ScriptableObject, IInventoryItem
    {
        [Header("Identifikation")]
        public string entityId;          // z.B. "Entity-001srp"
        public string displayName;       // z.B. "Handheld Flare Gun"
        [TextArea(3, 8)]
        public string description;

        [Header("Zuordnung")]
        public int discoveredOnLevel = 1;

        [Header("Visuals")]
        public GameObject worldPrefab;   // Aufsammel-Prefab in der Welt
        public Sprite icon;              // Inventar-Icon

        public enum EntityCategory
        {
            Weapon,
            Tool,
            Consumable,
            LevelExit,
            Anomalous
        }
        public EntityCategory category;

        [Header("Exit-Funktion (falls zutreffend)")]
        public bool canTriggerLevelExit;
        [Tooltip("Falls diese Entity direkt einen Exit ausloest, welcher Ziel-Level-Index?")]
        public int exitTargetLevelIndex = -1;

        [Header("Ausruestbare Waffe (falls Category = Weapon)")]
        [Tooltip("Prefab mit WeaponController (+ ggf. Entity001srp o.ae.) drauf - wird beim 'Benutzen' aus dem Inventar in die Hand genommen")]
        public GameObject equipablePrefab;

        [Header("Inventar")]
        public float weightKg = 1f;
        [Tooltip("Die meisten Entities sind Unikate (nicht stapelbar) - Ausnahmen manuell aktivieren")]
        public bool stackable = false;
        public int maxStackSize = 1;

        // --- IInventoryItem ---
        public string ItemId => entityId;
        public string DisplayName => displayName;
        public float WeightKgPerUnit => weightKg;
        public bool Stackable => stackable;
        public int MaxStackSize => maxStackSize;
        public Sprite Icon => icon;
    }
}