using System.Collections.Generic;
using UnityEngine;
using Unity.AI.Navigation;

namespace WAD.Procedural
{
    /// <summary>
    /// Chunk-basierte prozedurale Welt.
    /// Erzeugt Chunks rund um den Spieler, setzt automatisch den Layer und
    /// backt EIN NavMesh pro Chunk-Instanz (wird beim Wiederverwenden aus dem
    /// Pool nur verschoben, nicht neu berechnet).
    ///
    /// Performance:
    /// - Nur Chunks im Radius 'immediateRadiusInChunks' werden sofort erzeugt,
    ///   alle weiter entfernten werden ueber mehrere Frames verteilt
    ///   ('maxChunksPerFrame'), statt bis zu 49 Chunks + NavMeshes in einem Frame.
    /// - Chunk-Instanzen werden gepoolt und beim Wiederverwenden nicht neu gebackt.
    /// </summary>
    public class ProceduralGridGenerator : MonoBehaviour
    {
        [Header("Referenzen")]
        public Transform player;

        [Header("Grid Einstellungen")]
        public float chunkSize = 50f;
        public int viewDistanceInChunks = 3;
        public float updateInterval = 0.5f;

        [Header("Performance")]
        [Tooltip("Chunks bis zu diesem Abstand (in Chunks) zum Spieler werden sofort erzeugt, der Rest verteilt ueber mehrere Frames.")]
        public int immediateRadiusInChunks = 1;
        [Tooltip("Maximale Anzahl nachgeladener Chunks pro Frame (jeder Chunk kostet ein NavMesh-Baking, solange keine gepoolte Instanz frei ist).")]
        [Min(1)] public int maxChunksPerFrame = 1;

        [Header("Seed")]
        [Tooltip("Wird verwendet, wenn 'Randomize Seed On Start' aus ist (z.B. zum Debuggen eines bestimmten Levels).")]
        public int worldSeed = 12345;
        [Tooltip("Jeder Start (= jeder Run) erzeugt eine neue Welt. Der gewuerfelte Seed steht im Log.")]
        public bool randomizeSeedOnStart = true;

        [Header("Chunk Prefabs")]
        public List<WeightedChunkPrefab> chunkPrefabs = new List<WeightedChunkPrefab>();

        /// <summary> Der tatsaechlich benutzte Seed dieses Levels (Basis fuer Chunks, Loot und Gegner). </summary>
        public int ActiveSeed { get; private set; }

        private readonly Dictionary<Vector2Int, GameObject> activeChunks = new Dictionary<Vector2Int, GameObject>();
        private readonly Dictionary<int, Queue<GameObject>> pools = new Dictionary<int, Queue<GameObject>>();

        private readonly List<Vector2Int> spawnQueue = new List<Vector2Int>();
        private readonly HashSet<Vector2Int> queued = new HashSet<Vector2Int>();
        private readonly HashSet<Vector2Int> neededChunks = new HashSet<Vector2Int>();
        private readonly List<Vector2Int> removeBuffer = new List<Vector2Int>();

        private float timeSinceLastUpdate;
        private Vector2Int lastPlayerChunk;
        private bool initialized;

        /// <summary> Wird NACH dem Aktivieren, Positionieren und Backen eines Chunks ausgeloest. </summary>
        public event System.Action<GameObject, Vector2Int> OnChunkSpawned;

        /// <summary> Wird unmittelbar VOR dem Deaktivieren eines Chunks ausgeloest (Gegner/Loot aufraeumen). </summary>
        public event System.Action<GameObject, Vector2Int> OnChunkDespawned;

        private void Awake()
        {
            ActiveSeed = randomizeSeedOnStart ? Random.Range(1, int.MaxValue) : worldSeed;
            Debug.Log($"[ProceduralGridGenerator] Welt-Seed: {ActiveSeed}");
        }

        private void Update()
        {
            if (player == null) return;

            timeSinceLastUpdate += Time.deltaTime;

            if (timeSinceLastUpdate >= updateInterval)
            {
                timeSinceLastUpdate = 0f;

                Vector2Int currentChunk = WorldToChunkCoord(player.position);

                if (!initialized || currentChunk != lastPlayerChunk)
                {
                    initialized = true;
                    lastPlayerChunk = currentChunk;
                    RefreshChunks(currentChunk);
                }
            }

            ProcessSpawnQueue();
        }

        private Vector2Int WorldToChunkCoord(Vector3 position)
        {
            return new Vector2Int(
                Mathf.FloorToInt(position.x / chunkSize),
                Mathf.FloorToInt(position.z / chunkSize));
        }

