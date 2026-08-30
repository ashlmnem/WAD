using UnityEngine;
using UnityEngine.UI;
using WAD.Weapons;

namespace WAD.Weapons.UI
{
    /// <summary>
    /// Zeigt Munitionsinfo NUR waehrend 'V' gehalten wird. Reserve-Magazine
    /// zeigen jetzt ihren Typ-NAMEN (nicht nur Fuellstand) - wichtig, um
    /// Trommel-/Kurzmagazine im Vorrat auseinanderzuhalten (Punkt 8).
    /// </summary>
    public class WeaponInspectUI : MonoBehaviour
    {
        public PlayerWeaponHolder weaponHolder;
        public GameObject panelRoot;
        public Text weaponNameText;
        public Text ammoTypeText;
        public Text magazineFillText;
        public Text reserveMagazinesText;
        public KeyCode inspectKey = KeyCode.V;

        private void Start()
        {
            SetPanelVisible(false);
        }

        private void Update()
        {
            bool holdingInspect = Input.GetKey(inspectKey);
            SetPanelVisible(holdingInspect);

            if (holdingInspect)
            {
                RefreshDisplay();
            }
        }

        private void SetPanelVisible(bool visible)
        {
            if (panelRoot != null) panelRoot.SetActive(visible);
        }

        private void RefreshDisplay()
        {
            WeaponController active = weaponHolder != null ? weaponHolder.GetActiveWeapon() : null;
            if (active == null || active.weaponData == null)
            {
                SetPanelVisible(false);
                return;
            }

            if (weaponNameText != null)
            {
                weaponNameText.text = active.weaponData.displayName;
            }

            if (active.loadedMagazine != null && active.loadedMagazine.magazineType != null)
            {
                if (ammoTypeText != null) ammoTypeText.text = active.loadedMagazine.magazineType.displayName;
                if (magazineFillText != null)
                    magazineFillText.text = $"{active.loadedMagazine.currentRounds} / {active.loadedMagazine.capacity}";
            }
            else if (active.loadedMagazine != null)
            {
                if (ammoTypeText != null) ammoTypeText.text = "(Magazine Type fehlt!)";
                if (magazineFillText != null)
                    magazineFillText.text = $"{active.loadedMagazine.currentRounds} / -";
            }
            else
            {
                if (ammoTypeText != null) ammoTypeText.text = "Kein Magazin geladen";
                if (magazineFillText != null) magazineFillText.text = "-";
            }

            if (reserveMagazinesText != null)
            {
                if (active.reserveMagazines.Count == 0)
                {
                    reserveMagazinesText.text = "Keine Reserve-Magazine";
                }
                else
                {
                    string list = "";
                    foreach (var mag in active.reserveMagazines)
                    {
                        if (mag == null || mag.magazineType == null) continue;
                        list += $"{mag.magazineType.displayName}: {mag.currentRounds}/{mag.capacity}\n";
                    }
                    reserveMagazinesText.text = list.TrimEnd();
                }
            }
        }
    }
}