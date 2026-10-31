using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;

namespace WAD.CameraFx
{
    /// <summary>
    /// Wendet ein FoundFootageProfile auf einen URP-Volume-Stack an und sorgt
    /// fuer den "Found Footage"-Look (Korn, Vignette, Farbstich, Artefakte).
    ///
    /// Zugriff auf die URP-Overrides erfolgt ueber Reflexion, damit das Skript
    /// auch nach einem URP-Update weiter kompiliert (die URP-Volumes-API hat
    /// sich zwischen Versionen mehrfach geaendert).
    ///
    /// Verwendung: auf ein GameObject im Level legen und ein Profil zuweisen.
    /// Fuer Cutscenes kann per PlayProfile()/BlendBack() umgeschaltet werden.
    /// </summary>
    [RequireComponent(typeof(Volume))]
    public class FoundFootageController : MonoBehaviour
    {
        [Header("Profil")]
        [Tooltip("Standard-Look dieses Levels")]
        public FoundFootageProfile profile;

        [Header("Verhalten")]
        [Tooltip("Beim Start automatisch anwenden")]
        public bool applyOnStart = true;
        [Tooltip("Beim Zerstoeren die Overrides wieder entfernen (z.B. beim Szenenwechsel)")]
        public bool cleanupOnDestroy = true;
        [Tooltip("Prioritaet des erzeugten Volume (hoeher = staerker als das Level-Volume)")]
        public float volumePriority = 100f;

        [Header("Uebergaenge")]
        [Tooltip("Dauer fuer weiches Umschalten zwischen Profilen (Sekunden)")]
        public float blendDuration = 1.5f;

        private Volume volume;
        private FoundFootageProfile currentProfile;
        private FoundFootageProfile targetProfile;
        private float blendTimer;
        private bool blending;

        // Erzeugte Override-Komponenten
        private readonly Dictionary<Type, VolumeComponent> createdOverrides = new Dictionary<Type, VolumeComponent>();

        // Reflexions-Caches
        private sealed class OverrideBinding
        {
            public Type Type;
            public string TypeName;
            public Dictionary<string, string> ParameterMap = new Dictionary<string, string>();
        }

        private static readonly List<OverrideBinding> Bindings = new List<OverrideBinding>();
        private static bool bindingsReady;

        private void Awake()
        {
            volume = GetComponent<Volume>();
            EnsureBindings();
        }

        private void Start()
        {
            if (applyOnStart) ApplyProfile(profile, instant: true);
        }

        private void OnDestroy()
        {
            if (!cleanupOnDestroy) return;

            foreach (var kvp in createdOverrides)
            {
                if (kvp.Value != null) Destroy(kvp.Value);
            }
            createdOverrides.Clear();
        }

        private void Update()
        {
            if (!blending || targetProfile == null) return;

            blendTimer += Time.deltaTime;
            float t = blendDuration > 0f ? Mathf.Clamp01(blendTimer / blendDuration) : 1f;

            ApplyProfileBlended(currentProfile, targetProfile, t);

            if (t >= 1f)
            {
                blending = false;
                currentProfile = targetProfile;
            }
        }

        /// <summary> Setzt sofort ein Profil. </summary>
        public void ApplyProfile(FoundFootageProfile newProfile, bool instant = false)
        {
            if (instant || blendDuration <= 0f)
            {
                currentProfile = newProfile;
                targetProfile = newProfile;
                blending = false;
                ApplyProfileBlended(newProfile, newProfile, 1f);
                return;
            }

            targetProfile = newProfile;
            blendTimer = 0f;
            blending = true;
        }

        /// <summary> Blendet zu einem Cutscene-Look und spaeter wieder zurueck. </summary>
        public void PlayProfile(FoundFootageProfile cutsceneProfile) => ApplyProfile(cutsceneProfile);
        public void BlendBack() => ApplyProfile(profile);

        // ------------------------------------------------------------------
        //  Anwenden
        // ------------------------------------------------------------------

        private void ApplyProfileBlended(FoundFootageProfile from, FoundFootageProfile to, float t)
        {
            if (to == null) return;

            ApplyGrain(from, to, t);
            ApplyVignette(from, to, t);
            ApplyColor(from, to, t);
            ApplyChromatic(from, to, t);
            ApplyLens(from, to, t);
            ApplyBloom(from, to, t);
            ApplyMotionBlur(from, to, t);
        }

