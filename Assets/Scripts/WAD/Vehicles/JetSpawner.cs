using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WAD.Procedural;

namespace WAD.Vehicles
{
    /// <summary>
    /// Verteilt Jets zufaellig in der Welt (mehrere Exemplare, verschiedene
    /// Bereiche). Legt man dieses Skript einmal in die Level-Szene, spawnt es
    /// beim Start die konfigurierte Anzahl Jets an zufaelligen, erreichbaren
    /// Punkten.
    ///
    /// Jeder Jet bekommt dabei einen eigenen Zufalls-Seed, wodurch JEDES
    /// Exemplar andere Ersatzteile verlangt.
    /// </summary>
    public class JetSpawner : MonoBehaviour
    {
        [Header("Jets")]
        [Tooltip("Basis-Prefab(s) des reparierbaren Jets. Bei mehreren wird zufaellig gewaehlt.")]
        public GameObject[] jetPrefabs = System.Array.Empty<GameObject>();

        [Tooltip("Wie viele Jets in diesem Level erscheinen (zufaellige Anzahl zwischen Min und Max)")]
        public Vector2Int spawnCountRange = new Vector2Int(1, 2);
        [Tooltip("Procedural grid to wait for before searching for ground. Empty = find one in the scene.")]
        public ProceduralGridGenerator gridGenerator;
        [Min(0f)] public float worldReadyTimeout = 8f;

        [Header("Platzierung")]
        [Tooltip("Mittelpunkt der Zufallssuche. Leer = dieses Objekt.")]
        public Transform spawnAreaCenter;
        [Tooltip("Radius um das Zentrum, in dem Jets platziert werden")]
        public float spawnAreaRadius = 400f;
        [Tooltip("Mindestabstand zum Spieler beim Start (Jets sollen gesucht werden)")]
        public float minDistanceFromPlayer = 80f;
        [Tooltip("Mindestabstand zwischen zwei Jets")]
        public float minDistanceBetweenJets = 60f;
        [Tooltip("Maximale Steigung, auf der ein Jet stehen darf (Grad)")]
        public float maxGroundSlope = 12f;

        [Header("Bodensuche")]
        [Tooltip("Layer, auf denen der Boden gesucht wird")]
        public LayerMask groundLayers = ~0;
        [Tooltip("Aus wie grosser Hoehe nach unten gesucht wird")]
        public float raycastStartHeight = 200f;
        [Tooltip("Hoehe des Jet-Wurzelpunkts ueber dem gefundenen Untergrund")]
        public float heightAboveGround = 0.4f;
        [Tooltip("Maximale Anzahl Versuche pro Jet")]
        public int placementAttempts = 60;

        [Header("Zufall")]
        [Tooltip("Fester Seed fuer reproduzierbare Platzierung. 0 = zufaellig.")]
        public int randomSeed = 0;
        [Tooltip("Sollen die Jets als Kinder dieses Objekts erzeugt werden?")]
        public bool parentToThis = true;

        [Header("Ersatzteile pro Jet")]
        [Tooltip("Seed-Bereich fuer die zufaelligen Reparaturanforderungen der Jets")]
        public Vector2Int repairSeedRange = new Vector2Int(1, 999999);

        [Header("Debug")]
        public bool logPlacements = true;

        private readonly List<Vector3> placedPositions = new List<Vector3>();
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
                SpawnJets();
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
            SpawnJets();
        }

