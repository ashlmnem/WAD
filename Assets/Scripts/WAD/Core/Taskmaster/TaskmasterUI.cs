using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using WAD.Inventory;
using WAD.Weapons;

namespace WAD.Taskmaster
{
    /// <summary>
    /// Dialogfenster des Taskmasters:
    ///  - scrollbare Liste ALLER aktuell angebotenen Aufgaben (links)
    ///  - Detailansicht der gewaehlten Aufgabe (rechts)
    ///  - "Abgeben"-Button (nur aktiv, wenn die Anforderungen erfuellt sind)
    ///  - "Schliessen"-Button, der das Fenster schliesst
    ///
    /// Alles im Inspector verdrahten - kein Code noetig. Die Liste wird
    /// automatisch aus den Quests des Taskmasters gefuellt.
    /// </summary>
    public class TaskmasterUI : MonoBehaviour
    {
        [Header("Fenster")]
        public GameObject panelRoot;
        [Tooltip("Button, der das Fenster schliesst")]
        public Button closeButton;
        [Tooltip("Alternativ/zusaetzlich: Taste zum Schliessen")]
        public KeyCode closeKey = KeyCode.Escape;

        [Header("Aufgabenliste (scrollbar)")]
        [Tooltip("Parent mit LayoutGroup/ContentSizeFitter INNERHALB der ScrollView")]
        public Transform questListContent;
        [Tooltip("Prefab mit QuestListEntryUI - wird pro Aufgabe erzeugt")]
        public GameObject questEntryPrefab;
        [Tooltip("Optionaler Text, wenn keine Aufgaben verfuegbar sind")]
        public GameObject emptyListLabel;

        [Header("Detailansicht")]
        public Text titleText;
        public Text descriptionText;
        public Text requirementsText;
        public Text rewardsText;
        [Tooltip("Zeile, in der der Taskmaster spricht")]
        public Text dialogueText;

        [Header("Buttons")]
        public Button turnInButton;
        public Button acceptButton;
        [Tooltip("Beschriftung des Abgeben-Buttons")]
        public Text turnInButtonLabel;

        [Header("Spieler-Referenzen")]
        public InventoryManager inventory;
        public PlayerWeaponHolder weaponHolder;

        [Header("Verhalten")]
        [Tooltip("Beim Oeffnen den ersten Eintrag automatisch auswaehlen")]
        public bool autoSelectFirst = true;
        [Tooltip("Mauszeiger beim Oeffnen freigeben")]
        public bool manageCursor = true;
        [Tooltip("Spieler-Eingaben waehrend des Dialogs blockieren (Waffe/Aiming)")]
        public bool blockPlayerInput = true;

        private TaskmasterController currentTaskmaster;
        private TaskmasterQuestSO currentQuest;
        private readonly List<GameObject> spawnedEntries = new List<GameObject>();
        private readonly List<TaskmasterQuestSO> displayedQuests = new List<TaskmasterQuestSO>();

        public bool IsOpen { get; private set; }

        private void Start()
        {
            if (inventory == null) inventory = FindFirstObjectByType<InventoryManager>();
            if (weaponHolder == null) weaponHolder = FindFirstObjectByType<PlayerWeaponHolder>();

            if (closeButton != null)
            {
                closeButton.onClick.RemoveAllListeners();
                closeButton.onClick.AddListener(Close);
            }

            if (turnInButton != null)
            {
                turnInButton.onClick.RemoveAllListeners();
                turnInButton.onClick.AddListener(HandleTurnInClicked);
            }

            if (acceptButton != null)
            {
                acceptButton.onClick.RemoveAllListeners();
                acceptButton.onClick.AddListener(HandleAcceptClicked);
            }

            SetOpen(false);
        }

        private void Update()
        {
            if (IsOpen && closeKey != KeyCode.None && Input.GetKeyDown(closeKey))
            {
                Close();
            }
        }

        // ------------------------------------------------------------------
        //  Oeffnen / Schliessen
        // ------------------------------------------------------------------

        public void Open(TaskmasterController taskmaster)
        {
            currentTaskmaster = taskmaster;
            currentQuest = null;

            SetOpen(true);
            RebuildQuestList();

            if (autoSelectFirst && displayedQuests.Count > 0)
            {
                SelectQuest(displayedQuests[0]);
            }
            else
            {
                RefreshDetail();
            }
        }

        /// <summary> Schliesst das Dialogfenster. Am 'Schliessen'-Button verknuepft. </summary>
        public void Close()
        {
            if (currentQuest != null && currentTaskmaster != null)
            {
                // Fortschritt gilt als angesehen
            }

            currentTaskmaster?.NotifyDialogueClosed();

            SetOpen(false);
            currentTaskmaster = null;
            currentQuest = null;
            ClearQuestList();
        }

