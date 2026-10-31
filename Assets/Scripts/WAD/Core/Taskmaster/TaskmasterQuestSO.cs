using System.Collections.Generic;
using UnityEngine;
using WAD.Core;
using WAD.Weapons;
using WAD.Weapons.Attachments;

namespace WAD.Taskmaster
{
    /// <summary>
    /// Status einer Aufgabe - wird zur Laufzeit vom TaskmasterController gefuehrt.
    /// </summary>
    public enum QuestStatus
    {
        /// <summary>Noch nicht angenommen.</summary>
        Available,
        /// <summary>Angenommen, Anforderungen noch nicht erfuellt.</summary>
        Active,
        /// <summary>Alle Anforderungen erfuellt, kann abgegeben werden.</summary>
        ReadyToTurnIn,
        /// <summary>Abgeschlossen (keine Wiederholung).</summary>
        Completed
    }

    /// <summary>
    /// Eine Aufgabe des Taskmasters. Erstelle im Editor via:
    /// Assets > Create > WAD > Taskmaster Quest
    /// </summary>
    [CreateAssetMenu(fileName = "Quest_", menuName = "WAD/Taskmaster Quest")]
    public class TaskmasterQuestSO : ScriptableObject
    {
        [Header("Identifikation")]
        [Tooltip("EINDEUTIGE ID! Leer lassen fuehrt dazu, dass Aufgaben sich gegenseitig ueberschreiben.")]
        public string questId;
        public string title;
        [TextArea(2, 5)] public string description;

        [Header("Dialog")]
        [TextArea(2, 5)]
        [Tooltip("Was der Taskmaster sagt, wenn er diese Aufgabe anbietet")]
        public string offerLine;
        [TextArea(2, 5)]
        [Tooltip("Was der Taskmaster sagt, wenn der Spieler die Aufgabe abgibt")]
        public string completionLine;
        [TextArea(2, 5)]
        [Tooltip("Was der Taskmaster sagt, solange die Anforderungen fehlen")]
        public string inProgressLine;

        [Header("Anforderungen (was der Spieler abgeben muss)")]
        public List<ItemRequirement> requirements = new List<ItemRequirement>();

        [Header("Zusatz-Anforderungen (optional)")]
        [Tooltip("So viele Gegner muss der Spieler fuer diese Aufgabe toeten (0 = keine Anforderung)")]
        public int requiredKills = 0;
        [Tooltip("Dieses RunState-Flag muss gesetzt sein (leer = keine Anforderung)")]
        public string requiredFlag = "";

        [Header("Voraussetzungen")]
        [Tooltip("Diese Aufgaben muessen zuerst abgeschlossen sein, bevor diese hier erscheint")]
        public List<TaskmasterQuestSO> prerequisiteQuests = new List<TaskmasterQuestSO>();
        [Tooltip("Mindest-Level-Index im Run (0 = immer verfuegbar)")]
        public int requiredLevelIndex = 0;

        [Header("Belohnung: Items/Entities")]
        public List<ItemRequirement> itemRewards = new List<ItemRequirement>();

        [Header("Belohnung: Waffe/Attachment (optional)")]
        public WeaponSO weaponReward;
        public AttachmentSO attachmentReward;

        [Header("Belohnung: Kampfjet-Treibstoff (Punkt 2)")]
        public float fuelReward = 0f;

        [Header("Sonstiges")]
        [Tooltip("Kann mehrfach abgegeben werden")]
        public bool repeatable = false;
        [Tooltip("Kurztext fuer die Aufgabenliste im UI")]
        public string listSummary;

        public bool HasValidId => !string.IsNullOrEmpty(questId);

        /// <summary> Kurzzeile fuer die scrollbare Aufgabenliste. </summary>
        public string ListLabel => !string.IsNullOrEmpty(listSummary)
            ? listSummary
            : (string.IsNullOrEmpty(title) ? name : title);
    }
}
