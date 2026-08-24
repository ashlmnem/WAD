using System.Collections;
using UnityEngine;

namespace WAD.Audio
{
    /// <summary>
    /// Zentrale Musik-/Ambience-Steuerung. Zwei AudioSources fuer sanftes
    /// Crossfading zwischen Tracks (z.B. Ambient-Loop -> Combat-Sting).
    /// Singleton mit DontDestroyOnLoad, wie RunStateManager.
    /// </summary>
    public class MusicManager : MonoBehaviour
    {
        public static MusicManager Instance { get; private set; }

        [Header("Audio-Quellen (im Inspector 2x AudioSource-Komponenten anlegen)")]
        public AudioSource sourceA;
        public AudioSource sourceB;

        [Header("Einstellungen")]
        public float crossfadeDuration = 2.5f;
        [Range(0f, 1f)] public float musicVolume = 0.6f;

        private AudioSource activeSource;
        private AudioSource inactiveSource;
        private Coroutine fadeRoutine;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            activeSource = sourceA;
            inactiveSource = sourceB;
        }

        /// <summary> Spielt einen neuen Track mit Crossfade. Bei loop=true wiederholt er sich (z.B. Ambience). </summary>
        public void PlayTrack(AudioClip clip, bool loop = true)
        {
            if (clip == null || activeSource.clip == clip) return;

            if (fadeRoutine != null) StopCoroutine(fadeRoutine);
            fadeRoutine = StartCoroutine(CrossfadeRoutine(clip, loop));
        }

        public void StopMusic()
        {
            if (fadeRoutine != null) StopCoroutine(fadeRoutine);
            fadeRoutine = StartCoroutine(FadeOutRoutine());
        }

        private IEnumerator CrossfadeRoutine(AudioClip newClip, bool loop)
        {
            inactiveSource.clip = newClip;
            inactiveSource.loop = loop;
            inactiveSource.volume = 0f;
            inactiveSource.Play();

            float elapsed = 0f;
            float startActiveVolume = activeSource.volume;

            while (elapsed < crossfadeDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / crossfadeDuration;

                inactiveSource.volume = Mathf.Lerp(0f, musicVolume, t);
                activeSource.volume = Mathf.Lerp(startActiveVolume, 0f, t);

                yield return null;
            }

            activeSource.Stop();

            // Rollen tauschen
            var temp = activeSource;
            activeSource = inactiveSource;
            inactiveSource = temp;
        }

        private IEnumerator FadeOutRoutine()
        {
            float elapsed = 0f;
            float startVolume = activeSource.volume;

            while (elapsed < crossfadeDuration)
            {
                elapsed += Time.deltaTime;
                activeSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / crossfadeDuration);
                yield return null;
            }
            activeSource.Stop();
        }
    }
}
