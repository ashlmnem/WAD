using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WAD.Player;
using WAD.Weapons.Attachments;
using WAD.Enemies;

namespace WAD.Weapons
{
    /// <summary>
    /// Steuert eine einzelne, aktuell ausgeruestete Waffe.
    /// Animation-Slots (siehe [Header("Animation")]): Fire_Hip, Fire_ADS,
    /// BoltCycle, Inspect, Reload_Empty, Reload_Partial, Idle - jeweils als
    /// Animator-Trigger/Parameter-Name konfigurierbar.
    /// </summary>
    public class WeaponController : MonoBehaviour, WAD.Inventory.IInteractable
    {
        [Header("Daten")]
        public WeaponSO weaponData;

        [Header("Referenzen")]
        public Transform muzzlePoint;
        public Camera playerCamera;
        public TarkovMovementController movementController;
        public WeaponAttachmentManager attachments; // optional
        public PlayerLean playerLean; // optional, fuer Blindfeuer

        [Header("Munition")]
        public Magazine loadedMagazine;
        public List<Magazine> reserveMagazines = new List<Magazine>();

        [Header("Layer")]
        public LayerMask hittableLayers = ~0;

        [Header("Audio")]
        public AudioSource audioSource;

        [Header("Animation")]
        public Animator animator;
        [Tooltip("Getriggert beim Schuss aus der H�fte (nicht zielend)")]
        public string fireHipAnimTrigger = "Fire_Hip";
        [Tooltip("Getriggert beim Schuss waehrend ADS (zielend)")]
        public string fireADSAnimTrigger = "Fire_ADS";
        public string boltCycleAnimTrigger = "BoltCycle";
        public string inspectAnimTrigger = "Inspect";
        [Tooltip("Getriggert wenn das Magazin VOR dem Nachladen leer war")]
        public string emptyReloadAnimTrigger = "Reload_Empty";
        [Tooltip("Getriggert wenn das Magazin VOR dem Nachladen noch Patronen hatte")]
        public string partialReloadAnimTrigger = "Reload_Partial";
        [Tooltip("Getriggert wenn die Waffe aktiv wird (ausgeruestet) - fuer eine saubere Idle-Pose")]
        public string idleAnimTrigger = "Idle";
        public string isAimingBoolParam = "IsAiming";

        [Header("Blindfeuer (Punkt 7)")]
        [Tooltip("Streuungs-Multiplikator beim Feuern waehrend des Lehnens ohne ADS ('um die Ecke schiessen, ohne zu zielen')")]
        public float blindFireSpreadMultiplier = 5f;

        [Header("Near-Miss-Erkennung (Punkt 8, fuer Gegner-Unterdrueckung)")]
        public float nearMissDetectionRadius = 0.6f;

        [Header("Modell-Korrektur")]
        [Tooltip("Falls das importierte Modell falsch herum zeigt (z.B. Lauf zeigt rueckwaerts): hier das sichtbare Modell-Kindobjekt zuweisen")]
        public Transform modelRoot;
        [Tooltip("Korrektur-Rotation in Grad, z.B. (0, 180, 0) wenn das Modell um 180� verdreht importiert wurde")]
        public Vector3 modelRotationOffsetEuler = Vector3.zero;

        // --- Zustand ---
        private float lastFireTime = -999f;
        private bool isReloading;
        private bool isChamberedRoundReady = true;
        private float currentRecoilAccumulated;
        private bool loggedMissingWeaponData;
        public bool isEquipped  { get; private set; } // true erst NACHDEM PlayerWeaponHolder.PickUpWeapon() sie ausgeruestet hat
        public bool IsAiming { get; private set; }

        /// <summary> Von PlayerWeaponHolder aufgerufen - verhindert, dass lose in der Welt liegende (noch nicht aufgehobene) Waffen auf Eingaben reagieren. </summary>
        public void SetEquipped(bool equipped) => isEquipped = equipped;

        public event System.Action<int, int> OnAmmoChanged;
        public event System.Action OnFired;
        public static event System.Action GlobalOnFired; // fuer Range-Statistik, unabhaengig von der aktiven Waffe

        public string InteractionPrompt => $"F - {weaponData?.displayName} aufheben";

        public void Interact(WAD.Inventory.PlayerInteraction interactor)
        {
            interactor.weaponHolder?.PickUpWeapon(this);
        }

