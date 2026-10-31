using UnityEngine;
using Unity.AI.Navigation;

namespace WAD.Procedural
{
    /// <summary>
    /// LEGACY-Platzhalter. Dieses Skript hat frueher bei jedem Aktivieren das
    /// NavMesh des Chunks gebacken - zusaetzlich zum ProceduralGridGenerator,
    /// sodass jeder Chunk doppelt (und bei jedem Recyceln erneut) gebacken wurde.
    ///
    /// Das Backen uebernimmt jetzt ausschliesslich der ProceduralGridGenerator
    /// (einmal pro Chunk-Instanz). Die Komponente bleibt nur bestehen, damit die
    /// vorhandenen Chunk-Prefabs keine "Missing Script"-Eintraege bekommen.
    /// Sie kann in den Prefabs gefahrlos entfernt werden; danach darf auch
    /// diese Datei geloescht werden.
    /// </summary>
    [RequireComponent(typeof(NavMeshSurface))]
    [AddComponentMenu("")] // nicht mehr im Add-Component-Menue anbieten
    public class ChunkNavMeshBaker : MonoBehaviour
    {
    }
}
