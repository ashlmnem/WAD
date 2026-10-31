using System.Collections;
using UnityEngine;

namespace WAD.Player
{
    /// <summary>
    /// Kamera-Rotation getrennt von der Koerper-Rotation (Standard-FPS-Pattern).
    /// Liegt auf der Kamera selbst; playerBody ist das Root-Objekt mit dem
    /// CharacterController (siehe TarkovMovementController).
    /// </summary>
    public class FirstPersonCameraLook : MonoBehaviour
    {
        [Header("Referenzen")]
        public Transform playerBody;

        [Header("Sensitivität")]
        public float mouseSensitivity = 150f;
        [Tooltip("Wird von ADS/Waffensystem multipliziert (z.B. 0.5 beim Zielen)")]
        public float sensitivityMultiplier = 1f;

        [Header("Pitch-Limits")]
        public float minPitch = -85f;
        public float maxPitch = 85f;

        [Header("Sway (leichtes Waffenschwanken bei Bewegung - optional, Weapon-System haengt sich hier ein)")]
        public bool enableLookSmoothing = true;
        public float lookSmoothTime = 0.03f;

        [Header("Recoil")]
        [Tooltip("Wie schnell sich der Rueckstoss-Versatz wieder auf 0 zurueckbewegt")]
        public float recoilRecoverySpeed = 6f;
        private float recoilPitchOffset;
        private float recoilYawOffset;

        [Header("Nachlade-Kamera-Animation")]
        [Tooltip("Wie weit die Kamera waehrend des Nachladens nach unten kippt (Grad)")]
        public float reloadTiltDegrees = 6f;
        [Tooltip("Seitliche Neigung waehrend des Nachladens (Grad, positiver Wert = Kamera kippt zur Seite)")]
        public float reloadRollDegrees = 3f;
        [Tooltip("Dauer der Nachlade-Bewegung, wenn kein eigener Wert uebergeben wird (Sekunden)")]
        public float defaultReloadAnimationDuration = 1.5f;

        private float pitch;
        private float yaw;
        private Vector2 currentLookVelocity;
        private Vector2 smoothedInput;

        // Laufende Nachlade-Animation. Wird als additiver Versatz auf Pitch/Roll
        // gelegt, damit sie weder die Maus-Eingabe noch den Recoil ueberschreibt.
        private Coroutine reloadAnimationRoutine;
        private float reloadPitchOffset;
        private float reloadRollOffset;

        private void Start()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void Update()
        {
            float mouseX = Input.GetAxisRaw("Mouse X") * mouseSensitivity * sensitivityMultiplier * Time.deltaTime;
            float mouseY = Input.GetAxisRaw("Mouse Y") * mouseSensitivity * sensitivityMultiplier * Time.deltaTime;

            Vector2 targetInput = new Vector2(mouseX, mouseY);

            if (enableLookSmoothing)
            {
                smoothedInput = Vector2.SmoothDamp(smoothedInput, targetInput, ref currentLookVelocity, lookSmoothTime);
            }
            else
            {
                smoothedInput = targetInput;
            }

            yaw += smoothedInput.x;
            pitch -= smoothedInput.y;
            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

            // Recoil klingt selbststaendig ab, unabhaengig von Maus-Input
            recoilPitchOffset = Mathf.Lerp(recoilPitchOffset, 0f, recoilRecoverySpeed * Time.deltaTime);
            recoilYawOffset = Mathf.Lerp(recoilYawOffset, 0f, recoilRecoverySpeed * Time.deltaTime);

            float finalPitch = Mathf.Clamp(pitch + recoilPitchOffset + reloadPitchOffset, minPitch - 20f, maxPitch);
            transform.localRotation = Quaternion.Euler(finalPitch, 0f, reloadRollOffset);

            if (playerBody != null)
            {
                playerBody.localRotation = Quaternion.Euler(0f, yaw + recoilYawOffset, 0f);
            }
        }

        /// <summary> Von WeaponRecoil beim Schuss aufgerufen: stoesst die Kamera nach oben (und leicht seitlich). </summary>
        public void ApplyRecoilKick(float pitchKickDegrees, float yawKickDegrees)
        {
            recoilPitchOffset -= pitchKickDegrees; // negativ = Kamera kippt nach oben
            recoilYawOffset += yawKickDegrees;
        }

        /// <summary>
        /// Von WeaponController beim Nachladen aufgerufen. Laesst die Kamera kurz
        /// absinken und seitlich kippen (Blick auf das Magazin), und danach wieder
        /// sauber in die Ausgangslage zurueckkehren.
        /// </summary>
        /// <param name="duration">Gesamtdauer. &lt;= 0 verwendet defaultReloadAnimationDuration.</param>
        public void TriggerReloadAnimation(float duration = 0f)
        {
            float total = duration > 0f ? duration : defaultReloadAnimationDuration;
            if (total <= 0f) return;

            // Ein noch laufendes Nachladen wird sauber ersetzt statt sich zu stapeln.
            if (reloadAnimationRoutine != null) StopCoroutine(reloadAnimationRoutine);
            reloadAnimationRoutine = StartCoroutine(ReloadAnimationRoutine(total));
        }

        private IEnumerator ReloadAnimationRoutine(float duration)
        {
            // Der Blick senkt sich waehrend der ersten Haelfte des Nachladens ab und
            // in der zweiten Haelfte wieder hoch - sin-Fenster sorgt fuer weiche
            // Ein- und Ausgaenge ohne harte Richtungswechsel.
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float window = Mathf.Sin(t * Mathf.PI); // 0 -> 1 -> 0

                reloadPitchOffset = -reloadTiltDegrees * window;
                reloadRollOffset = reloadRollDegrees * window;

                yield return null;
            }

            reloadPitchOffset = 0f;
            reloadRollOffset = 0f;
            reloadAnimationRoutine = null;
        }
    }
}
