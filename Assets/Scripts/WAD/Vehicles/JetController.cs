using System.Collections;
using UnityEngine;
using WAD.Feedback;
using WAD.Inventory;
using WAD.Player;

namespace WAD.Vehicles
{
    /// <summary>
    /// Ein reparierbarer, vom Spieler fliegbarer Jet.
    ///
    /// Ablauf:
    ///  1) Spieler repariert den Jet (JetRepairSystem) - die benoetigten Teile
    ///     sind pro Exemplar zufaellig.
    ///  2) Interaktion uebernimmt die Kontrolle: Triebwerke fahren hoch
    ///     (Startsequenz mit Sound/Animation), dann fliegt der Spieler frei.
    ///  3) Erneute Interaktion steigt aus; der Spieler wird neben dem Jet abgesetzt.
    ///
    /// Alle Sounds/Animationen laufen ueber den ActionFeedbackPlayer auf
    /// demselben Objekt - dort im Inspector eintragen, kein Code noetig.
    /// </summary>
    [RequireComponent(typeof(ActionFeedbackPlayer))]
    public class JetController : MonoBehaviour, WAD.Inventory.IInteractable
    {
        public enum JetState { Parked, StartingUp, Ready, Flying, ShuttingDown }

        [Header("Daten")]
        public JetFlightProfile flightProfile;

        [Header("Referenzen")]
        [Tooltip("Das sichtbare Flugzeugmodell (wird fuer Animationen geneigt/gerollt)")]
        public Transform visualRoot;
        [Tooltip("Punkt, an dem die Kamera im Cockpit sitzt (leer = cameraLocalOffset wird benutzt)")]
        public Transform cockpitCameraPoint;
        [Tooltip("Punkt fuer Triebwerks-VFX (Duese)")]
        public Transform thrusterPoint;
        [Tooltip("Leer = automatisch gesucht")]
        public JetRepairSystem repairSystem;
        [Tooltip("Waffen des Jets (MGs, Raketen). Optional.")]
        public JetWeaponSystem weaponSystem;

        [Header("Kollision")]
        public LayerMask collisionLayers = ~0;
        [Tooltip("Layer, die beim Flug ignoriert werden (z.B. der Spieler selbst)")]
        public LayerMask ignoreLayers;

        [Header("Verhalten")]
        [Tooltip("Beim Aussteigen den Spieler neben dem Jet absetzen")]
        public bool placePlayerOnExit = true;
        [Tooltip("Wie weit neben dem Jet der Spieler abgesetzt wird")]
        public float exitOffsetDistance = 4f;
        [Tooltip("Triebwerk laeuft weiter, wenn der Spieler aussteigt")]
        public bool keepEngineRunningAfterExit = false;

        [Header("Tastenbelegung")]
        public KeyCode interactExitKey = KeyCode.F;
        public KeyCode throttleUpKey = KeyCode.W;
        public KeyCode throttleDownKey = KeyCode.S;
        public KeyCode pitchUpKey = KeyCode.UpArrow;
        public KeyCode pitchDownKey = KeyCode.DownArrow;
        public KeyCode yawLeftKey = KeyCode.A;
        public KeyCode yawRightKey = KeyCode.D;
        public KeyCode rollLeftKey = KeyCode.Q;
        public KeyCode rollRightKey = KeyCode.E;
        public KeyCode ascendKey = KeyCode.Space;
        public KeyCode descendKey = KeyCode.LeftControl;
        public KeyCode afterburnerKey = KeyCode.LeftShift;

        // --- Zustand ---
        private ActionFeedbackPlayer feedback;
        private JetState state = JetState.Parked;
        private PlayerInteraction currentPilot;
        private Camera pilotCamera;
        private Transform originalCameraParent;
        private Vector3 originalCameraLocalPosition;
        private Quaternion originalCameraLocalRotation;
        private float originalCameraFOV;
        private FirstPersonCameraLook pilotLook;
        private TarkovMovementController pilotMovement;
        private CharacterController pilotCharacterController;

