using System.Collections.Generic;
using UnityEngine;
using Unity.AI.Navigation;

namespace WAD.Procedural
{
    /// <summary>
    /// Chunk-basierte prozedurale Welt.
    /// Erzeugt Chunks, setzt automatisch Layer
    /// und erstellt ein eigenes NavMesh pro Chunk.
    /// </summary>
    public class ProceduralGridGenerator : MonoBehaviour
    {
        [Header("Referenzen")]
        public Transform player;


        [Header("Grid Einstellungen")]
        public float chunkSize = 50f;

        public int viewDistanceInChunks = 3;

        public float updateInterval = 0.5f;


        [Header("Seed")]
        public int worldSeed = 12345;


        [Header("Chunk Prefabs")]
        public List<WeightedChunkPrefab> chunkPrefabs =
            new List<WeightedChunkPrefab>();


        private readonly Dictionary<Vector2Int, GameObject> activeChunks =
            new Dictionary<Vector2Int, GameObject>();


        private readonly Dictionary<int, Queue<GameObject>> pools =
            new Dictionary<int, Queue<GameObject>>();



        private float timeSinceLastUpdate;

        private Vector2Int lastPlayerChunk;

        private bool initialized;



        public event System.Action<GameObject, Vector2Int> OnChunkSpawned;



        private void Update()
        {
            if(player == null)
                return;


            timeSinceLastUpdate += Time.deltaTime;


            if(timeSinceLastUpdate < updateInterval)
                return;


            timeSinceLastUpdate = 0f;



            Vector2Int currentChunk =
                WorldToChunkCoord(player.position);



            if(!initialized || currentChunk != lastPlayerChunk)
            {
                initialized = true;

                lastPlayerChunk = currentChunk;

                UpdateChunks(currentChunk);
            }
        }



        private Vector2Int WorldToChunkCoord(Vector3 position)
        {
            int x =
                Mathf.FloorToInt(position.x / chunkSize);

            int z =
                Mathf.FloorToInt(position.z / chunkSize);


            return new Vector2Int(x, z);
        }




        private void UpdateChunks(Vector2Int centerChunk)
        {
            HashSet<Vector2Int> neededChunks =
                new HashSet<Vector2Int>();


            for(int x = -viewDistanceInChunks;
                x <= viewDistanceInChunks;
                x++)
            {
                for(int z = -viewDistanceInChunks;
                    z <= viewDistanceInChunks;
                    z++)
                {
                    Vector2Int coord =
                        new Vector2Int(
                            centerChunk.x + x,
                            centerChunk.y + z);


                    neededChunks.Add(coord);



                    if(!activeChunks.ContainsKey(coord))
                    {
                        SpawnChunk(coord);
                    }
                }
            }



            List<Vector2Int> remove =
                new List<Vector2Int>();


            foreach(var chunk in activeChunks)
            {
                if(!neededChunks.Contains(chunk.Key))
                {
                    remove.Add(chunk.Key);
                }
            }



            foreach(Vector2Int coord in remove)
            {
                DespawnChunk(coord);
            }
        }





        private void SpawnChunk(Vector2Int coord)
        {
            int index =
                SelectPrefabIndexForCoord(coord);



            if(index < 0)
                return;



            GameObject chunk =
                GetFromPool(index);



            chunk.transform.position =
                new Vector3(
                    coord.x * chunkSize,
                    0f,
                    coord.y * chunkSize);



            chunk.SetActive(true);



            AssignChunkLayer(chunk);



            BuildChunkNavMesh(chunk);



            activeChunks[coord] = chunk;



            OnChunkSpawned?.Invoke(
                chunk,
                coord);
        }





        private void DespawnChunk(Vector2Int coord)
        {
            if(!activeChunks.TryGetValue(
                coord,
                out GameObject chunk))
                return;



            activeChunks.Remove(coord);



            chunk.SetActive(false);



            int index =
                chunk.GetComponent<ChunkPrefabTag>()
                ?.prefabIndex ?? 0;



            if(!pools.ContainsKey(index))
            {
                pools[index] =
                    new Queue<GameObject>();
            }


            pools[index].Enqueue(chunk);
        }





