using UnityEngine;
using WAD.Procedural;

namespace WAD.Levels.Level1
{
    /// <summary>
    /// Level 1 "The Storm": Der Sandsturm wird staerker, je weiter sich der
    /// Spieler vom Spawn-Zentrum entfernt (siehe Design: "the storm
    /// intensifies the further one moves from the center").
    ///
    /// Steuert Fog, Partikel-Wind-Intensitaet und optional Audio ueber
    /// eine einfache Distanz-Kurve, die im Inspector einstellbar ist.
    /// </summary>
    [RequireComponent(typeof(ProceduralGridGenerator))]
    public class DesertStormIntensity : MonoBehaviour
    {
        [Header("Referenzen")]
        public ProceduralGridGenerator gridGenerator;
        public ParticleSystem sandstormParticles;
        public AudioSource windAudioSource;

        [Header("Distanz-Kurve")]
        [Tooltip("X-Achse: Distanz vom Zentrum in Metern, Y-Achse: Intensitaet 0-1")]
        public AnimationCurve intensityByDistance = AnimationCurve.Linear(0f, 0f, 3000f, 1f);

        [Header("Fog")]
        public float minFogDensity = 0.01f;
        public float maxFogDensity = 0.15f;

        [Header("Sichtweite (Kamera Far Clip als grober Ersatz fuer Renderdistanz)")]
        public Camera playerCamera;
        public float maxViewDistanceClear = 500f;
        public float minViewDistanceStorm = 25f;

        [Header("Audio")]
        public float minWindVolume = 0.2f;
        public float maxWindVolume = 1f;

        [Header("Update-Rate")]
        public float updateInterval = 0.25f;
        private float timer;

        private void Reset()
        {
            gridGenerator = GetComponent<ProceduralGridGenerator>();
        }

        private void Update()
        {
            timer += Time.deltaTime;
            if (timer < updateInterval) return;
            timer = 0f;

            if (gridGenerator == null) return;

            float distance = gridGenerator.GetPlayerDistanceFromCenter();
            float intensity = Mathf.Clamp01(intensityByDistance.Evaluate(distance));

            ApplyFog(intensity);
            ApplyParticles(intensity);
            ApplyViewDistance(intensity);
            ApplyAudio(intensity);
        }

        private void ApplyFog(float intensity)
        {
            RenderSettings.fog = true;
            RenderSettings.fogDensity = Mathf.Lerp(minFogDensity, maxFogDensity, intensity);
        }

        private void ApplyParticles(float intensity)
        {
            if (sandstormParticles == null) return;

            var emission = sandstormParticles.emission;
            emission.rateOverTimeMultiplier = Mathf.Lerp(0.2f, 3f, intensity);

            var main = sandstormParticles.main;
            main.startSpeedMultiplier = Mathf.Lerp(1f, 4f, intensity);
        }

        private void ApplyViewDistance(float intensity)
        {
            if (playerCamera == null) return;
            playerCamera.farClipPlane = Mathf.Lerp(maxViewDistanceClear, minViewDistanceStorm, intensity);
        }

        private void ApplyAudio(float intensity)
        {
            if (windAudioSource == null) return;
            windAudioSource.volume = Mathf.Lerp(minWindVolume, maxWindVolume, intensity);
        }
    }
}
