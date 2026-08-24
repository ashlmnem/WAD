using System.Collections.Generic;
using UnityEngine;

namespace WAD.Enemies
{
    /// <summary>
    /// Punkt 8: Wird ein Gegner zu oft getroffen ODER fliegen genug Schuesse
    /// knapp an ihm vorbei (Near-Miss, siehe WeaponController), geraet er in
    /// Panik: sofortige Flucht zur naechsten Deckung + Blindfeuer.
    /// </summary>
    [RequireComponent(typeof(EnemyController))]
    public class EnemySuppressionController : MonoBehaviour
    {
        [Header("Schwelle: zu viele Treffer in kurzer Zeit")]
        public int hitsToSuppress = 3;
        public float hitWindowSeconds = 4f;

        [Header("Near-Miss (Kugeln fliegen knapp vorbei)")]
        public int nearMissesToSuppress = 4;
        public float nearMissWindowSeconds = 3f;

        [Header("Unterdrueckung")]
        public float suppressionDuration = 5f;
        public float suppressionCooldown = 3f;

        private readonly Queue<float> recentHitTimes = new Queue<float>();
        private readonly Queue<float> recentNearMissTimes = new Queue<float>();
        private float lastSuppressionTriggerTime = -999f;

        private EnemyController enemy;
        private EnemyCombatAI combatAI;

        public bool IsSuppressed { get; private set; }
        public event System.Action OnSuppressed; // fuer EnemyAudio (Paniklaut)

        private void Awake()
        {
            enemy = GetComponent<EnemyController>();
            combatAI = GetComponent<EnemyCombatAI>();
        }

        private void OnEnable()
        {
            enemy.OnDamaged += HandleDamaged;
        }

        private void OnDisable()
        {
            enemy.OnDamaged -= HandleDamaged;
        }

        private void HandleDamaged(EnemyController e, float amount)
        {
            recentHitTimes.Enqueue(Time.time);
            PruneQueue(recentHitTimes, hitWindowSeconds);

            if (recentHitTimes.Count >= hitsToSuppress)
            {
                TriggerSuppression();
            }
        }

        /// <summary> Von WeaponController.NotifyNearMisses() aufgerufen. </summary>
        public void NotifyNearMiss()
        {
            recentNearMissTimes.Enqueue(Time.time);
            PruneQueue(recentNearMissTimes, nearMissWindowSeconds);

            if (recentNearMissTimes.Count >= nearMissesToSuppress)
            {
                TriggerSuppression();
            }
        }

        private void PruneQueue(Queue<float> queue, float window)
        {
            while (queue.Count > 0 && Time.time - queue.Peek() > window)
            {
                queue.Dequeue();
            }
        }

        private void TriggerSuppression()
        {
            if (Time.time - lastSuppressionTriggerTime < suppressionCooldown) return;
            lastSuppressionTriggerTime = Time.time;

            recentHitTimes.Clear();
            recentNearMissTimes.Clear();

            IsSuppressed = true;
            OnSuppressed?.Invoke();
            Invoke(nameof(ClearSuppressed), suppressionDuration);

            enemy.ForceEngagement();

            if (combatAI != null && enemy.useCoverAI)
            {
                combatAI.ForceSuppressed(suppressionDuration);
            }
        }

        private void ClearSuppressed()
        {
            IsSuppressed = false;
        }
    }
}
