using UnityEngine;
using UnityEngine.UI;
using WAD.Weapons;

namespace WAD.Weapons.UI
{
    /// <summary>
    /// Simple HUD-Anzeige "12 / 45" (aktuelles Magazin / Reserve-Munition gesamt).
    /// Auf ein Text-UI-Element in einem HUD-Canvas ziehen, weaponHolder zuweisen.
    /// Bindet sich automatisch an die jeweils aktive Waffe.
    /// </summary>
    public class AmmoHUD : MonoBehaviour
    {
        [Header("Referenzen")]
        public PlayerWeaponHolder weaponHolder;
        public Text ammoText;

        private WeaponController currentlySubscribed;

        private void Update()
        {
            WeaponController active = weaponHolder != null ? weaponHolder.GetActiveWeapon() : null;

            if (active != currentlySubscribed)
            {
                if (currentlySubscribed != null) currentlySubscribed.OnAmmoChanged -= UpdateText;
                if (active != null)
                {
                    active.OnAmmoChanged += UpdateText;
                    // Sofort aktuellen Stand anzeigen
                    int current = active.loadedMagazine != null ? active.loadedMagazine.currentRounds : 0;
                    int reserve = 0;
                    foreach (var mag in active.reserveMagazines) reserve += mag.currentRounds;
                    UpdateText(current, reserve);
                }
                else
                {
                    if (ammoText != null) ammoText.text = "";
                }
                currentlySubscribed = active;
            }
        }

        private void UpdateText(int current, int reserve)
        {
            if (ammoText != null) ammoText.text = $"{current} / {reserve}";
        }
    }
}
