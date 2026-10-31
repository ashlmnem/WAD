using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WAD.Procedural;

namespace WAD.Enemies
{
    /// <summary>
    /// Abonniert ProceduralGridGenerator.OnChunkSpawned und verteilt Gegner an
    /// EnemySpawnPoint-Markierungen - MIT bewusster Begrenzung, damit die Welt
    /// nicht ueberfuellt wirkt:
    ///
    /// 1) spawnChance: nicht jeder Punkt bekommt einen Gegner
    /// 2) maxActiveEnemies: globale Obergrenze ueber die GESAMTE Welt gleichzeitig
    /// 3) minDistanceFromPlayer: kein Spawn direkt neben dem Spieler
    ///
    /// Aufraeumen: Gegner (und Leichen) gehoeren zu ihrem Chunk und werden mit
    /// ihm entfernt. Vorher blieben sie nach dem Despawn des Chunks im Nichts
    /// stehen (NavMesh weg -> Agent-Fehler), zaehlten aber weiter gegen die
    /// Obergrenze. Getoetete Gegner kommen beim erneuten Betreten des Chunks
    /// nicht mehr zurueck.
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

        [Header("Verzoegerung")]
        [Tooltip("Kurze Wartezeit nach dem Chunk-Spawn (das NavMesh wird inzwischen synchron vom ProceduralGridGenerator gebacken). 0 = im selben Frame.")]
        public float spawnDelayAfterChunk = 0.1f;

        private readonly List<EnemyController> activeEnemies = new List<EnemyController>();

        // Chunk-Objekt -> aktuelle Koordinate (nur solange der Chunk aktiv ist)
        private readonly Dictionary<GameObject, Vector2Int> chunkCoords = new Dictionary<GameObject, Vector2Int>();

        // Chunk-Koordinate -> erzeugte Gegner-Objekte (lebend und tot)
        private readonly Dictionary<Vector2Int, List<GameObject>> enemiesByCoord = new Dictionary<Vector2Int, List<GameObject>>();

        // Chunk-Koordinate -> Indizes der Spawn-Punkte, deren Gegner schon getoetet wurde
        private readonly Dictionary<Vector2Int, HashSet<int>> killedPoints = new Dictionary<Vector2Int, HashSet<int>>();

        private void Reset()
        {
            gridGenerator = GetComponent<ProceduralGridGenerator>();
        }

        private void OnEnable()
        {
            if (gridGenerator == null) return;

            gridGenerator.OnChunkSpawned += HandleChunkSpawned;
            gridGenerator.OnChunkDespawned += HandleChunkDespawned;
        }

        private void OnDisable()
        {
            if (gridGenerator == null) return;

            gridGenerator.OnChunkSpawned -= HandleChunkSpawned;
            gridGenerator.OnChunkDespawned -= HandleChunkDespawned;
        }

        private void HandleChunkSpawned(GameObject chunk, Vector2Int coord)
        {
            chunkCoords[chunk] = coord;

            if (enemyPrefabs == null || enemyPrefabs.Length == 0) return;

            StartCoroutine(DelayedSpawnCheck(chunk, coord));
        }

        private void HandleChunkDespawned(GameObject chunk, Vector2Int coord)
        {
            chunkCoords.Remove(chunk);

            if (!enemiesByCoord.TryGetValue(coord, out var list)) return;

            foreach (var obj in list)
            {
                if (obj == null) continue;

                var enemy = obj.GetComponent<EnemyController>();
                if (enemy != null) activeEnemies.Remove(enemy);

                Destroy(obj);
            }

            enemiesByCoord.Remove(coord);
        }

        private IEnumerator DelayedSpawnCheck(GameObject chunk, Vector2Int coord)
        {
            if (spawnDelayAfterChunk > 0f) yield return new WaitForSeconds(spawnDelayAfterChunk);
            else yield return null;

            // Chunk wurde inzwischen despawnt oder fuer eine andere Koordinate recycelt?
            if (chunk == null || !chunk.activeInHierarchy) yield break;
            if (!chunkCoords.TryGetValue(chunk, out Vector2Int currentCoord) || currentCoord != coord) yield break;

            // Doppelt spawnen verhindern (z.B. Chunk despawnt und innerhalb der Wartezeit erneut gespawnt)
            if (enemiesByCoord.ContainsKey(coord)) yield break;

            var spawnPoints = chunk.GetComponentsInChildren<EnemySpawnPoint>(true);
            if (spawnPoints.Length == 0) yield break;

            System.Random rng = new System.Random(gridGenerator.GetChunkSeed(enemySeed, coord));
            killedPoints.TryGetValue(coord, out HashSet<int> killed);

            for (int i = 0; i < spawnPoints.Length; i++)
            {
                // Zufallswuerfe IMMER ziehen, damit uebrige Punkte stabil bleiben,
                // auch wenn ein Punkt wegen Obergrenze/Abstand/Kill ausgelassen wird.
                bool rolledSpawn = rng.NextDouble() <= spawnChance;
                int prefabIndex = rng.Next(0, enemyPrefabs.Length);

                if (!rolledSpawn) continue;
                if (killed != null && killed.Contains(i)) continue;

                CleanupDeadReferences();
                if (activeEnemies.Count >= maxActiveEnemies) break; // globale Obergrenze erreicht

                var point = spawnPoints[i];
                if (player != null && Vector3.Distance(point.transform.position, player.position) < minDistanceFromPlayer)
                    continue; // zu nah am Spieler

                SpawnEnemyAt(point.transform, enemyPrefabs[prefabIndex], coord, i);
            }
        }

        private void SpawnEnemyAt(Transform point, GameObject prefab, Vector2Int coord, int pointIndex)
        {
            if (prefab == null) return;

            GameObject instance = Instantiate(prefab, point.position, point.rotation);

            if (!enemiesByCoord.TryGetValue(coord, out var list))
            {
                list = new List<GameObject>();
                enemiesByCoord[coord] = list;
            }
            list.Add(instance);

            var enemy = instance.GetComponent<EnemyController>();
            if (enemy == null) return;

            activeEnemies.Add(enemy);
            enemy.OnDeath += e =>
            {
                activeEnemies.Remove(e);
                MarkKilled(coord, pointIndex);
            };
        }

        private void MarkKilled(Vector2Int coord, int pointIndex)
        {
            if (!killedPoints.TryGetValue(coord, out var set))
            {
                set = new HashSet<int>();
                killedPoints[coord] = set;
            }

            set.Add(pointIndex);
        }

        private void CleanupDeadReferences()
        {
            activeEnemies.RemoveAll(e => e == null);
        }
    }
}
