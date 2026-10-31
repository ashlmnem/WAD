using System;
using UnityEngine;

namespace WAD.Feedback
{
    /// <summary>
    /// Alle Aktionen, denen ein Sound / eine Animation / ein Effekt zugeordnet
    /// werden kann. Diese Liste ist die gemeinsame "Sprache" von Spieler,
    /// Waffen und Gegnern.
    /// </summary>
    public enum ActionId
    {
        // --- Waffe ---
        WeaponDraw,
        WeaponHolster,
        WeaponSwitch,
        FireHip,
        FireADS,
        FireSuppressed,
        DryFire,
        ReloadStartEmpty,
        ReloadStartPartial,
        MagazineOut,
        MagazineIn,
        ReloadEnd,
        BoltCycle,
        InspectStart,
        InspectEnd,
        ChargeHandle,

        // --- Spieler-Bewegung ---
        FootstepWalk,
        FootstepSprint,
        FootstepCrouch,
        Jump,
        Land,
        LandHard,
        CrouchDown,
        CrouchUp,
        LeanStart,
        LeanEnd,
        SprintStart,
        SprintEnd,
        StaminaExhausted,

        // --- Spieler-Zustand ---
        PlayerHurtLight,
        PlayerHurtHeavy,
        PlayerDeath,
        HealStart,
        HealEnd,

        // --- Interaktion ---
        PickupItem,
        PickupWeapon,
        PickupMagazine,
        InteractStart,
        InteractEnd,
        DoorOpen,
        LootOpen,
        LootClose,

        // --- Gegner ---
        EnemyIdle,
        EnemySpottedPlayer,
        EnemyAlerted,
        EnemyHurt,
        EnemyDeath,
        EnemyAttackMelee,
        EnemyAttackRanged,
        EnemyReload,
        EnemySuppressed,
        EnemyCoverEnter,
        EnemyCoverExit,
        EnemyFootstep,

        // --- Jet / Fahrzeug ---
        /// <summary>Triebwerke fahren hoch (Startsequenz).</summary>
        JetEngineStart,
        /// <summary>Triebwerke fahren herunter (Aussteigen).</summary>
        JetEngineShutdown,
        /// <summary>Dauerschleife des Triebwerks (wird als Loop-AudioSource gespielt).</summary>
        JetEngineLoop,
        /// <summary>Nachbrenner zuenden.</summary>
        JetAfterburner,
        /// <summary>Abheben.</summary>
        JetTakeoff,
        /// <summary>Aufsetzen / Landung.</summary>
        JetLanding,
        /// <summary>Rakete abgefeuert.</summary>
        JetRocketFire,
        /// <summary>Aufprall / Kollision mit der Flugzelle.</summary>
        JetCrash,
        /// <summary>Fahrwerk ausfahren.</summary>
        JetGearDeploy,
        /// <summary>Fahrwerk einfahren.</summary>
        JetGearRetract,
        /// <summary>Reparatur vollstaendig abgeschlossen.</summary>
        JetRepairComplete,
        /// <summary>Ein Ersatzteil wurde abgeliefert.</summary>
        JetRepairProgress,
        /// <summary>Reparatur abgelehnt (Teile fehlen).</summary>
        JetRepairDenied
    }

    /// <summary>
    /// Ein einzelner Feedback-Eintrag: welche Aktion, welcher Sound, welche
    /// Animation, welcher Effekt. Alles optional - leere Felder werden ignoriert.
    /// </summary>
    [Serializable]
    public class ActionFeedback
    {
        public ActionId action;

        [Header("Audio")]
        [Tooltip("Zufaelliger Clip aus dieser Liste wird abgespielt")]
        public AudioClip[] sounds;

        [Range(0f, 1f)] public float volume = 1f;
        [Tooltip("Minimale/maximale Pitch-Variation, damit sich Wiederholungen natuerlicher anhoeren")]
        public Vector2 pitchRange = new Vector2(0.97f, 1.03f);

        [Header("Animation")]
        [Tooltip("Animator-Trigger, der ausgeloest wird (muss im Animator Controller existieren)")]
        public string animatorTrigger;
        [Tooltip("Animator-Bool, der gesetzt wird")]
        public string animatorBool;
        public bool animatorBoolValue = true;

        [Header("VFX")]
        [Tooltip("Prefab, das an 'effectSpawnPoint' erzeugt und nach 'effectLifetime' zerstoert wird")]
        public GameObject effectPrefab;
        public float effectLifetime = 2f;

        [Header("Timing")]
        [Tooltip("Verzoegerung in Sekunden, bevor Sound/Animation ausgeloest werden")]
        public float delay;
    }

    /// <summary>
    /// Zentrale, datengetriebene Feedback-Station. Auf JEDES Objekt legen, das
    /// Aktionen hat (Spieler, Waffe, Gegner). Im Inspector pro Aktion Sounds,
    /// Animator-Trigger und Effekte eintragen - kein Code noetig.
    ///
    /// Aufruf aus anderen Skripten: feedback.Play(ActionId.ReloadStartEmpty);
    /// </summary>
    public class ActionFeedbackPlayer : MonoBehaviour
    {
        [Header("Eintraege")]
        [Tooltip("Pro Aktion ein Eintrag. Mehrere Eintraege mit derselben Aktion sind erlaubt - alle werden abgespielt.")]
        public ActionFeedback[] feedbacks = Array.Empty<ActionFeedback>();

