using UnityEngine;

namespace WAD.Core
{
    [CreateAssetMenu(fileName = "Exit_KillCount_", menuName = "WAD/Exit Conditions/Kill Count")]
    public class KillCountExit : ExitCondition
    {
        public string requiredEnemyTag = "Illuminate";
        public int requiredKills = 4;
        private int currentKills;

        public override bool IsSatisfied(RunStateManager state)
        {
            return currentKills >= requiredKills;
        }

        public void RegisterKill(string enemyTag)
        {
            if (enemyTag == requiredEnemyTag) currentKills++;
        }
    }
}
