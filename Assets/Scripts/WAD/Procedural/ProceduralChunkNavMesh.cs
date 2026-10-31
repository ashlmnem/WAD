using UnityEngine;

namespace WAD.Procedural
{
    /// <summary>
    /// LEGACY-Platzhalter. Hat nie gebackt (Bake() wurde von keinem Skript
    /// aufgerufen) und fuegte in Awake nur eine NavMeshSurface hinzu, die der
    /// ProceduralGridGenerator ohnehin selbst anlegt und konfiguriert.
    /// Bleibt nur bestehen, damit die Chunk-Prefabs keine "Missing Script"-
    /// Eintraege bekommen - kann dort entfernt und danach geloescht werden.
    /// </summary>
    [AddComponentMenu("")]
    public class ProceduralChunkNavMesh : MonoBehaviour
    {
    }
}
