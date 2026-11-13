using System.Collections.Generic;
using UnityEngine;
using WAD.Core;
using WAD.Inventory;
using WAD.Weapons;
using WAD.Weapons.Attachments;

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
        private readonly HashSet<string> acceptedQuestIds = new HashSet<string>();
        private bool firstMeetingDone;

        public bool FirstMeetingDone => firstMeetingDone;

        public string InteractionPrompt => "F - Mit Taskmaster sprechen";

        private void Awake()
        {
            ResolveTaskmasterUI();
            if (cutscenePlayer == null) cutscenePlayer = FindFirstObjectByType<WAD.Cutscenes.CutscenePlayer>();

            ValidateQuests();
        }

        /// <summary> Meldet fehlerhaft konfigurierte Aufgaben direkt beim Start. </summary>
        private void ValidateQuests()
        {
            if (availableQuests == null) availableQuests = new List<TaskmasterQuestSO>();

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

                ValidateRequirements(quest, quest.requirements, "Anforderung");
                ValidateRequirements(quest, quest.itemRewards, "Belohnung");
            }
        }

        private void ValidateRequirements(TaskmasterQuestSO quest, List<ItemRequirement> requirements, string label)
        {
            if (requirements == null) return;
            foreach (ItemRequirement requirement in requirements)
            {
                if (requirement == null || (requirement.Item == null && requirement.MagazineType == null))
                {
                    Debug.LogError($"[Taskmaster:{name}] {label} in '{quest.title}' hat kein gueltiges Item/Magazin zugewiesen.");
                }
                else if (requirement.quantity <= 0)
                {
                    Debug.LogError($"[Taskmaster:{name}] {label} in '{quest.title}' muss eine Menge groesser 0 haben.");
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
            ResolveTaskmasterUI();

            if (taskmasterUI == null)
            {
                Debug.LogWarning($"[Taskmaster:{name}] Keine TaskmasterUI in der Szene gefunden - es kann kein Dialog geoeffnet werden.");
                return;
            }

            taskmasterUI.Open(this);
        }

        private void ResolveTaskmasterUI()
        {
            if (taskmasterUI != null)
            {
                var uiScene = taskmasterUI.gameObject.scene;
                if (!uiScene.IsValid() || !uiScene.isLoaded)
                {
                    taskmasterUI = Instantiate(taskmasterUI);
                    taskmasterUI.name = taskmasterUI.name.Replace("(Clone)", "").Trim();
                }
            }

            if (taskmasterUI == null)
            {
                taskmasterUI = FindFirstObjectByType<TaskmasterUI>(FindObjectsInactive.Include);
            }
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

            if (quest.requiredLevelIndex > 0 && (run == null || run.currentLevelIndex < quest.requiredLevelIndex)) return false;

            if (quest.prerequisiteQuests != null)
            {
                foreach (var prereq in quest.prerequisiteQuests)
                {
                    if (prereq != null && !IsCompleted(prereq)) return false;
                }
            }

            return true;
        }

        public QuestStatus GetStatus(TaskmasterQuestSO quest, InventoryManager inventory)
        {
            if (quest == null) return QuestStatus.Available;

            if (IsCompleted(quest)) return QuestStatus.Completed;
            if (!IsUnlocked(quest)) return QuestStatus.Available;

            if (AreRequirementsMet(quest, inventory)) return QuestStatus.ReadyToTurnIn;

            return acceptedQuestIds.Contains(quest.questId) ? QuestStatus.Active : QuestStatus.Available;
        }

        public bool AcceptQuest(TaskmasterQuestSO quest)
        {
            if (quest == null || !quest.HasValidId || IsCompleted(quest) || !IsUnlocked(quest)) return false;
            return acceptedQuestIds.Add(quest.questId);
        }

        /// <summary> Alle Anforderungen inkl. Kills und Flags erfuellt? </summary>
        private bool AreRequirementsMet(TaskmasterQuestSO quest, InventoryManager inventory)
        {
            if (quest == null) return false;

            // Item-Anforderungen
            if (quest.requirements != null)
            {
                PlayerWeaponHolder holder = inventory != null && inventory.weaponHolder != null
                    ? inventory.weaponHolder
                    : FindPlayerWeaponHolder();

                foreach (var req in quest.requirements)
                {
                    if (req == null || req.quantity <= 0) return false;

                    if (req.Item != null)
                    {
                        if (inventory == null || inventory.GetQuantity(req.Item.ItemId) < req.quantity) return false;
                    }
                    else if (req.MagazineType != null)
                    {
                        if (holder == null || holder.CountMagazines(req.MagazineType) < req.quantity) return false;
                    }
                    else return false;
                }
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
                if (run == null || !run.HasFlag(quest.requiredFlag)) return false;
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

            if (!AreRequirementsMet(quest, inventory)) return false;

            PlayerWeaponHolder holder = inventory != null && inventory.weaponHolder != null
                ? inventory.weaponHolder
                : FindPlayerWeaponHolder();

            if (quest.itemRewards != null)
            {
                foreach (ItemRequirement reward in quest.itemRewards)
                {
                    if (reward == null || reward.quantity <= 0) return false;
                    if (reward.Item != null)
                    {
                        if (inventory == null) return false;
                    }
                    else if (reward.MagazineType != null)
                    {
                        if (holder == null || !holder.CanAcceptMagazine(reward.MagazineType)) return false;
                    }
                    else return false;
                }
            }

            if (quest.weaponReward != null && (holder == null ||
                (quest.weaponReward.worldModelPrefab == null && quest.weaponReward.viewmodelPrefab == null))) return false;

            if (quest.attachmentReward != null && !CanGrantAttachment(quest.attachmentReward, holder)) return false;
            return true;
        }

        /// <summary> Gibt die Aufgabe ab: entfernt Anforderungen, gewaehrt Belohnungen. </summary>
        public bool TurnIn(TaskmasterQuestSO quest, InventoryManager inventory, PlayerWeaponHolder weaponHolder)
        {
            if (!CanTurnIn(quest, inventory)) return false;

            PlayerWeaponHolder holder = weaponHolder != null
                ? weaponHolder
                : inventory != null && inventory.weaponHolder != null
                    ? inventory.weaponHolder
                    : FindFirstObjectByType<PlayerWeaponHolder>();

            if (quest.requirements != null)
            {
                foreach (var req in quest.requirements)
                {
                    if (req == null) continue;
                    var item = req.Item;
                    if (item != null) inventory?.RemoveItem(item.ItemId, req.quantity);
                    else if (req.MagazineType != null) holder?.RemoveMagazines(req.MagazineType, req.quantity);
                }
            }

            if (quest.itemRewards != null)
            {
                foreach (var reward in quest.itemRewards)
                {
                    var item = reward != null ? reward.Item : null;
                    if (item != null) inventory?.AddItem(item, reward.quantity);
                    else if (reward != null && reward.MagazineType != null)
                    {
                        holder?.AddReserveMagazines(reward.MagazineType, reward.quantity);
                    }
                }
            }

            if (quest.weaponReward != null && holder != null)
            {
                GameObject prefab = quest.weaponReward.worldModelPrefab != null
                    ? quest.weaponReward.worldModelPrefab
                    : quest.weaponReward.viewmodelPrefab;
                GameObject instance = Instantiate(prefab);
                var controller = instance.GetComponentInChildren<WeaponController>(true);
                if (controller == null) controller = instance.AddComponent<WeaponController>();
                controller.weaponData = quest.weaponReward;
                EnsureWeaponAmmo(controller);
                holder.PickUpWeapon(controller);
            }

            if (quest.attachmentReward != null)
            {
                GrantAttachment(quest.attachmentReward, holder);
            }

            if (quest.fuelReward > 0f)
            {
                RunStateManager.Instance?.AddFuel(quest.fuelReward);
            }

            acceptedQuestIds.Remove(quest.questId);

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

            if (availableQuests == null) return result;
            foreach (var quest in availableQuests)
            {
                if (quest == null) continue;
                if (!showLockedQuests && !IsUnlocked(quest)) continue;
                result.Add(quest);
            }

            return result;
        }

        public void MarkFirstMeetingDone() => firstMeetingDone = true;

        private static PlayerWeaponHolder FindPlayerWeaponHolder() =>
            UnityEngine.Object.FindFirstObjectByType<PlayerWeaponHolder>();

        private static bool CanGrantAttachment(AttachmentSO attachment, PlayerWeaponHolder holder)
        {
            if (attachment == null || holder == null || holder.equippedWeapons == null) return false;
            foreach (WeaponController weapon in holder.equippedWeapons)
            {
                var manager = weapon != null ? weapon.GetComponent<WeaponAttachmentManager>() : null;
                if (manager == null || manager.railMounts == null || !attachment.IsCompatibleWith(weapon.weaponData)) continue;
                foreach (RailMount rail in manager.railMounts)
                {
                    if (rail != null && rail.Accepts(attachment.category)) return true;
                }
            }
            return false;
        }

        private static void GrantAttachment(AttachmentSO attachment, PlayerWeaponHolder holder)
        {
            foreach (WeaponController weapon in holder.equippedWeapons)
            {
                var manager = weapon != null ? weapon.GetComponent<WeaponAttachmentManager>() : null;
                if (manager == null || manager.railMounts == null || !attachment.IsCompatibleWith(weapon.weaponData)) continue;
                foreach (RailMount rail in manager.railMounts)
                {
                    if (rail != null && rail.Accepts(attachment.category) && manager.EquipAttachment(rail.railId, attachment)) return;
                }
            }
        }

        private static void EnsureWeaponAmmo(WeaponController weapon)
        {
            MagazineTypeSO defaultMagazine = weapon.weaponData != null ? weapon.weaponData.DefaultMagazineType : null;
            if (weapon.loadedMagazine == null || weapon.loadedMagazine.magazineType == null)
            {
                weapon.loadedMagazine = defaultMagazine != null ? new Magazine(defaultMagazine) : null;
            }
            if (weapon.reserveMagazines == null) weapon.reserveMagazines = new List<Magazine>();
            weapon.reserveMagazines.RemoveAll(magazine => magazine == null);
            if (defaultMagazine != null && weapon.reserveMagazines.Count == 0)
            {
                weapon.reserveMagazines.Add(new Magazine(defaultMagazine));
            }
        }
    }
}
