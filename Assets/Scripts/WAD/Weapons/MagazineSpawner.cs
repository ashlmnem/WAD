using System.Collections.Generic;
using UnityEngine;
using WAD.Inventory;

namespace WAD.Procedural
{
    /// <summary>
    /// Verteilt Magazine an MagazineSpawnPoint-Markierungen. Modell/Munition/
    /// Kapazitaet kommen komplett aus MagazineTypeSO.
    ///
    /// Wie beim LootSpawner: deterministisch pro Chunk, aber bereits
    /// aufgenommene Magazine tauchen beim erneuten Betreten nicht wieder auf.
    /// </summary>
    [RequireComponent(typeof(ProceduralGridGenerator))]
    public class MagazineSpawner : MonoBehaviour
    {
        [Header("Referenzen")]
        public ProceduralGridGenerator gridGenerator;
        public MagazineTableSO magazineTable;

        [Header("Seed")]
        public int magazineSeed = 24680;

        private readonly Dictionary<Vector2Int, HashSet<int>> collectedPoints = new Dictionary<Vector2Int, HashSet<int>>();
        private readonly Dictionary<GameObject, List<GameObject>> spawnedByChunk = new Dictionary<GameObject, List<GameObject>>();

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

        private void HandleChunkDespawned(GameObject chunk, Vector2Int coord)
        {
            if (!spawnedByChunk.TryGetValue(chunk, out var list)) return;

            foreach (var obj in list)
            {
                if (obj != null) Destroy(obj);
            }

            spawnedByChunk.Remove(chunk);
        }

        private void HandleChunkSpawned(GameObject chunk, Vector2Int coord)
        {
            HandleChunkDespawned(chunk, coord);

            if (magazineTable == null || magazineTable.entries.Count == 0) return;

            var spawnPoints = chunk.GetComponentsInChildren<MagazineSpawnPoint>(true);
            if (spawnPoints.Length == 0) return;

            System.Random rng = new System.Random(gridGenerator.GetChunkSeed(magazineSeed, coord));
            collectedPoints.TryGetValue(coord, out HashSet<int> collected);

            var spawned = new List<GameObject>();

            for (int i = 0; i < spawnPoints.Length; i++)
            {
                var point = spawnPoints[i];

                if (rng.NextDouble() > point.spawnChance) continue;

                var entry = magazineTable.RollWeighted(rng);
                if (entry == null || entry.magazineType == null) continue;

                // Fuellstand IMMER wuerfeln, damit andere Punkte stabil bleiben
                float fillPercent = (float)(rng.NextDouble() * (entry.maxFillPercent - entry.minFillPercent) + entry.minFillPercent);

                if (collected != null && collected.Contains(i)) continue;

                GameObject instance = SpawnMagazineAt(point.transform, entry, fillPercent);
                spawned.Add(instance);

                var pickup = instance.GetComponent<WorldMagazinePickup>();
                int pointIndex = i;
                Vector2Int chunkCoord = coord;
                pickup.PickedUp += _ => MarkCollected(chunkCoord, pointIndex);
            }

            if (spawned.Count > 0) spawnedByChunk[chunk] = spawned;
        }

        private void MarkCollected(Vector2Int coord, int pointIndex)
        {
            if (!collectedPoints.TryGetValue(coord, out var set))
            {
                set = new HashSet<int>();
                collectedPoints[coord] = set;
            }

            set.Add(pointIndex);
        }

        private GameObject SpawnMagazineAt(Transform point, MagazineLootEntry entry, float fillPercent)
        {
            GameObject modelPrefab = entry.magazineType.groundModelPrefab;
            GameObject instance = modelPrefab != null
                ? Instantiate(modelPrefab, point.position, point.rotation, point)
                : CreateFallbackPickup(point);

            var pickup = instance.GetComponent<WorldMagazinePickup>();
            if (pickup == null) pickup = instance.AddComponent<WorldMagazinePickup>();

            pickup.magazineType = entry.magazineType;
            pickup.currentRounds = Mathf.RoundToInt(entry.magazineType.baseCapacity * fillPercent);

            return instance;
        }

        private GameObject CreateFallbackPickup(Transform point)
        {
            GameObject instance = GameObject.CreatePrimitive(PrimitiveType.Cube);
            instance.name = "MagazinePickup_Placeholder";
            instance.transform.SetParent(point);
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localScale = new Vector3(0.05f, 0.15f, 0.05f);
            return instance;
        }
    }
}
