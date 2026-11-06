using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WAD.Inventory;
using WAD.Procedural;

namespace WAD.Weapons
{
    /// <summary>
    /// Verteilt Magazine als Weltloot in der Umgebung. Ergänzt den
    /// vorhandenen MagazineSpawner (der an Chunk-Markierungen hängt) um eine
    /// freie, zufällige Platzierung im Level - ähnlich dem JetSpawner.
    ///
    /// Ein leeres Objekt in die Level-Szene legen, Katalog zuweisen und
    /// Anzahl/Kaliber einstellen. Kein Code nötig.
    /// </summary>
    public class MagazineWorldSpawner : MonoBehaviour
    {
        [Header("Quelle")]
        [Tooltip("Procedural grid to wait for before searching for ground. Empty = find one in the scene.")]
        public ProceduralGridGenerator gridGenerator;
        [Min(0f)] public float worldReadyTimeout = 8f;
        [Tooltip("Katalog aller Magazin-Typen. Leer = automatisch gesucht.")]
        public MagazineCatalogSO magazineCatalog;
        [Tooltip("Alternativ: feste Liste von Magazin-Typen (überstimmt den Katalog)")]
        public List<MagazineTypeSO> specificMagazineTypes = new List<MagazineTypeSO>();

        [Header("Menge")]
        [Tooltip("Wie viele Magazine im Level erscheinen")]
        public Vector2Int spawnCountRange = new Vector2Int(4, 8);
        [Min(0)] public int maxSpawnCount = 64;

        [Header("Platzierung")]
        public Transform spawnAreaCenter;
        public float spawnAreaRadius = 300f;
        [Tooltip("Mindestabstand zum Spieler beim Start")]
        public float minDistanceFromPlayer = 20f;
        [Tooltip("Mindestabstand zwischen zwei Magazinen")]
        public float minDistanceBetweenMagazines = 8f;
        [Tooltip("Maximale Steigung des Untergrunds (Grad)")]
        public float maxGroundSlope = 30f;
        [Tooltip("Damit das Magazin nicht im Boden versinkt")]
        public float heightAboveGround = 0.1f;

        [Header("Bodensuche")]
        public LayerMask groundLayers = ~0;
        public float raycastStartHeight = 150f;
        public int placementAttempts = 40;

        [Header("Kaliber-Filter (optional)")]
        [Tooltip("Nur Magazine dieser Kaliber platzieren. Leer = alle.")]
        public List<AmmoTypeSO> allowedCalibers = new List<AmmoTypeSO>();
        [Tooltip("Nur Magazine dieser Grobklassen platzieren. Leer = alle.")]
        public List<MagazineClass> allowedClasses = new List<MagazineClass>();

        [Header("Inhalt")]
        [Tooltip("Magazine mit zufälligem Füllstand erzeugen (-1 = immer voll)")]
        public bool randomRounds = false;
        [Range(0f, 1f)] public float minFillFraction = 0.3f;

        [Header("Zufall")]
        [Tooltip("Fester Seed für reproduzierbare Platzierung. 0 = zufällig.")]
        public int randomSeed = 0;
        public bool parentToThis = true;
        public bool logPlacements = false;

        private bool worldHasSpawnedChunk;

        private void Awake()
        {
            if (gridGenerator == null) gridGenerator = GetComponent<ProceduralGridGenerator>();
            if (gridGenerator == null) gridGenerator = FindFirstObjectByType<ProceduralGridGenerator>();
        }

        private void OnEnable()
        {
            if (gridGenerator != null) gridGenerator.OnChunkSpawned += HandleChunkSpawned;
        }

        private void OnDisable()
        {
            if (gridGenerator != null) gridGenerator.OnChunkSpawned -= HandleChunkSpawned;
        }

        private void Start()
        {
            if (gridGenerator == null)
            {
                SpawnMagazines();
                return;
            }

            StartCoroutine(SpawnWhenWorldIsReady());
        }

        private void HandleChunkSpawned(GameObject chunk, Vector2Int coord)
        {
            worldHasSpawnedChunk = true;
        }

        private IEnumerator SpawnWhenWorldIsReady()
        {
            float elapsed = 0f;
            while (!worldHasSpawnedChunk && elapsed < worldReadyTimeout)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            yield return null;
            SpawnMagazines();
        }

        [ContextMenu("Magazine jetzt spawnen")]
        public void SpawnMagazines()
        {
            if (magazineCatalog == null) magazineCatalog = MagazineCatalogSO.Instance;
            if (specificMagazineTypes == null) specificMagazineTypes = new List<MagazineTypeSO>();
            if (magazineCatalog == null && specificMagazineTypes.Count == 0)
            {
                Debug.LogWarning($"[MagazineWorldSpawner:{name}] Weder Katalog noch feste Magazin-Typen zugewiesen - es wird nichts erzeugt. " +
                                 "Bitte 'Magazine Catalog' im Inspector setzen.");
                return;
            }

            var rng = randomSeed != 0 ? new System.Random(randomSeed) : new System.Random();
            Transform player = FindPlayerTransform();
            Vector3 center = spawnAreaCenter != null
                ? spawnAreaCenter.position
                : player != null ? player.position : transform.position;

            var pool = BuildPool();
            if (pool.Count == 0)
            {
                Debug.LogWarning($"[MagazineWorldSpawner:{name}] Der Magazin-Pool ist leer (Kaliber-/Klassenfilter zu streng?).");
                return;
            }

            int safeMaximum = Mathf.Clamp(maxSpawnCount, 0, 128);
            int upperBound = Mathf.Clamp(Mathf.Max(spawnCountRange.x, spawnCountRange.y), 0, safeMaximum);
            int lowerBound = Mathf.Clamp(Mathf.Min(spawnCountRange.x, spawnCountRange.y), 0, upperBound);
            int count = rng.Next(lowerBound, upperBound + 1);
            var placed = new List<Vector3>();
            int spawned = 0;

            for (int i = 0; i < count; i++)
            {
                if (!TryFindPlacement(rng, center, player, placed, out Vector3 position, out Quaternion rotation)) continue;

                MagazineTypeSO type = pool[rng.Next(pool.Count)];
                GameObject pickup = CreatePickup(type, position, rotation, rng);
                if (pickup == null) continue;

                if (parentToThis) pickup.transform.SetParent(transform, true);

                placed.Add(position);
                spawned++;
            }

            Debug.Log($"[MagazineWorldSpawner:{name}] {spawned} von {count} Magazinen platziert.");
        }

