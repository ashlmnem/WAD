using UnityEngine;
using WAD.Player;
using WAD.Feedback;

namespace WAD.Player
{
    /// <summary>
    /// Liegt auf dem Player-Objekt. Spielt Schritt-Sounds basierend auf
    /// Bewegung (Tarkov-typisch: unterschiedlich laut je nach Gehen/Sprinten/
    /// Ducken - relevant spaeter auch fuer Gegner-Hoerbarkeit) sowie
    /// Schmerz-/Sterbe-Sounds ueber PlayerHealth-Events.
    ///
    /// Sounds laufen bevorzugt ueber den ActionFeedbackPlayer (dort pro Aktion
    /// im Inspector eintragbar). Solange dort nichts eingetragen ist, gelten
    /// weiterhin die Clip-Listen auf dieser Komponente.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class PlayerAudio : MonoBehaviour
    {
        [Header("Referenzen")]
        public TarkovMovementController movementController;
        public PlayerHealth playerHealth;
        public CharacterController characterController;
        [Tooltip("Zentrale Feedback-Station. Leer = automatisch auf diesem Objekt gesucht.")]
        public ActionFeedbackPlayer feedback;

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
            if (feedback == null) feedback = GetComponent<ActionFeedbackPlayer>();
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
            bool crouching = movementController != null && movementController.IsCrouching;
            float interval = sprinting ? sprintStepInterval : walkStepInterval;

            stepTimer += Time.deltaTime;
            if (stepTimer >= interval)
            {
                stepTimer = 0f;

                if (sprinting)
                {
                    PlayOrFallback(ActionId.FootstepSprint, footstepSoundsSprint);
                }
                else if (crouching)
                {
                    // Leiser Schritt beim Ducken - eigener Slot, faellt auf Gehen zurueck
                    if (feedback != null && feedback.HasSounds(ActionId.FootstepCrouch))
                    {
                        feedback.Play(ActionId.FootstepCrouch);
                    }
                    else
                    {
                        PlayOrFallback(ActionId.FootstepWalk, footstepSoundsWalk, 0.5f);
                    }
                }
                else
                {
                    PlayOrFallback(ActionId.FootstepWalk, footstepSoundsWalk);
                }
            }
        }

        private void HandleDamaged(float amount)
        {
            // Schwerer Treffer ab 25 Schaden bekommt einen eigenen Slot
            var action = amount >= 25f ? ActionId.PlayerHurtHeavy : ActionId.PlayerHurtLight;

            if (feedback != null && feedback.HasSounds(action))
            {
                feedback.Play(action);
                return;
            }

            PlayRandom(hurtSounds);
        }

        private void HandleDied()
        {
            if (feedback != null && feedback.HasSounds(ActionId.PlayerDeath))
            {
                feedback.Play(ActionId.PlayerDeath);
                return;
            }

            if (deathSound != null && audioSource != null) audioSource.PlayOneShot(deathSound);
        }

        private void PlayOrFallback(ActionId action, AudioClip[] clips, float volume = 1f)
        {
            if (feedback != null)
            {
                feedback.PlayOrFallback(action, clips, volume);
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
