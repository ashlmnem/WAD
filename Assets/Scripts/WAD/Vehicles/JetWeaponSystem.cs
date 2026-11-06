using UnityEngine;
using WAD.Feedback;
using WAD.Weapons;

namespace WAD.Vehicles
{
    /// <summary>
    /// Ein einzelner Waffenplatz am Jet (z.B. zwei Maschinengewehre + Raketen).
    /// Alles im Inspector einstellbar - kein Code noetig.
    /// </summary>
    [System.Serializable]
    public class JetWeaponSlot
    {
        [Header("Identifikation")]
        public string slotName = "MG Links";
        public bool enabled = true;

        [Header("Munition")]
        public AmmoTypeSO ammoType;
        public int magazineSize = 200;
        public int spareMagazines = 3;
        public float reloadSeconds = 4f;

        [Header("Feuerverhalten")]
        public float roundsPerMinute = 900f;
        [Tooltip("Streuung in Grad")]
        public float spreadDegrees = 0.4f;
        public float maxRange = 1200f;
        public float damageMultiplier = 1f;
        [Tooltip("Gleichzeitig abgefeuerte Geschosse (Schrotflinte / Salve)")]
        public int pelletsPerShot = 2;

        [Header("Raketen-Modus")]
        [Tooltip("Als Rakete statt als MG behandeln")]
        public bool isRocket = false;
        public GameObject rocketPrefab;
        public float rocketSpeed = 120f;
        public float explosionRadius = 8f;
        public float explosionDamage = 250f;
        [Tooltip("Zielverfolgung 0 = gerade, 1 = perfekte Verfolgung")]
        [Range(0f, 1f)] public float homingStrength = 0.5f;

        [Header("Referenzen")]
        [Tooltip("Muendungspunkt(e). Mehrere Punkte = abwechselndes Feuern.")]
        public Transform[] muzzlePoints;
        [Tooltip("Alternativ ein Container, dessen Kinder als Muendungen benutzt werden")]
        public Transform muzzleContainer;

        [Header("Feedback-Aktionen")]
        [Tooltip("Welche Aktion wird pro Schuss ausgeloest (ActionFeedbackPlayer auf dem Jet)")]
        public ActionId fireAction = ActionId.FireHip;
        public ActionId reloadAction = ActionId.ReloadStartEmpty;
        public ActionId emptyAction = ActionId.DryFire;
        [Tooltip("Eigene Aktion fuer Raketen (nur bei Is Rocket benutzt)")]
        public ActionId rocketFireAction = ActionId.JetRocketFire;

        [Header("Tasten")]
        public KeyCode fireKey = KeyCode.Mouse0;

        // Laufzeit
        [System.NonSerialized] public int currentAmmo;
        [System.NonSerialized] public int spare;
        [System.NonSerialized] public float lastFireTime;
        [System.NonSerialized] public bool isReloading;
        [System.NonSerialized] public int muzzleIndex;

        public Transform[] ResolveMuzzles()
        {
            if (muzzlePoints != null && muzzlePoints.Length > 0) return muzzlePoints;

            if (muzzleContainer != null)
            {
                var list = new System.Collections.Generic.List<Transform>();
                for (int i = 0; i < muzzleContainer.childCount; i++) list.Add(muzzleContainer.GetChild(i));
                return list.ToArray();
            }

            return System.Array.Empty<Transform>();
        }

        public Transform GetNextMuzzle()
        {
            if (muzzlePoints != null && muzzlePoints.Length > 0)
            {
                Transform result = muzzlePoints[muzzleIndex % muzzlePoints.Length];
                muzzleIndex++;
                return result;
            }

            if (muzzleContainer != null && muzzleContainer.childCount > 0)
            {
                Transform result = muzzleContainer.GetChild(muzzleIndex % muzzleContainer.childCount);
                muzzleIndex++;
                return result;
            }

            return null;
        }
    }

    /// <summary>
    /// Waffensystem des Jets: mehrere Waffenplaetze (MG, Raketen).
    /// Wird von JetController.TickPilotInput() aufgerufen, solange der Spieler
    /// im Cockpit sitzt.
    /// </summary>
    [RequireComponent(typeof(ActionFeedbackPlayer))]
    public class JetWeaponSystem : MonoBehaviour
    {
        [Header("Waffenplaetze")]
        public JetWeaponSlot[] weaponSlots = System.Array.Empty<JetWeaponSlot>();

        [Header("Layer")]
        public LayerMask hittableLayers = ~0;

        [Header("Zielhilfe")]
        [Tooltip("Kamera fuer die Zielrichtung. Leer = Camera.main.")]
        public Camera aimCamera;
        [Tooltip("Geschosse aus der Kamera heraus ausrichten (ueblicher Shooter-Trick)")]
        public bool convergeFromCamera = true;

