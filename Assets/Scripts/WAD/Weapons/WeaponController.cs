using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WAD.Player;
using WAD.Weapons.Attachments;
using WAD.Enemies;
using WAD.Inventory.UI;

namespace WAD.Weapons
{
    /// <summary>
    /// Steuert eine einzelne, aktuell ausgeruestete Waffe.
    /// </summary>
    public class WeaponController : MonoBehaviour, WAD.Inventory.IInteractable
    {
        [Header("Daten")]
        public WeaponSO weaponData;

        [Header("Referenzen")]
        public Transform muzzlePoint;
        public Camera playerCamera;
        public TarkovMovementController movementController;
        public FirstPersonCameraLook cameraLook; // fuer Reload-Kamera-Animation
        public WeaponAttachmentManager attachments;
        public PlayerLean playerLean;
        public WAD.Weapons.Casings.ShellCasingEjector casingEjector;

        [Header("Munition")]
        public Magazine loadedMagazine;
        public List<Magazine> reserveMagazines = new List<Magazine>();

        [Header("Layer")]
        public LayerMask hittableLayers = ~0;

        [Header("Audio")]
        public AudioSource audioSource;

        [Header("Animation")]
        public Animator animator;
        public string fireHipAnimTrigger = "Fire_Hip";
        public string fireADSAnimTrigger = "Fire_ADS";
        public string boltCycleAnimTrigger = "BoltCycle";
        public string inspectAnimTrigger = "Inspect";
        public string emptyReloadAnimTrigger = "Reload_Empty";
        public string partialReloadAnimTrigger = "Reload_Partial";
        public string idleAnimTrigger = "Idle";
        public string isAimingBoolParam = "IsAiming";

        [Header("Blindfeuer")]
        public float blindFireSpreadMultiplier = 5f;

        [Header("Near-Miss-Erkennung")]
        public float nearMissDetectionRadius = 0.6f;

        [Header("Modell-Korrektur")]
        public Transform modelRoot;
        public Vector3 modelRotationOffsetEuler = Vector3.zero;

        [Header("ADS-Ausrichtung")]
        [Tooltip("Standard-Zielpunkt (Kimme/Korn), falls keine Optik mit eigenem 'ADS_AimPoint' montiert ist - Kind-Transform am Waffen-Root. WICHTIG: bei JEDER Waffe zuweisen, nicht nur bei Waffen mit Optik!")]
        public Transform defaultIronSightAimPoint;
        public float adsPositionLerpSpeed = 10f;
        [Tooltip("Richtet beim Zielen die Optik seitlich exakt an der Bildmitte aus: der seitliche Anteil des 'ADS_AimPoint' wird verworfen " +
                 "und durch die Mittellinie des Optik-Modells ersetzt. Behebt, dass die Waffe seitlich versetzt wirkt, wenn der Aim Point " +
                 "im Optik-Prefab um Millimeter neben der Mitte sitzt. Nur aktivieren, wenn der Optik-Montagepunkt der Waffe auf ihrer Mittellinie liegt.")]
        public bool autoCenterOpticLaterally = false;
        [Tooltip("Dreht die Waffe beim Zielen exakt in Blickrichtung der Kamera (Visierachse = Kamera-Vorwaerts, Oben = Kamera-Oben). " +
                 "Noetig fuer Waffen OHNE ADS-Animation: Der WeaponSocket traegt eine Huefthaltung (seitlich/nach oben gedreht und um ca. 25 Grad " +
                 "gerollt), die beim Zielen sonst bestehen bleibt - die Visierung sitzt dann nie genau vor der Kamera. " +
                 "Waffen MIT Animator (G3, AKM, M27) lassen das aus, dort uebernimmt die Animation das Ausrichten.")]
        public bool alignRotationOnADS = false;
        [Tooltip("Achse des Modells (im Modell-Root-Raum), die beim Zielen nach VORN zeigen soll (= Visierlinie/Laufrichtung). Mesh-Frame der Waffen: +Y.")]
        public Vector3 sightForwardAxis = Vector3.up;
        [Tooltip("Achse des Modells (im Modell-Root-Raum), die beim Zielen nach OBEN zeigen soll. Mesh-Frame der Waffen: +Z.")]
        public Vector3 sightUpAxis = Vector3.forward;
        [Tooltip("Sichtfeld (FOV) der Kamera, wenn NICHT gezielt wird. Vorher fest auf 60 verdrahtet.")]
        public float hipFOV = 60f;

