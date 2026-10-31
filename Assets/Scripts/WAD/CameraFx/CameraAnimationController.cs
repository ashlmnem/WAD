using System.Collections.Generic;
using UnityEngine;
using WAD.Player;
using WAD.Weapons;
using WAD.Combat;

namespace WAD.CameraFx
{
    /// <summary>
    /// Spielt ein CameraAnimationProfile auf die Kamera. Legt sich auf dasselbe
    /// Objekt wie FirstPersonCameraLook und arbeitet ADDITIV darauf: die
    /// Maussteuerung bleibt unveraendert, die Schichten legen nur einen kleinen
    /// Versatz oben drauf.
    ///
    /// Alles ist ueber das Profil (ScriptableObject) steuerbar - kein Code noetig.
    /// </summary>
    [DefaultExecutionOrder(100)] // nach FirstPersonCameraLook
    public class CameraAnimationController : MonoBehaviour
    {
        [Header("Profil")]
        public CameraAnimationProfile profile;

        [Header("Referenzen (leer = automatisch gesucht)")]
        public TarkovMovementController movement;
        public FirstPersonCameraLook cameraLook;
        public PlayerWeaponHolder weaponHolder;
        public PlayerLean playerLean;
        public PlayerHealth playerHealth;

        [Header("Verhalten")]
        [Tooltip("Kamera-Animationen komplett ausschalten (z.B. im Jet oder in Cutscenes)")]
        public bool disabled = false;
        [Tooltip("Basis-Position der Kamera merken und Versatz relativ dazu anwenden")]
        public bool useRelativeOffsets = true;

        // --- Laufzeit-Gewichte der einzelnen Schichten (0..1) ---
        private float wIdle, wTurning, wWalk, wSprint, wCrouch, wReload, wLanding, wAds, wLowHealth;

        private Vector3 baseLocalPosition;
        private bool baseCaptured;
        private float phase;

        private Quaternion lastWorldRotation;
        private bool rotationInitialized;
        private float turnYawVelocity;
        private float turnPitchVelocity;

        private void Awake()
        {
            if (movement == null) movement = GetComponentInParent<TarkovMovementController>();
            if (cameraLook == null) cameraLook = GetComponent<FirstPersonCameraLook>();
            if (weaponHolder == null) weaponHolder = GetComponentInParent<PlayerWeaponHolder>();
            if (playerLean == null) playerLean = GetComponentInParent<PlayerLean>();
            if (playerHealth == null) playerHealth = GetComponentInParent<PlayerHealth>();

            phase = Random.value * 1000f;
        }

        private void OnEnable()
        {
            CaptureBase();
            SubscribeWeapon(true);
        }

        private void OnDisable()
        {
            SubscribeWeapon(false);
            RestoreBase();
        }

        private void CaptureBase()
        {
            baseLocalPosition = transform.localPosition;
            baseCaptured = true;
        }

        private void RestoreBase()
        {
            if (baseCaptured && useRelativeOffsets) transform.localPosition = baseLocalPosition;
        }

        private WeaponController subscribedWeapon;

        private void SubscribeWeapon(bool subscribe)
        {
            var active = weaponHolder != null ? weaponHolder.GetActiveWeapon() : null;
            if (active == subscribedWeapon) return;

            if (subscribedWeapon != null)
            {
                subscribedWeapon.OnFired -= HandleFired;
            }

            subscribedWeapon = active;

            if (subscribe && subscribedWeapon != null)
            {
                subscribedWeapon.OnFired += HandleFired;
            }
        }

        private void OnDestroy()
        {
            if (subscribedWeapon != null) subscribedWeapon.OnFired -= HandleFired;
        }

        private void HandleFired()
        {
            // Kurzer Rueckstoss-Impuls - Details macht WeaponRecoil, hier nur
            // ein kleiner zusaetzlicher Kamera-Stoss.
            landingImpulse = Mathf.Max(landingImpulse, 0.35f);
        }

        // --- Impulse ---
        private float landingImpulse;
        private float reloadTimer;
        private float reloadDuration;

        /// <summary> Von WeaponController beim Nachladen aufrufen (oder automatisch ueber das Ereignis). </summary>
        public void NotifyReloadStarted(float duration)
        {
            reloadDuration = Mathf.Max(0.1f, duration);
            reloadTimer = reloadDuration;
        }

        /// <summary> Von aussen fuer Sprunglandungen. </summary>
        public void NotifyLanded(float impactStrength01)
        {
            landingImpulse = Mathf.Clamp01(impactStrength01);
        }