        [Header("Feedback beim Abfeuern")]
        [Tooltip("Prefab, das an der Muendung erzeugt wird (Muendungsfeuer)")]
        public GameObject muzzleFlashPrefab;
        public float muzzleFlashLifetime = 0.1f;

        private ActionFeedbackPlayer feedback;
        private static readonly RaycastHit[] jetRaycastBuffer = new RaycastHit[64];

        private void Awake()
        {
            feedback = GetComponent<ActionFeedbackPlayer>();
            InitializeSlots();
        }

        private void InitializeSlots()
        {
            if (weaponSlots == null) weaponSlots = System.Array.Empty<JetWeaponSlot>();

            foreach (var slot in weaponSlots)
            {
                if (slot == null) continue;
                slot.magazineSize = Mathf.Max(0, slot.magazineSize);
                slot.spareMagazines = Mathf.Max(0, slot.spareMagazines);
                slot.currentAmmo = slot.magazineSize;
                slot.spare = slot.spareMagazines;
                slot.muzzleIndex = 0;
            }
        }

        /// <summary> Vom JetController jeden Frame aufgerufen, solange geflogen wird. </summary>
        public void TickPilotInput()
        {
            foreach (var slot in weaponSlots)
            {
                if (slot == null || !slot.enabled) continue;
                TickSlot(slot);
            }
        }

        private void TickSlot(JetWeaponSlot slot)
        {
            if (slot.fireKey != KeyCode.None && Input.GetKeyDown(slot.fireKey) && slot.isRocket)
            {
                TryFire(slot);
                return;
            }

            // MG feuert, solange die Taste gehalten wird
            if (!slot.isRocket && slot.fireKey != KeyCode.None && Input.GetKey(slot.fireKey))
            {
                TryFire(slot);
            }

            if (Input.GetKeyDown(KeyCode.R)) TryReload(slot);
        }

        private float SecondsBetweenShots(JetWeaponSlot slot)
        {
            return 60f / Mathf.Max(1f, slot.roundsPerMinute);
        }

        public bool TryFire(JetWeaponSlot slot)
        {
            if (slot == null || slot.isReloading) return false;
            if (slot.isRocket && slot.currentAmmo <= 0) { ReloadOrDryFire(slot); return false; }

            // Raketen haben keine Kadenzbegrenzung ausser der Abklingzeit
            if (!slot.isRocket && Time.time - slot.lastFireTime < SecondsBetweenShots(slot)) return false;
            if (slot.currentAmmo <= 0) { ReloadOrDryFire(slot); return false; }

            slot.lastFireTime = Time.time;
            slot.currentAmmo--;

            Transform muzzle = slot.GetNextMuzzle();
            if (muzzle == null) muzzle = transform;

            if (slot.isRocket) FireRocket(slot, muzzle);
            else FireBullet(slot, muzzle);

            feedback?.Play(slot.isRocket ? slot.rocketFireAction : slot.fireAction);
            SpawnMuzzleFlash(muzzle);
            return true;
        }

        private void ReloadOrDryFire(JetWeaponSlot slot)
        {
            if (slot.spare > 0 && !slot.isReloading)
            {
                slot.isReloading = true;
                feedback?.Play(slot.reloadAction);
                StartCoroutine(ReloadAfterDelay(slot));
            }
            else
            {
                feedback?.Play(slot.emptyAction);
            }
        }

        private System.Collections.IEnumerator ReloadAfterDelay(JetWeaponSlot slot)
        {
            yield return new WaitForSeconds(slot.reloadSeconds);
            slot.currentAmmo = slot.magazineSize;
            slot.spare--;
            slot.isReloading = false;
        }

        private void TryReload(JetWeaponSlot slot)
        {
            if (slot == null || slot.isReloading) return;
            if (slot.currentAmmo >= slot.magazineSize) return;
            if (slot.spare <= 0) return;

            slot.isReloading = true;
            feedback?.Play(slot.reloadAction);
            StartCoroutine(ReloadAfterDelay(slot));
        }

        private Vector3 GetAimDirection(Transform muzzle)
        {
            Camera cam = aimCamera != null ? aimCamera : Camera.main;

            if (cam != null && convergeFromCamera)
            {
                // Vom Kamera-Zielpunkt zur Muendung: Geschoss fliegt dorthin, wo das Fadenkreuz zeigt
                Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
                Vector3 aimPoint = ray.origin + ray.direction * 500f;
                return (aimPoint - muzzle.position).normalized;
            }

            return muzzle.forward;
        }

