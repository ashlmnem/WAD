using UnityEngine;

namespace WAD.Procedural
{
    /// <summary>
    /// Platziere dieses Skript auf leere Kind-Objekte in deinen Chunk-Prefabs,
    /// um moegliche Loot-Spawn-Positionen zu markieren. LootSpawner sucht danach.
    /// </summary>
    public class LootSpawnPoint : MonoBehaviour
    {
        [Range(0f, 1f)]
        [Tooltip("Chance, dass an diesem Punkt ueberhaupt etwas spawnt (nicht jeder Punkt hat immer Loot)")]
        public float spawnChance = 0.4f;

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, 0.3f);
        }
    }
}
