using UnityEngine;
using WAD.Inventory;

namespace WAD.Procedural
{
    /// <summary>
    /// Abonniert ProceduralGridGenerator.OnChunkSpawned und verteilt Loot an
    /// allen LootSpawnPoint-Markierungen innerhalb des neuen Chunks.
    ///
    /// Deterministisch (Coord + Seed), passend zum Rest des liminalen Systems:
    /// derselbe Chunk hat bei jedem Betreten dieselbe Loot-Verteilung.
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
            if (lootTable == null || lootTable.entries.Count == 0) return;

            var spawnPoints = chunk.GetComponentsInChildren<LootSpawnPoint>(true);
            if (spawnPoints.Length == 0) return;

            int hash = unchecked(lootSeed * 486187739 + coord.x * 73856093 ^ coord.y * 19349663);
            System.Random rng = new System.Random(hash);

            foreach (var point in spawnPoints)
            {
                // Vorher evtl. vorhandenes Loot an diesem Punkt entfernen
                // (z.B. wenn der Chunk aus dem Pool wiederverwendet wird)
                for (int i = point.transform.childCount - 1; i >= 0; i--)
                {
                    Destroy(point.transform.GetChild(i).gameObject);
                }

                if (rng.NextDouble() > point.spawnChance) continue;

                SpawnLootAt(point.transform, rng);
            }
        }

        private void SpawnLootAt(Transform point, System.Random rng)
        {
            var entry = lootTable.RollWeighted(rng);
            if (entry == null || entry.itemAsset == null) return;

            IInventoryItem item = entry.itemAsset as IInventoryItem;
            if (item == null) return;

            GameObject instance = pickupPrefab != null
                ? Instantiate(pickupPrefab, point.position, point.rotation, point)
                : CreateFallbackPickup(point);

            var pickup = instance.GetComponent<WorldItemPickup>();
            if (pickup == null) pickup = instance.AddComponent<WorldItemPickup>();

            pickup.itemAsset = entry.itemAsset;
            pickup.quantity = rng.Next(entry.minQuantity, entry.maxQuantity + 1);
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
