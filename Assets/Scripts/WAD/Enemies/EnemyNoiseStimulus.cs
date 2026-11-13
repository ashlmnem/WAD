using UnityEngine;

namespace WAD.Enemies
{
    public static class EnemyNoiseStimulus
    {
        public static event System.Action<Vector3, float, Transform> Emitted;

        public static void Emit(Vector3 position, float audibleRadius, Transform source)
        {
            if (audibleRadius <= 0f) return;
            Emitted?.Invoke(position, audibleRadius, source);
        }
    }
}