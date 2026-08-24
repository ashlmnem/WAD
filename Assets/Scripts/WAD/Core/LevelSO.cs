using System.Collections.Generic;
using UnityEngine;

namespace WAD.Core
{
    /// <summary>
    /// Datenobjekt fuer ein Level. Erstelle im Editor via:
    /// Assets > Create > WAD > Level
    /// </summary>
    [CreateAssetMenu(fileName = "Level_", menuName = "WAD/Level")]
    public class LevelSO : ScriptableObject
    {
        [Header("Identifikation")]
        public int levelIndex;
        public string levelName;          // z.B. "The Storm"
        public string subtitle;           // z.B. "Straight into the storm, brothers"
        [TextArea(3, 6)]
        public string flavorText;

        [Header("Szene")]
        public string sceneName;          // Unity-Szenenname zum Laden

        [Header("Environment")]
        public bool proceduralGeneration = true;
        public GameObject[] roomPrefabs;  // Bausteine fuer prozedurale Generierung

        [Header("Exits")]
        public List<ExitCondition> exits = new List<ExitCondition>();

        [Header("Loot-Pool")]
        public EntitySO[] possibleEntities;
    }
}
