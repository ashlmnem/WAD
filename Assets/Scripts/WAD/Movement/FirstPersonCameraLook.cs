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

        private float pitch;
        private float yaw;
        private Vector2 currentLookVelocity;
        private Vector2 smoothedInput;

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

            float finalPitch = Mathf.Clamp(pitch + recoilPitchOffset, minPitch - 20f, maxPitch);
            transform.localRotation = Quaternion.Euler(finalPitch, 0f, 0f);

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
    }
}