#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;

namespace WAD.EditorTools
{
    /// <summary>
    /// Erstellt einfache Platzhalter-Chunk-Prefabs (Straße, Ecke, Gebäudeblock)
    /// aus Unity-Primitives, damit die ProceduralGridGenerator sofort getestet
    /// werden kann, ohne dass echte 3D-Assets vorhanden sein müssen.
    ///
    /// Menü: WAD > Generate Placeholder Chunk Prefabs
    ///
    /// WICHTIG: Alle Chunks sind exakt chunkSize (Standard 50m) groß und haben
    /// an allen 4 Seiten eine 8m breite "Straßenöffnung" auf Bodenhöhe, damit
    /// sie sich beliebig aneinanderreihen lassen, egal welches Prefab an
    /// welcher Nachbarposition landet.
    /// </summary>
    public static class PlaceholderChunkGenerator
    {
        private const float ChunkSize = 50f;
        private const float RoadWidth = 8f;
        private const float WallHeight = 12f;
        private const string OutputFolder = "Assets/Prefabs/PlaceholderChunks";

        [MenuItem("WAD/Generate Placeholder Chunk Prefabs")]
        public static void GenerateAll()
{
    chunkMaterial = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);

    if (chunkMaterial == null)
    {
        Debug.LogError("Could not find material at: " + MaterialPath);
        return;
    }

    EnsureFolder();

    CreateStraightRoadChunk();
    CreateCornerChunk();
    CreateBuildingBlockChunk();
    CreateOpenSquareChunk();

    AssetDatabase.SaveAssets();
    AssetDatabase.Refresh();

    Debug.Log($"[WAD] Platzhalter-Chunks erstellt in {OutputFolder}");
}


