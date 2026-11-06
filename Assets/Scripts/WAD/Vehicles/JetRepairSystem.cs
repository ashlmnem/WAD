using System.Collections.Generic;
using UnityEngine;
using WAD.Core;
using WAD.Feedback;
using WAD.Inventory;

namespace WAD.Vehicles
{
    /// <summary>
    /// Reparatur-Zustand eines Jets. Die benoetigten Teile werden beim Start
    /// ZUFAELLIG aus einem Pool gezogen - jeder Jet im Level verlangt also
    /// andere Teile und Mengen.
    ///
    /// Einrichten ohne Code: im Inspector 'Possible Parts' fuellen, Min/Max
    /// Mengen und die Anzahl der verschiedenen Teile einstellen.
    /// </summary>
    [RequireComponent(typeof(ActionFeedbackPlayer))]
    public class JetRepairSystem : MonoBehaviour
    {
        [Header("Moegliche Ersatzteile")]
        [Tooltip("Aus diesem Pool werden die tatsaechlich benoetigten Teile zufaellig gezogen.")]
        public List<ItemRequirement> possibleParts = new List<ItemRequirement>();

        [Header("Zufaellige Anforderungen")]
        [Tooltip("Wie viele VERSCHIEDENE Teile dieser Jet verlangt")]
        public Vector2Int distinctPartsRange = new Vector2Int(2, 3);
        [Tooltip("Mengenbereich pro verlangtem Teil")]
        public Vector2Int quantityPerPartRange = new Vector2Int(1, 3);
        [Tooltip("Fester Seed pro Jet. 0 = zufaellig bei jedem Spielstart.")]
        public int randomSeed = 0;

        [Header("Verhalten")]
        [Tooltip("Teile werden beim Abgeben sofort aus dem Inventar entfernt")]
        public bool consumePartsImmediately = true;
        [Tooltip("Reparaturfortschritt ueberlebt das Verlassen der Szene (im Spielstand nicht moeglich, nur laufzeitintern)")]
        public bool rememberProgress = true;

        [Header("Anzeige")]
        public string promptWhenBroken = "F - Teile abgeben";
        public string promptWhenRepaired = "Jet ist repariert";

        private readonly Dictionary<string, int> delivered = new Dictionary<string, int>();
        private readonly List<ItemRequirement> activeRequirements = new List<ItemRequirement>();
        private ActionFeedbackPlayer feedback;

        public bool IsRepaired { get; private set; }
        public IReadOnlyList<ItemRequirement> ActiveRequirements => activeRequirements;

        /// <summary> Wird ausgeloest, sobald alle Teile abgeliefert sind. </summary>
        public event System.Action OnRepaired;
        /// <summary> Wird bei jeder Teilabgabe ausgeloest. </summary>
        public event System.Action OnPartDelivered;

        private void Awake()
        {
            feedback = GetComponent<ActionFeedbackPlayer>();
            RollRequirements();
        }

        /// <summary> Zieht die benoetigten Teile zufaellig aus dem Pool. </summary>
        public void RollRequirements()
        {
            activeRequirements.Clear();
            delivered.Clear();
            IsRepaired = false;

            if (possibleParts == null || possibleParts.Count == 0)
            {
                // Ohne Pool gilt der Jet als sofort repariert (Platzhalter-Jet).
                IsRepaired = true;
                return;
            }

            var rng = randomSeed != 0 ? new System.Random(randomSeed) : new System.Random();

            var pool = new List<ItemRequirement>();
            foreach (var part in possibleParts)
            {
                if (part != null && part.itemAsset != null) pool.Add(part);
            }

            if (pool.Count == 0)
            {
                IsRepaired = true;
                return;
            }

            int minParts = Mathf.Clamp(Mathf.Min(distinctPartsRange.x, distinctPartsRange.y), 1, pool.Count);
            int maxParts = Mathf.Clamp(Mathf.Max(distinctPartsRange.x, distinctPartsRange.y), minParts, pool.Count);
            int wanted = rng.Next(minParts, maxParts + 1);

            for (int i = pool.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (pool[i], pool[j]) = (pool[j], pool[i]);
            }

            for (int i = 0; i < wanted && i < pool.Count; i++)
            {
                var source = pool[i];
                if (source?.itemAsset == null) continue;

                activeRequirements.Add(new ItemRequirement
                {
                    itemAsset = source.itemAsset,
                    quantity = rng.Next(
                        Mathf.Max(1, Mathf.Min(quantityPerPartRange.x, quantityPerPartRange.y)),
                        Mathf.Max(1, Mathf.Max(quantityPerPartRange.x, quantityPerPartRange.y)) + 1)
                });
            }

            if (activeRequirements.Count == 0) IsRepaired = true;
        }

        /// <summary> Prompt fuer die Blick-Interaktion. </summary>
        public string GetRepairPrompt()
        {
            if (IsRepaired) return promptWhenRepaired;
            return $"{promptWhenBroken} ({GetDeliveredCount()}/{GetTotalRequiredCount()})";
        }

        /// <summary> Liefert alle verfuegbaren Teile aus dem Inventar ab. </summary>
        public bool TryDeliverParts(InventoryManager inventory)
        {
            if (IsRepaired || inventory == null) return false;

            bool deliveredAny = false;

            foreach (var req in activeRequirements)
            {
                var item = req.Item;
                if (item == null) continue;

                delivered.TryGetValue(item.ItemId, out int already);
                int stillNeeded = req.quantity - already;
                if (stillNeeded <= 0) continue;

                int have = inventory.GetQuantity(item.ItemId);
                int toTake = Mathf.Min(have, stillNeeded);
                if (toTake <= 0) continue;

                if (consumePartsImmediately) inventory.RemoveItem(item.ItemId, toTake);

                delivered[item.ItemId] = already + toTake;
                deliveredAny = true;
            }

            if (deliveredAny)
            {
                feedback?.Play(ActionId.JetRepairProgress);
                OnPartDelivered?.Invoke();
                CheckIfRepaired();
            }
            else
            {
                feedback?.Play(ActionId.JetRepairDenied);
                Debug.Log($"[JetRepairSystem:{name}] Keine passenden oder noch benoetigten Teile im Inventar.");
            }

            return deliveredAny;
        }

        private void CheckIfRepaired()
        {
            foreach (var req in activeRequirements)
            {
                var item = req.Item;
                if (item == null) continue;

                delivered.TryGetValue(item.ItemId, out int got);
                if (got < req.quantity) return;
            }

            IsRepaired = true;
            feedback?.Play(ActionId.JetRepairComplete);
            OnRepaired?.Invoke();
            Debug.Log($"[JetRepairSystem:{name}] Jet vollstaendig repariert.");
        }

        public int GetDeliveredCount()
        {
            int total = 0;
            foreach (var kvp in delivered) total += kvp.Value;
            return total;
        }

        public int GetTotalRequiredCount()
        {
            int total = 0;
            foreach (var req in activeRequirements) total += req.quantity;
            return total;
        }

        /// <summary> Von aussen (z.B. Item aufheben) pruefen, ob dieser Jet schon fertig ist. </summary>
        public void ForceRepaired()
        {
            IsRepaired = true;
        }
    }
}