        private float throttle;              // 0..1
        private Vector3 controlInput;        // x = roll, y = pitch, z = yaw
        private float verticalInput;         // -1..1
        private float currentSpeed;
        private bool afterburnerActive;
        private bool afterburnerWasActive;
        private bool verticalMode;
        private bool wasAirborne;

        public JetState State => state;
        public bool IsFlyable => state == JetState.Ready || state == JetState.Flying;
        public bool IsPiloted => currentPilot != null;

        /// <summary> Wird ausgeloest, sobald der Spieler einsteigt (fuer Cutscenes/Trigger). </summary>
        public event System.Action<JetController> OnPilotEntered;
        /// <summary> Wird ausgeloest, sobald der Spieler aussteigt. </summary>
        public event System.Action<JetController> OnPilotExited;
        /// <summary> Wird ausgeloest, wenn die Triebwerke hochgefahren sind und der Jet flugbereit ist. </summary>
        public event System.Action<JetController> OnReadyToFly;

        public string InteractionPrompt
        {
            get
            {
                if (repairSystem != null && !repairSystem.IsRepaired) return repairSystem.GetRepairPrompt();
                if (state == JetState.Flying) return $"{interactExitKey} - Aussteigen";
                if (state == JetState.Parked || state == JetState.Ready) return $"{interactExitKey} - Einsteigen";
                return "Triebwerke laufen...";
            }
        }

        private void Awake()
        {
            feedback = GetComponent<ActionFeedbackPlayer>();
            if (repairSystem == null) repairSystem = GetComponent<JetRepairSystem>();
            if (weaponSystem == null) weaponSystem = GetComponent<JetWeaponSystem>();
        }

        public void Interact(PlayerInteraction interactor)
        {
            if (interactor == null) return;

            if (state == JetState.Flying)
            {
                ExitJet();
                return;
            }

            if (repairSystem != null && !repairSystem.IsRepaired)
            {
                repairSystem.TryDeliverParts(interactor.inventory);
                return;
            }

            if (state == JetState.Parked || state == JetState.Ready)
            {
                EnterJet(interactor);
            }
        }

        // ------------------------------------------------------------------
        //  Ein-/Aussteigen
        // ------------------------------------------------------------------

        public void EnterJet(PlayerInteraction pilot)
        {
            if (currentPilot != null) return;
            if (repairSystem != null && !repairSystem.IsRepaired) return;

            currentPilot = pilot;
            pilotCamera = pilot.playerCamera != null ? pilot.playerCamera : Camera.main;

            pilotLook = pilot.GetComponentInChildren<FirstPersonCameraLook>();
            pilotMovement = pilot.GetComponent<TarkovMovementController>();
            pilotCharacterController = pilot.GetComponent<CharacterController>();

            // Steuerung des Spielers stilllegen
            if (pilotMovement != null) pilotMovement.enabled = false;
            if (pilotLook != null) pilotLook.enabled = false;

            // Kamera uebernehmen, aber spaeter exakt zurueckgeben
            if (pilotCamera != null)
            {
                originalCameraParent = pilotCamera.transform.parent;
                originalCameraLocalPosition = pilotCamera.transform.localPosition;
                originalCameraLocalRotation = pilotCamera.transform.localRotation;
                originalCameraFOV = pilotCamera.fieldOfView;
                pilotCamera.transform.SetParent(cockpitCameraPoint != null ? cockpitCameraPoint : transform, false);
                pilotCamera.transform.localPosition = cockpitCameraPoint != null ? Vector3.zero : (flightProfile != null ? flightProfile.cameraLocalOffset : Vector3.zero);
                pilotCamera.fieldOfView = flightProfile != null ? flightProfile.cameraFOV : 70f;
            }

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            OnPilotEntered?.Invoke(this);

            StartCoroutine(StartupSequence());
        }

