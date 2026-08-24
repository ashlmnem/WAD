using UnityEngine;

namespace WAD.Player
{
    /// <summary>
    /// Realistisch-taktischer Bewegungscontroller (Tarkov-Referenz):
    /// - Traege Beschleunigung/Abbremsung statt sofortiger Vollgeschwindigkeit
    /// - Tragegewicht reduziert Geschwindigkeit, Sprint-Ausdauer und Sprungkraft
    /// - Begrenzte Sprint-Ausdauer mit Regeneration (langsamer bei hohem Gewicht)
    /// - Crouch reduziert Geschwindigkeit und (spaeter) Laufgeraeusch-Radius
    /// - ADS-Zustand (vom Waffensystem gesetzt) verlangsamt zusaetzlich
    ///
    /// Benoetigt CharacterController-Komponente auf demselben Objekt.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class TarkovMovementController : MonoBehaviour
    {
        [Header("Referenzen")]
        public Transform cameraTransform;

        [Header("Basiswerte (m/s)")]
        public float walkSpeed = 2.8f;
        public float sprintSpeed = 5.2f;
        public float crouchSpeed = 1.4f;
        public float adsSpeedMultiplier = 0.5f;

        [Header("Trägheit")]
        [Tooltip("Wie schnell die aktuelle Geschwindigkeit sich der Zielgeschwindigkeit annaehert. Niedriger = traeger/schwerer.")]
        public float acceleration = 4f;
        public float deceleration = 6f;

        [Header("Sprung & Schwerkraft")]
        public float jumpHeight = 0.5f; // bewusst niedrig - kein Arcade-Bunny-Hop
        public float gravity = -18f;

        [Header("Kamera-Höhe (Stehen/Ducken)")]
        public float standingCameraHeight = 1.6f;
        public float crouchingCameraHeight = 0.9f;
        public float cameraHeightTransitionSpeed = 8f;

        [Header("Collider-Höhe (Stehen/Ducken)")]
        public float standingControllerHeight = 1.8f;
        public float crouchingControllerHeight = 1.1f;
        public float controllerHeightTransitionSpeed = 8f;

        [Header("Gewichtssystem")]
        [Tooltip("Aktuelles Tragegewicht in kg - vom Inventarsystem zu setzen")]
        public float currentWeight = 15f;
        [Tooltip("Ab diesem Gewicht beginnt spuerbare Verlangsamung")]
        public float weightThreshold = 20f;
        [Tooltip("Maximal tragbares Gewicht - darueber quasi bewegungsunfaehig")]
        public float maxWeight = 40f;
        [Range(0f, 1f)]
        [Tooltip("Wie stark sich Uebergewicht auf die Geschwindigkeit auswirkt")]
        public float weightSpeedPenaltyFactor = 0.6f;

        [Header("Ausdauer (Sprint)")]
        public float maxStamina = 100f;
        public float staminaDrainPerSecond = 14f;
        public float staminaRegenPerSecond = 8f;
        [Tooltip("Unter diesem Wert kann Sprint nicht erneut gestartet werden (verhindert Sprint-Stottern bei fast leerer Ausdauer)")]
        public float minStaminaToStartSprint = 10f;

        private CharacterController controller;
        private Vector3 currentVelocity;      // horizontale, geglaettete Geschwindigkeit
        private float verticalVelocity;
        private float currentStamina;
        private bool isSprintingHeld;
        private bool isCrouching;
        private bool canSprint = true;

        // Vom Waffensystem gesetzt (ADS = Aiming Down Sights)
        [HideInInspector] public bool isAiming;

        public float CurrentStamina => currentStamina;
        public float MaxStamina => maxStamina;
        public bool IsSprinting { get; private set; }
        public bool IsCrouching => isCrouching;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            currentStamina = maxStamina;
        }

        private void Update()
        {
            HandleCrouchInput();
            HandleSprintInput();

            Vector3 moveInput = GetMoveInput();
            float targetSpeed = CalculateTargetSpeed(moveInput);

            Vector3 targetVelocity = moveInput * targetSpeed;
            float rate = (targetVelocity.magnitude > currentVelocity.magnitude) ? acceleration : deceleration;
            currentVelocity = Vector3.Lerp(currentVelocity, targetVelocity, rate * Time.deltaTime);

            HandleStamina(moveInput);
            HandleJumpAndGravity();
            UpdateCameraHeight();
            UpdateControllerHeight();

            Vector3 motion = currentVelocity * Time.deltaTime + Vector3.up * verticalVelocity * Time.deltaTime;
            controller.Move(motion);
        }

        private Vector3 GetMoveInput()
        {
            float x = Input.GetAxisRaw("Horizontal");
            float z = Input.GetAxisRaw("Vertical");
            Vector3 input = (transform.right * x + transform.forward * z);
            return input.sqrMagnitude > 1f ? input.normalized : input;
        }