        private void LateUpdate()
        {
            if (disabled || profile == null) return;

            TrackTurnVelocity();
            UpdateWeights();

            Vector3 posOffset = Vector3.zero;
            Vector3 rotOffset = Vector3.zero;

            AccumulateLayer(profile.idle, wIdle, ref posOffset, ref rotOffset, 0f);
            AccumulateLayer(profile.crouch, wCrouch, ref posOffset, ref rotOffset, 10f);
            AccumulateLayer(profile.walkBob, GetSpeedFactor() * wWalk, ref posOffset, ref rotOffset, 20f);
            AccumulateLayer(profile.sprintShake, GetSpeedFactor() * wSprint, ref posOffset, ref rotOffset, 30f);
            AccumulateLayer(profile.adsBreathHold, wAds, ref posOffset, ref rotOffset, 40f);
            AccumulateLayer(profile.lowHealthStagger, wLowHealth, ref posOffset, ref rotOffset, 50f);
            AccumulateLayer(profile.reload, wReload, ref posOffset, ref rotOffset, 60f);
            AccumulateLayer(profile.landingImpact, wLanding, ref posOffset, ref rotOffset, 70f);
            AccumulateTurnDrag(ref posOffset, ref rotOffset);

            posOffset *= profile.globalIntensity;
            rotOffset *= profile.globalIntensity;

            if (useRelativeOffsets)
            {
                transform.localPosition = baseLocalPosition + posOffset;
            }

            transform.localRotation = transform.localRotation * Quaternion.Euler(rotOffset);
        }

        private float GetSpeedFactor()
        {
            if (movement == null) return 0f;
            return Mathf.Clamp01(movement.CurrentSpeed / 5.2f);
        }

        /// <summary> Erkennt, wie schnell sich die Kamera dreht (fuer den Dreh-Schwung). </summary>
        private void TrackTurnVelocity()
        {
            Quaternion world = transform.rotation;

            if (!rotationInitialized)
            {
                lastWorldRotation = world;
                rotationInitialized = true;
                return;
            }

            Quaternion delta = world * Quaternion.Inverse(lastWorldRotation);
            Vector3 euler = delta.eulerAngles;
            turnYawVelocity = Mathf.DeltaAngle(0f, euler.y) / Mathf.Max(0.0001f, Time.deltaTime);
            turnPitchVelocity = Mathf.DeltaAngle(0f, euler.x) / Mathf.Max(0.0001f, Time.deltaTime);
            lastWorldRotation = world;
        }

        private void AccumulateLayer(CameraMotionLayer layer, float weight, ref Vector3 pos, ref Vector3 rot, float seed)
        {
            if (layer == null || !layer.enabled || weight <= 0.0001f) return;

            float t = (Time.time + (layer.randomPhase ? phase : 0f)) * layer.noiseSpeed;

            Vector3 wave;
            if (layer.useNoise)
            {
                wave = new Vector3(
                    Noise(t * layer.positionFrequency.x + seed),
                    Noise(t * layer.positionFrequency.y + seed + 11f),
                    Noise(t * layer.positionFrequency.z + seed + 23f));
            }
            else
            {
                wave = new Vector3(
                    Mathf.Sin(t * layer.positionFrequency.x * Mathf.PI * 2f + seed),
                    Mathf.Sin(t * layer.positionFrequency.y * Mathf.PI * 2f + seed + 11f),
                    Mathf.Sin(t * layer.positionFrequency.z * Mathf.PI * 2f + seed + 23f));
            }

            Vector3 rotWave;
            if (layer.useNoise)
            {
                rotWave = new Vector3(
                    Noise(t * layer.rotationFrequency.x + seed + 31f),
                    Noise(t * layer.rotationFrequency.y + seed + 47f),
                    Noise(t * layer.rotationFrequency.z + seed + 59f));
            }
            else
            {
                rotWave = new Vector3(
                    Mathf.Sin(t * layer.rotationFrequency.x * Mathf.PI * 2f + seed + 31f),
                    Mathf.Sin(t * layer.rotationFrequency.y * Mathf.PI * 2f + seed + 47f),
                    Mathf.Sin(t * layer.rotationFrequency.z * Mathf.PI * 2f + seed + 59f));
            }

            pos += Vector3.Scale(layer.positionAmplitude, wave) * weight;
            rot += Vector3.Scale(layer.rotationAmplitude, rotWave) * weight;
        }

