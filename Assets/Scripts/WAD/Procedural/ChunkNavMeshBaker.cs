using UnityEngine;
using Unity.AI.Navigation;

namespace WAD.Procedural
{
    /// <summary>
    /// Liegt auf jedem Chunk-Prefab (neben ProceduralGridGenerator's Chunk-Root).
    /// Backt beim Aktivieren automatisch ein lokales NavMesh fuer diesen Chunk.
    /// Mehrere Chunk-NavMeshes mit derselben Agent Type ID verschmelzen zu einer
    /// durchgehend begehbaren Flaeche, sobald benachbarte Chunks geladen sind.
    ///
    /// WICHTIG: Braucht das "AI Navigation"-Package (Package Manager).
    /// Jedes Chunk-Prefab braucht eine NavMeshSurface-Komponente (wird hier
    /// automatisch hinzugefuegt, falls sie fehlt).
    /// </summary>
    [RequireComponent(typeof(NavMeshSurface))]
    public class ChunkNavMeshBaker : MonoBehaviour
    {
        private NavMeshSurface surface;

        private void Awake()
        {
            surface = GetComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.Children;
            // Collider-basierte Geometrie statt Mesh-Geometrie: vermeidet den
            // "does not allow read access"-Fehler beim Runtime-Baking, da
            // Collider-Formen (BoxCollider etc.) keine lesbaren Mesh-Daten
            // brauchen - wichtig gerade fuer die Platzhalter-Primitives.
            surface.useGeometry = UnityEngine.AI.NavMeshCollectGeometry.PhysicsColliders;
        }

        private void OnEnable()
        {
            // Ein Frame warten, damit alle Kind-Objekte (Ruinen, Strassen etc.)
            // sicher aktiv/positioniert sind, bevor gebackt wird.
            Invoke(nameof(Bake), 0.01f);
        }

        private void Bake()
        {
            if (surface != null)
            {
                surface.BuildNavMesh();
            }
        }
    }
}