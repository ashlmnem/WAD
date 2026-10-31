using UnityEngine;
using UnityEngine.UI;

namespace WAD.Taskmaster
{
    /// <summary>
    /// Ein Eintrag in der scrollbaren Aufgabenliste des Taskmaster-Dialogs.
    /// Wird zur Laufzeit aus dem 'questEntryPrefab' erzeugt.
    /// </summary>
    public class QuestListEntryUI : MonoBehaviour
    {
        [Header("Referenzen (im Prefab belegen)")]
        public Button button;
        public Text titleText;
        [Tooltip("Optionales TextMeshPro-Element statt 'titleText'")]
        public TMPro.TMP_Text titleTextTMP;
        [Tooltip("Optionales Statusfeld (z.B. 'verfuegbar', 'erledigt')")]
        public Text statusText;
        [Tooltip("Optionales Icon fuer den Status")]
        public Image statusIcon;

        [Header("Status-Farben")]
        public Color availableColor = Color.white;
        public Color activeColor = new Color(1f, 0.85f, 0.4f);
        public Color readyColor = new Color(0.5f, 1f, 0.5f);
        public Color completedColor = new Color(0.55f, 0.55f, 0.55f);
        public Color lockedColor = new Color(0.35f, 0.35f, 0.35f);

        private TaskmasterQuestSO quest;
        private System.Action<TaskmasterQuestSO> onClick;

        public TaskmasterQuestSO Quest => quest;

        private void Awake()
        {
            if (button == null) button = GetComponent<Button>();
            if (button == null) button = GetComponentInChildren<Button>();

            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(HandleClick);
            }
        }

        public void Setup(TaskmasterQuestSO questData, string statusLabel, Color statusColor,
                          bool interactable, System.Action<TaskmasterQuestSO> clickHandler)
        {
            quest = questData;
            onClick = clickHandler;

            string label = quest != null ? quest.ListLabel : "(leer)";

            if (titleText != null) titleText.text = label;
            if (titleTextTMP != null) titleTextTMP.text = label;
            if (statusText != null) statusText.text = statusLabel;

            if (titleText != null) titleText.color = statusColor;
            if (titleTextTMP != null) titleTextTMP.color = statusColor;
            if (statusIcon != null) statusIcon.color = statusColor;

            if (button != null) button.interactable = interactable;
        }

        private void HandleClick()
        {
            onClick?.Invoke(quest);
        }
    }
}
