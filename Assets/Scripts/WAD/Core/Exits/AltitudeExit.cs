using UnityEngine;

namespace WAD.Core
{
    [CreateAssetMenu(fileName = "Exit_Altitude_", menuName = "WAD/Exit Conditions/Altitude")]
    public class AltitudeExit : ExitCondition
    {
        public float requiredAltitudeMeters = 2500f;

        private float spawnHeight;
        private bool spawnCaptured;

        public override bool IsSatisfied(RunStateManager state) => false;

        public bool CheckAltitude(Vector3 playerPosition)
        {
            if (!spawnCaptured)
            {
                spawnHeight = playerPosition.y;
                spawnCaptured = true;
            }

            return (playerPosition.y - spawnHeight) >= requiredAltitudeMeters;
        }
    }
}
