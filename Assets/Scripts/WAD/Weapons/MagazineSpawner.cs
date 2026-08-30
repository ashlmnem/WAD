using UnityEngine;
using WAD.Inventory;

namespace WAD.Procedural
{
    /// <summary>
    /// Verteilt Magazine an MagazineSpawnPoint-Markierungen. Modell/Munition/
    /// Kapazitaet kommen jetzt komplett aus MagazineTypeSO (Punkt 6+7+8).
    /// </summary>
    [RequireComponent(typeof(ProceduralGridGenerator))]
    public class MagazineSpawner : MonoBehaviour
    {
        [Header("Referenzen")]
        public ProceduralGridGenerator gridGenerator;
        public MagazineTableSO magazineTable;

        [Header("Seed")]
        public int magazineSeed = 24680;

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
            if (magazineTable == null || magazineTable.entries.Count == 0) return;

            var spawnPoints = chunk.GetComponentsInChildren<MagazineSpawnPoint>(true);
            if (spawnPoints.Length == 0) return;

            int hash = unchecked(magazineSeed * 486187739 + coord.x * 73856093 ^ coord.y * 19349663);
            System.Random rng = new System.Random(hash);

            foreach (var point in spawnPoints)
            {
                for (int i = point.transform.childCount - 1; i >= 0; i--)
                {
                    Destroy(point.transform.GetChild(i).gameObject);
                }

                if (rng.NextDouble() > point.spawnChance) continue;

                SpawnMagazineAt(point.transform, rng);
            }
        }

        private void SpawnMagazineAt(Transform point, System.Random rng)
        {
            var entry = magazineTable.RollWeighted(rng);
            if (entry == null || entry.magazineType == null) return;

            GameObject modelPrefab = entry.magazineType.groundModelPrefab;
            GameObject instance = modelPrefab != null
                ? Instantiate(modelPrefab, point.position, point.rotation, point)
                : CreateFallbackPickup(point);

            var pickup = instance.GetComponent<WorldMagazinePickup>();
            if (pickup == null) pickup = instance.AddComponent<WorldMagazinePickup>();

            float fillPercent = (float)(rng.NextDouble() * (entry.maxFillPercent - entry.minFillPercent) + entry.minFillPercent);
            pickup.magazineType = entry.magazineType;
            pickup.currentRounds = Mathf.RoundToInt(entry.magazineType.baseCapacity * fillPercent);
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