        private void Awake()
        {
            if (modelRoot != null)
            {
                modelRoot.localRotation = Quaternion.Euler(modelRotationOffsetEuler);
            }

            if (attachments != null)
            {
                attachments.OnAttachmentsChanged += ApplyMagazineCapacityOverride;
            }
        }

        private void OnEnable()
        {
            // Wird auch beim Waffenwechsel (SetActive(true)) ausgeloest -
            // sorgt fuer eine saubere Idle-Pose statt eines haengenden Frames
            // der letzten Aktion der vorherigen Aktivierung.
            if (animator != null && !string.IsNullOrEmpty(idleAnimTrigger))
            {
                animator.SetTrigger(idleAnimTrigger);
            }
        }

        private void OnDestroy()
        {
            if (attachments != null)
            {
                attachments.OnAttachmentsChanged -= ApplyMagazineCapacityOverride;
            }
        }

        private void ApplyMagazineCapacityOverride()
        {
            if (attachments == null || loadedMagazine == null) return;

            int overrideCapacity = attachments.GetMagazineCapacityOverride();
            if (overrideCapacity > 0)
            {
                loadedMagazine.capacity = overrideCapacity;
            }
            else if (weaponData != null)
            {
                loadedMagazine.capacity = weaponData.magazineCapacity;
            }
        }

        private void Update()
        {
            if (!isEquipped) return; // noch nicht aufgehoben/ausgeruestet - keine Eingaben verarbeiten

            if (weaponData == null)
            {
                if (!loggedMissingWeaponData)
                {
                    Debug.LogWarning($"[WeaponController:{gameObject.name}] 'Weapon Data' ist nicht zugewiesen - Waffe ist funktionsunf�hig, bis das im Inspector nachgetragen wird.");
                    loggedMissingWeaponData = true;
                }
                return;
            }

            HandleAimInput();
            HandleFireInput();
            HandleReloadInput();
            HandleBoltActionInput();
            HandleInspectInput();
            RecoverRecoil();
        }

        private bool IsBlindFiring => playerLean != null && playerLean.IsLeaning && !IsAiming;

        private void HandleAimInput()
        {
            IsAiming = Input.GetMouseButton(1) && !isReloading;
            if (movementController != null)
            {
                movementController.isAiming = IsAiming;
            }

            if (animator != null && !string.IsNullOrEmpty(isAimingBoolParam))
            {
                animator.SetBool(isAimingBoolParam, IsAiming);
            }

            if (playerCamera != null)
            {
                float baseFOV = attachments != null && attachments.GetADSFOVOverride() > 0f
                    ? attachments.GetADSFOVOverride() : weaponData.adsFOV;
                float targetFOV = IsAiming ? baseFOV : 60f;

                float adsSpeedMult = attachments != null ? attachments.GetADSSpeedMultiplier() : 1f;
                playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, targetFOV,
                    Time.deltaTime * weaponData.adsTransitionSpeed * adsSpeedMult);
            }
        }

        private void HandleFireInput()
        {
            if (!Application.isFocused) return; // verhindert "haengende" Eingaben bei Editor-Fensterwechsel

            bool wantsToFire = weaponData.fireMode == FireMode.FullAuto
                ? Input.GetMouseButton(0)
                : Input.GetMouseButtonDown(0);

            if (!wantsToFire) return;
            Debug.Log($"[WeaponController:{gameObject.name}] Feuer-Input erkannt (isEquipped, wird an TryFire uebergeben).");
            TryFire();
        }

        private void HandleReloadInput()
        {
            if (Input.GetKeyDown(KeyCode.R) && !isReloading)
            {
                TryReload();
            }
        }

        private void HandleBoltActionInput()
        {
            if (weaponData.fireMode != FireMode.BoltAction) return;

            if (!isChamberedRoundReady && Input.GetKeyDown(KeyCode.B))
            {
                StartCoroutine(CycleBoltRoutine());
            }
        }

        private void HandleInspectInput()
        {
            if (Input.GetKeyDown(KeyCode.V) && animator != null && !string.IsNullOrEmpty(inspectAnimTrigger))
            {
                animator.SetTrigger(inspectAnimTrigger);
            }
        }

