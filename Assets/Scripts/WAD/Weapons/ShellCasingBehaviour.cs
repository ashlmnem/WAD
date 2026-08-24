using UnityEngine;

namespace WAD.Weapons.Casings
{
    /// <summary>
    /// Liegt auf dem Huelsen-Prefab selbst (AmmoTypeSO.casingPrefab).
    /// Spielt beim ersten Bodenkontakt einen metallischen "Klick"-Sound ab.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class ShellCasingBehaviour : MonoBehaviour
    {
        public AudioClip[] landSounds;
        [Range(0f, 1f)] public float landVolume = 0.5f;
        [Tooltip("Mindest-Aufprallgeschwindigkeit, damit ueberhaupt ein Sound abgespielt wird (verhindert Sound bei sanftem Absetzen)")]
        public float minImpactVelocity = 0.5f;

        private bool hasPlayedLandSound;
        private AudioSource audioSource;

        private void Awake()
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1f; // 3D-Sound
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (hasPlayedLandSound) return;
            if (collision.relativeVelocity.magnitude < minImpactVelocity) return;

            hasPlayedLandSound = true;

            if (landSounds != null && landSounds.Length > 0)
            {
                audioSource.PlayOneShot(landSounds[Random.Range(0, landSounds.Length)], landVolume);
            }
        }
    }
}