using UnityEngine;

namespace WAD.Weapons.Attachments
{
    /// <summary>
    /// Steuert den LEUCHTPUNKT INNERHALB einer Optik (nicht die Optik selbst).
    /// Gehoert auf das Punkt-Objekt (Quad/Sprite/Mesh) im Optik-Visual-Prefab.
    ///
    /// Alles ist im Inspector einstellbar - kein Code noetig:
    ///  - Sichtbarkeit nur waehrend ADS (oder dauerhaft)
    ///  - weiches Ein-/Ausblenden ueber die Emission der Punkt-Materials
    ///  - optionales "Kollimator"-Verhalten: der Punkt folgt der Zielachse der
    ///    Kamera innerhalb eines begrenzten Radius, statt starr in der Optik zu
    ///    kleben. Das ist optisch der Eindruck eines echten Rotpunktvisiers,
    ///    kann aber die Optik nie verlassen (Radius wird hart begrenzt).
    ///  - optionales leichtes Atmen/Schwanken des Punktes
    /// </summary>
    public class RedDotReticle : MonoBehaviour
    {
        public enum ReticleBehaviour
        {
            /// <summary>Punkt bleibt exakt an seiner Stelle in der Optik (immer sicher).</summary>
            Fixed,
            /// <summary>Punkt folgt der Zielachse, begrenzt auf einen Radius innerhalb der Optik.</summary>
            Collimated
        }

        [Header("Referenzen")]
        [Tooltip("Leer lassen fuer automatisches Suchen in der Elternhierarchie")]
        public WeaponController weaponController;
        [Tooltip("Der sichtbare Punkt. Leer = Renderer auf diesem Objekt.")]
        public Renderer dotRenderer;
        [Tooltip("Transform des Punktes, falls er nicht auf demselben Objekt liegt.")]
        public Transform dotTransform;

        [Header("Sichtbarkeit")]
        [Tooltip("Nur waehrend ADS anzeigen. Aus = Punkt ist immer sichtbar.")]
        public bool onlyVisibleWhileAiming = true;
        [Tooltip("Ein-/Ausblendzeit in Sekunden (0 = hartes Umschalten)")]
        public float fadeDuration = 0.08f;
        [Tooltip("Das komplette Punkt-Objekt deaktivieren statt nur den Renderer (spart Draw-Calls).")]
        public bool toggleGameObject = false;

        [Header("Verhalten")]
        public ReticleBehaviour behaviour = ReticleBehaviour.Fixed;
        [Tooltip("Maximaler Radius, den der Punkt innerhalb der Optik wandern darf (Meter).")]
        public float maxWanderRadius = 0.004f;
        [Tooltip("Wie stark der Punkt der Zielachse folgt (0 = starr, 1 = voll).")]
        [Range(0f, 1f)] public float followStrength = 0.6f;

        [Header("Atmung (optional)")]
        [Tooltip("Amplitude des Punkt-Schwankens in Metern (0 = aus).")]
        public float breathAmplitude = 0f;
        public float breathSpeed = 1.5f;

        [Header("Emission")]
        [Tooltip("Material-Property fuer die Helligkeit. Leer = keine Anpassung.")]
        public string emissionProperty = "_EmissionColor";
        [ColorUsage(true, true)] public Color aimColor = new Color(1f, 0.15f, 0.05f, 1f);
        [Tooltip("Multiplikator fuer HDR-Intensitaet im Zielmodus")]
        public float aimIntensity = 2f;

        private Camera playerCamera;
        private MaterialPropertyBlock propertyBlock;
        private Vector3 baseLocalPosition;
        private Quaternion baseLocalRotation;
        private float currentVisibility;
        private bool baseCaptured;

        private void Awake()
        {
            if (dotRenderer == null) dotRenderer = GetComponent<Renderer>();
            if (dotTransform == null) dotTransform = transform;
            if (weaponController == null) weaponController = GetComponentInParent<WeaponController>();

            propertyBlock = new MaterialPropertyBlock();
            CaptureBasePose();
        }

        private void OnEnable()
        {
            // Die Optik wird erst zur Laufzeit montiert - Referenzen hier erneut versuchen.
            if (weaponController == null) weaponController = GetComponentInParent<WeaponController>();
            if (!baseCaptured) CaptureBasePose();
        }

        private void CaptureBasePose()
        {
            if (dotTransform == null) return;
            baseLocalPosition = dotTransform.localPosition;
            baseLocalRotation = dotTransform.localRotation;
            baseCaptured = true;
        }

        private void LateUpdate()
        {
            if (dotRenderer == null || dotTransform == null) return;

            bool aiming = weaponController != null && weaponController.IsAiming;
            float target = (!onlyVisibleWhileAiming || aiming) ? 1f : 0f;

            UpdateVisibility(target);
            UpdateReticlePose(aiming);
        }

        private void UpdateVisibility(float target)
        {
            float step = fadeDuration > 0f ? Time.deltaTime / fadeDuration : 1f;
            currentVisibility = Mathf.MoveTowards(currentVisibility, target, step);

            bool visible = currentVisibility > 0.001f;
            if (dotRenderer.enabled != visible) dotRenderer.enabled = visible;

            if (toggleGameObject && dotTransform.gameObject.activeSelf != visible)
            {
                dotTransform.gameObject.SetActive(visible);
            }

            if (string.IsNullOrEmpty(emissionProperty) || propertyBlock == null) return;

            int propertyId = Shader.PropertyToID(emissionProperty);
            Color c = aimColor * (currentVisibility * aimIntensity);
            dotRenderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetColor(propertyId, c);
            dotRenderer.SetPropertyBlock(propertyBlock);
        }

        private void UpdateReticlePose(bool aiming)
        {
            // Basispose immer wiederherstellen, damit sich kein Drift aufsummiert.
            Vector3 localPos = baseLocalPosition;

            if (aiming)
            {
                if (breathAmplitude > 0f)
                {
                    float t = Time.time * breathSpeed;
                    localPos += new Vector3(
                        Mathf.Sin(t * 0.9f) * breathAmplitude,
                        Mathf.Sin(t * 1.3f) * breathAmplitude,
                        0f);
                }

                if (behaviour == ReticleBehaviour.Collimated && followStrength > 0f)
                {
                    if (playerCamera == null)
                    {
                        playerCamera = weaponController != null && weaponController.playerCamera != null
                            ? weaponController.playerCamera
                            : Camera.main;
                    }

                    if (playerCamera != null)
                    {
                        // Zielachse der Kamera in den lokalen Raum der Optik bringen
                        // und den Punkt begrenzt darauf zulaufen lassen.
                        Vector3 camForwardLocal = dotTransform.parent != null
                            ? dotTransform.parent.InverseTransformDirection(playerCamera.transform.forward)
                            : playerCamera.transform.forward;

                        Vector3 offset = Vector3.ProjectOnPlane(camForwardLocal, Vector3.forward) * maxWanderRadius;
                        Vector3 clamped = Vector3.ClampMagnitude(offset, maxWanderRadius);

                        localPos += clamped * followStrength;
                    }
                }
            }

            dotTransform.localPosition = localPos;
            dotTransform.localRotation = baseLocalRotation;
        }
    }
}