        public void TryFire()
        {
            if (isReloading) { Debug.Log($"[WeaponController:{gameObject.name}] TryFire abgebrochen: isReloading=true"); return; }
            if (weaponData.fireMode == FireMode.BoltAction && !isChamberedRoundReady) { Debug.Log($"[WeaponController:{gameObject.name}] TryFire abgebrochen: Verschluss nicht bereit"); return; }
            if (Time.time - lastFireTime < weaponData.SecondsBetweenShots) { Debug.Log($"[WeaponController:{gameObject.name}] TryFire abgebrochen: Feuerrate-Cooldown"); return; }

            if (loadedMagazine == null || loadedMagazine.IsEmpty)
            {
                Debug.Log($"[WeaponController:{gameObject.name}] TryFire: Magazin leer/null -> Dry Fire");
                PlayDryFireSound();
                return;
            }

            if (loadedMagazine.ammoType == null)
            {
                Debug.LogWarning($"[WeaponController:{gameObject.name}] Geladenes Magazin hat keinen 'Ammo Type' zugewiesen - Schuss abgebrochen. Bitte im Inspector das Magazin vollstaendig konfigurieren.");
                PlayDryFireSound();
                return;
            }

            Debug.Log($"[WeaponController:{gameObject.name}] TryFire erfolgreich - loese Schuss aus. loadedMagazine.currentRounds vor Schuss: {loadedMagazine.currentRounds}");

            loadedMagazine.TryConsumeRound();
            lastFireTime = Time.time;

            FireRaycast();
            ApplyRecoil();
            NotifyAmmoChanged();
            OnFired?.Invoke();
            Debug.Log($"[WeaponController:{gameObject.name}] OnFired-Event ausgeloest.");
            GlobalOnFired?.Invoke();

            if (weaponData.fireMode == FireMode.BoltAction)
            {
                isChamberedRoundReady = false;
            }
        }

        private void FireRaycast()
        {
            if (playerCamera == null) return;

            float spread = IsAiming ? weaponData.adsSpreadDegrees : weaponData.baseSpreadDegrees;
            spread += currentRecoilAccumulated * 0.3f;

            float attachmentSpreadMult = attachments != null ? attachments.GetSpreadMultiplier() : 1f;
            spread *= attachmentSpreadMult;

            if (IsBlindFiring) spread *= blindFireSpreadMultiplier;

            Vector3 direction = ApplySpread(playerCamera.transform.forward, spread);

            Debug.DrawRay(playerCamera.transform.position, direction * weaponData.maxRange, Color.red, 0.5f);

            Vector3 impactPoint = playerCamera.transform.position + direction * weaponData.maxRange;
            Collider primaryHitCollider = null;

            if (Physics.Raycast(playerCamera.transform.position, direction, out RaycastHit hit,
                weaponData.maxRange, hittableLayers))
            {
                impactPoint = hit.point;
                primaryHitCollider = hit.collider;
                float distance = hit.distance;
                float falloff = weaponData.damageFalloff.Evaluate(distance);
                float damage = loadedMagazine.ammoType.baseDamage * falloff;

                var damageable = hit.collider.GetComponentInParent<IDamageable>();
                damageable?.ApplyDamage(damage, hit.point, direction);

                if (loadedMagazine.ammoType.impactVFXPrefab != null)
                {
                    Object.Instantiate(loadedMagazine.ammoType.impactVFXPrefab, hit.point,
                        Quaternion.LookRotation(hit.normal));
                }
            }

            NotifyNearMisses(playerCamera.transform.position, direction, weaponData.maxRange, primaryHitCollider);

            if (weaponData.muzzleFlashPrefab != null && muzzlePoint != null)
            {
                GameObject flash = Instantiate(weaponData.muzzleFlashPrefab, muzzlePoint.position, muzzlePoint.rotation, muzzlePoint);
                Destroy(flash, 0.15f);
            }

            if (loadedMagazine.ammoType.tracerPrefab != null && muzzlePoint != null)
            {
                GameObject tracerObj = Instantiate(loadedMagazine.ammoType.tracerPrefab, muzzlePoint.position, Quaternion.identity);
                var tracer = tracerObj.GetComponent<Tracer>();
                if (tracer == null) tracer = tracerObj.AddComponent<Tracer>();
                tracer.Init(muzzlePoint.position, impactPoint, loadedMagazine.ammoType.muzzleVelocity);
            }

            if (audioSource != null && weaponData.fireSound != null)
            {
                audioSource.PlayOneShot(weaponData.fireSound);
            }

            // Fire_Hip vs Fire_ADS - je nach aktuellem Zielzustand
            string fireTrigger = IsAiming ? fireADSAnimTrigger : fireHipAnimTrigger;
            if (animator != null && !string.IsNullOrEmpty(fireTrigger))
            {
                animator.SetTrigger(fireTrigger);
            }
        }