        private void HandleCrouchInput()
        {
            if (Input.GetKeyDown(KeyCode.LeftControl))
            {
                isCrouching = !isCrouching;
                // Hoehe/Center werden nicht mehr instant gesetzt, sondern
                // in UpdateControllerHeight() sanft angeglichen - vermeidet
                // Kollisions-Depenetration-Sprung beim Aufstehen.
            }
        }

        private void UpdateControllerHeight()
        {
            float targetHeight = isCrouching ? crouchingControllerHeight : standingControllerHeight;
            float newHeight = Mathf.Lerp(controller.height, targetHeight, controllerHeightTransitionSpeed * Time.deltaTime);

            controller.height = newHeight;
            // Center bleibt immer bei height/2, damit die Kapsel-Unterkante konstant bei 0 bleibt
            // (Fuesse bewegen sich nicht, nur der Kopf hebt/senkt sich) - verhindert Einsacken.
            controller.center = new Vector3(0f, newHeight * 0.5f, 0f);
        }

        private void HandleSprintInput()
        {
            isSprintingHeld = Input.GetKey(KeyCode.LeftShift) && !isCrouching && !isAiming;

            if (!isSprintingHeld)
            {
                IsSprinting = false;
                return;
            }

            if (!IsSprinting)
            {
                // Sprint darf nur starten, wenn genug Ausdauer da ist
                if (currentStamina >= minStaminaToStartSprint && canSprint)
                {
                    IsSprinting = true;
                }
            }
            else if (currentStamina <= 0f)
            {
                IsSprinting = false;
                canSprint = false; // muss sich erst etwas erholen, bevor erneut gesprintet werden kann
            }
        }

        private float CalculateTargetSpeed(Vector3 moveInput)
        {
            if (moveInput.sqrMagnitude < 0.01f) return 0f;

            float baseSpeed;
            if (isCrouching) baseSpeed = crouchSpeed;
            else if (IsSprinting) baseSpeed = sprintSpeed;
            else baseSpeed = walkSpeed;

            baseSpeed *= GetWeightSpeedMultiplier();

            if (isAiming) baseSpeed *= adsSpeedMultiplier;

            return baseSpeed;
        }

        /// <summary>
        /// Unterhalb von weightThreshold: kein Malus.
        /// Zwischen Threshold und maxWeight: linear abnehmend bis auf (1 - weightSpeedPenaltyFactor).
        /// Ueber maxWeight: stark bestraft (nahezu Stillstand), simuliert Ueberladung.
        /// </summary>
        private float GetWeightSpeedMultiplier()
        {
            if (currentWeight <= weightThreshold) return 1f;

            if (currentWeight >= maxWeight)
            {
                return Mathf.Max(0.05f, 1f - weightSpeedPenaltyFactor - 0.2f);
            }

            float t = (currentWeight - weightThreshold) / (maxWeight - weightThreshold);
            return Mathf.Lerp(1f, 1f - weightSpeedPenaltyFactor, t);
        }

        private void HandleStamina(Vector3 moveInput)
        {
            if (IsSprinting && moveInput.sqrMagnitude > 0.01f)
            {
                // Hoeheres Gewicht = schnellerer Ausdauerverlust
                float weightDrainMultiplier = Mathf.Lerp(1f, 1.8f, Mathf.InverseLerp(weightThreshold, maxWeight, currentWeight));
                currentStamina -= staminaDrainPerSecond * weightDrainMultiplier * Time.deltaTime;
                currentStamina = Mathf.Max(0f, currentStamina);
            }
            else
            {
                // Regeneration langsamer bei hohem Gewicht
                float weightRegenMultiplier = Mathf.Lerp(1f, 0.5f, Mathf.InverseLerp(weightThreshold, maxWeight, currentWeight));
                currentStamina += staminaRegenPerSecond * weightRegenMultiplier * Time.deltaTime;
                currentStamina = Mathf.Min(maxStamina, currentStamina);

                if (currentStamina >= minStaminaToStartSprint)
                {
                    canSprint = true;
                }
            }
        }

        private void UpdateCameraHeight()
        {
            if (cameraTransform == null) return;

            float targetHeight = isCrouching ? crouchingCameraHeight : standingCameraHeight;
            Vector3 localPos = cameraTransform.localPosition;
            localPos.y = Mathf.Lerp(localPos.y, targetHeight, cameraHeightTransitionSpeed * Time.deltaTime);
            cameraTransform.localPosition = localPos;
        }

        private void HandleJumpAndGravity()
        {
            if (controller.isGrounded)
            {
                if (verticalVelocity < 0f) verticalVelocity = -2f; // haelt Charakter am Boden "geklebt"

                bool canJump = currentWeight < maxWeight && currentStamina > 15f;
                if (Input.GetButtonDown("Jump") && canJump && !isCrouching)
                {
                    verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
                    currentStamina -= 10f; // Springen kostet Ausdauer, wie in Tarkov
                }
            }
            else
            {
                verticalVelocity += gravity * Time.deltaTime;
            }
        }
    }
}