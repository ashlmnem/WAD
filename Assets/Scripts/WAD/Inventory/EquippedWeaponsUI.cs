using UnityEngine;
using UnityEngine.UI;
using WAD.Weapons;

namespace WAD.Inventory.UI
{
    /// <summary>
    /// Zeigt die ausgeruesteten Waffen (PlayerWeaponHolder.equippedWeapons) als
    /// einfache Text-Liste im Inventar-Panel. Ergaenzt InventoryUIController,
    /// welches nur die Rucksack-Items (stapelbares Loot) zeigt - Waffen sind
    /// separat, da sie keine IInventoryItem-Stacks sind.
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
                if (weapon.weaponData == null) continue;

                int current = weapon.loadedMagazine != null ? weapon.loadedMagazine.currentRounds : 0;
                int capacity = weapon.weaponData.magazineCapacity;

                result += $"[{i + 1}] {weapon.weaponData.displayName}  ({current}/{capacity})\n";
            }
            weaponListText.text = result;
        }
    }
}