        private static int ChebyshevDistance(Vector2Int a, Vector2Int b)
        {
            return Mathf.Max(Mathf.Abs(a.x - b.x), Mathf.Abs(a.y - b.y));
        }

        private void RefreshChunks(Vector2Int centerChunk)
        {
            neededChunks.Clear();

            for (int x = -viewDistanceInChunks; x <= viewDistanceInChunks; x++)
            {
                for (int z = -viewDistanceInChunks; z <= viewDistanceInChunks; z++)
                {
                    Vector2Int coord = new Vector2Int(centerChunk.x + x, centerChunk.y + z);
                    neededChunks.Add(coord);

                    if (activeChunks.ContainsKey(coord)) continue;

                    if (ChebyshevDistance(coord, centerChunk) <= immediateRadiusInChunks)
                    {
                        SpawnChunk(coord);
                    }
                    else if (queued.Add(coord))
                    {
                        spawnQueue.Add(coord);
                    }
                }
            }

            // Nicht mehr benoetigte Chunks zurueck in den Pool
            removeBuffer.Clear();
            foreach (var pair in activeChunks)
            {
                if (!neededChunks.Contains(pair.Key)) removeBuffer.Add(pair.Key);
            }

            foreach (Vector2Int coord in removeBuffer) DespawnChunk(coord);

            // Veraltete Eintraege aus der Warteschlange werfen und den Rest
            // nach Abstand sortieren: nahe Chunks zuerst.
            spawnQueue.RemoveAll(c =>
            {
                bool stale = !neededChunks.Contains(c) || activeChunks.ContainsKey(c);
                if (stale) queued.Remove(c);
                return stale;
            });

            spawnQueue.Sort((a, b) => ChebyshevDistance(a, centerChunk).CompareTo(ChebyshevDistance(b, centerChunk)));
        }

        private void ProcessSpawnQueue()
        {
            int budget = Mathf.Max(1, maxChunksPerFrame);

            while (budget > 0 && spawnQueue.Count > 0)
            {
                Vector2Int coord = spawnQueue[0];
                spawnQueue.RemoveAt(0);
                queued.Remove(coord);

                if (activeChunks.ContainsKey(coord) || !neededChunks.Contains(coord)) continue;

                SpawnChunk(coord);
                budget--;
            }
        }

        private void SpawnChunk(Vector2Int coord)
        {
            int index = SelectPrefabIndexForCoord(coord);
            if (index < 0) return;

            Vector3 position = new Vector3(coord.x * chunkSize, 0f, coord.y * chunkSize);

            GameObject chunk = GetFromPool(index, position);
            if (chunk == null) return;

            chunk.transform.position = position;
            if (!chunk.activeSelf) chunk.SetActive(true);

            activeChunks[coord] = chunk;

            OnChunkSpawned?.Invoke(chunk, coord);
        }

        private void DespawnChunk(Vector2Int coord)
        {
            if (!activeChunks.TryGetValue(coord, out GameObject chunk)) return;

            activeChunks.Remove(coord);

            OnChunkDespawned?.Invoke(chunk, coord);

            chunk.SetActive(false);

            int index = 0;
            var tag = chunk.GetComponent<ChunkPrefabTag>();
            if (tag != null) index = tag.prefabIndex;

            if (!pools.TryGetValue(index, out Queue<GameObject> pool))
            {
                pool = new Queue<GameObject>();
                pools[index] = pool;
            }

            pool.Enqueue(chunk);
        }

        private GameObject GetFromPool(int index, Vector3 position)
        {
            if (pools.TryGetValue(index, out Queue<GameObject> pool))
            {
                while (pool.Count > 0)
                {
                    GameObject pooled = pool.Dequeue();
                    if (pooled != null) return pooled;
                }
            }

            GameObject prefab = chunkPrefabs[index].prefab;
            if (prefab == null)
            {
                Debug.LogWarning($"[ProceduralGridGenerator] Chunk-Prefab an Index {index} ist nicht zugewiesen.");
                return null;
            }

            // Direkt an der Zielposition erzeugen: Awake/OnEnable der Chunk-
            // Komponenten laufen sonst am Ursprung und das NavMesh wuerde dort
            // registriert, bevor der Chunk verschoben wird.
            GameObject instance = Instantiate(prefab, position, prefab.transform.rotation, transform);

            var tag = instance.GetComponent<ChunkPrefabTag>();
            if (tag == null) tag = instance.AddComponent<ChunkPrefabTag>();
            tag.prefabIndex = index;

            AssignChunkLayer(instance);
            BuildChunkNavMesh(instance);

            return instance;
        }

