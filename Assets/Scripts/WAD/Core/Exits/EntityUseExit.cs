using UnityEngine;

namespace WAD.Core
{
    [CreateAssetMenu(fileName = "Exit_EntityUse_", menuName = "WAD/Exit Conditions/Entity Use")]
    public class EntityUseExit : ExitCondition
    {
        public EntitySO requiredEntity;
        private bool used;

        public override bool IsSatisfied(RunStateManager state) => used;

        public void MarkUsed() => used = true;
    }
}
