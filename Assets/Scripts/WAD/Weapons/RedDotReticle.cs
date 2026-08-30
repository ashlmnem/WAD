using UnityEngine;

namespace WAD.Weapons.Attachments
{
    /// <summary>
    /// Liegt auf dem "Punkt"-Quad/Sprite INNERHALB einer Optik (z.B. T-1,
    /// Holosight) - NICHT auf der Optik selbst. Blendet den Punkt nur ein,
    /// waehrend der Spieler tatsaechlich durch diese Optik zielt.
    ///
    /// Setup: Als Kind des Optik-Visual-Prefabs platzieren, WeaponController
    /// der aktuell haltenden Waffe zuweisen (oder leer lassen fuer Auto-Find).
    /// </summary>
    public class RedDotReticle : MonoBehaviour
    {
        [Tooltip("Leer lassen fuer automatisches Suchen in der Elternhierarchie")]
        public WeaponController weaponController;
        public Renderer dotRenderer;

        private void Awake()
        {
            if (dotRenderer == null) dotRenderer = GetComponent<Renderer>();
            if (weaponController == null) weaponController = GetComponentInParent<WeaponController>();
        }

        private void Update()
        {
            if (dotRenderer == null || weaponController == null) return;

            dotRenderer.enabled = weaponController.IsAiming;
        }
    }
}