        private void SetOpen(bool open)
        {
            IsOpen = open;

            if (panelRoot != null) panelRoot.SetActive(open);

            if (manageCursor)
            {
                Cursor.lockState = open ? CursorLockMode.None : CursorLockMode.Locked;
                Cursor.visible = open;
            }
        }

        // ------------------------------------------------------------------
        //  Aufgabenliste
        // ------------------------------------------------------------------

        public void RebuildQuestList()
        {
            ClearQuestList();

            if (currentTaskmaster == null || questListContent == null || questEntryPrefab == null)
            {
                if (emptyListLabel != null) emptyListLabel.SetActive(true);
                return;
            }

            var quests = currentTaskmaster.GetDisplayableQuests();
            displayedQuests.Clear();
            displayedQuests.AddRange(quests);

            if (emptyListLabel != null) emptyListLabel.SetActive(quests.Count == 0);

            foreach (var quest in quests)
            {
                if (quest == null) continue;

                GameObject entryObj = Instantiate(questEntryPrefab, questListContent);
                var entry = entryObj.GetComponent<QuestListEntryUI>();
                if (entry == null) entry = entryObj.AddComponent<QuestListEntryUI>();

                var status = currentTaskmaster.GetStatus(quest, inventory);
                bool unlocked = currentTaskmaster.IsUnlocked(quest);

                entry.Setup(
                    quest,
                    StatusLabel(status, unlocked),
                    StatusColor(entry, status, unlocked),
                    interactable: true, // auch gesperrte Aufgaben anklickbar, damit man sie lesen kann
                    clickHandler: SelectQuest);

                spawnedEntries.Add(entryObj);
            }
        }

        private void ClearQuestList()
        {
            foreach (var entry in spawnedEntries)
            {
                if (entry != null) Destroy(entry);
            }
            spawnedEntries.Clear();
            displayedQuests.Clear();
        }

        private static string StatusLabel(QuestStatus status, bool unlocked)
        {
            if (!unlocked) return "gesperrt";

            switch (status)
            {
                case QuestStatus.Completed: return "erledigt";
                case QuestStatus.ReadyToTurnIn: return "abgabebereit";
                case QuestStatus.Active: return "laeuft";
                default: return "verfuegbar";
            }
        }

        private static Color StatusColor(QuestListEntryUI entry, QuestStatus status, bool unlocked)
        {
            if (entry == null) return Color.white;
            if (!unlocked) return entry.lockedColor;

            switch (status)
            {
                case QuestStatus.Completed: return entry.completedColor;
                case QuestStatus.ReadyToTurnIn: return entry.readyColor;
                case QuestStatus.Active: return entry.activeColor;
                default: return entry.availableColor;
            }
        }

        // ------------------------------------------------------------------
        //  Detailansicht
        // ------------------------------------------------------------------

        /// <summary> Wird von einem Listen-Eintrag aufgerufen. </summary>
        public void SelectQuest(TaskmasterQuestSO quest)
        {
            currentQuest = quest;
            RefreshDetail();
        }

        public void NextQuest()
        {
            if (displayedQuests.Count == 0) return;

            int index = currentQuest != null ? displayedQuests.IndexOf(currentQuest) : -1;
            index = (index + 1) % displayedQuests.Count;
            SelectQuest(displayedQuests[index]);
        }

        public void PreviousQuest()
        {
            if (displayedQuests.Count == 0) return;

            int index = currentQuest != null ? displayedQuests.IndexOf(currentQuest) : 0;
            index = (index - 1 + displayedQuests.Count) % displayedQuests.Count;
            SelectQuest(displayedQuests[index]);
        }

