using UnityEngine;

namespace WAD.Core
{
    [CreateAssetMenu(fileName = "Exit_Time_", menuName = "WAD/Exit Conditions/Time Based")]
    public class TimeBasedExit : ExitCondition
    {
        public float requiredSeconds = 300f;
        private float elapsed;

        public override bool IsSatisfied(RunStateManager state)
        {
            return elapsed >= requiredSeconds;
        }

        public void Tick(float deltaTime) => elapsed += deltaTime;
        public void ResetTimer() => elapsed = 0f;
    }
}
