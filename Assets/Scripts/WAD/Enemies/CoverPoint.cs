using UnityEngine;

namespace WAD.Enemies
{
    /// <summary>
    /// Platziere in der Welt (oder in Chunk-Prefabs) an Stellen, die tatsaechlich
    /// Deckung vor dem offenen Feld bieten (hinter Ruinen, Mauern etc.).
    /// EnemyCombatAI sucht sich freie CoverPoints zum Herumhuepfen.
    /// </summary>
    public class CoverPoint : MonoBehaviour
    {
        [HideInInspector] public EnemyController occupiedBy;
        public bool IsOccupied => occupiedBy != null;

        private void OnDrawGizmos()
        {
            Gizmos.color = IsOccupied ? Color.red : Color.green;
            Gizmos.DrawWireCube(transform.position, Vector3.one * 0.6f);
        }
    }
}
