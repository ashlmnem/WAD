using UnityEngine;

namespace WAD.Core
{
    /// <summary>
    /// Abstrakte Basis fuer eine Exit-Bedingung eines Levels.
    /// </summary>
    public abstract class ExitCondition : ScriptableObject
    {
        [Header("Basis")]
        public string exitLabel;
        public int targetLevelIndex;
        public bool returnsToHideout = false;

        [Header("Outro-Cutscene (optional)")]
        [Tooltip("Wird abgespielt, wenn dieser Exit ausgeloest wird - VOR einem eventuellen Szenenwechsel. " +
                 "Leer = keine Cutscene, der Exit wirkt sofort.")]
        public WAD.Cutscenes.CutsceneSequence outroCutscene;

        /// <summary> Wurde dieser Exit in diesem Level bereits ausgeloest? </summary>
        public bool HasTriggered { get; private set; }

        public abstract bool IsSatisfied(RunStateManager state);

        /// <summary>
        /// Setzt den Laufzeit-Zustand dieser Bedingung zurueck. WICHTIG: Die
        /// Laufzeit-Felder der Unterklassen liegen in ScriptableObject-ASSETS,
        /// die Unity im Editor ueber Play-Sessions hinweg speichert. Ohne diesen
        /// Reset startet der zweite Playtest mit dem Spawn-Punkt bzw. dem
        /// "used"-Flag des vorherigen Runs - Exits feuern dann sofort oder nie.
        /// Wird von LevelExitController beim Levelstart aufgerufen.
        /// </summary>
        public virtual void ResetState()
        {
            HasTriggered = false;
        }

        /// <summary> Von LevelExitController aufgerufen, NACHDEM die Cutscene durch ist. </summary>
        public virtual void OnTriggered(RunStateManager state)
        {
            HasTriggered = true;

            if (state == null) return;

            if (returnsToHideout)
            {
                state.OnExtraction();
            }
            else
            {
                state.AdvanceToLevel(targetLevelIndex);
            }
        }
    }
}