        private static void EnsureFolder()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
                AssetDatabase.CreateFolder("Assets", "Prefabs");
            if (!AssetDatabase.IsValidFolder(OutputFolder))
                AssetDatabase.CreateFolder("Assets/Prefabs", "PlaceholderChunks");
        }

        // Chunk mit gerader Straße durch die Mitte (verbindet Norden<->Süden)
        private static void CreateStraightRoadChunk()
        {
            GameObject root = NewChunkRoot("Chunk_StraightRoad");

            AddGroundPlane(root, new Color(0.75f, 0.65f, 0.45f)); // Sand
            AddRoadStrip(root, Vector3.zero, new Vector3(RoadWidth, 0.1f, ChunkSize), new Color(0.35f, 0.33f, 0.3f));

            // Ruinen links und rechts der Straße
            AddRuinBlock(root, new Vector3(-ChunkSize * 0.28f, 0f, ChunkSize * 0.2f));
            AddRuinBlock(root, new Vector3(-ChunkSize * 0.28f, 0f, -ChunkSize * 0.2f));
            AddRuinBlock(root, new Vector3(ChunkSize * 0.28f, 0f, ChunkSize * 0.2f));
            AddRuinBlock(root, new Vector3(ChunkSize * 0.28f, 0f, -ChunkSize * 0.2f));

            SaveAsPrefab(root, "Chunk_StraightRoad");
        }

        // Chunk mit Kreuzung / Ecke (alle 4 Richtungen offen)
        private static void CreateCornerChunk()
        {
            GameObject root = NewChunkRoot("Chunk_Crossroad");

            AddGroundPlane(root, new Color(0.75f, 0.65f, 0.45f));
            AddRoadStrip(root, Vector3.zero, new Vector3(RoadWidth, 0.1f, ChunkSize), new Color(0.35f, 0.33f, 0.3f));
            AddRoadStrip(root, Vector3.zero, new Vector3(ChunkSize, 0.1f, RoadWidth), new Color(0.35f, 0.33f, 0.3f));

            AddRuinBlock(root, new Vector3(-ChunkSize * 0.28f, 0f, ChunkSize * 0.28f));
            AddRuinBlock(root, new Vector3(ChunkSize * 0.28f, 0f, ChunkSize * 0.28f));
            AddRuinBlock(root, new Vector3(-ChunkSize * 0.28f, 0f, -ChunkSize * 0.28f));
            AddRuinBlock(root, new Vector3(ChunkSize * 0.28f, 0f, -ChunkSize * 0.28f));

            SaveAsPrefab(root, "Chunk_Crossroad");
        }

        // Dichter Ruinen-/Gebäudeblock, Straße nur durchgehend N-S (schmalerer Durchgang)
        private static void CreateBuildingBlockChunk()
        {
            GameObject root = NewChunkRoot("Chunk_BuildingBlock");

            AddGroundPlane(root, new Color(0.72f, 0.62f, 0.42f));
            AddRoadStrip(root, Vector3.zero, new Vector3(RoadWidth, 0.1f, ChunkSize), new Color(0.35f, 0.33f, 0.3f));

            // Dichter gepackte Ruinen fuer engeres, bedrohlicheres Gefuehl
            for (int i = -1; i <= 1; i += 2)
            {
                for (int j = -2; j <= 2; j++)
                {
                    Vector3 pos = new Vector3(i * ChunkSize * 0.3f, 0f, j * (ChunkSize / 6f));
                    AddRuinBlock(root, pos, randomizeHeight: true);
                }
            }

            SaveAsPrefab(root, "Chunk_BuildingBlock");
        }

        // Seltener Chunk: kleine "Basis" mit Fahrzeugen/Loot-Marker (laut Design: niedriges Gewicht in der Verteilung)
        private static void CreateOpenSquareChunk()
        {
            GameObject root = NewChunkRoot("Chunk_SmallBase");

            AddGroundPlane(root, new Color(0.78f, 0.68f, 0.5f));
            AddRoadStrip(root, Vector3.zero, new Vector3(RoadWidth, 0.1f, ChunkSize), new Color(0.35f, 0.33f, 0.3f));
            AddRoadStrip(root, Vector3.zero, new Vector3(ChunkSize, 0.1f, RoadWidth), new Color(0.35f, 0.33f, 0.3f));

            // Platzhalter-Fahrzeug (einfacher Wuerfel-Stack) + Loot-Marker
            GameObject vehicle = GameObject.CreatePrimitive(PrimitiveType.Cube);
            vehicle.name = "PlaceholderVehicle";
            vehicle.transform.SetParent(root.transform);
            vehicle.transform.localPosition = new Vector3(ChunkSize * 0.2f, 0.75f, ChunkSize * 0.2f);
            vehicle.transform.localScale = new Vector3(2f, 1.5f, 4.5f);
            SetColor(vehicle, new Color(0.5f, 0.45f, 0.3f)); // Wuestentarn-Platzhalterfarbe

            GameObject lootMarker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            lootMarker.name = "LootSpawnMarker";
            lootMarker.transform.SetParent(root.transform);
            lootMarker.transform.localPosition = new Vector3(ChunkSize * 0.15f, 0.5f, ChunkSize * 0.1f);
            lootMarker.transform.localScale = Vector3.one * 0.5f;
            SetColor(lootMarker, Color.yellow);

            SaveAsPrefab(root, "Chunk_SmallBase");
        }

        // ---- Hilfsfunktionen ----

        private static GameObject NewChunkRoot(string name)
        {
            GameObject root = new GameObject(name);
            return root;
        }

        private static void AddGroundPlane(GameObject parent, Color color)
        {
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.SetParent(parent.transform);
            ground.transform.localPosition = Vector3.zero;
            // Unity-Plane ist 10x10 Units -> skalieren auf ChunkSize
            ground.transform.localScale = Vector3.one * (ChunkSize / 10f);
            SetColor(ground, color);
        }

        private static void AddRoadStrip(GameObject parent, Vector3 localPos, Vector3 size, Color color)
        {
            GameObject road = GameObject.CreatePrimitive(PrimitiveType.Cube);
            road.name = "RoadStrip";
            road.transform.SetParent(parent.transform);
            road.transform.localPosition = localPos + new Vector3(0f, 0.06f, 0f);
            road.transform.localScale = size;
            SetColor(road, color);
        }

        private static void AddRuinBlock(GameObject parent, Vector3 localPos, bool randomizeHeight = false)
        {
            float height = randomizeHeight ? Random.Range(4f, WallHeight) : WallHeight * 0.6f;
            GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            block.name = "RuinBlock";
            block.transform.SetParent(parent.transform);
            block.transform.localPosition = localPos + new Vector3(0f, height * 0.5f, 0f);
            block.transform.localScale = new Vector3(
                Random.Range(6f, 10f),
                height,
                Random.Range(6f, 10f));
            SetColor(block, new Color(0.6f, 0.52f, 0.4f));
        }

        private const string MaterialPath = "Assets/Materials/Desert.mat";
        private static Material chunkMaterial;


        private static void SetColor(GameObject obj, Color color)
{
    Renderer renderer = obj.GetComponent<Renderer>();

    if (renderer == null)
        return;

    Material instance = new Material(chunkMaterial);
    Debug.Log($"Loaded material: {chunkMaterial}");

    if (instance.HasProperty("_Color"))
    {
        instance.color = color;
    }

    if (instance.HasProperty("_BaseColor"))
    {
        instance.SetColor("_BaseColor", color);
    }

    renderer.sharedMaterial = instance;
}


        private static void SaveAsPrefab(GameObject root, string name)
        {
            string path = $"{OutputFolder}/{name}.prefab";
            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
        }
    }
}
#endif

