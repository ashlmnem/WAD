using UnityEngine;

namespace WAD.Vehicles
{
    /// <summary>
    /// Ein fliegendes Geschoss (Rakete) des Jets. Wird von JetWeaponSystem
    /// erzeugt und konfiguriert. Alle Werte kommen aus dem Waffenplatz.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class JetRocket : MonoBehaviour
    {
        [Header("VFX (optional, im Prefab belegen)")]
        public GameObject explosionVFXPrefab;
        public float explosionVFXlifetime = 3f;
        [Tooltip("Rauch-/Triebwerksspur, die mitfliegt")]
        public GameObject trailPrefab;

        [Header("Lebensdauer")]
        [Tooltip("Nach dieser Zeit zerstoert sich die Rakete selbst (Sicherheit)")]
        public float maxLifetime = 15f;

        private Vector3 direction;
        private float speed;
        private float explosionRadius;
        private float explosionDamage;
        private float homingStrength;
        private LayerMask hittableLayers;
        private GameObject owner;
        private float lifeTimer;
        private Transform homingTarget;

        private bool initialized;

        public void Init(Vector3 dir, float rocketSpeed, float radius, float damage,
                         float homing, LayerMask layers, GameObject ownerObject)
        {
            direction = dir.normalized;
            speed = rocketSpeed;
            explosionRadius = radius;
            explosionDamage = damage;
            homingStrength = homing;
            hittableLayers = layers;
            owner = ownerObject;
            lifeTimer = 0f;
            initialized = true;

            transform.rotation = Quaternion.LookRotation(direction);

            if (trailPrefab != null)
            {
                Instantiate(trailPrefab, transform.position, transform.rotation, transform);
            }
        }

        private void Update()
        {
            if (!initialized) return;

            lifeTimer += Time.deltaTime;
            if (lifeTimer >= maxLifetime)
            {
                Detonate(transform.position);
                return;
            }

            // Leichte Zielverfolgung auf das Ziel unter dem Fadenkreuz
            if (homingStrength > 0f)
            {
                if (homingTarget == null && Physics.Raycast(transform.position, direction, out RaycastHit scan, 600f, hittableLayers))
                {
                    if (owner == null || scan.collider.transform.root != owner.transform.root)
                    {
                        homingTarget = scan.collider.transform;
                    }
                }

                if (homingTarget != null)
                {
                    Vector3 desired = (homingTarget.position - transform.position).normalized;
                    direction = Vector3.Slerp(direction, desired, homingStrength * Time.deltaTime);
                    transform.rotation = Quaternion.LookRotation(direction);
                }
            }

            float step = speed * Time.deltaTime;

            if (Physics.SphereCast(transform.position, 0.25f, direction, out RaycastHit hit, step, hittableLayers))
            {
                // Eigenen Jet nicht treffen
                if (owner == null || hit.collider.transform.root != owner.transform.root)
                {
                    Detonate(hit.point);
                    return;
                }
            }

            transform.position += direction * step;
        }

        private void Detonate(Vector3 position)
        {
            var processed = new System.Collections.Generic.HashSet<WAD.Weapons.IDamageable>();

            foreach (var col in Physics.OverlapSphere(position, explosionRadius, hittableLayers))
            {
                var damageable = col.GetComponentInParent<WAD.Weapons.IDamageable>();
                if (damageable == null || !processed.Add(damageable)) continue;

                float distance = Vector3.Distance(position, col.ClosestPoint(position));
                float falloff = Mathf.Clamp01(1f - distance / Mathf.Max(0.01f, explosionRadius));
                damageable.ApplyDamage(explosionDamage * falloff, position, (col.transform.position - position).normalized);
            }

            if (explosionVFXPrefab != null)
            {
                GameObject fx = Instantiate(explosionVFXPrefab, position, Quaternion.identity);
                if (explosionVFXlifetime > 0f) Destroy(fx, explosionVFXlifetime);
            }

            Destroy(gameObject);
        }
    }
}