        private GameObject GetFromPool(int index)
        {
            if(pools.TryGetValue(
                index,
                out Queue<GameObject> pool))
            {
                if(pool.Count > 0)
                {
                    return pool.Dequeue();
                }
            }



            GameObject instance =
                Instantiate(
                    chunkPrefabs[index].prefab,
                    transform);



            ChunkPrefabTag tag =
                instance.GetComponent<ChunkPrefabTag>();


            if(tag == null)
            {
                tag =
                    instance.AddComponent<ChunkPrefabTag>();
            }


            tag.prefabIndex = index;



            return instance;
        }





        // =====================================================
        // LAYER SYSTEM
        // =====================================================


        private void AssignChunkLayer(GameObject chunk)
        {
            string name =
                chunk.name.Replace("(Clone)", "")
                .Trim();



            int layer = -1;



            switch(name)
            {
                case "Chunk_BuildingBlock":
                    layer =
                    LayerMask.NameToLayer(
                    "BuildingBlock");
                    break;


                case "Chunk_Crossroad":
                    layer =
                    LayerMask.NameToLayer(
                    "Crossroad");
                    break;


                case "Chunk_SmallBase":
                    layer =
                    LayerMask.NameToLayer(
                    "SmallBase");
                    break;


                case "Chunk_StraightRoad":
                    layer =
                    LayerMask.NameToLayer(
                    "StraightRoad");
                    break;
            }



            if(layer == -1)
            {
                Debug.LogWarning(
                    "Kein Layer für: "
                    + name);

                return;
            }



            SetLayerRecursive(
                chunk,
                layer);
        }





        private void SetLayerRecursive(
            GameObject obj,
            int layer)
        {
            obj.layer = layer;


            foreach(Transform child in obj.transform)
            {
                SetLayerRecursive(
                    child.gameObject,
                    layer);
            }
        }





        // =====================================================
        // CHUNK NAVMESH
        // =====================================================


        private void BuildChunkNavMesh(GameObject chunk)
{
    NavMeshSurface surface =
        chunk.GetComponent<NavMeshSurface>();



    if(surface == null)
    {
        surface =
            chunk.AddComponent<NavMeshSurface>();
    }



    surface.collectObjects =
        CollectObjects.Children;

    // Collider-basierte statt Mesh-basierte Geometrie: vermeidet
    // "does not allow read access" bei Meshes ohne Read/Write Enabled
    // (z.B. importierte Modelle wie "Mag", "Houses")
    surface.useGeometry =
        UnityEngine.AI.NavMeshCollectGeometry.PhysicsColliders;



    surface.BuildNavMesh();
}





        // =====================================================
        // RANDOM
        // =====================================================


        private int SelectPrefabIndexForCoord(
            Vector2Int coord)
        {
            if(chunkPrefabs.Count == 0)
                return -1;



            int hash =
                DeterministicHash(
                    coord.x,
                    coord.y,
                    worldSeed);



            System.Random rng =
                new System.Random(hash);



            float total = 0f;


            foreach(var entry in chunkPrefabs)
            {
                total += entry.weight;
            }



            float roll =
                (float)rng.NextDouble()
                * total;



            float current = 0f;



            for(int i = 0; i < chunkPrefabs.Count; i++)
            {
                current += chunkPrefabs[i].weight;


                if(roll <= current)
                    return i;
            }



            return chunkPrefabs.Count - 1;
        }





        private static int DeterministicHash(
            int x,
            int z,
            int seed)
        {
            unchecked
            {
                int hash = seed;

                hash =
                hash * 486187739 + x;


                hash =
                hash * 486187739 + z;


                return hash;
            }
        }





        public float GetPlayerDistanceFromCenter()
        {
            if(player == null)
                return 0f;


            Vector3 pos =
                new Vector3(
                    player.position.x,
                    0f,
                    player.position.z);



            return pos.magnitude;
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
