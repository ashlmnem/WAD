using UnityEngine;
using Unity.AI.Navigation;

namespace WAD.Procedural
{
    public class ProceduralChunkNavMesh : MonoBehaviour
    {
        private NavMeshSurface surface;

        private bool baked;


        private void Awake()
        {
            surface = GetComponent<NavMeshSurface>();

            if (surface == null)
            {
                surface = gameObject.AddComponent<NavMeshSurface>();
            }

            // Nur Objekte unter diesem Chunk benutzen
            surface.collectObjects = CollectObjects.Children;
        }


        public void Bake()
        {
            if (baked)
                return;

            surface.BuildNavMesh();

            baked = true;
        }


        public void ResetBake()
        {
            baked = false;
        }
    }
}