        // --- Zustand ---
        private float lastFireTime = -999f;
        private float lastDryFireTime = -999f;
        [Tooltip("Mindestabstand zwischen zwei Klick-Sounds bei leerem Magazin")]
        public float dryFireCooldown = 0.25f;
        private bool isReloading;
        private bool isChamberedRoundReady = true;
        private float currentRecoilAccumulated;
        private bool loggedMissingWeaponData;
        public bool isEquipped { get; private set; }
        public bool IsAiming { get; private set; }
        public bool IsReloadingPublic => isReloading;

        /// <summary> Gesamtanzahl Patronen in allen Reserve-Magazinen (fuer HUD/Inspect). </summary>
        public int ReserveRoundsTotal
        {
            get
            {
                int total = 0;
                foreach (var mag in reserveMagazines) total += mag.currentRounds;
                return total;
            }
        }

        /// <summary> Effektive Nachladedauer inkl. Magazin-Multiplikator (Trommelmagazine sind langsamer). </summary>
        public float EffectiveReloadSeconds
        {
            get
            {
                if (weaponData == null) return 0f;

                float mult = loadedMagazine != null && loadedMagazine.magazineType != null
                    ? loadedMagazine.magazineType.reloadTimeMultiplier
                    : 1f;

                return weaponData.reloadTimeSeconds * Mathf.Max(0.1f, mult);
            }
        }

        public event System.Action<int, int> OnAmmoChanged;
        public event System.Action OnFired;
        public static event System.Action GlobalOnFired;

        public string InteractionPrompt => $"F - {weaponData?.displayName} aufheben";

        public void Interact(WAD.Inventory.PlayerInteraction interactor)
        {
            interactor.weaponHolder?.PickUpWeapon(this);
        }

        /// <summary> Von PlayerWeaponHolder aufgerufen - verhindert, dass lose in der Welt liegende Waffen auf Eingaben reagieren. </summary>
        public void SetEquipped(bool equipped) => isEquipped = equipped;

        private void Awake()
        {
            if (modelRoot != null)
            {
                modelRoot.localRotation = Quaternion.Euler(modelRotationOffsetEuler);
            }

            EnsureAudioSource();

            if (attachments != null)
            {
                attachments.OnAttachmentsChanged += ApplyMagazineTypeOverride;
            }
        }

        /// <summary>
        /// Stellt sicher, dass eine AudioSource zum Abspielen der Schuss-/
        /// Nachlade-/Klick-Sounds existiert. Wichtig fuer Waffen, deren Prefab
        /// gar keine AudioSource mitbringt (z.B. M1911, XM607) - die waren
        /// sonst komplett stumm, obwohl WeaponSO die Clips zuweist.
        /// </summary>
        private void EnsureAudioSource()
        {
            if (audioSource == null) audioSource = GetComponent<AudioSource>();

            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                // PlayOnAwake aus: sonst warnt Unity, weil kein Clip gesetzt ist.
                audioSource.playOnAwake = false;
                audioSource.spatialBlend = 0f; // Waffen-Sounds sind am Spieler ohnehin 2D-nah
            }
        }

        private void OnEnable()
        {
            if (animator != null && !string.IsNullOrEmpty(idleAnimTrigger))
            {
                animator.SetTrigger(idleAnimTrigger);
            }
        }