        /// <summary> Baut die Auswahlliste aus Katalog bzw. fester Liste inkl. Filter. </summary>
        private List<MagazineTypeSO> BuildPool()
        {
            var pool = new List<MagazineTypeSO>();

            IEnumerable<MagazineTypeSO> source = specificMagazineTypes.Count > 0
                ? specificMagazineTypes
                : (magazineCatalog != null && magazineCatalog.allMagazineTypes != null
                    ? magazineCatalog.allMagazineTypes
                    : new List<MagazineTypeSO>());

            foreach (var mag in source)
            {
                if (mag == null) continue;
                if (mag.groundModelPrefab == null) continue; // ohne Modell nicht sichtbar/aufhebbar

                if (allowedCalibers.Count > 0 && !allowedCalibers.Contains(mag.ammoType)) continue;
                if (allowedClasses.Count > 0 && !allowedClasses.Contains(mag.magazineClass)) continue;

                pool.Add(mag);
            }

            return pool;
        }

        /// <summary>
        /// Erzeugt ein aufhebbares Magazin aus dem Ground-Model-Prefab.
        /// Hat das Prefab bereits ein WorldMagazinePickup, wird nur der
        /// Magazin-Typ gesetzt; sonst werden Komponente und Collider ergaenzt.
        /// </summary>
        private GameObject CreatePickup(MagazineTypeSO type, Vector3 position, Quaternion rotation, System.Random rng)
        {
            if (type.groundModelPrefab == null) return null;

            GameObject instance = Instantiate(type.groundModelPrefab, position, rotation);

            // GetComponentInChildren(true) findet auch inaktive Kinder -
            // ohne das wuerde ein deaktiviertes Pickup unbrauchbar bleiben.
            var pickup = instance.GetComponentInChildren<WorldMagazinePickup>(true);
            if (pickup == null)
            {
                pickup = instance.AddComponent<WorldMagazinePickup>();
            }

            pickup.magazineType = type;

            // Collider sicherstellen, sonst kann der Blick-Raycast nichts treffen
            if (pickup.GetComponentInChildren<Collider>(true) == null && instance.GetComponentInChildren<Collider>(true) == null)
            {
                var col = instance.AddComponent<BoxCollider>();
                col.size = Vector3.one * 0.15f;
            }

            if (randomRounds)
            {
                float fill = Mathf.Lerp(minFillFraction, 1f, (float)rng.NextDouble());
                pickup.currentRounds = Mathf.Max(1, Mathf.RoundToInt(type.baseCapacity * fill));
            }
            else
            {
                pickup.currentRounds = -1; // voll
            }

            if (logPlacements) Debug.Log($"[MagazineWorldSpawner] '{type.displayName}' platziert bei {position}.");

            return instance;
        }

        private bool TryFindPlacement(System.Random rng, Vector3 center, Transform player, List<Vector3> placed, out Vector3 position, out Quaternion rotation)
        {
            for (int attempt = 0; attempt < placementAttempts; attempt++)
            {
                float angle = (float)(rng.NextDouble() * Mathf.PI * 2.0);
                float radius = Mathf.Sqrt((float)rng.NextDouble()) * spawnAreaRadius;
                Vector3 candidate = center + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);

                if (player != null && Vector3.Distance(candidate, player.position) < minDistanceFromPlayer) continue;

                bool tooClose = false;
                foreach (var other in placed)
                {
                    if (Vector3.Distance(candidate, other) < minDistanceBetweenMagazines) { tooClose = true; break; }
                }
                if (tooClose) continue;

                Vector3 rayStart = candidate + Vector3.up * raycastStartHeight;
                if (!Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit,
                    Mathf.Max(1f, raycastStartHeight * 2f), groundLayers, QueryTriggerInteraction.Ignore)) continue;

                if (Vector3.Angle(hit.normal, Vector3.up) > maxGroundSlope) continue;

                position = hit.point + Vector3.up * heightAboveGround;
                rotation = Quaternion.Euler(0f, (float)(rng.NextDouble() * 360.0), 0f);
                return true;
            }

            position = Vector3.zero;
            rotation = Quaternion.identity;
            return false;
        }

        private Transform FindPlayerTransform()
        {
            var movement = FindFirstObjectByType<WAD.Player.TarkovMovementController>();
            return movement != null ? movement.transform : null;
        }

        private void OnDrawGizmosSelected()
        {
            Vector3 center = spawnAreaCenter != null ? spawnAreaCenter.position : transform.position;
            Gizmos.color = new Color(1f, 0.85f, 0.2f, 0.3f);
            Gizmos.DrawWireSphere(center, spawnAreaRadius);
        }
    }
}
