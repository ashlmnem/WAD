using UnityEngine;

namespace WAD.Vehicles
{
    /// <summary>
    /// Loest die Startsequenz-Cutscene aus, wenn der Spieler einen Jet zum
    /// ersten Mal besteigt (der "erste Flug"-Moment).
    ///
    /// Auf dasselbe Objekt wie JetController legen und eine Cutscene zuweisen.
    /// Es sind keine Code-Aenderungen noetig, um die Cutscene zu ersetzen.
    /// </summary>
    [RequireComponent(typeof(JetController))]
    public class JetCutsceneTrigger : MonoBehaviour
    {
        [Header("Cutscenes")]
        [Tooltip("Wird abgespielt, wenn dieser Jet das ERSTE Mal bestiegen wird")]
        public WAD.Cutscenes.CutsceneSequence firstBoardingCutscene;
        [Tooltip("Wird bei jedem weiteren Einsteigen abgespielt (kann leer bleiben)")]
        public WAD.Cutscenes.CutsceneSequence subsequentBoardingCutscene;
        [Tooltip("Wird abgespielt, sobald der Jet vollstaendig repariert ist")]
        public WAD.Cutscenes.CutsceneSequence repairedCutscene;

        [Header("Referenzen")]
        [Tooltip("Leer = automatisch in der Szene gesucht")]
        public WAD.Cutscenes.CutscenePlayer cutscenePlayer;
        public JetRepairSystem repairSystem;

        [Header("Verhalten")]
        [Tooltip("Nur einmal pro Spielsitzung abspielen")]
        public bool oncePerSession = true;
        [Tooltip("Erst nach der Cutscene das Steuer uebergeben (Spieler kann waehrend der Cutscene nicht fliegen)")]
        public bool delayControlUntilCutsceneDone = false;

        private static bool firstBoardingPlayedThisSession;
        private bool repairedCutscenePlayed;

        private void Awake()
        {
            if (repairSystem == null) repairSystem = GetComponent<JetRepairSystem>();
        }

        private void OnEnable()
        {
            var controller = GetComponent<JetController>();
            if (controller != null) controller.OnPilotEntered += HandlePilotEntered;

            if (repairSystem != null) repairSystem.OnRepaired += HandleRepaired;
        }

        private void OnDisable()
        {
            var controller = GetComponent<JetController>();
            if (controller != null) controller.OnPilotEntered -= HandlePilotEntered;

            if (repairSystem != null) repairSystem.OnRepaired -= HandleRepaired;
        }

        private void HandlePilotEntered(JetController jet)
        {
            var sequence = ResolveBoardingSequence();
            if (sequence == null) return;

            Play(sequence);
            if (oncePerSession) firstBoardingPlayedThisSession = true;
        }

        private WAD.Cutscenes.CutsceneSequence ResolveBoardingSequence()
        {
            bool firstTime = !firstBoardingPlayedThisSession;

            if (firstTime && firstBoardingCutscene != null) return firstBoardingCutscene;
            if (!firstTime && subsequentBoardingCutscene != null) return subsequentBoardingCutscene;
            if (subsequentBoardingCutscene == null && firstTime) return null;

            return null;
        }

        private void HandleRepaired()
        {
            if (repairedCutscenePlayed || repairedCutscene == null) return;
            repairedCutscenePlayed = true;
            Play(repairedCutscene);
        }

        private void Play(WAD.Cutscenes.CutsceneSequence sequence)
        {
            if (cutscenePlayer == null) cutscenePlayer = FindFirstObjectByType<WAD.Cutscenes.CutscenePlayer>();

            if (cutscenePlayer == null)
            {
                Debug.LogWarning($"[JetCutsceneTrigger:{name}] Cutscene '{sequence.displayName}' ist gesetzt, " +
                                 "aber es gibt keinen CutscenePlayer in der Szene.");
                return;
            }

            cutscenePlayer.Play(sequence);
        }
    }
}
