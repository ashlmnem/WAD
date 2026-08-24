using UnityEngine;

namespace WAD.Enemies
{
    /// <summary>
    /// Platziere dieses Skript auf leere Kind-Objekte in Chunk-Prefabs, um
    /// moegliche Gegner-Spawn-Positionen zu markieren. Analog zu LootSpawnPoint.
    /// </summary>
    public class EnemySpawnPoint : MonoBehaviour
    {
        private void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, 0.5f);
        }
    }
}