        private void OnDisable()
        {
            // Beim Waffenwechsel wird das GameObject per SetActive(false)
            // deaktiviert - laufende Coroutinen werden dabei endgueltig beendet
            // und laufen beim Wiederaktivieren NICHT weiter. Ohne dieses
            // Aufraeumen blieb 'isReloading' fuer immer true und die Waffe
            // konnte danach nie wieder schiessen, nachladen oder zielen.
            StopAllCoroutines();
            isReloading = false;
        }

        private void OnDestroy()
        {
            if (attachments != null)
            {
                attachments.OnAttachmentsChanged -= ApplyMagazineTypeOverride;
            }
        }

        private void ApplyMagazineTypeOverride()
        {
            if (attachments == null || loadedMagazine == null) return;

            MagazineTypeSO overrideType = attachments.GetMagazineTypeOverride();
            MagazineTypeSO targetType = overrideType != null ? overrideType : weaponData?.DefaultMagazineType;

            if (targetType != null && loadedMagazine.magazineType != targetType)
            {
                int oldRounds = loadedMagazine.currentRounds;
                loadedMagazine.magazineType = targetType;
                loadedMagazine.currentRounds = Mathf.Min(oldRounds, targetType.baseCapacity);
                NotifyAmmoChanged();
            }
        }

