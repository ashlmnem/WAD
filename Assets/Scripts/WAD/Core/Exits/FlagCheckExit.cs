using UnityEngine;

namespace WAD.Core
{
    [CreateAssetMenu(fileName = "Exit_FlagCheck_", menuName = "WAD/Exit Conditions/Flag Check")]
    public class FlagCheckExit : ExitCondition
    {
        public string requiredFlag;
        public float additionalWaitSeconds = 300f;
        private float elapsed;

        public override bool IsSatisfied(RunStateManager state)
        {
            return state.HasFlag(requiredFlag) && elapsed >= additionalWaitSeconds;
        }

        public void Tick(float deltaTime) => elapsed += deltaTime;
    }
}
