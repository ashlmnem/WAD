using UnityEngine;
using WAD.Weapons;

namespace WAD.Inventory
{
    /// <summary>
    /// Liegt auf einem Magazin-Objekt in der Welt (z.B. gefunden in einer Kiste).
    /// Anders als generisches Loot (WorldItemPickup) landet ein Magazin NICHT im
    /// InventoryManager, sondern direkt in der reserveMagazines-Liste der
    /// passenden Waffe - Magazine sind kein stapelbares IInventoryItem, sondern
    /// eigene Objekte mit individuellem Fuellstand.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class WorldMagazinePickup : MonoBehaviour, IInteractable
    {
        [Header("Magazin-Daten")]
        public AmmoTypeSO ammoType;
        public int capacity = 15;
        [Tooltip("-1 = voll. Sonst exakte Anzahl Patronen in diesem gefundenen Magazin.")]
        public int currentRounds = -1;

        public string InteractionPrompt => $"F - Magazin ({ammoType?.displayName}) aufheben";

        public void Interact(PlayerInteraction interactor)
        {
            TryPickUp(interactor.weaponHolder);
        }

        public bool TryPickUp(PlayerWeaponHolder weaponHolder)
        {
            if (weaponHolder == null || ammoType == null) return false;

            foreach (var weapon in weaponHolder.equippedWeapons)
            {
                if (weapon != null && weapon.weaponData != null && weapon.weaponData.compatibleAmmoType == ammoType)
                {
                    var magazine = new Magazine(ammoType, capacity, currentRounds);
                    weapon.reserveMagazines.Add(magazine);
                    Destroy(gameObject);
                    return true;
                }
            }

            return false;
        }
    }
}