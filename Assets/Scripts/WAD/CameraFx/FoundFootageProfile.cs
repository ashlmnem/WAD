using UnityEngine;

namespace WAD.CameraFx
{
    /// <summary>
    /// Optik-Einstellungen fuer den "Found Footage"-Look. Erstelle im Editor via:
    /// Assets > Create > WAD > Found Footage Profile
    ///
    /// Die Werte werden zur Laufzeit auf einen URP-Volume-Stack angewendet.
    /// Mehrere Profile koennen sich ueberlagern (z.B. Level-Basis + Cutscene).
    /// </summary>
    [CreateAssetMenu(fileName = "Footage_", menuName = "WAD/Found Footage Profile")]
    public class FoundFootageProfile : ScriptableObject
    {
        [Header("Filmkorn")]
        [Tooltip("Kornstaerke (0 = aus)")]
        [Range(0f, 1f)] public float grainIntensity = 0.45f;
        [Tooltip("Korngroesse / Film-ISO")]
        [Range(0.3f, 3f)] public float grainResponse = 1.2f;
        public bool grainEnabled = true;

        [Header("Vignette")]
        public bool vignetteEnabled = true;
        [Range(0f, 1f)] public float vignetteIntensity = 0.42f;
        [Range(0.01f, 1f)] public float vignetteSmoothness = 0.45f;
        public Color vignetteColor = Color.black;

        [Header("Farbanpassung")]
        public bool colorEnabled = true;
        public float postExposure;
        [Range(-100f, 100f)] public float contrast = 12f;
        [Range(-100f, 100f)] public float saturation = -22f;
        [Range(-180f, 180f)] public float hueShift = -6f;
        public Color colorFilter = new Color(1f, 0.96f, 0.88f);

        [Header("Kamera-Artefakte")]
        public bool chromaticAberrationEnabled = true;
        [Range(0f, 1f)] public float chromaticAberration = 0.12f;

        public bool lensDistortionEnabled = false;
        [Range(-1f, 1f)] public float lensIntensity = -0.08f;

        public bool bloomEnabled = true;
        [Range(0f, 3f)] public float bloomIntensity = 0.35f;
        public float bloomThreshold = 1.1f;

        [Header("Bewegungsunschaerfe")]
        public bool motionBlurEnabled = false;
        [Range(0f, 1f)] public float motionBlurIntensity = 0.35f;

        [Header("Kamera-Bewegung")]
        [Tooltip("Zusaetzliches Handheld-Wackeln, das dieses Profil erzwingt (0 = aus)")]
        [Range(0f, 3f)] public float handheldIntensity = 0f;
    }
}
