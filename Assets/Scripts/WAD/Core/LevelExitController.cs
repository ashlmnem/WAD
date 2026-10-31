using System.Collections.Generic;
using UnityEngine;

namespace WAD.Core
{
    /// <summary>
    /// Liegt einmal pro Level-Szene in der Welt. Kennt alle Exit-Bedingungen
    /// dieses Levels (aus LevelSO) und prueft sie laufend. Sobald eine erfuellt
    /// ist, wird automatisch der Levelwechsel ausgeloest.
    ///
    /// Fuer Level 1 konkret: 2 aktiv geprüfte Exits (Distanz, Höhe) + 1 durch
    /// Entity-001srp ausgeloester Exit (siehe EntityUseExit + Entity001srp.cs).
    /// </summary>
    public class LevelExitController : MonoBehaviour
    {
        [Header("Referenzen")]
        public LevelSO levelData;
        public Transform player;
        [Tooltip("Spielt die Outro-Cutscene eines Exits. Leer = automatisch gesucht.")]
        public WAD.Cutscenes.CutscenePlayer cutscenePlayer;

        [Header("Intro")]
        [Tooltip("Wird beim Levelstart abgespielt, bevor die Exits aktiv werden (kann leer bleiben)")]
        public WAD.Cutscenes.CutsceneSequence introCutscene;

        [Header("Szenenwechsel")]
        [Tooltip("Szene, die nach einem erfolgreichen Exit geladen wird. Leer = kein Wechsel (nur Run-State).")]
        public string nextSceneName = "";
        [Tooltip("Diese Szene laden, wenn der Exit 'returnsToHideout' ist. Leer = Hauptmenue.")]
        public string hideoutSceneName = "MainMenu";

        /// <summary>
        /// Merkt sich, welche Exits in DIESEM Level bereits ausgeloest haben.
        /// Vorher war das ein einzelnes globales bool, wodurch der erste
        /// ausgeloeste Exit alle anderen Exits des Levels dauerhaft deaktiviert hat.
        /// </summary>
        private readonly HashSet<ExitCondition> firedExits = new HashSet<ExitCondition>();

        private bool introDone;
        private bool transitionRunning;

        private void Awake()
        {
            if (cutscenePlayer == null) cutscenePlayer = FindFirstObjectByType<WAD.Cutscenes.CutscenePlayer>();
        }

        private void Start()
        {
            ResetExitStates();
            StartCoroutine(PlayIntroThenArm());
        }

        /// <summary>
        /// Spielt die Intro-Cutscene und gibt die Exits erst danach frei -
        /// sonst koennte ein Exit waehrend der Cutscene feuern.
        /// </summary>
        private System.Collections.IEnumerator PlayIntroThenArm()
        {
            if (introCutscene != null && cutscenePlayer != null)
            {
                cutscenePlayer.Play(introCutscene);

                // Warten, bis die Cutscene fertig ist
                while (cutscenePlayer.IsPlaying) yield return null;
            }

            introDone = true;
        }

        /// <summary>
        /// Setzt den Laufzeit-Zustand aller Exits zurueck. Noetig, weil die
        /// Bedingungen ScriptableObjects sind und Unity deren Felder im Editor
        /// ueber Play-Sessions hinweg speichert.
        /// </summary>
        private void ResetExitStates()
        {
            firedExits.Clear();
            transitionRunning = false;
            introDone = introCutscene == null || cutscenePlayer == null;

            if (levelData == null) return;

            foreach (var exit in levelData.exits)
            {
                exit?.ResetState();
            }
        }

        private void Update()
        {
            if (levelData == null || player == null) return;
            if (!introDone || transitionRunning) return;

            foreach (var exit in levelData.exits)
            {
                if (exit == null) continue;
                if (exit.HasTriggered || firedExits.Contains(exit)) continue;
                CheckExit(exit);
            }
        }

        private void CheckExit(ExitCondition exit)
        {
            switch (exit)
            {
                case DistanceFromSpawnExit distanceExit:
                    if (distanceExit.CheckDistance(player.position))
                        TriggerExit(exit);
                    break;

                case AltitudeExit altitudeExit:
                    if (altitudeExit.CheckAltitude(player.position))
                        TriggerExit(exit);
                    break;

                case TimeBasedExit timeExit:
                    timeExit.Tick(Time.deltaTime);
                    if (timeExit.IsSatisfied(RunStateManager.Instance))
                        TriggerExit(exit);
                    break;

                case FlagCheckExit flagExit:
                    flagExit.Tick(Time.deltaTime);
                    if (flagExit.IsSatisfied(RunStateManager.Instance))
                        TriggerExit(exit);
                    break;

                case EntityUseExit entityExit:
                    if (entityExit.IsSatisfied(RunStateManager.Instance))
                        TriggerExit(exit);
                    break;

                case KillCountExit killExit:
                    if (killExit.IsSatisfied(RunStateManager.Instance))
                        TriggerExit(exit);
                    break;

                // ChanceBasedExit wird NICHT hier automatisch geprueft - der wird
                // manuell von der Interaktion ausgeloest (z.B. "Russisches Roulette"-Trigger-Skript).
            }
        }

        private void TriggerExit(ExitCondition exit)
        {
            if (exit == null || firedExits.Contains(exit) || exit.HasTriggered) return;
            firedExits.Add(exit);

            Debug.Log($"[LevelExit] Exit ausgeloest: {exit.exitLabel}");
            StartCoroutine(RunExitSequence(exit));
        }

        /// <summary>
        /// Fuehrt den Exit aus: erst die Outro-Cutscene (falls gesetzt), dann
        /// den Run-State, dann - falls konfiguriert - den Szenenwechsel.
        /// Vorher war der Szenenwechsel nur ein TODO und nichts passierte.
        /// </summary>
        private System.Collections.IEnumerator RunExitSequence(ExitCondition exit)
        {
            transitionRunning = true;

            if (exit.outroCutscene != null)
            {
                if (cutscenePlayer == null) cutscenePlayer = FindFirstObjectByType<WAD.Cutscenes.CutscenePlayer>();

                if (cutscenePlayer != null)
                {
                    cutscenePlayer.Play(exit.outroCutscene);
                    while (cutscenePlayer.IsPlaying) yield return null;
                }
                else
                {
                    Debug.LogWarning($"[LevelExit] Exit '{exit.exitLabel}' hat eine Outro-Cutscene, aber es gibt keinen CutscenePlayer in der Szene.");
                }
            }

            exit.OnTriggered(RunStateManager.Instance);

            // Szenenwechsel nur, wenn ausdruecklich konfiguriert
            string target = exit.returnsToHideout ? hideoutSceneName : nextSceneName;

            if (!string.IsNullOrEmpty(target))
            {
                if (Application.CanStreamedLevelBeLoaded(target))
                {
                    UnityEngine.SceneManagement.SceneManager.LoadScene(target);
                    yield break; // Szenenwechsel beendet diese Coroutine
                }

                Debug.LogError($"[LevelExit] Szene '{target}' ist nicht ladbar - bitte in File > Build Settings aufnehmen. " +
                               $"Exit '{exit.exitLabel}' wirkt daher nur auf den Run-State.");
            }

            transitionRunning = false;
        }
    }
}