using UnityEngine;
using UnityEngine.UI;
using TMP_Text = TMPro.TMP_Text;

namespace WAD.Cutscenes
{
    /// <summary>
    /// Zeigt Untertitel/Dialogzeilen fuer Cutscenes an. Unterstuetzt sowohl
    /// TextMeshPro- als auch klassische UI-Text-Elemente - einfach das passende
    /// Feld belegen. Alles im Inspector einstellbar.
    /// </summary>
    public class CutsceneSubtitleUI : MonoBehaviour
    {
        [Header("Referenzen")]
        [Tooltip("Panel, das ein-/ausgeblendet wird (leer = dieses Objekt)")]
        public GameObject panelRoot;
        [Tooltip("TextMeshPro-Element (bevorzugt)")]
        public TMP_Text tmpText;
        [Tooltip("Klassisches UI-Text-Element (Alternative zu TMP)")]
        public Text legacyText;

        [Header("Darstellung")]
        [Tooltip("Ein-/Ausblendzeit in Sekunden")]
        public float fadeDuration = 0.25f;
        [Tooltip("Zusanzeige-Dauer fuer Buchstaben-Einblendung (0 = sofort)")]
        public float typewriterSpeed = 45f;
        [Tooltip("Zeilenabstand der Untertitelbox zum unteren Rand in Pixeln")]
        public float bottomMargin = 120f;

        private CanvasGroup canvasGroup;
        private string fullText = "";
        private float typewriterTimer;
        private float hideTimer;

        private void Awake()
        {
            if (panelRoot == null) panelRoot = gameObject;

            canvasGroup = panelRoot.GetComponent<CanvasGroup>();
            if (canvasGroup == null && panelRoot != this.gameObject)
            {
                canvasGroup = panelRoot.AddComponent<CanvasGroup>();
            }

            SetAlpha(0f);
            SetActive(false);
        }

        /// <summary> Zeigt eine Zeile. duration &lt;= 0 = bleibt bis Clear(). </summary>
        public void Show(string text, float duration = 0f)
        {
            fullText = text ?? "";
            typewriterTimer = 0f;
            hideTimer = duration;

            SetActive(true);
            SetAlpha(1f);

            if (typewriterSpeed <= 0f)
            {
                SetText(fullText);
            }
            else
            {
                SetText("");
            }
        }

        public void Clear()
        {
            fullText = "";
            hideTimer = 0f;
            SetText("");
            SetAlpha(0f);
            SetActive(false);
        }

        private void Update()
        {
            if (string.IsNullOrEmpty(fullText)) return;

            // Schreibmaschinen-Effekt
            if (typewriterSpeed > 0f && GetText().Length < fullText.Length)
            {
                typewriterTimer += Time.unscaledDeltaTime * typewriterSpeed;
                int chars = Mathf.Clamp(Mathf.FloorToInt(typewriterTimer), 0, fullText.Length);
                SetText(fullText.Substring(0, chars));
            }

            // Auto-Ausblenden
            if (hideTimer > 0f)
            {
                hideTimer -= Time.deltaTime;
                if (hideTimer <= 0f) Clear();
            }

            // Weiches Einblenden
            if (canvasGroup != null && canvasGroup.alpha < 1f && fadeDuration > 0f)
            {
                SetAlpha(Mathf.MoveTowards(canvasGroup.alpha, 1f, Time.deltaTime / fadeDuration));
            }
        }

        private void SetActive(bool active)
        {
            if (panelRoot != null && panelRoot.activeSelf != active) panelRoot.SetActive(active);
        }

        private void SetAlpha(float a)
        {
            if (canvasGroup != null) canvasGroup.alpha = a;
        }

        private void SetText(string value)
        {
            if (tmpText != null) tmpText.text = value;
            else if (legacyText != null) legacyText.text = value;
        }

        private string GetText()
        {
            if (tmpText != null) return tmpText.text ?? "";
            if (legacyText != null) return legacyText.text ?? "";
            return "";
        }
    }
}
