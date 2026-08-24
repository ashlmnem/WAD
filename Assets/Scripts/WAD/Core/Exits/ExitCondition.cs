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

        public abstract bool IsSatisfied(RunStateManager state);

        public virtual void OnTriggered(RunStateManager state)
        {
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