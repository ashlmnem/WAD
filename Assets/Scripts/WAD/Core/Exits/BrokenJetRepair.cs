using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WAD.Core;
using WAD.Inventory;
using WAD.Player;

namespace WAD.Levels
{
    /// <summary>
    /// Ein kaputter Kampfjet: Der Spieler liefert per Interaktion Teile ab
    /// (mehrere Besuche moeglich, Fortschritt wird gemerkt). Sobald alle
    /// Anforderungen erfuellt sind, kann er "losfliegen" - das hebt den
    /// Spieler ueber die Zeit auf die im verlinkten AltitudeExit geforderte
    /// Hoehe an. Der eigentliche Exit-Trigger passiert automatisch ueber
    /// LevelExitController, der jeden Frame die Hoehe prueft - hier muss
    /// nichts manuell ausgeloest werden.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class BrokenJetRepair : MonoBehaviour, IInteractable
    {
        [Header("Reparatur-Anforderungen")]
        public List<ItemRequirement> repairRequirements = new List<ItemRequirement>();

        [Header("Verknuepfter Exit (fuer die Ziel-Hoehe)")]
        public AltitudeExit altitudeExit;
        [Tooltip("Zusaetzlicher Sicherheitspuffer ueber der geforderten Mindesthoehe")]
        public float altitudeBuffer = 100f;
        public float climbDurationSeconds = 15f;

        private readonly Dictionary<string, int> delivered = new Dictionary<string, int>();
        private bool isLaunching;

        public bool IsRepaired { get; private set; }

        public string InteractionPrompt =>
            isLaunching ? "Abflug läuft..." :
            IsRepaired ? "F - Losfliegen" :
            $"F - Teile abgeben ({GetDeliveredCount()}/{GetTotalRequiredCount()})";

        public void Interact(PlayerInteraction interactor)
        {
            if (isLaunching) return;

            if (!IsRepaired)
            {
                DeliverParts(interactor.inventory);
            }
            else
            {
                StartCoroutine(LaunchRoutine(interactor));
            }
        }

        private void DeliverParts(InventoryManager inventory)
        {
            if (inventory == null) return;
            bool deliveredAny = false;

            foreach (var req in repairRequirements)
            {
                if (req.Item == null) continue;

                delivered.TryGetValue(req.Item.ItemId, out int already);
                int stillNeeded = req.quantity - already;
                if (stillNeeded <= 0) continue;

                int have = inventory.GetQuantity(req.Item.ItemId);
                int toTake = Mathf.Min(have, stillNeeded);
                if (toTake <= 0) continue;

                inventory.RemoveItem(req.Item.ItemId, toTake);
                delivered[req.Item.ItemId] = already + toTake;
                deliveredAny = true;
            }

            if (deliveredAny)
            {
                Debug.Log($"[BrokenJetRepair] Teile abgeliefert ({GetDeliveredCount()}/{GetTotalRequiredCount()}).");
                CheckIfRepaired();
            }
            else
            {
                Debug.Log("[BrokenJetRepair] Keine passenden/noch benoetigten Teile im Inventar.");
            }
        }

        private void CheckIfRepaired()
        {
            foreach (var req in repairRequirements)
            {
                if (req.Item == null) continue;
                delivered.TryGetValue(req.Item.ItemId, out int got);
                if (got < req.quantity) return;
            }

            IsRepaired = true;
            Debug.Log("[BrokenJetRepair] Jet repariert - bereit zum Abflug.");
        }

        private int GetDeliveredCount()
        {
            int total = 0;
            foreach (var kvp in delivered) total += kvp.Value;
            return total;
        }

        private int GetTotalRequiredCount()
        {
            int total = 0;
            foreach (var req in repairRequirements) total += req.quantity;
            return total;
        }

        private IEnumerator LaunchRoutine(PlayerInteraction interactor)
        {
            isLaunching = true;

            var movement = interactor.GetComponent<TarkovMovementController>();
            var look = interactor.GetComponentInChildren<FirstPersonCameraLook>();
            if (movement != null) movement.enabled = false;
            if (look != null) look.enabled = false;

            Transform player = interactor.transform;
            Vector3 start = player.position;
            float requiredAltitude = altitudeExit != null ? altitudeExit.requiredAltitudeMeters : 2500f;
            float targetY = start.y + requiredAltitude + altitudeBuffer;

            float elapsed = 0f;
            while (elapsed < climbDurationSeconds)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / climbDurationSeconds;
                // Sanfte Beschleunigung/Verzoegerung statt linearem Steigflug
                float smoothT = Mathf.SmoothStep(0f, 1f, t);
                player.position = new Vector3(start.x, Mathf.Lerp(start.y, targetY, smoothT), start.z);
                yield return null;
            }

            // LevelExitController prueft AltitudeExit.CheckAltitude() bereits jeden
            // Frame selbststaendig - sobald die Hoehe erreicht ist, loest der Exit
            // automatisch aus. Hier ist kein manueller Trigger noetig.
        }
    }
}