        private IEnumerator StartupSequence()
        {
            state = JetState.StartingUp;
            PlayFeedback(ActionId.JetEngineStart);

            float duration = flightProfile != null ? flightProfile.engineStartupSeconds : 4f;
            float t = 0f;

            // Triebwerksschleife starten und mit der Startsequenz hochfahren
            feedback?.StartLoop(ActionId.JetEngineLoop);

            while (t < duration)
            {
                t += Time.deltaTime;

                float spinUp = duration > 0f ? Mathf.Clamp01(t / duration) : 1f;

                // Drehzahl: Sound-Lautstaerke/Tonhoehe folgen, VFX-Slot kann hier andocken
                feedback?.SetLoopIntensity(spinUp * (throttle * 0.6f + 0.4f));

                yield return null;
            }

            state = JetState.Ready;
            feedback?.SetLoopIntensity(Mathf.Max(0.25f, throttle * 0.6f + 0.4f));
            OnReadyToFly?.Invoke(this);
        }

        public void ExitJet()
        {
            if (currentPilot == null) return;
            if (state == JetState.ShuttingDown) return;

            StartCoroutine(ShutdownSequence());
        }

        private IEnumerator ShutdownSequence()
        {
            state = JetState.ShuttingDown;

            PlayFeedback(ActionId.JetEngineShutdown);

            float duration = keepEngineRunningAfterExit ? 0f : (flightProfile != null ? flightProfile.engineShutdownSeconds : 2f);
            if (duration > 0f) yield return new WaitForSeconds(duration);

            // Triebwerksschleife ausblenden (laueft weiter, wenn der Motor laufen soll)
            if (!keepEngineRunningAfterExit) feedback?.StopLoop();

            // Kamera zurueckgeben
            if (pilotCamera != null)
            {
                pilotCamera.transform.SetParent(originalCameraParent, false);
                pilotCamera.transform.localPosition = originalCameraLocalPosition;
                pilotCamera.transform.localRotation = originalCameraLocalRotation;
                pilotCamera.fieldOfView = originalCameraFOV;
            }

            // Spieler neben dem Jet absetzen
            if (placePlayerOnExit && currentPilot != null)
            {
                Vector3 exitPos = FindSafeExitPosition();
                if (pilotCharacterController != null) pilotCharacterController.enabled = false;
                currentPilot.transform.position = exitPos;
                if (pilotCharacterController != null) pilotCharacterController.enabled = true;
            }

            if (pilotMovement != null) pilotMovement.enabled = true;
            if (pilotLook != null) pilotLook.enabled = true;

            currentPilot = null;
            throttle = 0f;
            currentSpeed = 0f;
            verticalMode = false;
            state = keepEngineRunningAfterExit ? JetState.Ready : JetState.Parked;

            PlayFeedback(ActionId.WeaponHolster);
            OnPilotExited?.Invoke(this);        }

        private Vector3 FindSafeExitPosition()
        {
            Vector3 start = transform.position;
            Vector3[] dirs =
            {
                transform.right, -transform.right, transform.forward, -transform.forward
            };

            foreach (var dir in dirs)
            {
                Vector3 candidate = start + dir.normalized * exitOffsetDistance + Vector3.up * 1.5f;

                // Boden darunter suchen
                if (Physics.Raycast(candidate + Vector3.up * 5f, Vector3.down, out RaycastHit hit, 30f, collisionLayers & ~ignoreLayers))
                {
                    candidate = hit.point + Vector3.up * 1.1f;
                }

                // Frei?
                if (!Physics.CheckSphere(candidate, 0.6f, collisionLayers & ~ignoreLayers))
                {
                    return candidate;
                }
            }

            return start + Vector3.up * 3f;
        }

        // ------------------------------------------------------------------
        //  Flug
        // ------------------------------------------------------------------

        private void Update()
        {
            if (currentPilot == null) return;

            if (state == JetState.Ready || state == JetState.Flying)
            {
                ReadInput();
                if (Input.GetKeyDown(interactExitKey)) ExitJet();
            }

            if (weaponSystem != null && currentPilot != null)
            {
                weaponSystem.TickPilotInput();
            }
        }

