using UnityEngine;

namespace WAD.Core
{
    /// <summary>
    /// Zeigt/versteckt UI-Panels, OHNE sich dabei selbst lahmzulegen.
    ///
    /// Problem vorher: Mehrere UI-Skripte sitzen auf genau dem Panel, das sie ein-/ausblenden
    /// (z.B. InventoryUIController auf "InventoryPanel", WeaponInspectUI auf "WeaponInspectUI").
    /// GameObject.SetActive(false) schaltet dann auch das Skript ab - es kann sein Panel nie
    /// wieder einschalten. Schlimmer: Die automatische Panel-Suche von WeaponInspectUI lief bis
    /// zur Wurzel hoch und hat das GANZE Canvas deaktiviert (HUD, Fadenkreuz, Magazin-Anzeige, Inventar).
    ///
    /// Regeln hier:
    /// - Ein Root-Canvas wird nie ausgeblendet.
    /// - Liegt der Besitzer (owner) im Panel, bleibt das Panel aktiv und wird per CanvasGroup
    ///   unsichtbar gemacht (Alpha 0, keine Raycasts).
    /// - Sonst wird ganz normal SetActive verwendet.
    /// </summary>
    public static class UiPanelVisibility
    {
        public static bool IsRootCanvas(GameObject go)
        {
            if (go == null) return false;
            var canvas = go.GetComponent<Canvas>();
            return canvas != null && canvas.isRootCanvas;
        }

        public static void Set(GameObject panel, bool visible, Component owner)
        {
            if (panel == null) return;

            if (IsRootCanvas(panel))
            {
                Debug.LogWarning($"[UiPanelVisibility] '{panel.name}' ist ein Root-Canvas und wird nie aus-/eingeblendet " +
                                 "(sonst verschwindet das komplette HUD). Bitte ein eigenes Panel zuweisen.", owner);
                return;
            }

            bool hostsOwner = owner != null && owner.transform.IsChildOf(panel.transform);

            if (hostsOwner)
            {
                if (!panel.activeSelf) panel.SetActive(true);

                var group = panel.GetComponent<CanvasGroup>();
                if (group == null) group = panel.AddComponent<CanvasGroup>();

                float alpha = visible ? 1f : 0f;
                if (!Mathf.Approximately(group.alpha, alpha)) group.alpha = alpha;
                if (group.interactable != visible) group.interactable = visible;
                if (group.blocksRaycasts != visible) group.blocksRaycasts = visible;
                return;
            }

            if (panel.activeSelf != visible) panel.SetActive(visible);
        }
    }
}
