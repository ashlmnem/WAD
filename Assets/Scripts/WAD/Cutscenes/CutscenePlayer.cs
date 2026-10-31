using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using WAD.CameraFx;
using WAD.Inventory;
using WAD.Player;
using WAD.Weapons;

namespace WAD.Cutscenes
{
    /// <summary>
    /// Spielt CutsceneSequence-Assets ab: fuehrt die Kamera in
    /// First-Person-Optik, zeigt Untertitel, spielt Sounds und wendet das
    /// Found-Footage-Profil an.
    ///
    /// Einmal in die Szene legen (z.B. auf das Level-Manager-Objekt). Danach
    /// koennen beliebig viele Cutscenes ueber Play(sequence) gestartet werden -
    /// z.B. per CutsceneExitTrigger an einem Exit. Kein Code noetig.
    /// </summary>
    public class CutscenePlayer : MonoBehaviour
    {
        [Header("Referenzen")]
        [Tooltip("Kamera, die gefuehrt wird. Leer = Camera.main.")]
        public Camera targetCamera;
        [Tooltip("Untertitel-Anzeige (kann leer bleiben)")]
        public CutsceneSubtitleUI subtitleUI;
        [Tooltip("Found-Footage-Steuerung des Levels (kann leer bleiben)")]
        public FoundFootageController footageController;

        [Header("Quellen fuer Automatik")]
        [Tooltip("Spieler-Interaktion (wird waehrend der Cutscene stillgelegt)")]
        public PlayerInteraction playerInteraction;
        [Tooltip("Waffenhalter (Waffe wird waehrend der Cutscene ausgeblendet)")]
        public PlayerWeaponHolder weaponHolder;

        [Header("Audio")]
        [Tooltip("AudioSource fuer 2D-Cutscene-Sounds. Leer = automatisch ergaenzen.")]
        public AudioSource audioSource;

        [Header("Verhalten")]
        [Tooltip("Zeitskalierung des Spiels waehrend Cutscenes auf 0 halten, wenn kein eigener Wert gesetzt ist")]
        public bool freezeWorldByDefault = false;
        [Tooltip("Beim Start der Szene automatisch eine Intro-Cutscene abspielen")]
        public CutsceneSequence introCutscene;
        [Tooltip("Verzoegerung, bevor die Intro-Cutscene startet")]
        public float introDelay = 0.5f;

        private bool isPlaying;
        private Coroutine routine;
        private CameraAnimationController cameraAnimation;
        private float savedTimeScale = 1f;
        private FirstPersonCameraLook savedLook;
        private TarkovMovementController savedMovement;

        public bool IsPlaying => isPlaying;
        public event System.Action<CutsceneSequence> OnCutsceneFinished;

        private void Awake()
        {
            if (targetCamera == null) targetCamera = Camera.main;
            if (playerInteraction == null) playerInteraction = FindFirstObjectByType<PlayerInteraction>();
            if (weaponHolder == null) weaponHolder = FindFirstObjectByType<PlayerWeaponHolder>();

            if (targetCamera != null) cameraAnimation = targetCamera.GetComponent<CameraAnimationController>();

            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
                if (audioSource == null)
                {
                    audioSource = gameObject.AddComponent<AudioSource>();
                    audioSource.playOnAwake = false;
                    audioSource.spatialBlend = 0f;
                }
            }
        }

        private void Start()
        {
            if (introCutscene != null)
            {
                StartCoroutine(PlayIntroDelayed());
            }
        }

        private IEnumerator PlayIntroDelayed()
        {
            if (introDelay > 0f) yield return new WaitForSeconds(introDelay);
            Play(introCutscene);
        }

        /// <summary> Startet eine Cutscene. Laufende Cutscenes werden abgebrochen. </summary>
        public void Play(CutsceneSequence sequence)
        {
            if (sequence == null)
            {
                Debug.LogWarning($"[CutscenePlayer:{name}] Keine Cutscene uebergeben.");
                return;
            }
            if (sequence.steps == null || sequence.steps.Length == 0)
            {
                Debug.LogWarning($"[CutscenePlayer:{name}] Cutscene '{sequence.displayName}' hat keine Schritte.");
                return;
            }

            if (routine != null) StopCoroutine(routine);
            routine = StartCoroutine(PlayRoutine(sequence));
        }

