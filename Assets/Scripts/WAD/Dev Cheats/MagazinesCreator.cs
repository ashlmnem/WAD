using Cheats;
using UnityEngine;

namespace WAD.Weapons
{
    public class MagazinesCreator : MonoBehaviour
    {
        [Header("Activation Bind")]
        public KeyCode cheat_magazine_bind = KeyCode.F6;

        private WeaponController weapon;
        [Header("Magazine Data")]
        public int capacity = 15;
        [Tooltip("-1 = voll. Sonst exakte Anzahl Patronen in diesem gefundenen Magazin.")]
        public int currentRounds = -1;

        void Start()
        {
            if (TryGetComponent<WeaponController>(out WeaponController weapon)) {
                this.weapon = weapon;
            }
            else
            {
                Debug.LogWarning("Cheat Magazine script is connected to non-weapon gameobject", gameObject);
                this.enabled = false;
            }
        }

        void Update()
        {
            if (Input.GetKeyDown(cheat_magazine_bind))
            {
                if (CheatManager.cheats_enabled) CheatMagazine();
                else Debug.Log($"Cheats are disabled, but caught a try to cheat magazine", gameObject);
            }
        }

        public void CheatMagazine()
        {
            if (weapon.isEquipped && weapon != null && weapon.weaponData != null)
            {
                var magazine = new Magazine(weapon.weaponData.compatibleAmmoType, capacity, currentRounds);
                weapon.reserveMagazines.Add(magazine);
                Debug.Log($"Added 1 magazine to {gameObject.name}, now {weapon.reserveMagazines.Count} magazines", gameObject);
            }
        }
    }
}