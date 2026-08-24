using UnityEngine;
using WAD.Audio;
using WAD.Enemies;
using WAD.Player;

namespace WAD.Levels
{
    public enum MusicState { Idle, Combat, LowHealth }

    /// <summary>
    /// Ueberwacht den Spielzustand und wechselt entsprechend die Musik ueber
    /// den persistenten MusicManager. Prioritaet: LowHealth > Combat > Idle.
    ///
    /// Combat wird erkannt, indem alle EnemyController in der Szene periodisch
    /// nach IsEngagingPlayer gefragt werden (kein Performance-Problem bei den
    /// ueblichen Gegnermengen pro Level).
    /// </summary>
    public class DynamicMusicController : MonoBehaviour
    {
        [Header("Referenzen")]
        public PlayerHealth playerHealth;

        [Header("Tracks")]
        public AudioClip idleTrack;
        public AudioClip combatTrack;
        public AudioClip lowHealthTrack;

        [Header("Einstellungen")]
        [Range(0f, 1f)] public float lowHealthThreshold = 0.25f;
        [Tooltip("Wie lange NACH dem letzten Kampfkontakt noch Combat-Musik weiterlaeuft, bevor zurueck zu Idle gewechselt wird")]
        public float combatCooldownSeconds = 6f;
        public float checkInterval = 0.5f;

        private MusicState currentMusicState = MusicState.Idle;
        private float timeSinceLastCombat = 999f;
        private float checkTimer;

        private void Update()
        {
            checkTimer += Time.deltaTime;
            if (checkTimer < checkInterval) return;
            checkTimer = 0f;

            EvaluateState();
        }

        private void EvaluateState()
        {
            bool isLowHealth = playerHealth != null && playerHealth.HealthPercent01 <= lowHealthThreshold && !playerHealth.IsDead;
            bool isInCombatNow = IsAnyEnemyEngaging();

            if (isInCombatNow) timeSinceLastCombat = 0f;
            else timeSinceLastCombat += checkInterval;

            bool isInCombat = timeSinceLastCombat < combatCooldownSeconds;

            MusicState targetState;
            if (isLowHealth) targetState = MusicState.LowHealth;
            else if (isInCombat) targetState = MusicState.Combat;
            else targetState = MusicState.Idle;

            if (targetState != currentMusicState)
            {
                currentMusicState = targetState;
                ApplyMusicState(targetState);
            }
        }

        private bool IsAnyEnemyEngaging()
        {
            // FindObjectsOfType ist bei ueblichen Gegneranzahlen pro Level unkritisch;
            // bei sehr vielen Gegnern spaeter ggf. auf einen zentralen "aktive Gegner"-
            // Registry-Ansatz wie in EnemySpawner umstellen.
            var enemies = FindObjectsOfType<EnemyController>();
            foreach (var enemy in enemies)
            {
                if (enemy.IsEngagingPlayer) return true;
            }
            return false;
        }

        private void ApplyMusicState(MusicState newState)
        {
            if (MusicManager.Instance == null) return;

            AudioClip clip = newState switch
            {
                MusicState.Combat => combatTrack,
                MusicState.LowHealth => lowHealthTrack,
                _ => idleTrack
            };

            MusicManager.Instance.PlayTrack(clip, loop: true);
        }
    }
}