        /// <summary> Kann auch manuell (z.B. per Button/Taste) ausgeloest werden. </summary>
        [ContextMenu("Jets jetzt spawnen")]
        public void SpawnJets()
        {
            if (jetPrefabs == null || jetPrefabs.Length == 0)
            {
                Debug.LogWarning($"[JetSpawner:{name}] Keine Jet-Prefabs zugewiesen - es wird nichts erzeugt.");
                return;
            }

            Transform player = FindPlayerTransform();
            Vector3 center = spawnAreaCenter != null
                ? spawnAreaCenter.position
                : player != null ? player.position : transform.position;
            var rng = randomSeed != 0 ? new System.Random(randomSeed) : new System.Random();

            int minCount = Mathf.Clamp(Mathf.Min(spawnCountRange.x, spawnCountRange.y), 0, 32);
            int maxCount = Mathf.Clamp(Mathf.Max(spawnCountRange.x, spawnCountRange.y), minCount, 32);
            int count = rng.Next(minCount, maxCount + 1);
            placedPositions.Clear();

            int spawned = 0;
            for (int i = 0; i < count; i++)
            {
                if (TryFindPlacement(rng, center, player, out Vector3 position, out Quaternion rotation))
                {
                    GameObject prefab = SelectValidPrefab(rng);
                    if (prefab == null) continue;

                    GameObject jet = Instantiate(prefab, position, rotation, parentToThis ? transform : null);
                    jet.name = $"{prefab.name}_Instance{i + 1}";

                    // Eigenen Reparatur-Seed setzen, damit jeder Jet andere Teile verlangt
                    var repair = jet.GetComponentInChildren<JetRepairSystem>(true);
                    if (repair != null)
                    {
                        int minSeed = Mathf.Min(repairSeedRange.x, repairSeedRange.y);
                        int maxSeed = Mathf.Max(repairSeedRange.x, repairSeedRange.y);
                        repair.randomSeed = rng.Next(minSeed, maxSeed == int.MaxValue ? maxSeed : maxSeed + 1);
                        repair.RollRequirements();
                    }

                    placedPositions.Add(position);
                    spawned++;

                    if (logPlacements) Debug.Log($"[JetSpawner] Jet {i + 1}/{count} platziert bei {position}.");
                }
                else if (logPlacements)
                {
                    Debug.LogWarning($"[JetSpawner] Kein gueltiger Platz fuer Jet {i + 1}/{count} gefunden " +
                                     "(Radius erhoehen, Bodenlayer pruefen oder minDistanceFromPlayer senken).");
                }
            }

            Debug.Log($"[JetSpawner:{name}] {spawned} von {count} Jets erzeugt.");
        }

        private GameObject SelectValidPrefab(System.Random rng)
        {
            GameObject selected = null;
            int validCount = 0;

            foreach (GameObject prefab in jetPrefabs)
            {
                if (prefab == null || prefab.GetComponentInChildren<JetController>(true) == null) continue;

                validCount++;
                if (rng.Next(0, validCount) == 0) selected = prefab;
            }

            if (selected == null)
            {
                Debug.LogWarning($"[JetSpawner:{name}] No configured prefab contains a JetController.", this);
            }

            return selected;
        }

        private bool TryFindPlacement(System.Random rng, Vector3 center, Transform player, out Vector3 position, out Quaternion rotation)
        {
            for (int attempt = 0; attempt < placementAttempts; attempt++)
            {
                // Zufaelliger Punkt in der Flaeche
                float angle = (float)(rng.NextDouble() * Mathf.PI * 2.0);
                float radius = Mathf.Sqrt((float)rng.NextDouble()) * spawnAreaRadius;
                Vector3 candidate = center + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);

                // Abstand zum Spieler pruefen
                if (player != null && Vector3.Distance(candidate, player.position) < minDistanceFromPlayer) continue;

                // Abstand zu anderen Jets pruefen
                bool tooClose = false;
                foreach (var other in placedPositions)
                {
                    if (Vector3.Distance(candidate, other) < minDistanceBetweenJets) { tooClose = true; break; }
                }
                if (tooClose) continue;

                // Boden finden
                Vector3 rayStart = candidate + Vector3.up * raycastStartHeight;
                if (!Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit,
                    Mathf.Max(1f, raycastStartHeight * 2f), groundLayers, QueryTriggerInteraction.Ignore))
                {
                    continue;
                }

                // Steigung pruefen
                float slope = Vector3.Angle(hit.normal, Vector3.up);
                if (slope > maxGroundSlope) continue;

                position = hit.point + Vector3.up * Mathf.Max(0f, heightAboveGround);
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
            Gizmos.color = new Color(0.2f, 0.6f, 1f, 0.35f);
            Gizmos.DrawWireSphere(center, spawnAreaRadius);

            Gizmos.color = Color.green;
            foreach (var pos in placedPositions) Gizmos.DrawWireCube(pos, Vector3.one * 3f);
        }
    }
}
