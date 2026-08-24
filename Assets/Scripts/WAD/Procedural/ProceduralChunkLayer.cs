using UnityEngine;

namespace WAD.Procedural
{
    public class ProceduralChunkLayer : MonoBehaviour
    {
        public ChunkLayerType layerType;
    }


    public enum ChunkLayerType
    {
        BuildingBlock,
        Crossroad,
        SmallBase,
        StraightRoad
    }
}
