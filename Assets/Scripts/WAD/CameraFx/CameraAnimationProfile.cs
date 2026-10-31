using UnityEngine;

namespace WAD.CameraFx
{
    /// <summary>
    /// Eine einzelne Kamera-Animationsschicht (Idle-Atmen, Dreh-Schwung,
    /// Lauf-Wippen, Sprint-Rütteln, Nachladen, Landung, ...).
    /// Alle Felder im Inspector einstellbar.
    /// </summary>
    [System.Serializable]
    public class CameraMotionLayer
    {
        [Tooltip("Anzeigename nur zur Orientierung im Inspector")]
        public string label = "Layer";
        public bool enabled = true;

        [Header("Amplitude (Grad bzw. Meter)")]
        public Vector3 positionAmplitude = Vector3.zero;
        public Vector3 rotationAmplitude = Vector3.zero;

        [Header("Frequenz (Hz) pro Achse")]
        public Vector3 positionFrequency = Vector3.one;
        public Vector3 rotationFrequency = Vector3.one;

        [Header("Rauschen statt Sinus")]
        [Tooltip("Statt einer sauberen Sinuswelle weiches Rauschen verwenden (natuerlicheres Wackeln)")]
        public bool useNoise = true;
        [Tooltip("Geschwindigkeit des Rauschens")]
        public float noiseSpeed = 1f;

        [Header("Zeitverlauf")]
        [Tooltip("Einblendzeit in Sekunden")]
        public float fadeIn = 0.4f;
        [Tooltip("Ausblendzeit in Sekunden")]
        public float fadeOut = 0.4f;
        [Tooltip("Zufaelliger Zeitversatz, damit nicht alles synchron wackelt")]
        public bool randomPhase = true;

        [Header("Geschwindigkeitsabhaengig")]
        [Tooltip("Amplitude mit der Bewegungsgeschwindigkeit skalieren")]
        public bool scaleWithSpeed = false;
        [Tooltip("Geschwindigkeit, ab der die volle Amplitude erreicht ist (m/s)")]
        public float speedForFullAmplitude = 5f;
    }

    /// <summary>
    /// Komplettes Kamera-Animationsprofil. Erstelle im Editor via:
    /// Assets > Create > WAD > Camera Animation Profile
    ///
    /// Danach im CameraAnimationController zuweisen - kein Code noetig.
    /// </summary>
    [CreateAssetMenu(fileName = "CameraAnimProfile_", menuName = "WAD/Camera Animation Profile")]
    public class CameraAnimationProfile : ScriptableObject
    {
        [Header("Globale Skalierung")]
        [Tooltip("Master-Multiplikator fuer alle Schichten (zum Abschwaechen des Gesamteindrucks)")]
        [Range(0f, 2f)] public float globalIntensity = 1f;

        [Header("--- Idle (Atmen) ---")]
        public CameraMotionLayer idle = new CameraMotionLayer
        {
            label = "Idle Breathing",
            positionAmplitude = new Vector3(0.006f, 0.010f, 0.004f),
            positionFrequency = new Vector3(0.10f, 0.14f, 0.09f),
            rotationAmplitude = new Vector3(0.25f, 0.18f, 0.20f),
            rotationFrequency = new Vector3(0.09f, 0.12f, 0.07f),
            fadeIn = 0.8f,
            fadeOut = 0.4f
        };

        [Header("--- Drehen (Schwung) ---")]
        [Tooltip("Kamera laeuft der Mausbewegung leicht nach (Traegheit)")]
        public CameraMotionLayer turning = new CameraMotionLayer
        {
            label = "Turn Drag",
            positionAmplitude = new Vector3(0.010f, 0.004f, 0f),
            positionFrequency = new Vector3(1f, 1f, 1f),
            rotationAmplitude = new Vector3(0.6f, 1.1f, 0.8f),
            rotationFrequency = new Vector3(1f, 1f, 1f),
            useNoise = false,
            fadeIn = 0.15f,
            fadeOut = 0.25f
        };

        [Header("--- Gehen ---")]
        public CameraMotionLayer walkBob = new CameraMotionLayer
        {
            label = "Walk Bob",
            positionAmplitude = new Vector3(0.018f, 0.030f, 0.008f),
            positionFrequency = new Vector3(0.9f, 1.8f, 0.9f),
            rotationAmplitude = new Vector3(0.5f, 0.35f, 0.9f),
            rotationFrequency = new Vector3(1.8f, 0.9f, 0.9f),
            useNoise = false,
            fadeIn = 0.25f,
            fadeOut = 0.3f,
            scaleWithSpeed = true,
            speedForFullAmplitude = 2.8f
        };

