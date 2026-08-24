using System.Collections;
using UnityEngine;
using WAD.Weapons;

namespace WAD.ShootingRange
{
    /// <summary>
    /// Einfaches Schiessstand-Ziel: kippt bei Treffer um, richtet sich nach
    /// einer Weile wieder auf. Bewusst UNABHAENGIG vom Limb-Health-System,
    /// da es hier nicht um echten Kampf geht, sondern ums Einschiessen.
    /// </summary>
    public class ShootingRangeTarget : MonoBehaviour, IDamageable
    {
        [Header("Reaktion")]
        public Transform visualPivot; // das kippende Modell (Kind-Objekt)
        public float fallDuration = 0.2f;
        public float resetAfterSeconds = 3f;
        public AudioClip hitSound;

        [Header("Treffer-Statistik (fuer HUD/Auswertung)")]
        public int totalHits;
        public float lastHitDamage;

        private AudioSource audioSource;
        private bool isDown;

        public event System.Action<ShootingRangeTarget> OnHit;

        private void Awake()
        {
            audioSource = GetComponent<AudioSource>();
        }

        public void ApplyDamage(float amount, Vector3 hitPoint, Vector3 hitDirection)
        {
            totalHits++;
            lastHitDamage = amount;
            Debug.Log($"[ShootingRangeTarget:{gameObject.name}] Treffer registriert! Schaden: {amount}, isDown={isDown}, visualPivot={(visualPivot != null ? visualPivot.name : "NULL")}");
            OnHit?.Invoke(this);

            if (audioSource != null && hitSound != null) audioSource.PlayOneShot(hitSound);

            if (!isDown)
            {
                StartCoroutine(FallAndResetRoutine());
            }
        }

        private IEnumerator FallAndResetRoutine()
        {
            isDown = true;

            if (visualPivot != null)
            {
                Quaternion startRot = visualPivot.localRotation;
                Quaternion downRot = startRot * Quaternion.Euler(-85f, 0f, 0f);

                float t = 0f;
                while (t < fallDuration)
                {
                    t += Time.deltaTime;
                    visualPivot.localRotation = Quaternion.Slerp(startRot, downRot, t / fallDuration);
                    yield return null;
                }
            }

            yield return new WaitForSeconds(resetAfterSeconds);

            if (visualPivot != null)
            {
                Quaternion downRot = visualPivot.localRotation;
                Quaternion upRot = Quaternion.identity;

                float t = 0f;
                while (t < fallDuration)
                {
                    t += Time.deltaTime;
                    visualPivot.localRotation = Quaternion.Slerp(downRot, upRot, t / fallDuration);
                    yield return null;
                }
            }

            isDown = false;
        }
    }
}