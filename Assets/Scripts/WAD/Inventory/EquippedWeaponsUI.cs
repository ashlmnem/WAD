using UnityEngine;
using UnityEngine.UI;
using WAD.Weapons;

namespace WAD.Inventory
{
    /// <summary>
    /// Zeigt die ausgeruesteten Waffen (PlayerWeaponHolder.equippedWeapons) als
    /// einfache Text-Liste im Inventar-Panel an.
    /// </summary>
    public class EquippedWeaponsUI : MonoBehaviour
    {
        public PlayerWeaponHolder weaponHolder;
        public Text weaponListText;

        private void Update()
        {
            if (weaponHolder == null || weaponListText == null) return;

            if (weaponHolder.equippedWeapons.Count == 0)
            {
                weaponListText.text = "Keine Waffen ausgerüstet";
                return;
            }

            string result = "";
            for (int i = 0; i < weaponHolder.equippedWeapons.Count; i++)
            {
                var weapon = weaponHolder.equippedWeapons[i];
                if (weapon == null || weapon.weaponData == null) continue;

                int current = weapon.loadedMagazine != null ? weapon.loadedMagazine.currentRounds : 0;
                // Kapazitaet kommt jetzt vom tatsaechlich geladenen Magazin (kann sich durch
                // Attachments/Magazin-Typ-Wechsel von der Basis-Kapazitaet unterscheiden),
                // Fallback auf den Standard-Magazintyp der Waffe falls nichts geladen ist.
                int capacity = weapon.loadedMagazine != null
                    ? weapon.loadedMagazine.capacity
                    : (weapon.weaponData.DefaultMagazineType != null ? weapon.weaponData.DefaultMagazineType.baseCapacity : 0);

                result += $"[{i + 1}] {weapon.weaponData.displayName} ({current}/{capacity})\n";
            }
            weaponListText.text = result;
        }
    }
}