        private void NotifyNearMisses(Vector3 origin, Vector3 direction, float maxRange, Collider primaryHitCollider)
        {
            RaycastHit[] hits = Physics.SphereCastAll(origin, nearMissDetectionRadius, direction, maxRange, hittableLayers);

            foreach (var h in hits)
            {
                if (primaryHitCollider != null && h.collider.transform.root == primaryHitCollider.transform.root) continue;

                var enemy = h.collider.GetComponentInParent<EnemyController>();
                if (enemy == null) continue;

                var suppression = enemy.GetComponent<EnemySuppressionController>();
                suppression?.NotifyNearMiss();
            }
        }

        private Vector3 ApplySpread(Vector3 forward, float spreadDegrees)
        {
            float x = Random.Range(-spreadDegrees, spreadDegrees);
            float y = Random.Range(-spreadDegrees, spreadDegrees);
            Quaternion spreadRotation = Quaternion.Euler(y, x, 0f);
            return spreadRotation * forward;
        }

        private void ApplyRecoil()
        {
            float mult = attachments != null ? attachments.GetRecoilMultiplier() : 1f;
            currentRecoilAccumulated += weaponData.recoilPerShot * mult;
        }

        private void RecoverRecoil()
        {
            currentRecoilAccumulated = Mathf.Max(0f,
                currentRecoilAccumulated - weaponData.recoilRecoverySpeed * Time.deltaTime);
        }

        public void TryReload()
        {
            Magazine bestReserve = FindBestReserveMagazine();
            if (bestReserve == null) return;

            StartCoroutine(ReloadRoutine(bestReserve));
        }

        private Magazine FindBestReserveMagazine()
        {
            Magazine best = null;
            foreach (var mag in reserveMagazines)
            {
                if (mag.IsEmpty) continue;
                if (best == null || mag.currentRounds > best.currentRounds) best = mag;
            }
            return best;
        }

        private IEnumerator ReloadRoutine(Magazine newMagazine)
        {
            isReloading = true;

            // VOR dem Austausch pruefen: war das alte Magazin leer oder teilvoll?
            // Bestimmt, welche der beiden Reload-Animationen gespielt wird.
            bool wasEmpty = loadedMagazine == null || loadedMagazine.IsEmpty;

            if (audioSource != null && weaponData.reloadSound != null)
            {
                audioSource.PlayOneShot(weaponData.reloadSound);
            }

            string reloadTrigger = wasEmpty ? emptyReloadAnimTrigger : partialReloadAnimTrigger;
            if (animator != null && !string.IsNullOrEmpty(reloadTrigger))
            {
                animator.SetTrigger(reloadTrigger);
            }

            yield return new WaitForSeconds(weaponData.reloadTimeSeconds);

            if (loadedMagazine != null && !loadedMagazine.IsEmpty)
            {
                reserveMagazines.Add(loadedMagazine);
            }

            reserveMagazines.Remove(newMagazine);
            loadedMagazine = newMagazine;

            if (weaponData.requiresChamberAfterReload)
            {
                isChamberedRoundReady = false;
            }

            isReloading = false;
            NotifyAmmoChanged();
        }

        private IEnumerator CycleBoltRoutine()
        {
            isReloading = true;

            if (animator != null && !string.IsNullOrEmpty(boltCycleAnimTrigger))
            {
                animator.SetTrigger(boltCycleAnimTrigger);
            }

            yield return new WaitForSeconds(weaponData.boltActionCycleTime);
            isChamberedRoundReady = true;
            isReloading = false;
        }

        private void NotifyAmmoChanged()
        {
            int reserveTotal = 0;
            foreach (var mag in reserveMagazines) reserveTotal += mag.currentRounds;
            int current = loadedMagazine != null ? loadedMagazine.currentRounds : 0;
            OnAmmoChanged?.Invoke(current, reserveTotal);
        }

        private void PlayDryFireSound()
        {
            if (audioSource != null && weaponData.dryFireSound != null)
            {
                audioSource.PlayOneShot(weaponData.dryFireSound);
            }
        }

        public float GetTotalWeightKg()
        {
            float total = weaponData != null ? weaponData.weightKg : 0f;
            if (loadedMagazine != null) total += loadedMagazine.GetWeightKg();
            foreach (var mag in reserveMagazines) total += mag.GetWeightKg();
            if (attachments != null) total += attachments.GetTotalWeightBonusKg();
            return total;
        }
    }

    public interface IDamageable
    {
        void ApplyDamage(float amount, Vector3 hitPoint, Vector3 hitDirection);
    }
}