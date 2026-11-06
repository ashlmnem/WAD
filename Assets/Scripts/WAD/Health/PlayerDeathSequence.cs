using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using WAD.Inventory;
using WAD.Weapons;

namespace WAD.Player
{
    [DisallowMultipleComponent]
    public class PlayerDeathSequence : MonoBehaviour
    {
        [Header("Camera")]
        [Tooltip("Leer = Kamera des Spielers automatisch suchen")]
        public Camera deathCamera;
        public float cameraFallDuration = 1.1f;
        public Vector3 cameraDeathRotation = new Vector3(55f, 0f, 18f);
        public Vector3 cameraDropOffset = new Vector3(0f, -0.25f, -0.12f);

        [Header("Flash und Sound")]
        public AudioClip deathSound;
        [Range(0f, 1f)] public float deathSoundVolume = 1f;
        [Min(0f)] public float flashDelay = 0.1f;
        [Min(0f)] public float flashInDuration = 0.12f;
        [Min(0f)] public float whiteHoldDuration = 0.2f;
        [Range(0f, 1f)] public float flashPeakAlpha = 1f;

        [Header("Rueckkehr zum Hauptmenue")]
        public string mainMenuSceneName = "MainMenu";
        [Min(0f)] public float menuLoadDelay = 0.25f;

        private AudioSource audioSource;
        private Image flashImage;
        private bool isPlaying;

        private void Awake()
        {
            if (deathCamera == null) deathCamera = GetComponentInChildren<Camera>(true);
            if (deathCamera == null) deathCamera = Camera.main;

            if (audioSource == null)
            {
                var audioObject = new GameObject("DeathSequenceAudio");
                audioObject.transform.SetParent(transform, false);
                audioSource = audioObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                audioSource.spatialBlend = 0f;
            }
        }

        public void Play()
        {
            if (isPlaying) return;
            isPlaying = true;
            StartCoroutine(PlayRoutine());
        }

        private IEnumerator PlayRoutine()
        {
            DisablePlayerInput();
            CreateFlashOverlay();

            if (deathSound != null) audioSource.PlayOneShot(deathSound, deathSoundVolume);

            if (deathCamera != null)
            {
                Transform cameraTransform = deathCamera.transform;
                Vector3 startPosition = cameraTransform.localPosition;
                Quaternion startRotation = cameraTransform.localRotation;
                Vector3 endPosition = startPosition + cameraDropOffset;
                Quaternion endRotation = startRotation * Quaternion.Euler(cameraDeathRotation);

                float elapsed = 0f;
                float duration = Mathf.Max(0.01f, cameraFallDuration);
                while (elapsed < duration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
                    cameraTransform.localPosition = Vector3.Lerp(startPosition, endPosition, t);
                    cameraTransform.localRotation = Quaternion.Slerp(startRotation, endRotation, t);
                    yield return null;
                }
            }

            if (flashDelay > 0f) yield return new WaitForSecondsRealtime(flashDelay);

            float flashElapsed = 0f;
            float flashDuration = Mathf.Max(0.01f, flashInDuration);
            while (flashElapsed < flashDuration)
            {
                flashElapsed += Time.unscaledDeltaTime;
                SetFlashAlpha(Mathf.Lerp(0f, flashPeakAlpha, Mathf.Clamp01(flashElapsed / flashDuration)));
                yield return null;
            }

            SetFlashAlpha(flashPeakAlpha);
            if (whiteHoldDuration > 0f) yield return new WaitForSecondsRealtime(whiteHoldDuration);
            if (menuLoadDelay > 0f) yield return new WaitForSecondsRealtime(menuLoadDelay);

            if (!Application.CanStreamedLevelBeLoaded(mainMenuSceneName))
            {
                Debug.LogError($"[PlayerDeathSequence] Main menu scene '{mainMenuSceneName}' is not in Build Settings.", this);
                yield break;
            }

            SceneManager.LoadScene(mainMenuSceneName);
        }

        private void DisablePlayerInput()
        {
            var movement = GetComponent<TarkovMovementController>();
            if (movement != null) movement.enabled = false;

            var look = GetComponentInChildren<FirstPersonCameraLook>(true);
            if (look != null) look.enabled = false;

            var interaction = GetComponentInChildren<PlayerInteraction>(true);
            if (interaction != null) interaction.enabled = false;

            var weaponHolder = GetComponentInChildren<PlayerWeaponHolder>(true);
            if (weaponHolder != null)
            {
                weaponHolder.enabled = false;
                if (weaponHolder.equippedWeapons != null)
                {
                    foreach (var weapon in weaponHolder.equippedWeapons)
                    {
                        if (weapon == null) continue;
                        weapon.SetEquipped(false);
                        weapon.gameObject.SetActive(false);
                    }
                }
            }

            if (deathCamera != null)
            {
                var cameraAnimation = deathCamera.GetComponent<WAD.CameraFx.CameraAnimationController>();
                if (cameraAnimation != null) cameraAnimation.disabled = true;
            }
        }

        private void CreateFlashOverlay()
        {
            var canvasObject = new GameObject("DeathFlashCanvas", typeof(Canvas));
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = short.MaxValue;

            var imageObject = new GameObject("WhiteFlash", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            imageObject.transform.SetParent(canvasObject.transform, false);

            var rect = imageObject.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            flashImage = imageObject.GetComponent<Image>();
            flashImage.color = Color.clear;
            flashImage.raycastTarget = false;
        }

        private void SetFlashAlpha(float alpha)
        {
            if (flashImage == null) return;
            Color color = Color.white;
            color.a = alpha;
            flashImage.color = color;
        }
    }
}