        private void ApplyGrain(FoundFootageProfile from, FoundFootageProfile to, float t)
        {
            if (!to.grainEnabled)
            {
                Disable("FilmGrain");
                return;
            }

            var c = EnsureOverride("FilmGrain");
            if (c == null) return;

            SetParam(c, "intensity", Mathf.Lerp(Safe(from, f => f.grainIntensity, to.grainIntensity), to.grainIntensity, t));
            SetParam(c, "response", Mathf.Lerp(Safe(from, f => f.grainResponse, to.grainResponse), to.grainResponse, t));
            SetEnumParam(c, "type", "Custom");
            SetBoolParam(c, "intensity", true);
        }

        private void ApplyVignette(FoundFootageProfile from, FoundFootageProfile to, float t)
        {
            if (!to.vignetteEnabled) { Disable("Vignette"); return; }

            var c = EnsureOverride("Vignette");
            if (c == null) return;

            SetParam(c, "intensity", Mathf.Lerp(Safe(from, f => f.vignetteIntensity, to.vignetteIntensity), to.vignetteIntensity, t));
            SetParam(c, "smoothness", Mathf.Lerp(Safe(from, f => f.vignetteSmoothness, to.vignetteSmoothness), to.vignetteSmoothness, t));
            SetColorParam(c, "color", to.vignetteColor);
        }

        private void ApplyColor(FoundFootageProfile from, FoundFootageProfile to, float t)
        {
            if (!to.colorEnabled) { Disable("ColorAdjustments"); return; }

            var c = EnsureOverride("ColorAdjustments");
            if (c == null) return;

            SetParam(c, "postExposure", Mathf.Lerp(Safe(from, f => f.postExposure, to.postExposure), to.postExposure, t));
            SetParam(c, "contrast", Mathf.Lerp(Safe(from, f => f.contrast, to.contrast), to.contrast, t));
            SetParam(c, "saturation", Mathf.Lerp(Safe(from, f => f.saturation, to.saturation), to.saturation, t));
            SetParam(c, "hueShift", Mathf.Lerp(Safe(from, f => f.hueShift, to.hueShift), to.hueShift, t));
            SetColorParam(c, "colorFilter", to.colorFilter);
        }

        private void ApplyChromatic(FoundFootageProfile from, FoundFootageProfile to, float t)
        {
            if (!to.chromaticAberrationEnabled) { Disable("ChromaticAberration"); return; }

            var c = EnsureOverride("ChromaticAberration");
            if (c == null) return;

            SetParam(c, "intensity", Mathf.Lerp(Safe(from, f => f.chromaticAberration, to.chromaticAberration), to.chromaticAberration, t));
        }

        private void ApplyLens(FoundFootageProfile from, FoundFootageProfile to, float t)
        {
            if (!to.lensDistortionEnabled) { Disable("LensDistortion"); return; }

            var c = EnsureOverride("LensDistortion");
            if (c == null) return;

            SetParam(c, "intensity", Mathf.Lerp(Safe(from, f => f.lensIntensity, to.lensIntensity), to.lensIntensity, t));
        }

        private void ApplyBloom(FoundFootageProfile from, FoundFootageProfile to, float t)
        {
            if (!to.bloomEnabled) { Disable("Bloom"); return; }

            var c = EnsureOverride("Bloom");
            if (c == null) return;

            SetParam(c, "intensity", Mathf.Lerp(Safe(from, f => f.bloomIntensity, to.bloomIntensity), to.bloomIntensity, t));
            SetParam(c, "threshold", Mathf.Lerp(Safe(from, f => f.bloomThreshold, to.bloomThreshold), to.bloomThreshold, t));
        }

        private void ApplyMotionBlur(FoundFootageProfile from, FoundFootageProfile to, float t)
        {
            if (!to.motionBlurEnabled) { Disable("MotionBlur"); return; }

            var c = EnsureOverride("MotionBlur");
            if (c == null) return;

            SetParam(c, "intensity", Mathf.Lerp(Safe(from, f => f.motionBlurIntensity, to.motionBlurIntensity), to.motionBlurIntensity, t));
        }

        private static float Safe(FoundFootageProfile p, Func<FoundFootageProfile, float> getter, float fallback)
        {
            if (p == null) return fallback;
            try { return getter(p); }
            catch { return fallback; }
        }

        // ------------------------------------------------------------------
        //  Volume-Override-Verwaltung (reflexionsbasiert)
        // ------------------------------------------------------------------