        private void Update()
        {
            if (!isEquipped) return;

            if (weaponData == null)
            {
                if (!loggedMissingWeaponData)
                {
                    Debug.LogWarning($"[WeaponController:{gameObject.name}] 'Weapon Data' ist nicht zugewiesen.");
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

        /// <summary>
        /// LATE Update statt Update: laeuft garantiert NACH der Animator-
        /// Auswertung dieses Frames. Dadurch "gewinnt" die ADS-Ausrichtung
        /// immer, egal was der aktuell abgespielte Animation-Clip an
        /// Positions-/Rotationswerten gesetzt hat - kein Kampf mehr zwischen
        /// Code und Animation, ohne dass die Hierarchie umgebaut werden muss.
        /// </summary>
        private void LateUpdate()
        {
            if (!isEquipped || weaponData == null) return;
            HandleADSPositioning();
        }

        private bool IsBlindFiring => playerLean != null && playerLean.IsLeaning && !IsAiming;

        private void HandleAimInput()
        {
            IsAiming = Input.GetMouseButton(1) && !isReloading && !InventoryUIController.IsOpen;
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
                float targetFOV = IsAiming ? baseFOV : hipFOV;

                float adsSpeedMult = attachments != null ? attachments.GetADSSpeedMultiplier() : 1f;
                playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, targetFOV,
                    Time.deltaTime * weaponData.adsTransitionSpeed * adsSpeedMult);
            }
        }

        private void HandleADSPositioning()
        {
            if (playerCamera == null || transform.parent == null) return;

            Transform aimPoint = attachments != null ? attachments.GetOpticAimPoint() : null;
            Transform opticCenterline = null;

            if (aimPoint != null && autoCenterOpticLaterally)
            {
                opticCenterline = attachments.GetOpticVisualRoot();
            }

            if (aimPoint == null) aimPoint = defaultIronSightAimPoint;

            Vector3 targetLocalPosition = Vector3.zero;
            Quaternion targetLocalRotation = Quaternion.identity;
            bool alignRotation = alignRotationOnADS;

            if (IsAiming && aimPoint != null)
            {
                Vector3 originalLocalPos = transform.localPosition;
                Quaternion originalLocalRot = transform.localRotation;

                // Zuerst die Zielrotation bestimmen und fuer die Positionsberechnung temporaer anwenden -
                // die Position des Aim Points haengt von der Waffenrotation ab.
                if (alignRotation)
                {
                    targetLocalRotation = ComputeSightAlignedLocalRotation();
                    transform.localRotation = targetLocalRotation;
                }

                transform.localPosition = Vector3.zero;

                Vector3 aimWorldPosition = aimPoint.position;

                if (opticCenterline != null)
                {
                    // Nur die SEITLICHE Komponente korrigieren (Tiefe = Augenabstand und Hoehe bleiben wie eingestellt).
                    Vector3 right = transform.right;
                    aimWorldPosition += right * Vector3.Dot(opticCenterline.position - aimWorldPosition, right);
                }

                Vector3 worldOffsetNeeded = playerCamera.transform.position - aimWorldPosition;
                Vector3 localOffsetNeeded = transform.parent.InverseTransformVector(worldOffsetNeeded);

                transform.localPosition = originalLocalPos;
                transform.localRotation = originalLocalRot;
                targetLocalPosition = localOffsetNeeded;
            }

            float lerp = adsPositionLerpSpeed * Time.deltaTime;

            transform.localPosition = Vector3.Lerp(transform.localPosition, targetLocalPosition, lerp);

            if (alignRotation)
            {
                transform.localRotation = Quaternion.Slerp(transform.localRotation, targetLocalRotation, lerp);
            }
        }

        /// <summary>
        /// Lokale Rotation (relativ zum WeaponSocket), bei der Visierachse und Oben-Achse der Waffe
        /// exakt mit Vorwaerts/Oben der Kamera uebereinstimmen.
        /// </summary>
        private Quaternion ComputeSightAlignedLocalRotation()
        {
            Transform frame = modelRoot != null ? modelRoot : transform;

            Vector3 forward = frame.TransformDirection(sightForwardAxis);
            Vector3 up = frame.TransformDirection(sightUpAxis);

            if (forward.sqrMagnitude < 1e-6f || up.sqrMagnitude < 1e-6f || Mathf.Abs(Vector3.Dot(forward.normalized, up.normalized)) > 0.999f)
            {
                return transform.localRotation; // ungueltige Achsen-Einstellung: nichts veraendern
            }

            Quaternion modelFrame = Quaternion.LookRotation(forward, up);
            Quaternion cameraFrame = Quaternion.LookRotation(playerCamera.transform.forward, playerCamera.transform.up);

            Quaternion targetWorld = cameraFrame * Quaternion.Inverse(modelFrame) * transform.rotation;
            return Quaternion.Inverse(transform.parent.rotation) * targetWorld;
        }

        private void HandleFireInput()
        {
            if (!Application.isFocused) return;
            if (InventoryUIController.IsOpen) return; // nicht schiessen, waehrend das Inventar offen ist

            bool wantsToFire = weaponData.fireMode == FireMode.FullAuto
                ? Input.GetMouseButton(0)
                : Input.GetMouseButtonDown(0);

            if (!wantsToFire) return;
            TryFire();
        }

        private void HandleReloadInput()
        {
            if (InventoryUIController.IsOpen) return;

            if (Input.GetKeyDown(KeyCode.R) && !isReloading)
            {
                TryReload();
            }
        }

        private void HandleBoltActionInput()
        {
            if (weaponData.fireMode != FireMode.BoltAction) return;

            if (!isReloading && !isChamberedRoundReady && Input.GetKeyDown(KeyCode.B))
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
            if (isReloading) return;
            if (weaponData.fireMode == FireMode.BoltAction && !isChamberedRoundReady) return;
            if (Time.time - lastFireTime < weaponData.SecondsBetweenShots) return;

            if (loadedMagazine == null || loadedMagazine.IsEmpty)
            {
                PlayDryFireSound();
                return;
            }

            if (loadedMagazine.ammoType == null)
            {
                Debug.LogWarning($"[WeaponController:{gameObject.name}] Geladenes Magazin hat keinen 'Ammo Type' zugewiesen.");
                PlayDryFireSound();
                return;
            }

            loadedMagazine.TryConsumeRound();
            lastFireTime = Time.time;

            FireRaycast();
            ApplyRecoil();
            NotifyAmmoChanged();
            OnFired?.Invoke();
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
                weaponData.maxRange, hittableLayers, QueryTriggerInteraction.Ignore))
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
                    GameObject impactVfx = Instantiate(loadedMagazine.ammoType.impactVFXPrefab, hit.point,
                        Quaternion.LookRotation(hit.normal));
                    Destroy(impactVfx, Mathf.Max(0.5f, loadedMagazine.ammoType.impactVFXLifetime));
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

