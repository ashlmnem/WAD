using UnityEngine;

namespace WAD.Core
{
    [CreateAssetMenu(fileName = "Exit_Distance_", menuName = "WAD/Exit Conditions/Distance From Spawn")]
    public class DistanceFromSpawnExit : ExitCondition
    {
        public float requiredDistanceMeters = 15000f;
        [Tooltip("Richtung relativ zum Spawn - Standard: Norden (+Z)")]
        public Vector3 direction = Vector3.forward;

        private Vector3 spawnPosition;
        private bool spawnCaptured;

        public override bool IsSatisfied(RunStateManager state) => false;

        public bool CheckDistance(Vector3 playerPosition)
        {
            if (!spawnCaptured)
            {
                spawnPosition = playerPosition;
                spawnCaptured = true;
            }

            Vector3 offset = playerPosition - spawnPosition;
            float distanceInDirection = Vector3.Dot(offset, direction.normalized);
            return distanceInDirection >= requiredDistanceMeters;
        }
    }
}
