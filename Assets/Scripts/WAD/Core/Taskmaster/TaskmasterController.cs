using System.Collections.Generic;
using UnityEngine;
using WAD.Core;
using WAD.Inventory;
using WAD.Weapons;

namespace WAD.Taskmaster
{
    /// <summary>
    /// Liegt auf dem Taskmaster-NPC-Objekt. Haelt eine Liste moeglicher
    /// Aufgaben, prueft ob der Spieler die Anforderungen erfuellt und
    /// verteilt Belohnungen bei Abgabe.
    ///
    /// Neue Aufgaben hinzufuegen: Quest-Asset erstellen und in
    /// 'Available Quests' ziehen - kein Code noetig.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class TaskmasterController : MonoBehaviour, WAD.Inventory.IInteractable
    {
        [Header("Verfuegbare Aufgaben")]
        public List<TaskmasterQuestSO> availableQuests = new List<TaskmasterQuestSO>();

        [Header("UI")]
        [Tooltip("Wird beim Interagieren geoeffnet. Leer = automatisch in der Szene gesucht.")]
        public TaskmasterUI taskmasterUI;

        [Header("Cutscene")]
        [Tooltip("Cutscene, die beim ERSTEN Zusammentreffen abgespielt wird (kann leer bleiben)")]
        public WAD.Cutscenes.CutsceneSequence firstMeetingCutscene;
        [Tooltip("Cutscene-Player der Szene. Leer = automatisch gesucht.")]
        public WAD.Cutscenes.CutscenePlayer cutscenePlayer;
        [Tooltip("Wartezeit nach der Cutscene, bevor das Dialogfenster aufgeht")]
        public float dialogueDelayAfterCutscene = 0.4f;

        [Header("Verhalten")]
        [Tooltip("Aufgaben, deren Voraussetzungen fehlen, trotzdem in der Liste zeigen (ausgegraut)")]
        public bool showLockedQuests = true;
        [Tooltip("Beim Interagieren automatisch die Cutscene abspielen, falls noch nicht gesehen")]
        public bool playCutsceneOnFirstMeeting = true;

        private readonly HashSet<string> completedQuestIds = new HashSet<string>();
        private readonly HashSet<string> turnedInOnceIds = new HashSet<string>();
        private bool firstMeetingDone;

        public bool FirstMeetingDone => firstMeetingDone;

        public string InteractionPrompt => "F - Mit Taskmaster sprechen";

        private void Awake()
        {
            if (taskmasterUI == null) taskmasterUI = FindFirstObjectByType<TaskmasterUI>(FindObjectsInactive.Include);
            if (cutscenePlayer == null) cutscenePlayer = FindFirstObjectByType<WAD.Cutscenes.CutscenePlayer>();

            ValidateQuests();
        }

        /// <summary> Meldet fehlerhaft konfigurierte Aufgaben direkt beim Start. </summary>
        private void ValidateQuests()
        {
            var seen = new HashSet<string>();

            foreach (var quest in availableQuests)
            {
                if (quest == null) continue;

                if (!quest.HasValidId)
                {
                    Debug.LogError($"[Taskmaster:{name}] Aufgabe '{quest.name}' hat keine questId. " +
                                   "Ohne eindeutige ID werden Aufgaben im Fortschritt verwechselt. Bitte im Asset setzen.");
                    continue;
                }

                if (!seen.Add(quest.questId))
                {
                    Debug.LogError($"[Taskmaster:{name}] Doppelte questId '{quest.questId}' - Aufgaben muessen eindeutig sein.");
                }
            }
        }

        public void Interact(WAD.Inventory.PlayerInteraction interactor)
        {
            if (interactor == null) return;

            if (playCutsceneOnFirstMeeting && !firstMeetingDone && firstMeetingCutscene != null && cutscenePlayer != null)
            {
                StartCoroutine(PlayIntroThenOpenDialogue());
                return;
            }

            OpenDialogue();
        }

        private System.Collections.IEnumerator PlayIntroThenOpenDialogue()
        {
            firstMeetingDone = true;
            cutscenePlayer.Play(firstMeetingCutscene);

            while (cutscenePlayer.IsPlaying) yield return null;

            if (dialogueDelayAfterCutscene > 0f) yield return new WaitForSeconds(dialogueDelayAfterCutscene);

            OpenDialogue();
        }

        private void OpenDialogue()
        {
            if (taskmasterUI == null) taskmasterUI = FindFirstObjectByType<TaskmasterUI>(FindObjectsInactive.Include);

            if (taskmasterUI == null)
            {
                Debug.LogWarning($"[Taskmaster:{name}] Keine TaskmasterUI in der Szene gefunden - es kann kein Dialog geoeffnet werden.");
                return;
            }

            taskmasterUI.Open(this);
        }

        /// <summary> Wird von der UI aufgerufen, wenn der Schliessen-Button gedrueckt wird. </summary>
        public void NotifyDialogueClosed()
        {
            // Platzhalter fuer spaetere Logik (z.B. Sound, Flag setzen)
        }

        // ------------------------------------------------------------------
        //  Aufgaben-Status
        // ------------------------------------------------------------------

        public bool IsCompleted(TaskmasterQuestSO quest)
        {
            if (quest == null || !quest.HasValidId) return false;
            return completedQuestIds.Contains(quest.questId);
        }

        /// <summary> Ist die Aufgabe ueberhaupt sichtbar (Voraussetzungen erfuellt)? </summary>
        public bool IsUnlocked(TaskmasterQuestSO quest)
        {
            if (quest == null) return false;

            var run = RunStateManager.Instance;

            if (quest.requiredLevelIndex > 0 && run != null && run.currentLevelIndex < quest.requiredLevelIndex) return false;

            foreach (var prereq in quest.prerequisiteQuests)
            {
                if (prereq == null) continue;
                if (!IsCompleted(prereq)) return false;
            }

            return true;
        }

        public QuestStatus GetStatus(TaskmasterQuestSO quest, InventoryManager inventory)
        {
            if (quest == null) return QuestStatus.Available;

            if (IsCompleted(quest)) return QuestStatus.Completed;
            if (!IsUnlocked(quest)) return QuestStatus.Available;

            if (AreRequirementsMet(quest, inventory)) return QuestStatus.ReadyToTurnIn;

            return turnedInOnceIds.Contains(quest.questId) ? QuestStatus.Active : QuestStatus.Available;
        }

        /// <summary> Alle Anforderungen inkl. Kills und Flags erfuellt? </summary>
        private bool AreRequirementsMet(TaskmasterQuestSO quest, InventoryManager inventory)
        {
            if (quest == null) return false;

            // Item-Anforderungen
            foreach (var req in quest.requirements)
            {
                var item = req.Item;
                if (item == null) continue;
                if (inventory == null) return false;
                if (inventory.GetQuantity(item.ItemId) < req.quantity) return false;
            }

            // Kill-Anforderung
            if (quest.requiredKills > 0)
            {
                var run = RunStateManager.Instance;
                int kills = run != null ? run.killsThisRun : 0;
                if (kills < quest.requiredKills) return false;
            }

            // Flag-Anforderung
            if (!string.IsNullOrEmpty(quest.requiredFlag))
            {
                var run = RunStateManager.Instance;
                if (run != null && !run.HasFlag(quest.requiredFlag)) return false;
            }

            return true;
        }

        /// <summary> Prueft, ob der Spieler alle Anforderungen einer Aufgabe erfuellt. </summary>
        public bool CanTurnIn(TaskmasterQuestSO quest, InventoryManager inventory)
        {
            if (quest == null) return false;
            if (!quest.HasValidId)
            {
                Debug.LogError($"[Taskmaster] Aufgabe '{quest.name}' hat keine questId - Abgabe verweigert.");
                return false;
            }

            if (IsCompleted(quest)) return false;
            if (!IsUnlocked(quest)) return false;

            return AreRequirementsMet(quest, inventory);
        }

        /// <summary> Gibt die Aufgabe ab: entfernt Anforderungen, gewaehrt Belohnungen. </summary>
        public bool TurnIn(TaskmasterQuestSO quest, InventoryManager inventory, PlayerWeaponHolder weaponHolder)
        {
            if (!CanTurnIn(quest, inventory)) return false;

            foreach (var req in quest.requirements)
            {
                var item = req.Item;
                if (item == null) continue;
                inventory.RemoveItem(item.ItemId, req.quantity);
            }

            foreach (var reward in quest.itemRewards)
            {
                var item = reward.Item;
                if (item == null) continue;
                inventory.AddItem(item, reward.quantity);
            }

            if (quest.weaponReward != null && weaponHolder != null && quest.weaponReward.viewmodelPrefab != null)
            {
                GameObject instance = Instantiate(quest.weaponReward.viewmodelPrefab);
                var controller = instance.GetComponent<WeaponController>();
                if (controller != null) weaponHolder.PickUpWeapon(controller);
            }

            if (quest.attachmentReward != null)
            {
                var active = weaponHolder?.GetActiveWeapon();
                Debug.Log($"[Taskmaster] Attachment-Belohnung '{quest.attachmentReward.displayName}' erhalten - manuell an einer Rail montieren.");
                // Bewusst kein Auto-Mount: der Spieler waehlt selbst die passende Rail.
            }

            if (quest.fuelReward > 0f)
            {
                RunStateManager.Instance?.AddFuel(quest.fuelReward);
            }

            turnedInOnceIds.Add(quest.questId);

            if (!quest.repeatable)
            {
                completedQuestIds.Add(quest.questId);
                Debug.Log($"[Taskmaster] Aufgabe '{quest.title}' abgeschlossen.");
            }
            else
            {
                Debug.Log($"[Taskmaster] Wiederholbare Aufgabe '{quest.title}' abgegeben.");
            }

            return true;
        }

        /// <summary> Aufgaben, die im UI angezeigt werden sollen (optional gefiltert). </summary>
        public List<TaskmasterQuestSO> GetDisplayableQuests()
        {
            var result = new List<TaskmasterQuestSO>();

            foreach (var quest in availableQuests)
            {
                if (quest == null) continue;
                if (!showLockedQuests && !IsUnlocked(quest)) continue;
                result.Add(quest);
            }

            return result;
        }

        public void MarkFirstMeetingDone() => firstMeetingDone = true;
    }
}
