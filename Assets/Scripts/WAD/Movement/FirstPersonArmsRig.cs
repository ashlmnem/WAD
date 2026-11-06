using UnityEngine;
using WAD.Weapons;

namespace WAD.Player
{
    /// <summary>
    /// Haelt das Waffenmodell in erster Person und blendet Arme/Haende ein.
    ///
    /// WICHTIG - zwei Wege, Haende ins Spiel zu bringen:
    ///
    ///  A) HAENDE AM WAFFENMODELL (einfachster Weg, empfohlen)
    ///     Die Arme/Haende werden als Kinder des Waffen-Prefabs modelliert und
    ///     ueber die Griff-Slots unten feinjustiert. Da sie am Waffen-Root
    ///     haengen, bewegen sie sich automatisch mit ADS, Nachladen und Recoil
    ///     mit - genau wie in den meisten Shootern.
    ///
    ///  B) SEPARATE ARM-PREFABS
    ///     'armsPrefab' zuweisen. Das Prefab wird unter 'armsParent' erzeugt und
    ///     ueber die gleichen Griff-Slots an die Waffe geheftet.
    ///
    /// Alle Werte (Positionen, Winkel, Wippen, Schwanken) sind im Inspector
    /// einstellbar - kein Code noetig.
    /// </summary>
    public class FirstPersonArmsRig : MonoBehaviour
    {
        [Header("Quellen")]
        [Tooltip("Waffenhalter, dessen aktive Waffe gehalten wird. Leer = automatisch gesucht.")]
        public PlayerWeaponHolder weaponHolder;
        [Tooltip("Optionales separates Arm-Prefab (Weg B). Leer = Arme sind Teil des Waffenmodells.")]
        public GameObject armsPrefab;
        [Tooltip("Erzeugt einen einfachen, austauschbaren Blockout, bis ein echtes Arm-Prefab zugewiesen wird.")]
        public bool createPlaceholderWhenNoPrefab = true;
        [Tooltip("Elternobjekt fuer das Arm-Prefab. Leer = dieses Transform.")]
        public Transform armsParent;

        [Header("Arm-Blockout")]
        public Vector3 rightForearmPosition = new Vector3(1.35f, -3.2f, -0.15f);
        public Vector3 rightForearmScale = new Vector3(0.55f, 3.4f, 0.55f);
        public Vector3 rightHandPosition = new Vector3(1.35f, -0.25f, 0.2f);
        public Vector3 leftForearmPosition = new Vector3(-1.35f, 1.1f, -0.15f);
        public Vector3 leftForearmScale = new Vector3(0.55f, 3.1f, 0.55f);
        public Vector3 leftHandPosition = new Vector3(-1.35f, 2.5f, 0.2f);
        public Color sleeveColor = new Color(0.23f, 0.26f, 0.19f, 1f);
        public Color gloveColor = new Color(0.18f, 0.17f, 0.13f, 1f);

        [Header("Referenzen (bei Weg A im Waffen-Prefab zuweisen)")]
        [Tooltip("Transform im Waffen-Prefab, an dem die rechte Hand sitzt")]
        public Transform rightHandSlot;
        [Tooltip("Transform im Waffen-Prefab, an dem die linke Hand sitzt")]
        public Transform leftHandSlot;

        [Header("Griff-Feinjustierung (Weg B)")]
        public Vector3 rightHandLocalPosition = Vector3.zero;
        public Vector3 rightHandLocalEuler = Vector3.zero;
        public Vector3 leftHandLocalPosition = Vector3.zero;
        public Vector3 leftHandLocalEuler = Vector3.zero;

        [Header("Wippen (Gehen)")]
        [Tooltip("Waffen-Wippen beim Gehen - Amplitude in Metern")]
        public Vector3 walkBobPosition = new Vector3(0.012f, 0.018f, 0f);
        [Tooltip("Frequenz des Wippens pro Achse")]
        public Vector3 walkBobFrequency = new Vector3(0.9f, 1.8f, 0f);
        [Tooltip("Zusaetzliches Kippen beim Gehen in Grad")]
        public Vector3 walkBobRotation = new Vector3(0.6f, 0.8f, 1.2f);
        [Tooltip("Wipp-Geschwindigkeit, ab der die volle Amplitude erreicht ist (m/s)")]
        public float speedForFullBob = 4.5f;

        [Header("Schwanken (Mausbewegung)")]
        [Tooltip("Wie stark die Waffe der Mausdrehung nachlaeuft")]
        public float swayAmount = 0.02f;
        public float swayRotationAmount = 2.5f;
        [Tooltip("Wie schnell das Schwanken zurueckfedert")]
        public float swayRecovery = 6f;
        [Tooltip("Maximaler Schwank-Weg in Metern")]
        public float swayMaxDistance = 0.05f;

