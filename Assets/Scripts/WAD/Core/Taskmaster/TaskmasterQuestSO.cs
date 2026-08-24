using System.Collections.Generic;
using UnityEngine;
using WAD.Core;
using WAD.Weapons;
using WAD.Weapons.Attachments;

namespace WAD.Taskmaster
{
    /// <summary>
    /// Eine Aufgabe des Taskmasters. Erstelle im Editor via:
    /// Assets > Create > WAD > Taskmaster Quest
    /// </summary>
    [CreateAssetMenu(fileName = "Quest_", menuName = "WAD/Taskmaster Quest")]
    public class TaskmasterQuestSO : ScriptableObject
    {
        [Header("Identifikation")]
        public string questId;
        public string title;
        [TextArea(2, 5)] public string description;

        [Header("Anforderungen (was der Spieler abgeben muss)")]
        public List<ItemRequirement> requirements = new List<ItemRequirement>();

        [Header("Belohnung: Items/Entities")]
        public List<ItemRequirement> itemRewards = new List<ItemRequirement>();

        [Header("Belohnung: Waffe/Attachment (optional)")]
        public WeaponSO weaponReward;
        public AttachmentSO attachmentReward;

        [Header("Belohnung: Kampfjet-Treibstoff (Punkt 2)")]
        public float fuelReward = 0f;
    }
}
