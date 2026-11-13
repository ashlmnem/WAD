using UnityEngine;

namespace WAD.Weapons.Attachments
{
    public class LaserAttachmentController : MonoBehaviour
    {
        public Transform laserOrigin;
        public LineRenderer lineRenderer;

        private Color laserColor = Color.red;
        private float laserRange = 50f;
        private float laserWidth = 0.01f;
        private KeyCode toggleKey = KeyCode.L;
        private Vector3 originOffset;
        private Vector3 localDirection = Vector3.forward;
        private float hipfireImpactOffset;
        private bool laserOn;
        private WeaponController weapon;
        private static readonly RaycastHit[] hitBuffer = new RaycastHit[32];
        private static Material sharedLaserMaterial;

        public bool IsOn => laserOn;

        private void Awake()
        {
            if (laserOrigin == null) laserOrigin = transform;
            EnsureLineRenderer();
            SetLaserEnabled(false);
        }

        public void Configure(AttachmentSO attachment, WeaponController owner, Transform visualRoot)
        {
            if (attachment == null) return;

            weapon = owner;
            laserColor = attachment.laserColor;
            laserRange = Mathf.Max(0.1f, attachment.laserRange);
            laserWidth = Mathf.Max(0.001f, attachment.laserWidth);
            toggleKey = attachment.laserToggleKey;
            originOffset = attachment.laserOriginOffset;
            localDirection = attachment.laserLocalDirection.sqrMagnitude > 0.0001f
                ? attachment.laserLocalDirection.normalized
                : Vector3.forward;
            hipfireImpactOffset = Mathf.Max(0f, attachment.laserHipfireImpactOffset);

            if (!string.IsNullOrEmpty(attachment.laserOriginName) && visualRoot != null)
            {
                foreach (Transform child in visualRoot.GetComponentsInChildren<Transform>(true))
                {
                    if (child.name != attachment.laserOriginName) continue;
                    laserOrigin = child;
                    break;
                }
            }
            if (laserOrigin == null) laserOrigin = visualRoot != null ? visualRoot : transform;

            EnsureLineRenderer();
            SetLaserEnabled(attachment.laserOnByDefault);
        }

        private void Update()
        {
            if (weapon != null && !weapon.isEquipped) return;

            if (toggleKey != KeyCode.None && Input.GetKeyDown(toggleKey))
            {
                SetLaserEnabled(!laserOn);
            }
            if (laserOn) UpdateLaser();
        }

        public void SetLaserEnabled(bool enabled)
        {
            laserOn = enabled;
            if (lineRenderer != null) lineRenderer.enabled = laserOn;
            if (laserOn) UpdateLaser();
        }

        private void EnsureLineRenderer()
        {
            if (lineRenderer == null) lineRenderer = GetComponent<LineRenderer>();
            if (lineRenderer == null) lineRenderer = gameObject.AddComponent<LineRenderer>();

            lineRenderer.useWorldSpace = true;
            lineRenderer.positionCount = 2;
            lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lineRenderer.receiveShadows = false;

            if (sharedLaserMaterial == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
                if (shader == null) shader = Shader.Find("Sprites/Default");
                if (shader != null)
                {
                    sharedLaserMaterial = new Material(shader)
                    {
                        name = "WAD Shared Laser Material",
                        hideFlags = HideFlags.HideAndDontSave
                    };
                }
            }
            if (lineRenderer.sharedMaterial == null && sharedLaserMaterial != null)
            {
                lineRenderer.sharedMaterial = sharedLaserMaterial;
            }

            lineRenderer.enabled = false;
        }

        private void UpdateLaser()
        {
            if (lineRenderer == null || laserOrigin == null) return;
            lineRenderer.enabled = laserOn;
            if (!laserOn) return;
            lineRenderer.startColor = laserColor;
            lineRenderer.endColor = laserColor;
            lineRenderer.startWidth = laserWidth;
            lineRenderer.endWidth = laserWidth;

            GetBeam(out Vector3 origin, out _, out Vector3 end);

            lineRenderer.SetPosition(0, origin);
            lineRenderer.SetPosition(1, end);
        }

        public bool TryGetHipfireAimPoint(Camera camera, out Vector3 aimPoint)
        {
            aimPoint = Vector3.zero;
            if (!laserOn || camera == null || laserOrigin == null) return false;

            GetBeam(out _, out _, out Vector3 beamEnd);
            aimPoint = beamEnd - camera.transform.up * hipfireImpactOffset;
            return true;
        }

        private void GetBeam(out Vector3 origin, out Vector3 direction, out Vector3 end)
        {
            origin = laserOrigin.TransformPoint(originOffset);
            direction = laserOrigin.TransformDirection(localDirection).normalized;
            end = origin + direction * laserRange;

            int hitCount = Physics.RaycastNonAlloc(origin, direction, hitBuffer, laserRange,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);

            float closestDistance = laserRange;
            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit hit = hitBuffer[i];
                if (hit.collider == null || (weapon != null && hit.collider.transform.root == weapon.transform.root)) continue;
                if (hit.distance >= closestDistance) continue;

                closestDistance = hit.distance;
                end = hit.point;
            }
        }
    }
}
