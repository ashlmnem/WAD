using UnityEngine;
using WAD.Player;

namespace WAD.Player
{
    /// <summary>
    /// Liegt auf dem Player-Objekt. Spielt Schritt-Sounds basierend auf
    /// Bewegung (Tarkov-typisch: unterschiedlich laut je nach Gehen/Sprinten/
    /// Ducken - relevant spaeter auch fuer Gegner-Hoerbarkeit) sowie
    /// Schmerz-/Sterbe-Sounds ueber PlayerHealth-Events.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class PlayerAudio : MonoBehaviour
    {
        [Header("Referenzen")]
        public TarkovMovementController movementController;
        public PlayerHealth playerHealth;
        public CharacterController characterController;

        [Header("Schritte")]
        public AudioClip[] footstepSoundsWalk;
        public AudioClip[] footstepSoundsSprint;
        public float walkStepInterval = 0.5f;
        public float sprintStepInterval = 0.32f;

        [Header("Schmerz/Tod")]
        public AudioClip[] hurtSounds;
        public AudioClip deathSound;

        private AudioSource audioSource;
        private float stepTimer;

        private void Awake()
        {
            audioSource = GetComponent<AudioSource>();
        }

        private void OnEnable()
        {
            if (playerHealth != null)
            {
                playerHealth.OnDamaged += HandleDamaged;
                playerHealth.OnDied += HandleDied;
            }
        }

        private void OnDisable()
        {
            if (playerHealth != null)
            {
                playerHealth.OnDamaged -= HandleDamaged;
                playerHealth.OnDied -= HandleDied;
            }
        }

        private void Update()
        {
            HandleFootsteps();
        }

        private void HandleFootsteps()
        {
            if (characterController == null || !characterController.isGrounded) return;

            Vector3 horizontalVelocity = new Vector3(characterController.velocity.x, 0f, characterController.velocity.z);
            if (horizontalVelocity.magnitude < 0.5f) return; // steht still

            bool sprinting = movementController != null && movementController.IsSprinting;
            float interval = sprinting ? sprintStepInterval : walkStepInterval;

            stepTimer += Time.deltaTime;
            if (stepTimer >= interval)
            {
                stepTimer = 0f;
                PlayRandom(sprinting ? footstepSoundsSprint : footstepSoundsWalk);
            }
        }

        private void HandleDamaged(float amount) => PlayRandom(hurtSounds);

        private void HandleDied()
        {
            if (deathSound != null && audioSource != null) audioSource.PlayOneShot(deathSound);
        }

        private void PlayRandom(AudioClip[] clips)
        {
            if (clips == null || clips.Length == 0 || audioSource == null) return;
            audioSource.PlayOneShot(clips[Random.Range(0, clips.Length)]);
        }
    }
}
