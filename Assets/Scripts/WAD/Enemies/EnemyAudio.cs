using UnityEngine;

namespace WAD.Enemies
{
    /// <summary>
    /// Liegt auf demselben Objekt wie EnemyController. Spielt passende Sounds
    /// bei Idle (zufaellig, periodisch), Sichtkontakt zum Spieler, Schaden und Tod.
    /// </summary>
    [RequireComponent(typeof(EnemyController))]
    [RequireComponent(typeof(AudioSource))]
    public class EnemyAudio : MonoBehaviour
    {
        [Header("Sounds")]
        public AudioClip[] idleSounds;
        public AudioClip[] spottedPlayerSounds; // z.B. "telepathischer Schrei" laut Design
        public AudioClip[] damagedSounds;
        public AudioClip[] deathSounds;

        [Header("Idle-Timing")]
        public float minIdleInterval = 6f;
        public float maxIdleInterval = 14f;

        private EnemyController enemy;
        private AudioSource audioSource;
        private float idleTimer;
        private float nextIdleDelay;

        private void Awake()
        {
            enemy = GetComponent<EnemyController>();
            audioSource = GetComponent<AudioSource>();
            SetNextIdleDelay();
        }

        private void OnEnable()
        {
            enemy.OnSpottedPlayer += HandleSpottedPlayer;
            enemy.OnDamaged += HandleDamaged;
            enemy.OnDeath += HandleDeath;
        }

        private void OnDisable()
        {
            enemy.OnSpottedPlayer -= HandleSpottedPlayer;
            enemy.OnDamaged -= HandleDamaged;
            enemy.OnDeath -= HandleDeath;
        }

        private void Update()
        {
            if (enemy.CurrentState != EnemyState.Patrol) return; // nur im Idle/Patrol zufaellige Laute

            idleTimer += Time.deltaTime;
            if (idleTimer >= nextIdleDelay)
            {
                idleTimer = 0f;
                SetNextIdleDelay();
                PlayRandom(idleSounds);
            }
        }

        private void SetNextIdleDelay()
        {
            nextIdleDelay = Random.Range(minIdleInterval, maxIdleInterval);
        }

        private void HandleSpottedPlayer(EnemyController e) => PlayRandom(spottedPlayerSounds);
        private void HandleDamaged(EnemyController e, float amount) => PlayRandom(damagedSounds);
        private void HandleDeath(EnemyController e) => PlayRandom(deathSounds);

        private void PlayRandom(AudioClip[] clips)
        {
            if (clips == null || clips.Length == 0 || audioSource == null) return;
            audioSource.PlayOneShot(clips[Random.Range(0, clips.Length)]);
        }
    }
}