        private void FixedUpdate()
        {
            if (currentPilot == null) return;
            if (state != JetState.Ready && state != JetState.Flying) return;

            ApplyFlight(Time.fixedDeltaTime);
        }

        private void ReadInput()
        {
            float throttleAxis = 0f;
            if (Input.GetKey(throttleUpKey)) throttleAxis += 1f;
            if (Input.GetKey(throttleDownKey)) throttleAxis -= 1f;
            throttle = Mathf.Clamp01(throttle + throttleAxis * Time.deltaTime * 0.8f);

            float pitch = 0f;
            if (Input.GetKey(pitchDownKey)) pitch += 1f;
            if (Input.GetKey(pitchUpKey)) pitch -= 1f;

            float yaw = 0f;
            if (Input.GetKey(yawRightKey)) yaw += 1f;
            if (Input.GetKey(yawLeftKey)) yaw -= 1f;

            float roll = 0f;
            if (Input.GetKey(rollRightKey)) roll += 1f;
            if (Input.GetKey(rollLeftKey)) roll -= 1f;

            // Maus als Alternative fuer Nick/Gier
            if (Cursor.lockState == CursorLockMode.Locked)
            {
                pitch -= Input.GetAxisRaw("Mouse Y") * 1.5f;
                yaw += Input.GetAxisRaw("Mouse X") * 1.5f;
            }

            float response = flightProfile != null ? flightProfile.controlResponse : 3.5f;
            Vector3 targetControl = new Vector3(Mathf.Clamp(roll, -1f, 1f), Mathf.Clamp(pitch, -1f, 1f), Mathf.Clamp(yaw, -1f, 1f));
            controlInput = Vector3.Lerp(controlInput, targetControl, response * Time.deltaTime);

            verticalInput = 0f;
            if (Input.GetKey(ascendKey)) verticalInput += 1f;
            if (Input.GetKey(descendKey)) verticalInput -= 1f;

            afterburnerActive = Input.GetKey(afterburnerKey);

            // Nachbrenner nur beim Einschalten melden, nicht jeden Frame
            if (afterburnerActive != afterburnerWasActive)
            {
                afterburnerWasActive = afterburnerActive;
                if (afterburnerActive) PlayFeedback(ActionId.JetAfterburner);
            }

            // VTOL nur wenn erlaubt und (fast) stillstanden oder im Senkrechtmodus
            verticalMode = flightProfile != null && flightProfile.canVerticalTakeOff && (verticalMode || currentSpeed < 20f);
            if (flightProfile != null && !flightProfile.canVerticalTakeOff) verticalMode = false;
        }