        [Header("Idle-Atmung")]
        public Vector3 idleBreathPosition = new Vector3(0.002f, 0.003f, 0f);
        public float idleBreathSpeed = 0.6f;

        [Header("Sprint")]
        [Tooltip("Waffe beim Sprinten absenken/kippen")]
        public Vector3 sprintPosition = new Vector3(0.02f, -0.05f, -0.03f);
        public Vector3 sprintEuler = new Vector3(-12f, 18f, 8f);
        public float sprintTransitionSpeed = 6f;

        [Header("Zielen (ADS)")]
        [Tooltip("Beim Zielen werden Wippen/Schwanken beruhigt")]
        [Range(0f, 1f)] public float adsDampening = 0.7f;

        [Header("Verhalten")]
        [Tooltip("Waffe waehrend des Nachladens absenken")]
        public bool lowerWhileReloading = false;
        public Vector3 reloadPosition = new Vector3(0f, -0.06f, -0.02f);
        public Vector3 reloadEuler = new Vector3(-10f, 0f, 0f);

        private FirstPersonCameraLook cameraLook;
        private TarkovMovementController movement;
        private WeaponController activeWeapon;
        private GameObject armsInstance;
        private Transform armsRoot;

        private Vector2 swayOffset;
        private Vector3 currentSprintBlend;
        private Vector3 bobPhase;
        private float phase;
        private MaterialPropertyBlock placeholderProperties;

        private void Awake()
        {
            if (weaponHolder == null) weaponHolder = GetComponentInParent<PlayerWeaponHolder>();
            cameraLook = GetComponentInParent<FirstPersonCameraLook>();
            movement = GetComponentInParent<TarkovMovementController>();
            placeholderProperties = new MaterialPropertyBlock();
            if (armsParent == null) armsParent = transform;

            phase = Random.value * 100f;

            if (armsPrefab != null)
            {
                armsInstance = Instantiate(armsPrefab, armsParent);
                armsRoot = armsInstance.transform;
                AttachArmsToWeapon();
            }
            else if (createPlaceholderWhenNoPrefab)
            {
                CreatePlaceholderArms();
            }
        }

        private void CreatePlaceholderArms()
        {
            armsInstance = new GameObject("ArmsPlaceholder");
            armsRoot = armsInstance.transform;
            armsRoot.SetParent(armsParent, false);

            CreatePlaceholderLimb("RightForearm", rightForearmPosition, rightForearmScale, sleeveColor);
            CreatePlaceholderLimb("RightHand", rightHandPosition, Vector3.one * 0.5f, gloveColor);
            CreatePlaceholderLimb("LeftForearm", leftForearmPosition, leftForearmScale, sleeveColor);
            CreatePlaceholderLimb("LeftHand", leftHandPosition, Vector3.one * 0.5f, gloveColor);
        }

        private void CreatePlaceholderLimb(string limbName, Vector3 localPosition, Vector3 localScale, Color color)
        {
            GameObject limb = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            limb.name = limbName;
            limb.layer = 2;
            limb.transform.SetParent(armsRoot, false);
            limb.transform.localPosition = localPosition;
            limb.transform.localScale = localScale;

            var collider = limb.GetComponent<Collider>();
            if (collider != null) collider.enabled = false;

            var renderer = limb.GetComponent<Renderer>();
            if (renderer == null) return;

            placeholderProperties.SetColor("_BaseColor", color);
            placeholderProperties.SetColor("_Color", color);
            renderer.SetPropertyBlock(placeholderProperties);
        }

        private void LateUpdate()
        {
            var weapon = weaponHolder != null ? weaponHolder.GetActiveWeapon() : null;

            if (weapon != activeWeapon)
            {
                activeWeapon = weapon;
                if (armsInstance != null) AttachArmsToWeapon();
            }

            if (armsRoot == null && rightHandSlot == null && leftHandSlot == null) return;

            UpdateSway();
            UpdatePose();
        }

        /// <summary> Heftet die separaten Arme an die aktuell gehaltene Waffe. </summary>
        private void AttachArmsToWeapon()
        {
            if (armsRoot == null || activeWeapon == null) return;

            // Die Arme folgen der Waffe, damit alles zusammen bewegt wird.
            armsRoot.SetParent(activeWeapon.transform, worldPositionStays: false);
            armsRoot.localPosition = Vector3.zero;
            armsRoot.localRotation = Quaternion.identity;
        }

        private void UpdateSway()
        {
            float mouseX = Input.GetAxisRaw("Mouse X");
            float mouseY = Input.GetAxisRaw("Mouse Y");

            Vector2 target = new Vector2(-mouseX, -mouseY) * swayAmount * 0.1f;

            if (target.magnitude > swayMaxDistance) target = target.normalized * swayMaxDistance;

            swayOffset = Vector2.Lerp(swayOffset, target, Time.deltaTime * swayRecovery);
        }