        // =====================================================
        // LAYER SYSTEM (einmal pro Chunk-Instanz)
        // =====================================================

        private void AssignChunkLayer(GameObject chunk)
        {
            string layerName = null;

            // Bevorzugt die Komponente am Prefab (robust gegen Umbenennen) ...
            var layerComponent = chunk.GetComponent<ProceduralChunkLayer>();
            if (layerComponent != null)
            {
                layerName = layerComponent.layerType.ToString();
            }
            else
            {
                // ... sonst wie bisher ueber den Prefab-Namen.
                string name = chunk.name.Replace("(Clone)", "").Trim();
                switch (name)
                {
                    case "Chunk_BuildingBlock": layerName = "BuildingBlock"; break;
                    case "Chunk_Crossroad": layerName = "Crossroad"; break;
                    case "Chunk_SmallBase": layerName = "SmallBase"; break;
                    case "Chunk_StraightRoad": layerName = "StraightRoad"; break;
                }
            }

            int layer = layerName != null ? LayerMask.NameToLayer(layerName) : -1;

            if (layer == -1)
            {
                Debug.LogWarning($"[ProceduralGridGenerator] Kein Layer fuer Chunk '{chunk.name}' gefunden.");
                return;
            }

            SetLayerRecursive(chunk, layer);
        }

        private static void SetLayerRecursive(GameObject obj, int layer)
        {
            obj.layer = layer;

            foreach (Transform child in obj.transform)
            {
                SetLayerRecursive(child.gameObject, layer);
            }
        }

        // =====================================================
        // CHUNK NAVMESH (einmal pro Chunk-Instanz)
        // =====================================================

        private static void BuildChunkNavMesh(GameObject chunk)
        {
            NavMeshSurface surface = chunk.GetComponent<NavMeshSurface>();
            if (surface == null) surface = chunk.AddComponent<NavMeshSurface>();

            surface.collectObjects = CollectObjects.Children;

            // Collider-basierte statt Mesh-basierte Geometrie: vermeidet
            // "does not allow read access" bei Meshes ohne Read/Write Enabled
            // (z.B. importierte Modelle wie "Mag", "Houses")
            surface.useGeometry = UnityEngine.AI.NavMeshCollectGeometry.PhysicsColliders;

            Physics.SyncTransforms();
            surface.BuildNavMesh();
        }

        // =====================================================
        // RANDOM
        // =====================================================

        private int SelectPrefabIndexForCoord(Vector2Int coord)
        {
            if (chunkPrefabs.Count == 0) return -1;

            System.Random rng = new System.Random(GetChunkSeed(0, coord));

            float total = 0f;
            foreach (var entry in chunkPrefabs) total += Mathf.Max(0f, entry.weight);

            if (total <= 0f) return 0;

            float roll = (float)rng.NextDouble() * total;
            float current = 0f;
            int lastValid = 0;

            for (int i = 0; i < chunkPrefabs.Count; i++)
            {
                float w = Mathf.Max(0f, chunkPrefabs[i].weight);
                if (w <= 0f) continue;

                lastValid = i;
                current += w;

                if (roll < current) return i;
            }

            return lastValid;
        }

        /// <summary>
        /// Deterministischer Seed fuer einen Chunk. 'salt' trennt die Zufallsstroeme
        /// (Chunk-Wahl, Loot, Gegner) voneinander; der Welt-Seed fliesst immer ein.
        /// </summary>
        public int GetChunkSeed(int salt, Vector2Int coord)
        {
            unchecked
            {
                int hash = ActiveSeed ^ salt;
                hash = hash * 486187739 + coord.x;
                hash = hash * 486187739 + coord.y;

                // Avalanche-Schritt: benachbarte Koordinaten liefern sonst sehr aehnliche Seeds
                hash ^= hash >> 15;
                hash *= 0x2c1b3c6d;
                hash ^= hash >> 12;
                return hash;
            }
        }

        public float GetPlayerDistanceFromCenter()
        {
            if (player == null) return 0f;

            return new Vector3(player.position.x, 0f, player.position.z).magnitude;
        }
    }

    [System.Serializable]
    public class WeightedChunkPrefab
    {
        public GameObject prefab;
        public float weight = 1f;
    }

    public class ChunkPrefabTag : MonoBehaviour
    {
        public int prefabIndex;
    }
}
