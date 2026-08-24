using UnityEngine;

namespace WAD.Core
{
    [CreateAssetMenu(fileName = "Exit_Chance_", menuName = "WAD/Exit Conditions/Chance Based")]
    public class ChanceBasedExit : ExitCondition
    {
        [Range(0f, 1f)]
        public float failureChance = 1f / 6f;
        public bool failureIsLethal = true;

        public bool AttemptRoll(RunStateManager state)
        {
            bool failed = Random.value < failureChance;
            if (failed && failureIsLethal)
            {
                state.OnDeath();
                return false;
            }
            return true;
        }

        public override bool IsSatisfied(RunStateManager state) => false;
    }
}
