using UnityEngine;

namespace WAD.Core
{
    /// <summary>
    /// Platzhalter fuer die "Weiter erkunden oder extrahieren?"-Entscheidung,
    /// die alle 5 Level angeboten wird (siehe RunStateManager.AdvanceToLevel).
    /// TODO: echtes UI (Canvas/Buttons) anbinden - hier nur die Logik-Schnittstelle.
    /// </summary>
    public class ExtractionDecisionUI : MonoBehaviour
    {
        public static ExtractionDecisionUI Instance { get; private set; }

        private void Awake()
        {
            Instance = this;
        }

        public void PromptDecision()
        {
            // TODO: Spielzeit pausieren, Panel einblenden mit zwei Buttons:
            // "Extrahieren" -> OnExtractChosen()
            // "Weiter erkunden" -> OnContinueChosen()
            Debug.Log("[ExtractionUI] Entscheidung faellig: Extrahieren oder weiter erkunden?");
        }

        public void OnExtractChosen()
        {
            RunStateManager.Instance.OnExtraction();
        }

        public void OnContinueChosen()
        {
            // Nichts weiter noetig, Run laeuft normal weiter
            Debug.Log("[ExtractionUI] Spieler erkundet weiter.");
        }
    }
}
