using UnityEngine;
using WAD.Inventory;
using WAD.Weapons;

namespace WAD.Procedural
{
    /// <summary>
    /// Verteilt Magazine an MagazineSpawnPoint-Markierungen.
    /// Das visuelle Modell kommt von MagazineTypeSO.groundModelPrefab.
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
            if (gridGenerator != null)
                gridGenerator.OnChunkSpawned += HandleChunkSpawned;
        }

        private void OnDisable()
        {
            if (gridGenerator != null)
                gridGenerator.OnChunkSpawned -= HandleChunkSpawned;
        }

        private void HandleChunkSpawned(GameObject chunk, Vector2Int coord)
        {
            if (magazineTable == null ||
                magazineTable.entries == null ||
                magazineTable.entries.Count == 0)
                return;

            var spawnPoints = chunk.GetComponentsInChildren<MagazineSpawnPoint>(true);

            if (spawnPoints.Length == 0)
                return;

            int hash = unchecked(
                magazineSeed * 486187739
                + coord.x * 73856093
                ^ coord.y * 19349663
            );

            System.Random rng = new System.Random(hash);

            foreach (var point in spawnPoints)
            {
                // Alte Objekte am SpawnPoint entfernen
                for (int i = point.transform.childCount - 1; i >= 0; i--)
                {
                    Destroy(point.transform.GetChild(i).gameObject);
                }

                if (rng.NextDouble() > point.spawnChance)
                    continue;

                SpawnMagazineAt(point.transform, rng);
            }
        }

        private void SpawnMagazineAt(Transform point, System.Random rng)
        {
            var entry = magazineTable.RollWeighted(rng);

            // Kein gültiger Loot-Eintrag
            if (entry == null || entry.magazineType == null)
                return;

            MagazineTypeSO magazineType = entry.magazineType;

            // MagazineTypeSO enthält Ammo-Typ
            if (magazineType.ammoType == null)
                return;

            // Das Weltmodell kommt vom Magazin-Typ
            GameObject modelPrefab = magazineType.groundModelPrefab;

            GameObject instance = modelPrefab != null
                ? Instantiate(
                    modelPrefab,
                    point.position,
                    point.rotation,
                    point
                )
                : CreateFallbackPickup(point);

            // WorldMagazinePickup holen/erzeugen
            var pickup = instance.GetComponent<WorldMagazinePickup>();

            if (pickup == null)
                pickup = instance.AddComponent<WorldMagazinePickup>();

            // Zufälliger Füllstand
            float fillPercent = (float)(
                rng.NextDouble()
                * (entry.maxFillPercent - entry.minFillPercent)
                + entry.minFillPercent
            );

            // Magazin-Daten setzen
            pickup.ammoType = magazineType.ammoType;
            pickup.capacity = magazineType.baseCapacity;

            pickup.currentRounds = Mathf.RoundToInt(
                magazineType.baseCapacity * fillPercent
            );
        }

        private GameObject CreateFallbackPickup(Transform point)
        {
            GameObject instance = GameObject.CreatePrimitive(
                PrimitiveType.Cube
            );

            instance.name = "MagazinePickup_Placeholder";

            instance.transform.SetParent(point);
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = new Vector3(
                0.05f,
                0.15f,
                0.05f
            );

            return instance;
        }
    }
}
