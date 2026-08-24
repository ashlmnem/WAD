using UnityEngine;
using UnityEngine.AI;

namespace WAD.Taskmaster
{
    /// <summary>
    /// Platziert den Taskmaster einmalig beim Levelstart an einer zufaelligen,
    /// per NavMesh erreichbaren Stelle innerhalb eines Radius um den Spieler-
    /// Spawn-Punkt. Wartet kurz, damit umliegende Chunks/NavMesh-Daten zuerst
    /// existieren (siehe ChunkNavMeshBaker).
    /// </summary>
    public class TaskmasterSpawner : MonoBehaviour
    {
        public GameObject taskmasterPrefab;
        public Transform playerSpawnPoint;
        public float minDistance = 100f;
        public float maxDistance = 800f;
        public float spawnDelay = 2f;
        [Tooltip("Wie oft ein zufaelliger Punkt probiert wird, bevor aufgegeben wird")]
        public int maxAttempts = 20;

        private void Start()
        {
            Invoke(nameof(TrySpawn), spawnDelay);
        }

        private void TrySpawn()
        {
            if (taskmasterPrefab == null || playerSpawnPoint == null) return;

            for (int i = 0; i < maxAttempts; i++)
            {
                Vector2 randomDir = Random.insideUnitCircle.normalized;
                float distance = Random.Range(minDistance, maxDistance);
                Vector3 candidate = playerSpawnPoint.position + new Vector3(randomDir.x, 0f, randomDir.y) * distance;

                if (NavMesh.SamplePosition(candidate, out NavMeshHit navHit, 25f, NavMesh.AllAreas))
                {
                    Instantiate(taskmasterPrefab, navHit.position, Quaternion.identity);
                    Debug.Log($"[TaskmasterSpawner] Taskmaster gespawnt bei {navHit.position}, Distanz {distance:F0}m.");
                    return;
                }
            }

            Debug.LogWarning("[TaskmasterSpawner] Kein gueltiger NavMesh-Punkt nach mehreren Versuchen gefunden - evtl. sind noch nicht genug Chunks/NavMesh geladen.");
        }
    }
}