        [Header("Referenzen")]
        [Tooltip("Leer = AudioSource auf diesem Objekt, wird sonst automatisch ergaenzt")]
        public AudioSource audioSource;
        [Tooltip("Leer = Animator auf diesem Objekt oder in den Kindern")]
        public Animator animator;
        [Tooltip("Punkt, an dem VFX erzeugt werden. Leer = dieses Transform.")]
        public Transform effectSpawnPoint;

        [Header("Verhalten")]
        [Tooltip("Sounds ueber 3D-Raum hoeren (Gegner). Aus = 2D (eigene Waffe).")]
        public bool spatialSounds = false;
        [Tooltip("Fehlende Eintraege im Konsolenlog melden (beim Einrichten hilfreich).")]
        public bool logMissingActions = false;

        [Header("Dauerschleife (z.B. Triebwerk)")]
        [Tooltip("Eigene AudioSource fuer Schleifen-Sounds. Leer = wird automatisch ergaenzt.")]
        public AudioSource loopSource;
        [Tooltip("Lautstaerke der Schleife")]
        [Range(0f, 1f)] public float loopVolume = 1f;
        [Tooltip("Wie schnell die Schleife ein-/ausgeblendet wird (Sekunden)")]
        public float loopFadeSeconds = 1.5f;

        private Coroutine loopRoutine;
        private float loopTargetVolume;

        private void Awake()
        {
            if (animator == null) animator = GetComponentInChildren<Animator>();
            if (effectSpawnPoint == null) effectSpawnPoint = transform;

            if (audioSource == null) audioSource = GetComponent<AudioSource>();
            if (audioSource == null && HasAnySound())
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
            }

            if (audioSource != null) audioSource.spatialBlend = spatialSounds ? 1f : 0f;
        }

        private bool HasAnySound()
        {
            foreach (var f in feedbacks)
            {
                if (f != null && f.sounds != null && f.sounds.Length > 0) return true;
            }
            return false;
        }

        /// <summary> Spielt alle Eintraege fuer diese Aktion ab. </summary>
        public void Play(ActionId action)
        {
            bool found = false;

            foreach (var entry in feedbacks)
            {
                if (entry == null || entry.action != action) continue;
                found = true;
                Trigger(entry);
            }

            if (!found && logMissingActions)
            {
                Debug.Log($"[ActionFeedback:{name}] Keine Zuordnung fuer '{action}' - im Inspector ergaenzen.");
            }
        }