        private static void EnsureBindings()
        {
            if (bindingsReady) return;
            bindingsReady = true;

            // Bekannte URP-Override-Typnamen und ihre Property-Namen
            AddBinding("FilmGrain", "intensity", "response", "type", "texture");
            AddBinding("Vignette", "color", "center", "intensity", "smoothness", "rounded");
            AddBinding("ColorAdjustments", "postExposure", "contrast", "colorFilter", "hueShift", "saturation");
            AddBinding("ChromaticAberration", "intensity");
            AddBinding("LensDistortion", "intensity", "xMultiplier", "yMultiplier", "center", "scale");
            AddBinding("Bloom", "threshold", "intensity", "scatter", "tint", "highQualityFiltering");
            AddBinding("MotionBlur", "intensity", "quality", "clamp");
        }

        private static void AddBinding(string typeName, params string[] parameters)
        {
            var binding = new OverrideBinding { TypeName = typeName };
            foreach (var p in parameters) binding.ParameterMap[p] = p;
            Bindings.Add(binding);
        }

        private static Type FindOverrideType(string shortName)
        {
            // In UnityEngine.Rendering.Universal liegt z.B. FilmGrain
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    foreach (var t in asm.GetTypes())
                    {
                        if (t.Name == shortName && typeof(VolumeComponent).IsAssignableFrom(t)) return t;
                    }
                }
                catch { /* Assembly ohne lesbare Typen ueberspringen */ }
            }
            return null;
        }

        private VolumeComponent EnsureOverride(string shortName)
        {
            foreach (var b in Bindings)
            {
                if (b.TypeName != shortName) continue;

                b.Type ??= FindOverrideType(shortName);
                if (b.Type == null) return null;

                if (createdOverrides.TryGetValue(b.Type, out var existing) && existing != null) return existing;

                var comp = volume.profile.Add(b.Type, true);
                if (comp == null) return null;

                comp.active = true;
                createdOverrides[b.Type] = comp;
                return comp;
            }
            return null;
        }

        private void Disable(string shortName)
        {
            foreach (var b in Bindings)
            {
                if (b.TypeName != shortName) continue;
                b.Type ??= FindOverrideType(shortName);
                if (b.Type == null) continue;

                if (createdOverrides.TryGetValue(b.Type, out var comp) && comp != null)
                {
                    comp.active = false;
                }
                return;
            }
        }

        private static void SetParam(VolumeComponent comp, string name, float value)
        {
            var field = comp.GetType().GetField(name, BindingFlags.Public | BindingFlags.Instance);
            if (field == null) return;

            object param = field.GetValue(comp);
            if (param == null) return;

            var valueField = param.GetType().GetField("m_Value", BindingFlags.NonPublic | BindingFlags.Instance)
                             ?? param.GetType().GetField("value", BindingFlags.Public | BindingFlags.Instance);
            if (valueField == null) return;

            valueField.SetValue(param, value);
            field.SetValue(comp, param);
        }

        private static void SetColorParam(VolumeComponent comp, string name, Color value)
        {
            var field = comp.GetType().GetField(name, BindingFlags.Public | BindingFlags.Instance);
            if (field == null) return;

            object param = field.GetValue(comp);
            if (param == null) return;

            var valueField = param.GetType().GetField("m_Value", BindingFlags.NonPublic | BindingFlags.Instance)
                             ?? param.GetType().GetField("value", BindingFlags.Public | BindingFlags.Instance);
            if (valueField == null) return;

            valueField.SetValue(param, value);
            field.SetValue(comp, param);
        }

        private static void SetBoolParam(VolumeComponent comp, string name, bool value)
        {
            var field = comp.GetType().GetField(name, BindingFlags.Public | BindingFlags.Instance);
            if (field == null) return;

            object param = field.GetValue(comp);
            if (param == null) return;

            // BoolParameter heisst ebenfalls m_Value
            var valueField = param.GetType().GetField("m_Value", BindingFlags.NonPublic | BindingFlags.Instance);
            if (valueField == null) return;

            if (valueField.FieldType == typeof(bool)) valueField.SetValue(param, value);
        }

        private static void SetEnumParam(VolumeComponent comp, string name, string enumValueName)
        {
            var field = comp.GetType().GetField(name, BindingFlags.Public | BindingFlags.Instance);
            if (field == null) return;

            object param = field.GetValue(comp);
            if (param == null) return;

            var valueField = param.GetType().GetField("m_Value", BindingFlags.NonPublic | BindingFlags.Instance);
            if (valueField == null) return;

            try { valueField.SetValue(param, Enum.Parse(valueField.FieldType, enumValueName, true)); }
            catch { /* Enum-Wert existiert in dieser URP-Version nicht */ }
        }
    }
}