        private void UpdatePose()
        {
            bool aiming = activeWeapon != null && activeWeapon.IsAiming;
            bool sprinting = movement != null && movement.IsSprinting;
            bool reloading = activeWeapon != null && activeWeapon.IsReloadingPublic;

            float damp = aiming ? (1f - adsDampening) : 1f;
            float speed = movement != null ? movement.CurrentSpeed : 0f;
            float speedFactor = Mathf.Clamp01(speed / Mathf.Max(0.01f, speedForFullBob));

            // --- Wippen ---
            Vector3 bob = Vector3.zero;
            Vector3 bobRot = Vector3.zero;

            if (speedFactor > 0.01f && !aiming)
            {
                float t = (Time.time + phase) * Mathf.PI * 2f;
                bob = new Vector3(
                    Mathf.Sin(t * walkBobFrequency.x) * walkBobPosition.x,
                    Mathf.Abs(Mathf.Sin(t * walkBobFrequency.y)) * walkBobPosition.y,
                    Mathf.Sin(t * walkBobFrequency.z) * walkBobPosition.z);

                bobRot = new Vector3(
                    Mathf.Sin(t * walkBobFrequency.x) * walkBobRotation.x,
                    Mathf.Sin(t * walkBobFrequency.y) * walkBobRotation.y,
                    Mathf.Sin(t * walkBobFrequency.x) * walkBobRotation.z);

                bob *= speedFactor;
                bobRot *= speedFactor;
            }

            // --- Idle-Atmung ---
            Vector3 breath = Vector3.zero;
            if (speedFactor < 0.05f)
            {
                float bt = (Time.time + phase) * idleBreathSpeed;
                breath = new Vector3(
                    Mathf.Sin(bt) * idleBreathPosition.x,
                    Mathf.Sin(bt * 1.3f) * idleBreathPosition.y,
                    Mathf.Sin(bt * 0.8f) * idleBreathPosition.z);
            }

            // --- Sprint-Absenkung ---
            Vector3 targetSprint = sprinting && !aiming ? Vector3.one : Vector3.zero;
            currentSprintBlend = Vector3.MoveTowards(currentSprintBlend, targetSprint, Time.deltaTime * sprintTransitionSpeed);

            Vector3 sprintPos = Vector3.Scale(sprintPosition, currentSprintBlend);
            Vector3 sprintRot = Vector3.Scale(sprintEuler, currentSprintBlend);

            // --- Nachladen ---
            Vector3 reloadPos = Vector3.zero;
            Vector3 reloadRot = Vector3.zero;
            if (lowerWhileReloading && reloading)
            {
                reloadPos = reloadPosition;
                reloadRot = reloadEuler;
            }

            // --- Schwanken ---
            Vector3 swayPos = new Vector3(swayOffset.x, swayOffset.y, 0f);
            Vector3 swayRot = new Vector3(-swayOffset.y * swayRotationAmount * 10f, swayOffset.x * swayRotationAmount * 10f, swayOffset.x * swayRotationAmount * 20f);

            // --- Zusammenrechnen ---
            Vector3 finalPos = (bob * damp) + breath + sprintPos + reloadPos + swayPos;
            Vector3 finalRot = (bobRot * damp) + sprintRot + reloadRot + swayRot;

            ApplyPose(finalPos, finalRot);
        }

        private void ApplyPose(Vector3 positionOffset, Vector3 rotationOffset)
        {
            Quaternion rot = Quaternion.Euler(rotationOffset);

            // Weg A: Slots im Waffen-Prefab werden relativ verschoben
            if (rightHandSlot != null)
            {
                rightHandSlot.localPosition = rightHandLocalPosition + positionOffset;
                rightHandSlot.localRotation = Quaternion.Euler(rightHandLocalEuler) * rot;
            }

            if (leftHandSlot != null)
            {
                leftHandSlot.localPosition = leftHandLocalPosition + positionOffset;
                leftHandSlot.localRotation = Quaternion.Euler(leftHandLocalEuler) * rot;
            }

            // Weg B: separates Arm-Prefab
            if (armsRoot != null && rightHandSlot == null)
            {
                armsRoot.localPosition = positionOffset;
                armsRoot.localRotation = rot;
            }
        }

        /// <summary> Blendet die Arme ein/aus (z.B. in Cutscenes oder im Jet). </summary>
        public void SetArmsVisible(bool visible)
        {
            if (armsInstance != null) armsInstance.SetActive(visible);
            if (rightHandSlot != null) rightHandSlot.gameObject.SetActive(visible);
            if (leftHandSlot != null) leftHandSlot.gameObject.SetActive(visible);
        }
    }
}
