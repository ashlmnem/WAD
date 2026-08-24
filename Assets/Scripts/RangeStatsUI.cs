using UnityEngine;
using UnityEngine.UI;
using WAD.Weapons;

namespace WAD.ShootingRange
{
    /// <summary>
    /// Einfache Trefferstatistik fuer den Schiessstand: zaehlt globale Schuesse
    /// (ueber WeaponController.GlobalOnFired) und Treffer (ueber alle
    /// ShootingRangeTarget-Objekte in der Szene), zeigt Trefferquote an.
    /// </summary>
    public class RangeStatsUI : MonoBehaviour
    {
        public Text statsText;

        private int shotsFired;
        private int hits;

        private void OnEnable()
        {
            WeaponController.GlobalOnFired += HandleShotFired;

            foreach (var target in FindObjectsOfType<ShootingRangeTarget>())
            {
                target.OnHit += HandleTargetHit;
            }
        }

        private void OnDisable()
        {
            WeaponController.GlobalOnFired -= HandleShotFired;

            foreach (var target in FindObjectsOfType<ShootingRangeTarget>())
            {
                target.OnHit -= HandleTargetHit;
            }
        }

        private void HandleShotFired()
        {
            shotsFired++;
            RefreshText();
        }

        private void HandleTargetHit(ShootingRangeTarget target)
        {
            hits++;
            RefreshText();
        }

        private void RefreshText()
        {
            if (statsText == null) return;

            float accuracy = shotsFired > 0 ? (100f * hits / shotsFired) : 0f;
            statsText.text = $"Schüsse: {shotsFired}   Treffer: {hits}   Genauigkeit: {accuracy:F0}%";
        }

        /// <summary> Fuer einen "Reset"-Button im Range-UI. </summary>
        public void ResetStats()
        {
            shotsFired = 0;
            hits = 0;
            RefreshText();
        }
    }
}
