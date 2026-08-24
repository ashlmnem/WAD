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
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class TaskmasterController : MonoBehaviour, WAD.Inventory.IInteractable
    {
        [Header("Verfuegbare Aufgaben")]
        public List<TaskmasterQuestSO> availableQuests = new List<TaskmasterQuestSO>();

        [Header("UI (in der Level-Szene, siehe TaskmasterUI)")]
        public TaskmasterUI taskmasterUI;

        private readonly HashSet<string> completedQuestIds = new HashSet<string>();

        public string InteractionPrompt => "F - Mit Taskmaster sprechen";

        public void Interact(WAD.Inventory.PlayerInteraction interactor)
        {
            if (taskmasterUI == null)
            {
                taskmasterUI = FindObjectOfType<TaskmasterUI>();
            }
            taskmasterUI?.Open(this);
        }

        public bool IsCompleted(TaskmasterQuestSO quest) => completedQuestIds.Contains(quest.questId);

        /// <summary> Prueft, ob der Spieler alle Anforderungen einer Aufgabe im Inventar hat. </summary>
        public bool CanTurnIn(TaskmasterQuestSO quest, InventoryManager inventory)
        {
            if (quest == null || inventory == null) return false;
            if (IsCompleted(quest)) return false;

            foreach (var req in quest.requirements)
            {
                if (req.Item == null) continue;
                if (inventory.GetQuantity(req.Item.ItemId) < req.quantity) return false;
            }
            return true;
        }

        /// <summary> Gibt die Aufgabe ab: entfernt Anforderungen, gewaehrt Belohnungen. </summary>
        public bool TurnIn(TaskmasterQuestSO quest, InventoryManager inventory, PlayerWeaponHolder weaponHolder)
        {
            if (!CanTurnIn(quest, inventory)) return false;

            foreach (var req in quest.requirements)
            {
                if (req.Item == null) continue;
                inventory.RemoveItem(req.Item.ItemId, req.quantity);
            }

            foreach (var reward in quest.itemRewards)
            {
                if (reward.Item == null) continue;
                inventory.AddItem(reward.Item, reward.quantity);
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
                // Bewusst kein Auto-Mount: der Spieler waehlt selbst die passende Rail,
                // da Attachments jetzt pro Rail montiert werden (siehe Rail-System).
            }

            if (quest.fuelReward > 0f)
            {
                RunStateManager.Instance?.AddFuel(quest.fuelReward);
            }

            completedQuestIds.Add(quest.questId);
            Debug.Log($"[Taskmaster] Aufgabe '{quest.title}' abgeschlossen.");
            return true;
        }
    }
}