        private void FireBullet(JetWeaponSlot slot, Transform muzzle)
        {
            int pellets = Mathf.Max(1, slot.pelletsPerShot);
            float spread = slot.spreadDegrees;
            float damage = slot.ammoType != null ? slot.ammoType.baseDamage * slot.damageMultiplier : 20f;

            for (int i = 0; i < pellets; i++)
            {
                Vector3 dir = GetAimDirection(muzzle);

                if (spread > 0f)
                {
                    dir = Quaternion.Euler(
                        Random.Range(-spread, spread),
                        Random.Range(-spread, spread),
                        0f) * dir;
                }

                Vector3 endPoint = muzzle.position + dir * slot.maxRange;

                if (RaycastIgnoringJet(muzzle.position, dir, slot.maxRange, out RaycastHit hit))
                {
                    endPoint = hit.point;

                    var damageable = hit.collider.GetComponentInParent<IDamageable>();
                    damageable?.ApplyDamage(damage, hit.point, dir);

                    if (slot.ammoType != null && slot.ammoType.impactVFXPrefab != null)
                    {
                        GameObject impact = Instantiate(slot.ammoType.impactVFXPrefab, hit.point, Quaternion.LookRotation(hit.normal));
                        if (slot.ammoType.impactVFXLifetime > 0f) Destroy(impact, slot.ammoType.impactVFXLifetime);
                    }
                }

                if (slot.ammoType != null && slot.ammoType.tracerPrefab != null)
                {
                    GameObject tracerObj = Instantiate(slot.ammoType.tracerPrefab, muzzle.position, Quaternion.identity);
                    var tracer = tracerObj.GetComponent<Tracer>();
                    if (tracer == null) tracer = tracerObj.AddComponent<Tracer>();
                    tracer.Init(muzzle.position, endPoint, slot.ammoType.muzzleVelocity);
                }
            }
        }

        private void FireRocket(JetWeaponSlot slot, Transform muzzle)
        {
            Vector3 direction = GetAimDirection(muzzle);

            if (slot.rocketPrefab != null)
            {
                GameObject rocket = Instantiate(slot.rocketPrefab, muzzle.position, Quaternion.LookRotation(direction));
                var behaviour = rocket.GetComponent<JetRocket>();
                if (behaviour == null) behaviour = rocket.AddComponent<JetRocket>();

                behaviour.Init(direction, slot.rocketSpeed, slot.explosionRadius, slot.explosionDamage,
                    slot.homingStrength, hittableLayers, gameObject);
                return;
            }

            // Kein Raketen-Prefab: direkt als Explosion am Einschlagpunkt auswerten
            Vector3 endPoint = muzzle.position + direction * slot.maxRange;
            if (RaycastIgnoringJet(muzzle.position, direction, slot.maxRange, out RaycastHit hit))
            {
                endPoint = hit.point;
            }
            Explode(endPoint, slot.explosionRadius, slot.explosionDamage);
        }

        private bool RaycastIgnoringJet(Vector3 origin, Vector3 direction, float maxDistance, out RaycastHit closestHit)
        {
            int hitCount = Physics.RaycastNonAlloc(origin, direction, jetRaycastBuffer, maxDistance, hittableLayers,
                QueryTriggerInteraction.Ignore);

            closestHit = default;
            float closestDistance = float.PositiveInfinity;
            bool foundHit = false;

            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit candidate = jetRaycastBuffer[i];
                if (candidate.collider == null || candidate.collider.transform.root == transform.root) continue;
                if (candidate.distance >= closestDistance) continue;

                closestDistance = candidate.distance;
                closestHit = candidate;
                foundHit = true;
            }

            return foundHit;
        }

        private void Explode(Vector3 position, float radius, float damage)
        {
            Collider[] hits = Physics.OverlapSphere(position, radius, hittableLayers);
            var processed = new System.Collections.Generic.HashSet<IDamageable>();

            foreach (var col in hits)
            {
                var damageable = col.GetComponentInParent<IDamageable>();
                if (damageable == null || !processed.Add(damageable)) continue;

                // Schaden faellt mit der Entfernung ab
                float distance = Vector3.Distance(position, col.ClosestPoint(position));
                float falloff = Mathf.Clamp01(1f - distance / Mathf.Max(0.01f, radius));
                damageable.ApplyDamage(damage * falloff, position, (col.transform.position - position).normalized);
            }
        }

        private void SpawnMuzzleFlash(Transform muzzle)
        {
            if (muzzleFlashPrefab == null || muzzle == null) return;
            GameObject flash = Instantiate(muzzleFlashPrefab, muzzle.position, muzzle.rotation, muzzle);
            if (muzzleFlashLifetime > 0f) Destroy(flash, muzzleFlashLifetime);
        }

        /// <summary> Gesamtmunition fuer das HUD. </summary>
        public int GetTotalAmmo()
        {
            int total = 0;
            foreach (var slot in weaponSlots)
            {
                if (slot == null) continue;
                total += slot.currentAmmo + slot.spare * slot.magazineSize;
            }
            return total;
        }
    }
}
