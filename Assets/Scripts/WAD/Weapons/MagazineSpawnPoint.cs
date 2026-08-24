using UnityEngine;

namespace WAD.Procedural
{
    /// <summary> Analog zu LootSpawnPoint, aber speziell fuer Magazine. </summary>
    public class MagazineSpawnPoint : MonoBehaviour
    {
        [Range(0f, 1f)] public float spawnChance = 0.3f;

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, 0.3f);
        }
    }
}