            if (casingEjector != null && loadedMagazine.ammoType.casingPrefab != null)
            {
                casingEjector.EjectCasing(loadedMagazine.ammoType.casingPrefab);
            }

            string fireTrigger = IsAiming ? fireADSAnimTrigger : fireHipAnimTrigger;
            if (animator != null && !string.IsNullOrEmpty(fireTrigger))
            {
                animator.SetTrigger(fireTrigger);
            }
        }

        // Wiederverwendete Puffer: SphereCastAll alloziert pro Schuss ein neues Array (GC-Spikes bei Vollauto).
        private static readonly RaycastHit[] nearMissBuffer = new RaycastHit[64];
        private static readonly HashSet<EnemyController> nearMissEnemies = new HashSet<EnemyController>();

        private void NotifyNearMisses(Vector3 origin, Vector3 direction, float maxRange, Collider primaryHitCollider)
        {
            int count = Physics.SphereCastNonAlloc(origin, nearMissDetectionRadius, direction, nearMissBuffer,
                maxRange, hittableLayers, QueryTriggerInteraction.Ignore);

            nearMissEnemies.Clear();

            for (int i = 0; i < count; i++)
            {
                Collider col = nearMissBuffer[i].collider;
                if (col == null) continue;
                if (primaryHitCollider != null && col.transform.root == primaryHitCollider.transform.root) continue;

                var enemy = col.GetComponentInParent<EnemyController>();
                if (enemy == null || !nearMissEnemies.Add(enemy)) continue; // pro Gegner nur EINE Meldung (er hat mehrere Hitboxen)

                var suppression = enemy.GetComponent<EnemySuppressionController>();
                if (suppression != null) suppression.NotifyNearMiss();
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
            if (isReloading || weaponData == null) return;

            Magazine bestReserve = FindBestReserveMagazine();
            if (bestReserve == null) return;

            // Kein Tausch gegen ein Magazin, das nicht voller ist als das eingelegte
            if (loadedMagazine != null && bestReserve.currentRounds <= loadedMagazine.currentRounds) return;

            StartCoroutine(ReloadRoutine(bestReserve));
        }

        private Magazine FindBestReserveMagazine()
        {
            Magazine best = null;
            foreach (var mag in reserveMagazines)
            {
                if (mag.IsEmpty) continue;
                if (weaponData != null && !weaponData.AcceptsMagazine(mag.magazineType)) continue;
                if (best == null || mag.currentRounds > best.currentRounds) best = mag;
            }
            return best;
        }

        private IEnumerator ReloadRoutine(Magazine newMagazine)
        {
            isReloading = true;

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

            // Dauer an die echte Nachladezeit koppeln (inkl. Magazin-Multiplikator,
            // z.B. Trommelmagazine), damit Kamera-Bewegung und Magazinwechsel
            // synchron enden.
            float reloadSeconds = EffectiveReloadSeconds;

            cameraLook?.TriggerReloadAnimation(reloadSeconds);

            yield return new WaitForSeconds(reloadSeconds);

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

        /// <summary> Oeffentlich, damit z.B. WorldMagazinePickup das HUD nach dem Aufsammeln aktualisieren kann. </summary>
        public void NotifyAmmoChanged()
        {
            int reserveTotal = 0;
            foreach (var mag in reserveMagazines) reserveTotal += mag.currentRounds;
            int current = loadedMagazine != null ? loadedMagazine.currentRounds : 0;
            OnAmmoChanged?.Invoke(current, reserveTotal);
        }

        private void PlayDryFireSound()
        {
            // Ohne Cooldown wurde bei Vollautomatik mit leerem Magazin JEDEN
            // Frame ein weiterer Klick-Sound gestartet (Sound-Spam).
            if (Time.time - lastDryFireTime < dryFireCooldown) return;
            lastDryFireTime = Time.time;

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