        [Header("--- Sprinten ---")]
        public CameraMotionLayer sprintShake = new CameraMotionLayer
        {
            label = "Sprint Shake",
            positionAmplitude = new Vector3(0.035f, 0.055f, 0.020f),
            positionFrequency = new Vector3(1.4f, 2.4f, 1.2f),
            rotationAmplitude = new Vector3(1.3f, 0.8f, 1.8f),
            rotationFrequency = new Vector3(2.4f, 1.4f, 1.6f),
            fadeIn = 0.35f,
            fadeOut = 0.4f,
            scaleWithSpeed = true,
            speedForFullAmplitude = 5.2f
        };

        [Header("--- Ducken ---")]
        public CameraMotionLayer crouch = new CameraMotionLayer
        {
            label = "Crouch Sway",
            positionAmplitude = new Vector3(0.008f, 0.006f, 0.004f),
            positionFrequency = new Vector3(0.2f, 0.25f, 0.2f),
            rotationAmplitude = new Vector3(0.15f, 0.12f, 0.10f),
            rotationFrequency = new Vector3(0.18f, 0.20f, 0.16f),
            fadeIn = 0.5f,
            fadeOut = 0.4f
        };

        [Header("--- Nachladen ---")]
        [Tooltip("Kamera neigt sich beim Nachladen zur Waffe (wird vom Reload ausgeloest)")]
        public CameraMotionLayer reload = new CameraMotionLayer
        {
            label = "Reload Cradle",
            positionAmplitude = new Vector3(0.012f, -0.025f, 0.015f),
            positionFrequency = new Vector3(0.5f, 0.5f, 0.5f),
            rotationAmplitude = new Vector3(1.8f, 1.2f, 2.4f),
            rotationFrequency = new Vector3(0.5f, 0.5f, 0.5f),
            useNoise = false,
            fadeIn = 0.2f,
            fadeOut = 0.35f
        };

        [Header("--- Landung ---")]
        [Tooltip("Kurzer Kamera-Stoss bei der Landung nach einem Sprung")]
        public CameraMotionLayer landingImpact = new CameraMotionLayer
        {
            label = "Landing Impact",
            positionAmplitude = new Vector3(0f, -0.06f, 0f),
            positionFrequency = new Vector3(1f, 1f, 1f),
            rotationAmplitude = new Vector3(3.5f, 0f, 0f),
            rotationFrequency = new Vector3(1f, 1f, 1f),
            useNoise = false,
            fadeIn = 0.05f,
            fadeOut = 0.45f
        };

        [Header("--- Zielen (ADS) ---")]
        [Tooltip("Beim Zielen werden die Wackel-Schichten beruhigt")]
        [Range(0f, 1f)] public float adsMotionDampening = 0.45f;
        [Tooltip("Zusaetzliches leichtes Zittern beim Zielen (Atem anhalten)")]
        public CameraMotionLayer adsBreathHold = new CameraMotionLayer
        {
            label = "ADS Breath Hold",
            positionAmplitude = new Vector3(0.002f, 0.003f, 0.001f),
            positionFrequency = new Vector3(0.35f, 0.42f, 0.30f),
            rotationAmplitude = new Vector3(0.12f, 0.10f, 0.08f),
            rotationFrequency = new Vector3(0.32f, 0.38f, 0.28f),
            fadeIn = 0.3f,
            fadeOut = 0.3f
        };

        [Header("--- Verletzung ---")]
        [Tooltip("Ab diesem Gesundheitswert beginnt das Taumeln")]
        [Range(0f, 1f)] public float lowHealthThreshold = 0.35f;
        public CameraMotionLayer lowHealthStagger = new CameraMotionLayer
        {
            label = "Low Health Stagger",
            positionAmplitude = new Vector3(0.02f, 0.012f, 0.02f),
            positionFrequency = new Vector3(0.5f, 0.4f, 0.45f),
            rotationAmplitude = new Vector3(1.6f, 1.1f, 2.2f),
            rotationFrequency = new Vector3(0.45f, 0.55f, 0.38f),
            fadeIn = 1.2f,
            fadeOut = 0.8f
        };

        public CameraMotionLayer[] AllLayers()
        {
            return new[] { idle, turning, walkBob, sprintShake, crouch, reload, landingImpact, adsBreathHold, lowHealthStagger };
        }
    }
}
