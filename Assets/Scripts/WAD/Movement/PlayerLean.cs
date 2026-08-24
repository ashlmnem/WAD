using UnityEngine;

namespace WAD.Player
{
    /// <summary>
    /// Taktisches Lehnen um Ecken (Q = links, E = rechts), wie in Tarkov/Insurgency.
    /// Bewegt NICHT den ganzen Player-Body, sondern nur die Kamera + einen Pivot,
    /// damit Kollision separat geprueft werden kann (kein Clipping durch Waende).
    ///
    /// Setup: Liegt auf einem "LeanPivot"-Objekt, das zwischen Player-Root und
    /// Kamera sitzt: Player > LeanPivot > Main Camera.
    /// </summary>
    public class PlayerLean : MonoBehaviour
    {
        [Header("Referenzen")]
        [Tooltip("Der Pivot, der tatsaechlich gekippt/verschoben wird (Elternteil der Kamera)")]
        public Transform leanPivot;
        public Transform playerRoot;

        [Header("Einstellungen")]
        public float leanAngle = 15f;
        public float leanSideOffset = 0.5f;
        public float leanSpeed = 8f;

        [Header("Hindernis-Check")]
        public float obstructionCheckRadius = 0.3f;
        public LayerMask obstructionLayers = ~0;

        private float targetLeanDirection; // -1 links, 0 neutral, 1 rechts
        private float currentLeanAmount;    // 0 bis 1, geglaettet

        /// <summary> True, sobald spuerbar gelehnt wird - Grundlage fuer Blindfeuer (Punkt 7). </summary>
        public bool IsLeaning => Mathf.Abs(currentLeanAmount) > 0.3f;

        private void Update()
        {
            HandleLeanInput();
            ApplyLean();
        }

        private void HandleLeanInput()
        {
            bool leanLeft = Input.GetKey(KeyCode.Q);
            bool leanRight = Input.GetKey(KeyCode.E);

            if (leanLeft && !leanRight) targetLeanDirection = -1f;
            else if (leanRight && !leanLeft) targetLeanDirection = 1f;
            else targetLeanDirection = 0f;

            // Falls Hindernis in die gewuenschte Richtung: nicht weiter lehnen
            if (targetLeanDirection != 0f && IsObstructed(targetLeanDirection))
            {
                targetLeanDirection = 0f;
            }
        }

        private bool IsObstructed(float direction)
        {
            if (playerRoot == null) return false;

            Vector3 sideDirection = playerRoot.right * direction;
            Vector3 origin = leanPivot.position;

            return Physics.SphereCast(origin, obstructionCheckRadius, sideDirection,
                out _, leanSideOffset, obstructionLayers);
        }

        private void ApplyLean()
        {
            currentLeanAmount = Mathf.Lerp(currentLeanAmount, targetLeanDirection, leanSpeed * Time.deltaTime);

            float zRotation = -currentLeanAmount * leanAngle;
            float xOffset = currentLeanAmount * leanSideOffset;

            leanPivot.localRotation = Quaternion.Euler(0f, 0f, zRotation);
            leanPivot.localPosition = new Vector3(xOffset, leanPivot.localPosition.y, leanPivot.localPosition.z);
        }
    }
}