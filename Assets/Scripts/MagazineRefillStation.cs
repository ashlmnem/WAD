using UnityEngine;
using WAD.Inventory;
using WAD.Weapons;

namespace WAD.ShootingRange
{
    /// <summary>
    /// Steht in der Shooting Range (oder spaeter an Hideout-Stationen).
    /// Fuellt beim Interagieren alle Magazine der ausgeruesteten Waffen
    /// (geladen + Reserve) auf ihre volle Kapazitaet auf.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class MagazineRefillStation : MonoBehaviour, IInteractable
    {
        public string interactionPrompt = "F - Magazine auffüllen";
        public string InteractionPrompt => interactionPrompt;

        public void Interact(PlayerInteraction interactor)
        {
            TryRefill(interactor.weaponHolder);
        }

        public bool TryRefill(PlayerWeaponHolder weaponHolder)
        {
            if (weaponHolder == null) return false;

            bool refilledAny = false;

            foreach (var weapon in weaponHolder.equippedWeapons)
            {
                if (weapon == null || weapon.weaponData == null) continue;

                if (weapon.loadedMagazine != null)
                {
                    weapon.loadedMagazine.currentRounds = weapon.loadedMagazine.capacity;
                    refilledAny = true;
                }

                foreach (var mag in weapon.reserveMagazines)
                {
                    mag.currentRounds = mag.capacity;
                    refilledAny = true;
                }
            }

            if (refilledAny)
            {
                Debug.Log("[MagazineRefillStation] Alle Magazine aufgefüllt.");
            }

            return refilledAny;
        }
    }
}