        private void RefreshDetail()
        {
            if (currentTaskmaster == null || currentQuest == null)
            {
                if (titleText != null) titleText.text = "Keine Aufgabe ausgewaehlt";
                if (descriptionText != null) descriptionText.text = "";
                if (requirementsText != null) requirementsText.text = "";
                if (rewardsText != null) rewardsText.text = "";
                if (dialogueText != null) dialogueText.text = "";
                if (turnInButton != null) turnInButton.interactable = false;
                if (acceptButton != null) acceptButton.gameObject.SetActive(false);
                return;
            }

            bool unlocked = currentTaskmaster.IsUnlocked(currentQuest);
            var status = currentTaskmaster.GetStatus(currentQuest, inventory);
            bool completed = status == QuestStatus.Completed;
            bool canTurnIn = currentTaskmaster.CanTurnIn(currentQuest, inventory);

            if (titleText != null)
            {
                titleText.text = unlocked
                    ? currentQuest.title + (completed ? "  (erledigt)" : "")
                    : currentQuest.title + "  (gesperrt)";
            }

            if (descriptionText != null)
            {
                descriptionText.text = unlocked
                    ? currentQuest.description
                    : "Voraussetzungen nicht erfuellt.";
            }

            // Taskmaster-Zeile
            if (dialogueText != null)
            {
                if (!unlocked) dialogueText.text = "Dafuer bist du noch nicht bereit.";
                else if (completed) dialogueText.text = currentQuest.completionLine;
                else if (canTurnIn) dialogueText.text = string.IsNullOrEmpty(currentQuest.completionLine)
                    ? "Gut gemacht. Gib es her."
                    : currentQuest.completionLine;
                else dialogueText.text = string.IsNullOrEmpty(currentQuest.offerLine)
                    ? "Ich habe Arbeit fuer dich."
                    : currentQuest.offerLine;
            }

            // Anforderungen
            if (requirementsText != null)
            {
                string reqs = "";
                foreach (var req in currentQuest.requirements)
                {
                    var item = req.Item;
                    if (item == null) continue;

                    int have = inventory != null ? inventory.GetQuantity(item.ItemId) : 0;
                    reqs += $"{(have >= req.quantity ? "[x]" : "[ ]")} {item.DisplayName}: {have}/{req.quantity}\n";
                }

                if (currentQuest.requiredKills > 0)
                {
                    int kills = WAD.Core.RunStateManager.Instance != null ? WAD.Core.RunStateManager.Instance.killsThisRun : 0;
                    reqs += $"{(kills >= currentQuest.requiredKills ? "[x]" : "[ ]")} Gegner getoetet: {kills}/{currentQuest.requiredKills}\n";
                }

                if (!string.IsNullOrEmpty(currentQuest.requiredFlag))
                {
                    bool hasFlag = WAD.Core.RunStateManager.Instance != null && WAD.Core.RunStateManager.Instance.HasFlag(currentQuest.requiredFlag);
                    reqs += $"{(hasFlag ? "[x]" : "[ ]")} Bedingung: {currentQuest.requiredFlag}\n";
                }

                if (string.IsNullOrEmpty(reqs)) reqs = "Keine Anforderungen.";
                requirementsText.text = reqs;
            }

            // Belohnungen
            if (rewardsText != null)
            {
                string rewards = "";
                foreach (var reward in currentQuest.itemRewards)
                {
                    var item = reward.Item;
                    if (item == null) continue;
                    rewards += $"{item.DisplayName} x{reward.quantity}\n";
                }
                if (currentQuest.weaponReward != null) rewards += $"Waffe: {currentQuest.weaponReward.displayName}\n";
                if (currentQuest.attachmentReward != null) rewards += $"Attachment: {currentQuest.attachmentReward.displayName}\n";
                if (currentQuest.fuelReward > 0f) rewards += $"Treibstoff: +{currentQuest.fuelReward}\n";

                if (string.IsNullOrEmpty(rewards)) rewards = "Keine Belohnung.";
                rewardsText.text = rewards;
            }

            if (turnInButton != null)
            {
                turnInButton.interactable = canTurnIn && !completed;
                turnInButton.gameObject.SetActive(!completed);
            }

            if (turnInButtonLabel != null)
            {
                turnInButtonLabel.text = canTurnIn ? "Abgeben" : "Anforderungen fehlen";
            }

            if (acceptButton != null)
            {
                // Der "Annehmen"-Button ist nur sinnvoll, wenn die Aufgabe noch nicht laeuft
                acceptButton.gameObject.SetActive(!completed && status == QuestStatus.Available && unlocked);
            }
        }

        // ------------------------------------------------------------------
        //  Buttons
        // ------------------------------------------------------------------

        private void HandleTurnInClicked()
        {
            if (currentTaskmaster == null || currentQuest == null) return;

            if (currentTaskmaster.TurnIn(currentQuest, inventory, weaponHolder))
            {
                RebuildQuestList();
                RefreshDetail();
            }
        }

        private void HandleAcceptClicked()
        {
            if (currentTaskmaster == null || currentQuest == null) return;

            // "Annehmen" markiert die Aufgabe als laufend (fuer die Statusanzeige).
            // Der eigentliche Fortschritt haengt an den Anforderungen.
            RebuildQuestList();
            RefreshDetail();
        }
    }
}
