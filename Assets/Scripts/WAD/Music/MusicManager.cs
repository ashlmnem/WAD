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

            EnsureAudioSources();
            sourceA.playOnAwake = false;
            sourceB.playOnAwake = false;
            sourceA.spatialBlend = 0f;
            sourceB.spatialBlend = 0f;
            sourceA.Stop();
            sourceB.Stop();
            sourceA.volume = 0f;
            sourceB.volume = 0f;

            activeSource = sourceA;
            inactiveSource = sourceB;
        }

        private void EnsureAudioSources()
        {
            var sources = GetComponents<AudioSource>();
            if (sourceA == null && sources.Length > 0) sourceA = sources[0];
            if (sourceA == null) sourceA = gameObject.AddComponent<AudioSource>();

            if (sourceB == null || sourceB == sourceA)
            {
                foreach (var source in sources)
                {
                    if (source != sourceA)
                    {
                        sourceB = source;
                        break;
                    }
                }
            }

            if (sourceB == null || sourceB == sourceA) sourceB = gameObject.AddComponent<AudioSource>();
        }

        /// <summary> Spielt einen neuen Track mit Crossfade. Bei loop=true wiederholt er sich (z.B. Ambience). </summary>
        public void PlayTrack(AudioClip clip, bool loop = true)
        {
            if (clip == null || activeSource == null || inactiveSource == null) return;
            if (activeSource.clip == clip && activeSource.isPlaying) return;

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
            float duration = Mathf.Max(0.01f, crossfadeDuration);

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / duration;

                inactiveSource.volume = Mathf.Lerp(0f, musicVolume, t);
                activeSource.volume = Mathf.Lerp(startActiveVolume, 0f, t);

                yield return null;
            }

            activeSource.Stop();

            // Rollen tauschen
            var temp = activeSource;
            activeSource = inactiveSource;
            inactiveSource = temp;
            activeSource.volume = musicVolume;
            fadeRoutine = null;
        }

        private IEnumerator FadeOutRoutine()
        {
            float startVolumeA = sourceA != null ? sourceA.volume : 0f;
            float startVolumeB = sourceB != null ? sourceB.volume : 0f;
            float elapsed = 0f;
            float duration = Mathf.Max(0.01f, crossfadeDuration);

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                if (sourceA != null) sourceA.volume = Mathf.Lerp(startVolumeA, 0f, t);
                if (sourceB != null) sourceB.volume = Mathf.Lerp(startVolumeB, 0f, t);
                yield return null;
            }

            if (sourceA != null) { sourceA.Stop(); sourceA.volume = 0f; }
            if (sourceB != null) { sourceB.Stop(); sourceB.volume = 0f; }
            fadeRoutine = null;
        }
    }
}