        public void Skip()
        {
            if (isPlaying && routine != null)
            {
                StopCoroutine(routine);
                routine = null;
                EndCutscene();
            }
        }

        private IEnumerator PlayRoutine(CutsceneSequence sequence)
        {
            BeginCutscene(sequence);

            foreach (var step in sequence.steps)
            {
                if (step == null) continue;
                yield return PlayStep(sequence, step);
            }

            EndCutscene();
            OnCutsceneFinished?.Invoke(sequence);

            if (!string.IsNullOrEmpty(sequence.loadSceneAfter))
            {
                if (Application.CanStreamedLevelBeLoaded(sequence.loadSceneAfter))
                {
                    SceneManager.LoadScene(sequence.loadSceneAfter);
                }
                else
                {
                    Debug.LogError($"[CutscenePlayer] Szene '{sequence.loadSceneAfter}' ist nicht ladbar (Build Settings pruefen).");
                }
            }
        }

        private void BeginCutscene(CutsceneSequence sequence)
        {
            isPlaying = true;
            savedTimeScale = Time.timeScale;

            if (sequence.lockPlayerInput)
            {
                savedLook = targetCamera != null ? targetCamera.GetComponent<FirstPersonCameraLook>() : null;
                savedMovement = playerInteraction != null ? playerInteraction.GetComponent<TarkovMovementController>() : null;

                if (savedLook != null) savedLook.enabled = false;
                if (savedMovement != null) savedMovement.enabled = false;
                if (playerInteraction != null) playerInteraction.enabled = false;

                if (sequence.showCursor)
                {
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                }
            }

            if (sequence.hideWeapon && weaponHolder != null)
            {
                foreach (var weapon in weaponHolder.equippedWeapons)
                {
                    if (weapon != null) weapon.gameObject.SetActive(false);
                }
            }

            // Kamera-Animationen pausieren, damit sie die Cutscene nicht stoeren
            if (cameraAnimation != null) cameraAnimation.disabled = true;

            // Optik umschalten
            if (footageController != null && sequence.footageProfile != null)
            {
                footageController.PlayProfile(sequence.footageProfile);
            }
        }

        private void EndCutscene()
        {
            isPlaying = false;

            Time.timeScale = savedTimeScale;

            if (savedLook != null) savedLook.enabled = true;
            if (savedMovement != null) savedMovement.enabled = true;
            if (playerInteraction != null) playerInteraction.enabled = true;

            if (Camera.main != null)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }

            // Waffe wieder einblenden
            if (weaponHolder != null)
            {
                var active = weaponHolder.GetActiveWeapon();
                if (active != null) active.gameObject.SetActive(true);
            }

            if (cameraAnimation != null) cameraAnimation.disabled = false;

