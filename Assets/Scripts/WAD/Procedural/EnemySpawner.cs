using System.Collections.Generic;
using UnityEngine;
using WAD.Procedural;

namespace WAD.Enemies
{
    /// <summary>
    /// Abonniert ProceduralGridGenerator.OnChunkSpawned und verteilt Gegner an
    /// EnemySpawnPoint-Markierungen - MIT bewusster Begrenzung, damit die Welt
    /// nicht ueberfuellt wirkt (Punkt 2 aus der Anfrage):
    ///
    /// 1) spawnChance: nicht jeder Punkt bekommt einen Gegner
    /// 2) maxActiveEnemies: globale Obergrenze ueber die GESAMTE Welt gleichzeitig
    /// 3) minDistanceFromPlayer: kein Spawn direkt neben dem Spieler (unfair/immersion-brechend)
    /// </summary>
    [RequireComponent(typeof(ProceduralGridGenerator))]
    public class EnemySpawner : MonoBehaviour
    {
        [Header("Referenzen")]
        public ProceduralGridGenerator gridGenerator;
        public Transform player;
        public GameObject[] enemyPrefabs; // z.B. Death Insurgency Prefab-Varianten

        [Header("Haeufigkeit")]
        [Range(0f, 1f)]
        [Tooltip("Chance pro Spawn-Punkt, dass dort UEBERHAUPT ein Gegner erscheint")]
        public float spawnChance = 0.25f;
        [Tooltip("Absolute Obergrenze gleichzeitig existierender Gegner in der GESAMTEN Welt")]
        public int maxActiveEnemies = 12;
        [Tooltip("Mindestabstand zum Spieler beim Spawnen, damit nichts direkt vor der Nase auftaucht")]
        public float minDistanceFromPlayer = 20f;

        [Header("Seed")]
        public int enemySeed = 99999;

        [Header("Bake-Verzoegerung")]
        [Tooltip("Muss groesser sein als die Verzoegerung in ChunkNavMeshBaker, damit das NavMesh vor dem Spawnen fertig ist")]
        public float spawnDelayAfterChunk = 0.1f;

        private readonly List<EnemyController> activeEnemies = new List<EnemyController>();

        private void Reset()
        {
            gridGenerator = GetComponent<ProceduralGridGenerator>();
        }

        private void OnEnable()
        {
            if (gridGenerator != null) gridGenerator.OnChunkSpawned += HandleChunkSpawned;
        }

        private void OnDisable()
        {
            if (gridGenerator != null) gridGenerator.OnChunkSpawned -= HandleChunkSpawned;
        }

        private void HandleChunkSpawned(GameObject chunk, Vector2Int coord)
        {
            if (enemyPrefabs == null || enemyPrefabs.Length == 0) return;

            // Verzoegerung, damit ChunkNavMeshBaker sein NavMesh zuerst fertigstellt
            StartCoroutine(DelayedSpawnCheck(chunk, coord));
        }

        private System.Collections.IEnumerator DelayedSpawnCheck(GameObject chunk, Vector2Int coord)
        {
            yield return new WaitForSeconds(spawnDelayAfterChunk);

            if (chunk == null) yield break; // Chunk wurde inzwischen wieder despawnt

            var spawnPoints = chunk.GetComponentsInChildren<EnemySpawnPoint>(true);
            if (spawnPoints.Length == 0) yield break;

            int hash = unchecked(enemySeed * 486187739 + coord.x * 73856093 ^ coord.y * 19349663);
            System.Random rng = new System.Random(hash);

            foreach (var point in spawnPoints)
            {
                CleanupDeadReferences();

                if (activeEnemies.Count >= maxActiveEnemies) break; // globale Obergrenze erreicht - fertig

                if (rng.NextDouble() > spawnChance) continue; // Wuerfel nicht getroffen

                if (player != null && Vector3.Distance(point.transform.position, player.position) < minDistanceFromPlayer)
                    continue; // zu nah am Spieler

                SpawnEnemyAt(point.transform, rng);
            }
        }

        private void SpawnEnemyAt(Transform point, System.Random rng)
        {
            GameObject prefab = enemyPrefabs[rng.Next(0, enemyPrefabs.Length)];
            GameObject instance = Instantiate(prefab, point.position, point.rotation);

            var enemy = instance.GetComponent<EnemyController>();
            if (enemy != null)
            {
                activeEnemies.Add(enemy);
                enemy.OnDeath += HandleEnemyDeath;
            }
        }

        private void HandleEnemyDeath(EnemyController enemy)
        {
            activeEnemies.Remove(enemy);
        }

        private void CleanupDeadReferences()
        {
            activeEnemies.RemoveAll(e => e == null);
        }
    }
}