        /// <summary> Der Dreh-Schwung haengt an der tatsaechlichen Drehgeschwindigkeit, nicht an der Zeit. </summary>
        private void AccumulateTurnDrag(ref Vector3 pos, ref Vector3 rot)
        {
            var layer = profile.turning;
            if (layer == null || !layer.enabled || wTurning <= 0.0001f) return;

            // Begrenzen, damit ein Ruck nicht die Kamera wegschleudert
            float yaw = Mathf.Clamp(turnYawVelocity, -400f, 400f) / 400f;
            float pitch = Mathf.Clamp(turnPitchVelocity, -400f, 400f) / 400f;

            Vector3 dir = new Vector3(pitch, -yaw, -yaw);

            // Traegheit: der Versatz laeuft der Drehung nach
            turnDrag += (dir - turnDrag) * Mathf.Clamp01(Time.deltaTime * 8f);

            pos += Vector3.Scale(layer.positionAmplitude, turnDrag) * wTurning;
            rot += Vector3.Scale(layer.rotationAmplitude, turnDrag) * wTurning;
        }

        private Vector3 turnDrag;

        private void UpdateWeights()
        {
            float dt = Time.deltaTime;

            bool moving = movement != null && movement.CurrentSpeed > 0.15f;
            bool sprinting = movement != null && movement.IsSprinting;
            bool crouching = movement != null && movement.IsCrouching;
            bool leaning = playerLean != null && playerLean.IsLeaning;

            bool aiming = weaponHolder != null && weaponHolder.GetActiveWeapon() != null && weaponHolder.GetActiveWeapon().IsAiming;

            // Idle: nur wenn nicht gelaufen wird
            wIdle = Move(wIdle, (!moving && !sprinting) ? 1f : 0f, profile.idle, dt);

            wTurning = Move(wTurning, Mathf.Clamp01(Mathf.Abs(turnYawVelocity) / 120f + Mathf.Abs(turnPitchVelocity) / 120f), profile.turning, dt);

            wWalk = Move(wWalk, (moving && !sprinting) ? 1f : 0f, profile.walkBob, dt);
            wSprint = Move(wSprint, sprinting ? 1f : 0f, profile.sprintShake, dt);
            wCrouch = Move(wCrouch, crouching ? 1f : 0f, profile.crouch, dt);

            // Nachladen laeuft ueber einen Timer
            if (reloadTimer > 0f)
            {
                reloadTimer -= dt;
                wReload = Mathf.Clamp01(reloadTimer / Mathf.Max(0.01f, reloadDuration * 0.5f));
            }
            else
            {
                wReload = Move(wReload, 0f, profile.reload, dt);
            }

            // Landung klingt schnell ab
            landingImpulse = Mathf.MoveTowards(landingImpulse, 0f, dt / Mathf.Max(0.01f, profile.landingImpact.fadeOut));
            wLanding = landingImpulse;

            // ADS beruhigt, Zielen fuegt eigenes Mikro-Zittern hinzu
            float damp = aiming ? (1f - profile.adsMotionDampening) : 1f;
            wIdle *= damp;
            wWalk *= damp;
            wSprint *= damp;
            wAds = Move(wAds, aiming ? 1f : 0f, profile.adsBreathHold, dt);

            // Verletzung
            float health = playerHealth != null ? playerHealth.HealthPercent01 : 1f;
            bool hurt = playerHealth != null && !playerHealth.IsDead && health <= profile.lowHealthThreshold;
            float hurtTarget = hurt ? Mathf.InverseLerp(profile.lowHealthThreshold, 0f, health) : 0f;
            wLowHealth = Move(wLowHealth, hurtTarget, profile.lowHealthStagger, dt);
        }

        private static float Move(float current, float target, CameraMotionLayer layer, float dt)
        {
            if (layer == null) return target;

            float rate = target > current
                ? (layer.fadeIn > 0f ? dt / layer.fadeIn : 1f)
                : (layer.fadeOut > 0f ? dt / layer.fadeOut : 1f);

            return Mathf.MoveTowards(current, target, rate);
        }

        /// <summary> Weiches Rauschen im Bereich -1..1 (Perlin, auf 0 zentriert). </summary>
        private static float Noise(float t)
        {
            return Mathf.PerlinNoise(t, 0.37f) * 2f - 1f;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (!Application.isPlaying && profile == null)
            {
                Debug.LogWarning($"[CameraAnimationController:{name}] Kein Profil zugewiesen - es wird keine Kamera-Animation abgespielt.");
            }
        }
#endif
    }
}