            if (footageController != null) footageController.BlendBack();
            if (subtitleUI != null) subtitleUI.Clear();
        }

        private IEnumerator PlayStep(CutsceneSequence sequence, CutsceneStep step)
        {
            // Sound
            if (step.sound != null)
            {
                if (step.soundPosition != null)
                {
                    AudioSource.PlayClipAtPoint(step.sound, step.soundPosition.position, step.soundVolume);
                }
                else
                {
                    audioSource.PlayOneShot(step.sound, step.soundVolume);
                }
            }

            // Untertitel
            if (subtitleUI != null && !string.IsNullOrEmpty(step.subtitleText))
            {
                float subDuration = step.subtitleDuration > 0f ? step.subtitleDuration : step.duration;
                subtitleUI.Show(step.subtitleText, subDuration);
            }

            // Optik pro Schritt
            if (footageController != null && step.footageOverride != null)
            {
                footageController.ApplyProfile(step.footageOverride, instant: step.hardCut);
            }

            Time.timeScale = step.timeScale > 0f
                ? step.timeScale
                : (freezeWorldByDefault ? 0f : 1f);

            // Kamera-Pose am Anfang des Schritts
            Vector3 startPos = Vector3.zero;
            Quaternion startRot = Quaternion.identity;
            bool hasStart = false;

            if (step.pointA != null)
            {
                startPos = step.pointA.position;
                startRot = step.pointA.rotation;
                hasStart = true;
            }
            else if (targetCamera != null)
            {
                startPos = targetCamera.transform.position;
                startRot = targetCamera.transform.rotation;
                hasStart = true;
            }

            float elapsed = 0f;
            float duration = Mathf.Max(0.01f, step.duration);

            while (elapsed < duration)
            {
                // Ueberspringen
                if (sequence.skippable && Input.GetKeyDown(sequence.skipKey))
                {
                    EndCutscene();
                    OnCutsceneFinished?.Invoke(sequence);
                    yield break;
                }

                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                ApplyCameraStep(sequence, step, startPos, startRot, hasStart, t);

                yield return null;
            }

            step.onStepEnd?.Invoke();
        }

        private void ApplyCameraStep(CutsceneSequence sequence, CutsceneStep step, Vector3 startPos, Quaternion startRot, bool hasStart, float t)
        {
            if (targetCamera == null) return;

            Vector3 position = targetCamera.transform.position;
            Quaternion rotation = targetCamera.transform.rotation;

            switch (step.cameraMode)
            {
                case CutsceneCameraMode.HoldPlayerView:
                    // nichts bewegen
                    break;

                case CutsceneCameraMode.DollyBetweenPoints:
                    {
                        Vector3 endPos = step.pointB != null ? step.pointB.position : startPos + step.lookDirection.normalized * 10f;
                        Vector3 from = hasStart ? startPos : position;
                        position = Vector3.Lerp(from, endPos, Smooth(t));

                        Vector3 lookTarget = step.pointB != null ? step.pointB.position : from;
                        Vector3 dir = step.pointB != null && step.pointA != null
                            ? (step.pointB.position - position)
                            : (lookTarget - from);
                        if (dir.sqrMagnitude > 0.0001f) rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);
                        break;
                    }

                case CutsceneCameraMode.PanAtPoint:
                    {
                        position = hasStart ? startPos : position;
                        Vector3 dirA = step.lookDirection.sqrMagnitude > 0.0001f ? step.lookDirection.normalized : startRot * Vector3.forward;
                        Vector3 dirB = step.pointB != null ? (step.pointB.position - position).normalized : dirA;
                        Vector3 dir = Vector3.Slerp(dirA, dirB, Smooth(t));
                        if (dir.sqrMagnitude > 0.0001f) rotation = Quaternion.LookRotation(dir, Vector3.up);
                        break;
                    }

                case CutsceneCameraMode.FollowTarget:
                    {
                        if (step.followTarget != null)
                        {
                            position = step.followTarget.position + step.followTarget.rotation * step.followOffset;
                            Vector3 dir = step.followTarget.position - position;
                            if (dir.sqrMagnitude > 0.0001f) rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);
                        }
                        break;
                    }
            }

            // Handheld-Wackeln im Found-Footage-Stil
            if (step.handheldShake > 0f)
            {
                float time = Time.unscaledTime * 1.6f;
                float amp = step.handheldShake * 0.02f;
                position += new Vector3(
                    (Mathf.PerlinNoise(time, 0.1f) * 2f - 1f) * amp,
                    (Mathf.PerlinNoise(time, 5.3f) * 2f - 1f) * amp,
                    (Mathf.PerlinNoise(time, 9.7f) * 2f - 1f) * amp);

                float rotAmp = step.handheldShake * 0.6f;
                rotation *= Quaternion.Euler(
                    (Mathf.PerlinNoise(time, 2.1f) * 2f - 1f) * rotAmp,
                    (Mathf.PerlinNoise(time, 7.7f) * 2f - 1f) * rotAmp,
                    (Mathf.PerlinNoise(time, 4.4f) * 2f - 1f) * rotAmp);
            }

            targetCamera.transform.position = position;
            targetCamera.transform.rotation = rotation;

            if (step.fieldOfView > 0f) targetCamera.fieldOfView = step.fieldOfView;
        }

        private static float Smooth(float t) => t * t * (3f - 2f * t);
    }
}
