using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using WAD.Inventory;
using WAD.Weapons;

namespace WAD.Taskmaster
{
    /// <summary>
    /// Einfaches Aufgaben-Panel: zeigt die aktuell verfuegbare Aufgabe des
    /// Taskmasters, mit dem der Spieler gerade interagiert, und einen
    /// "Abgeben"-Button (nur aktiv, wenn die Anforderungen erfuellt sind).
    /// </summary>
    public class TaskmasterUI : MonoBehaviour
    {
        [Header("Referenzen")]
        public GameObject panelRoot;
        public Text titleText;
        public Text descriptionText;
        public Text requirementsText;
        public Text rewardsText;
        public Button turnInButton;

        [Header("Spieler-Referenzen")]
        public InventoryManager inventory;
        public PlayerWeaponHolder weaponHolder;

        private TaskmasterController currentTaskmaster;
        private TaskmasterQuestSO currentQuest;
        private int currentQuestIndex;

        private void Start()
        {
            SetOpen(false);
            if (turnInButton != null) turnInButton.onClick.AddListener(HandleTurnInClicked);
        }

        public void Open(TaskmasterController taskmaster)
        {
            currentTaskmaster = taskmaster;
            currentQuestIndex = 0;
            SetOpen(true);
            RefreshDisplay();
        }

        public void Close()
        {
            SetOpen(false);
            currentTaskmaster = null;
        }

        public void NextQuest()
        {
            if (currentTaskmaster == null || currentTaskmaster.availableQuests.Count == 0) return;
            currentQuestIndex = (currentQuestIndex + 1) % currentTaskmaster.availableQuests.Count;
            RefreshDisplay();
        }

        private void SetOpen(bool open)
        {
            if (panelRoot != null) panelRoot.SetActive(open);
            Cursor.lockState = open ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = open;
        }

        private void RefreshDisplay()
        {
            if (currentTaskmaster == null || currentTaskmaster.availableQuests.Count == 0)
            {
                currentQuest = null;
                if (titleText != null) titleText.text = "Keine Aufgaben verfügbar";
                if (turnInButton != null) turnInButton.interactable = false;
                return;
            }

            currentQuest = currentTaskmaster.availableQuests[currentQuestIndex];

            bool completed = currentTaskmaster.IsCompleted(currentQuest);
            bool canTurnIn = currentTaskmaster.CanTurnIn(currentQuest, inventory);

            if (titleText != null) titleText.text = currentQuest.title + (completed ? " (erledigt)" : "");
            if (descriptionText != null) descriptionText.text = currentQuest.description;

            if (requirementsText != null)
            {
                string reqs = "";
                foreach (var req in currentQuest.requirements)
                {
                    if (req.Item == null) continue;
                    int have = inventory != null ? inventory.GetQuantity(req.Item.ItemId) : 0;
                    reqs += $"{req.Item.DisplayName}: {have}/{req.quantity}\n";
                }
                requirementsText.text = reqs;
            }

            if (rewardsText != null)
            {
                string rewards = "";
                foreach (var reward in currentQuest.itemRewards)
                {
                    if (reward.Item == null) continue;
                    rewards += $"{reward.Item.DisplayName} x{reward.quantity}\n";
                }
                if (currentQuest.weaponReward != null) rewards += $"Waffe: {currentQuest.weaponReward.displayName}\n";
                if (currentQuest.attachmentReward != null) rewards += $"Attachment: {currentQuest.attachmentReward.displayName}\n";
                if (currentQuest.fuelReward > 0f) rewards += $"Treibstoff: +{currentQuest.fuelReward}\n";
                rewardsText.text = rewards;
            }

            if (turnInButton != null) turnInButton.interactable = canTurnIn && !completed;
        }

        private void HandleTurnInClicked()
        {
            if (currentTaskmaster == null || currentQuest == null) return;

            if (currentTaskmaster.TurnIn(currentQuest, inventory, weaponHolder))
            {
                RefreshDisplay();
            }
        }
    }
}