        /// <summary>
        /// True, wenn fuer diese Aktion mindestens ein Sound eingetragen ist.
        /// Erlaubt es Altsystemen, auf ihre eigenen Clips zurueckzufallen,
        /// solange hier noch nichts eingetragen wurde.
        /// </summary>
        public bool HasSounds(ActionId action)
        {
            foreach (var entry in feedbacks)
            {
                if (entry != null && entry.action == action && entry.sounds != null && entry.sounds.Length > 0)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Aktion abspielen, wenn sie hier eingetragen ist - sonst die
        /// uebergebenen Ersatz-Clips benutzen. So funktionieren alte
        /// Inspector-Verdrahtungen weiter, waehrend das neue System Vorrang hat.
        /// </summary>
        public void PlayOrFallback(ActionId action, AudioClip[] fallbackClips, float fallbackVolume = 1f)
        {
            if (HasSounds(action))
            {
                Play(action);
                return;
            }

            PlayRandomFrom(fallbackClips, fallbackVolume);
        }

        /// <summary> Spielt einen zufaelligen Clip mit Pitch-Variation. </summary>
        public void PlayRandomFrom(AudioClip[] clips, float volume = 1f)
        {
            if (audioSource == null || clips == null || clips.Length == 0) return;

            AudioClip clip = clips[UnityEngine.Random.Range(0, clips.Length)];
            if (clip == null) return;

            audioSource.pitch = UnityEngine.Random.Range(0.95f, 1.05f);
            audioSource.PlayOneShot(clip, volume);
        }

        /// <summary> Animator-Trigger ohne Audio (fuer Animations-Slots). </summary>
        public void TriggerAnimation(ActionId action)
        {
            foreach (var entry in feedbacks)
            {
                if (entry == null || entry.action != action) continue;
                if (animator == null) continue;

                if (!string.IsNullOrEmpty(entry.animatorTrigger)) animator.SetTrigger(entry.animatorTrigger);
                if (!string.IsNullOrEmpty(entry.animatorBool)) animator.SetBool(entry.animatorBool, entry.animatorBoolValue);
            }
        }

        // ------------------------------------------------------------------
        //  Dauerschleife (Triebwerk, Generatoren, ...)
        // ------------------------------------------------------------------

        /// <summary>
        /// Startet eine Schleife fuer diese Aktion (z.B. Triebwerksbrummen).
        /// Der erste eingetragene Clip wird als Loop gespielt; die Lautstaerke
        /// wird weich eingeblendet.
        /// </summary>
        public void StartLoop(ActionId action)
        {
            AudioClip clip = FindFirstClip(action);
            if (clip == null) return;

            EnsureLoopSource();

            if (loopSource == null) return;

            loopSource.clip = clip;
            loopSource.loop = true;
            loopSource.pitch = 1f;
            loopSource.Play();

            FadeLoopTo(loopVolume);
        }

        /// <summary> Stoppt die Schleife weich. </summary>
        public void StopLoop()
        {
            if (loopSource == null) return;
            FadeLoopTo(0f, stopAfterFade: true);
        }

        /// <summary> Passt die Schleifenlautstaerke an (z.B. Drehzahl/Gas). </summary>
        public void SetLoopIntensity(float t01)
        {
            if (loopSource == null || !loopSource.isPlaying) return;

            float target = Mathf.Clamp01(t01) * loopVolume;
            loopTargetVolume = target;

            // Drehzahl auch ueber die Tonhoehe hoerbar machen
            loopSource.pitch = Mathf.Lerp(0.85f, 1.25f, Mathf.Clamp01(t01));
        }

        private AudioClip FindFirstClip(ActionId action)
        {
            foreach (var entry in feedbacks)
            {
                if (entry == null || entry.action != action) continue;
                if (entry.sounds == null) continue;

                foreach (var clip in entry.sounds)
                {
                    if (clip != null) return clip;
                }
            }
            return null;
        }

        private void EnsureLoopSource()
        {
            if (loopSource != null) return;

            loopSource = gameObject.AddComponent<AudioSource>();
            loopSource.playOnAwake = false;
            loopSource.loop = true;
            loopSource.volume = 0f;
            loopSource.spatialBlend = spatialSounds ? 1f : 0f;
        }

        private void FadeLoopTo(float target, bool stopAfterFade = false)
        {
            if (loopSource == null) return;

            loopTargetVolume = target;

            if (loopRoutine != null) StopCoroutine(loopRoutine);
            loopRoutine = StartCoroutine(FadeLoopRoutine(target, stopAfterFade));
        }

        private System.Collections.IEnumerator FadeLoopRoutine(float target, bool stopAfterFade)
        {
            float start = loopSource.volume;
            float duration = Mathf.Max(0.01f, loopFadeSeconds);
            float t = 0f;

            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                loopSource.volume = Mathf.Lerp(start, target, t / duration);
                yield return null;
            }

            loopSource.volume = target;

            if (stopAfterFade && target <= 0.001f)
            {
                loopSource.Stop();
                loopSource.clip = null;
            }

            loopRoutine = null;
        }

        private void Trigger(ActionFeedback entry)
        {
            if (entry.delay > 0f)
            {
                StartCoroutine(TriggerDelayed(entry, entry.delay));
                return;
            }
            TriggerNow(entry);
        }

        private System.Collections.IEnumerator TriggerDelayed(ActionFeedback entry, float delay)
        {
            yield return new WaitForSeconds(delay);
            TriggerNow(entry);
        }

        private void TriggerNow(ActionFeedback entry)
        {
            // --- Audio ---
            if (audioSource != null && entry.sounds != null && entry.sounds.Length > 0)
            {
                AudioClip clip = entry.sounds[UnityEngine.Random.Range(0, entry.sounds.Length)];
                if (clip != null)
                {
                    audioSource.pitch = UnityEngine.Random.Range(entry.pitchRange.x, entry.pitchRange.y);
                    audioSource.PlayOneShot(clip, entry.volume);
                }
            }

            // --- Animation ---
            if (animator != null)
            {
                if (!string.IsNullOrEmpty(entry.animatorTrigger)) animator.SetTrigger(entry.animatorTrigger);
                if (!string.IsNullOrEmpty(entry.animatorBool)) animator.SetBool(entry.animatorBool, entry.animatorBoolValue);
            }

            // --- VFX ---
            if (entry.effectPrefab != null)
            {
                GameObject fx = Instantiate(entry.effectPrefab, effectSpawnPoint.position, effectSpawnPoint.rotation);
                if (entry.effectLifetime > 0f) Destroy(fx, entry.effectLifetime);
            }
        }

#if UNITY_EDITOR
        /// <summary>
        /// Menue im Inspector: fuellt die Liste mit einem leeren Eintrag pro
        /// ActionId, damit man nur noch Sounds hineinziehen muss.
        /// </summary>
        [ContextMenu("Alle Aktionen als Eintraege anlegen")]
        private void PopulateAllActions()
        {
            var existing = new System.Collections.Generic.List<ActionFeedback>(feedbacks);
            foreach (ActionId id in Enum.GetValues(typeof(ActionId)))
            {
                if (existing.Exists(f => f != null && f.action == id)) continue;
                existing.Add(new ActionFeedback { action = id });
            }
            feedbacks = existing.ToArray();
        }
#endif
    }
}
