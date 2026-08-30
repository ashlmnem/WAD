using UnityEngine;
using WAD.Weapons;

namespace WAD.Inventory
{
    /// <summary>
    /// Liegt auf einem Magazin-Objekt in der Welt. Referenziert jetzt einen
    /// MagazineTypeSO (Punkt 6+7+8) - beim Aufheben wird geprueft, welche
    /// ausgeruestete Waffe diesen TYP akzeptiert (nicht mehr nur "gleicher
    /// Munitionstyp", sondern explizit ueber WeaponSO.compatibleMagazineTypes).
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class WorldMagazinePickup : MonoBehaviour, IInteractable
    {
        [Header("Magazin-Typ")]
        public MagazineTypeSO magazineType;
        [Tooltip("-1 = voll. Sonst exakte Anzahl Patronen in diesem gefundenen Magazin.")]
        public int currentRounds = -1;

        public string InteractionPrompt => $"F - Magazin ({magazineType?.displayName}) aufheben";

        public void Interact(PlayerInteraction interactor)
        {
            TryPickUp(interactor.weaponHolder);
        }

        public bool TryPickUp(PlayerWeaponHolder weaponHolder)
        {
            if (weaponHolder == null || magazineType == null) return false;

            foreach (var weapon in weaponHolder.equippedWeapons)
            {
                if (weapon != null && weapon.weaponData != null && weapon.weaponData.AcceptsMagazine(magazineType))
                {
                    var magazine = new Magazine(magazineType, currentRounds);
                    weapon.reserveMagazines.Add(magazine);
                    Destroy(gameObject);
                    return true;
                }
            }

            return false;
        }
    }
}