using UnityEngine;
using WAD.Feedback;

namespace WAD.Enemies
{
    /// <summary>
    /// Liegt auf demselben Objekt wie EnemyController. Spielt passende Sounds
    /// bei Idle (zufaellig, periodisch), Sichtkontakt zum Spieler, Schaden und Tod.
    ///
    /// Sounds laufen bevorzugt ueber den ActionFeedbackPlayer (pro Aktion im
    /// Inspector eintragbar). Solange dort nichts steht, gelten die Clip-Listen
    /// auf dieser Komponente.
    /// </summary>
    [RequireComponent(typeof(EnemyController))]
    [RequireComponent(typeof(AudioSource))]
    public class EnemyAudio : MonoBehaviour
    {
        [Header("Sounds (Altbestand - wird vom Feedback-System ueberstimmt, sobald dort eingetragen)")]
        public AudioClip[] idleSounds;
        public AudioClip[] spottedPlayerSounds; // z.B. "telepathischer Schrei" laut Design
        public AudioClip[] damagedSounds;
        public AudioClip[] deathSounds;

        [Header("Erweiterte Aktionen")]
        public AudioClip[] attackSounds;
        public AudioClip[] suppressedSounds;
        public AudioClip[] alertedSounds;

        [Header("Idle-Timing")]
        public float minIdleInterval = 6f;
        public float maxIdleInterval = 14f;

        [Header("Feedback")]
        [Tooltip("Zentrale Feedback-Station. Leer = automatisch gesucht.")]
        public ActionFeedbackPlayer feedback;
        [Tooltip("Kampf-Komponente (Kampf-State). Leer = automatisch gesucht.")]
        public EnemyCombatAI combatAI;

        private EnemyController enemy;
        private AudioSource audioSource;
        private float idleTimer;
        private float nextIdleDelay;

        private void Awake()
        {
            enemy = GetComponent<EnemyController>();
            audioSource = GetComponent<AudioSource>();
            if (feedback == null) feedback = GetComponent<ActionFeedbackPlayer>();
            if (combatAI == null) combatAI = GetComponent<EnemyCombatAI>();
            SetNextIdleDelay();
        }

        private void OnEnable()
        {
            enemy.OnSpottedPlayer += HandleSpottedPlayer;
            enemy.OnDamaged += HandleDamaged;
            enemy.OnDeath += HandleDeath;

            // Unterdrueckung (Near-Miss) -> eigener Sound-Slot
            var suppression = GetComponent<EnemySuppressionController>();
            if (suppression != null) suppression.OnSuppressed += HandleSuppressed;
        }

        private void OnDisable()
        {
            enemy.OnSpottedPlayer -= HandleSpottedPlayer;
            enemy.OnDamaged -= HandleDamaged;
            enemy.OnDeath -= HandleDeath;

            var suppression = GetComponent<EnemySuppressionController>();
            if (suppression != null) suppression.OnSuppressed -= HandleSuppressed;
        }

        private void Update()
        {
            if (enemy.CurrentState != EnemyState.Patrol) return; // nur im Idle/Patrol zufaellige Laute

            idleTimer += Time.deltaTime;
            if (idleTimer >= nextIdleDelay)
            {
                idleTimer = 0f;
                SetNextIdleDelay();
                PlayOrFallback(ActionId.EnemyIdle, idleSounds);
            }
        }

        private void SetNextIdleDelay()
        {
            nextIdleDelay = Random.Range(minIdleInterval, maxIdleInterval);
        }

        private void HandleSpottedPlayer(EnemyController e) => PlayOrFallback(ActionId.EnemySpottedPlayer, spottedPlayerSounds);
        private void HandleDamaged(EnemyController e, float amount) => PlayOrFallback(ActionId.EnemyHurt, damagedSounds);
        private void HandleDeath(EnemyController e) => PlayOrFallback(ActionId.EnemyDeath, deathSounds);
        private void HandleSuppressed() => PlayOrFallback(ActionId.EnemySuppressed, suppressedSounds);

        /// <summary> Vom Kampf-System aufgerufen, wenn der Gegner feuert. </summary>
        public void NotifyAttack()
        {
            PlayOrFallback(ActionId.EnemyAttackRanged, attackSounds);
        }

        /// <summary> Vom Kampf-System aufgerufen, wenn der Gegner alarmiert wird. </summary>
        public void NotifyAlerted()
        {
            PlayOrFallback(ActionId.EnemyAlerted, alertedSounds);
        }

        private void PlayOrFallback(ActionId action, AudioClip[] clips)
        {
            if (feedback != null)
            {
                feedback.PlayOrFallback(action, clips);
                return;
            }

            PlayRandom(clips);
        }

        private void PlayRandom(AudioClip[] clips)
        {
            if (clips == null || clips.Length == 0 || audioSource == null) return;
            audioSource.PlayOneShot(clips[Random.Range(0, clips.Length)]);
        }
    }
}