        private void ApplyFlight(float dt)
        {
            var p = flightProfile;
            if (p == null) return;

            if (state == JetState.Ready) state = JetState.Flying;

            // --- Rotation (Flugzeugachse, relativ zum eigenen Raum) ---
            float pitchDelta = controlInput.y * p.pitchRate * dt;
            float yawDelta = controlInput.z * p.yawRate * dt;
            float rollDelta = controlInput.x * p.rollRate * dt;

            transform.Rotate(Vector3.right, pitchDelta, Space.Self);
            transform.Rotate(Vector3.up, yawDelta, Space.Self);

            // Roll ueber das visuelle Modell, damit die Kamera nicht mitkippt (angenehmer)
            if (visualRoot != null)
            {
                Vector3 euler = visualRoot.localEulerAngles;
                euler.z = Mathf.LerpAngle(euler.z, -controlInput.x * 60f, p.controlResponse * dt);
                visualRoot.localEulerAngles = euler;
            }

            // --- Geschwindigkeit ---
            float thrust = p.baseThrust * throttle;
            if (afterburnerActive) thrust *= p.afterburnerMultiplier;

            currentSpeed = Mathf.MoveTowards(currentSpeed, p.maxForwardSpeed * throttle, thrust * dt);
            currentSpeed = Mathf.MoveTowards(currentSpeed, 0f, p.brakeAcceleration * dt * (throttle <= 0.01f ? 1f : 0f));

            // --- Position ---
            Vector3 velocity = transform.forward * currentSpeed;

            // Schwerkraft wirkt, abhaengig von der Vorwaertsfahrt (Auftrieb)
            float lift = Mathf.InverseLerp(0f, p.takeoffSpeed, currentSpeed);
            float gravityEffect = p.gravityInfluence * (1f - lift);
            velocity += Vector3.down * 9.81f * gravityEffect;

            // Vertikaler Schub (VTOL)
            if (verticalMode && p.verticalThrust > 0f)
            {
                velocity += Vector3.up * verticalInput * p.verticalThrust;
            }

            Vector3 motion = velocity * dt;

            // --- Kollision ---
            if (motion.sqrMagnitude > 0.0001f)
            {
                LayerMask mask = collisionLayers & ~ignoreLayers;
                if (Physics.SphereCast(transform.position, p.collisionRadius, motion.normalized, out RaycastHit hit, motion.magnitude, mask))
                {
                    // Aufprall: eigener Crash-Sound, Schaden-VFX-Slot
                    PlayFeedback(ActionId.JetCrash);

                    if (currentSpeed > p.crashSpeedThreshold)
                    {
                        currentSpeed *= 0.2f;
                    }
                    else
                    {
                        currentSpeed *= 0.5f;
                    }

                    transform.position += motion.normalized * Mathf.Max(0f, hit.distance - 0.05f);
                    return;
                }
            }

            transform.position += motion;

            // --- Abheben / Landen erkennen ---
            UpdateAirborneState(p);

            // --- Treibstoff ---
            ConsumeFuel(dt, p);
        }

        /// <summary>
        /// Meldet Abheben und Landung genau einmal, damit die zugehoerigen
        /// Sound-/Animations-Slots nicht jeden Frame feuern.
        /// </summary>
        private void UpdateAirborneState(JetFlightProfile p)
        {
            bool grounded = Physics.Raycast(transform.position, Vector3.down, out _, p.groundProximity * 2f, collisionLayers & ~ignoreLayers);

            if (wasAirborne && grounded)
            {
                PlayFeedback(ActionId.JetLanding);
                wasAirborne = false;
            }
            else if (!wasAirborne && !grounded)
            {
                PlayFeedback(ActionId.JetTakeoff);
                wasAirborne = true;
            }

            // Triebwerksschleife folgt dem Schub
            feedback?.SetLoopIntensity(Mathf.Max(0.25f, throttle * 0.6f + 0.4f));
        }

        private void ConsumeFuel(float dt, JetFlightProfile p)
        {
            if (p.fuelBurnPerSecond <= 0f) return;

            var run = WAD.Core.RunStateManager.Instance;
            if (run == null) return;

            float burn = p.fuelBurnPerSecond * throttle * dt;
            if (afterburnerActive && p.afterburnerBurnsExtra) burn *= p.afterburnerMultiplier;

            if (burn <= 0f) return;

            run.jetFuel = Mathf.Max(0f, run.jetFuel - burn);

            // Ohne Treibstoff faellt der Schub weg
            if (run.jetFuel <= 0f)
            {
                throttle = Mathf.MoveTowards(throttle, 0f, dt * 0.5f);
            }
        }

        private void PlayFeedback(ActionId action)
        {
            feedback?.Play(action);
        }

        /// <summary> Von aussen (z.B. Waffensystem) genutzter Zugriff auf den Feedback-Player. </summary>
        public void PlayAction(ActionId action) => PlayFeedback(action);

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            float radius = flightProfile != null ? flightProfile.collisionRadius : 3f;
            Gizmos.DrawWireSphere(transform.position, radius);

            Vector3 camOffset = cockpitCameraPoint != null ? cockpitCameraPoint.position : transform.position + (flightProfile != null ? flightProfile.cameraLocalOffset : Vector3.up * 1.6f);
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(camOffset, Vector3.one * 0.3f);
            Gizmos.DrawLine(transform.position, camOffset);
        }
    }
}
