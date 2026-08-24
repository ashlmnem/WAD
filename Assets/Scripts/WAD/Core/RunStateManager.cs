using System.Collections.Generic;
using UnityEngine;

namespace WAD.Core
{
    /// <summary>
    /// Persistenter Zustand eines einzelnen Runs durch das WAD.
    /// Ueberlebt Level-Wechsel (DontDestroyOnLoad), wird bei Tod
    /// oder erfolgreicher Extraktion zurueckgesetzt bzw. committed.
    ///
    /// Design-Entscheidung: Singleton statt ScriptableObject-Runtime-Set,
    /// weil wir hier klare Lifecycle-Events brauchen (Run start/end).
    /// </summary>
    public class RunStateManager : MonoBehaviour
    {
        public static RunStateManager Instance { get; private set; }

        [Header("Run-Fortschritt")]
        public int currentLevelIndex = 1;
        public int levelsSurvivedThisRun = 0;

        [Header("Story-Flags (levelübergreifende Bedingungen)")]
        // Beispiel: Level 2 -> beeinflusst Level 5 Exit 3/4
        public HashSet<string> flags = new HashSet<string>();

        [Header("Inventar (nur das, was NACH Extraktion gesichert wird)")]
        public List<InventoryItem> carriedItems = new List<InventoryItem>();

        [Header("Kampfjet-Treibstoff (Punkt 2, ueber Taskmaster-Aufgaben verdient)")]
        public float jetFuel = 0f;

        public void AddFuel(float amount)
        {
            jetFuel += amount;
            Debug.Log($"[RunState] Treibstoff erhalten: +{amount} (gesamt: {jetFuel})");
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        // ---- Flags ----
        public void SetFlag(string flagId)
        {
            flags.Add(flagId);
            Debug.Log($"[RunState] Flag gesetzt: {flagId}");
        }

        public bool HasFlag(string flagId) => flags.Contains(flagId);

        // ---- Inventar ----
        public void AddItem(InventoryItem item)
        {
            carriedItems.Add(item);
        }

        public void RemoveItem(InventoryItem item)
        {
            carriedItems.Remove(item);
        }

        /// <summary>
        /// Wird aufgerufen, wenn der Spieler stirbt: alles auf diesem Run verloren.
        /// </summary>
        public void OnDeath()
        {
            Debug.Log("[RunState] Spieler gestorben - Run-Loot verloren.");
            carriedItems.Clear();
            flags.Clear();
            jetFuel = 0f;
            currentLevelIndex = 1;
            levelsSurvivedThisRun = 0;
            // TODO: Szenenwechsel zurueck zum Hideout/Hauptmenue
        }

        /// <summary>
        /// Wird aufgerufen, wenn der Spieler erfolgreich extrahiert:
        /// Loot wird ins persistente Stash-System (zwischen Runs) uebertragen.
        /// </summary>
        public void OnExtraction()
        {
            Debug.Log("[RunState] Extraktion erfolgreich - Loot gesichert.");
            // TODO: StashManager.Instance.CommitItems(carriedItems);
            carriedItems.Clear();
            flags.Clear();
            jetFuel = 0f;
            currentLevelIndex = 1;
            levelsSurvivedThisRun = 0;
        }

        public void AdvanceToLevel(int levelIndex)
        {
            currentLevelIndex = levelIndex;
            levelsSurvivedThisRun++;

            // Alle 5 Level: Extraktions-Entscheidung anbieten
            if (levelsSurvivedThisRun % 5 == 0)
            {
                ExtractionDecisionUI.Instance?.PromptDecision();
            }
        }
    }

    [System.Serializable]
    public class InventoryItem
    {
        public string itemId;
        public int quantity;
    }
}