using System.Collections.Generic;
using UnityEngine;
using WAD.Inventory;

namespace WAD.Procedural
{
    /// <summary>
    /// Abonniert ProceduralGridGenerator.OnChunkSpawned und verteilt Loot an
    /// allen LootSpawnPoint-Markierungen innerhalb des neuen Chunks.
    ///
    /// Deterministisch (Welt-Seed + Coord + lootSeed): derselbe Chunk hat bei
    /// jedem Betreten dieselbe Loot-Verteilung - ABER bereits aufgesammelte
    /// Items bleiben weg. Vorher tauchte alles wieder auf, sobald man einen
    /// Chunk verlassen und erneut betreten hat (Loot-Duplizierung).
    /// </summary>
    [RequireComponent(typeof(ProceduralGridGenerator))]
    public class LootSpawner : MonoBehaviour
    {
        [Header("Referenzen")]
        public ProceduralGridGenerator gridGenerator;
        public LootTableSO lootTable;

        [Tooltip("Falls leer, wird automatisch ein einfacher Wuerfel-Platzhalter mit WorldItemPickup erzeugt")]
        public GameObject pickupPrefab;

        [Header("Seed")]
        public int lootSeed = 54321;

        // Chunk-Koordinate -> Indizes der Spawn-Punkte, deren Loot schon eingesammelt wurde
        private readonly Dictionary<Vector2Int, HashSet<int>> collectedPoints = new Dictionary<Vector2Int, HashSet<int>>();

        // Aktuell erzeugte Pickups je Chunk-Objekt (zum sauberen Aufraeumen beim Despawn)
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
            // Falls dieser Chunk (aus dem Pool) noch Altlasten hat
            HandleChunkDespawned(chunk, coord);

            if (lootTable == null || lootTable.entries.Count == 0) return;

            var spawnPoints = chunk.GetComponentsInChildren<LootSpawnPoint>(true);
            if (spawnPoints.Length == 0) return;

            System.Random rng = new System.Random(gridGenerator.GetChunkSeed(lootSeed, coord));
            collectedPoints.TryGetValue(coord, out HashSet<int> collected);

            var spawned = new List<GameObject>();

            for (int i = 0; i < spawnPoints.Length; i++)
            {
                var point = spawnPoints[i];

                // WICHTIG: Die Zufallswuerfe werden IMMER gezogen - auch fuer bereits
                // gepluenderte Punkte -, damit alle anderen Punkte stabil bleiben.
                if (rng.NextDouble() > point.spawnChance) continue;

                var entry = lootTable.RollWeighted(rng);
                if (entry == null || entry.itemAsset == null) continue;

                int lo = Mathf.Min(entry.minQuantity, entry.maxQuantity);
                int hi = Mathf.Max(entry.minQuantity, entry.maxQuantity);
                int quantity = rng.Next(lo, hi + 1);

                if (collected != null && collected.Contains(i)) continue;

                GameObject pickupObj = SpawnLootAt(point.transform, entry, quantity);
                if (pickupObj == null) continue;

                spawned.Add(pickupObj);

                var pickup = pickupObj.GetComponent<WorldItemPickup>();
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

        private GameObject SpawnLootAt(Transform point, LootTableEntry entry, int quantity)
        {
            if (!(entry.itemAsset is IInventoryItem)) return null;

            GameObject instance = pickupPrefab != null
                ? Instantiate(pickupPrefab, point.position, point.rotation, point)
                : CreateFallbackPickup(point);

            var pickup = instance.GetComponent<WorldItemPickup>();
            if (pickup == null) pickup = instance.AddComponent<WorldItemPickup>();

            pickup.itemAsset = entry.itemAsset;
            pickup.quantity = quantity;

            return instance;
        }

        private GameObject CreateFallbackPickup(Transform point)
        {
            GameObject instance = GameObject.CreatePrimitive(PrimitiveType.Cube);
            instance.name = "LootPickup_Placeholder";
            instance.transform.SetParent(point);
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localScale = Vector3.one * 0.25f;
            return instance;
        }
    }
}
