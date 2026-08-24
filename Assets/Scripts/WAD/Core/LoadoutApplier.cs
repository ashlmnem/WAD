using UnityEngine;
using WAD.Weapons;
using WAD.Weapons.Attachments;
using WAD.Player;

namespace WAD.Menu
{
    /// <summary>
    /// In die Level-Szene legen (z.B. neben LevelMusicStarter). Liest beim
    /// Start die Auswahl aus WeaponLoadoutManager und stattet den Spieler
    /// entsprechend aus - falls kein Loadout gewaehlt wurde (z.B. direkter
    /// Test der Level-Szene ohne Main Menu durchlaufen zu haben), passiert nichts.
    /// </summary>
    public class LoadoutApplier : MonoBehaviour
    {
        public PlayerWeaponHolder weaponHolder;
        public FirstPersonCameraLook cameraLook;
        [Tooltip("Leeres Basis-Prefab mit WeaponController drauf, worauf weaponData gesetzt wird")]
        public GameObject weaponControllerBasePrefab;

        private void Start()
        {
            if (WeaponLoadoutManager.Instance == null || WeaponLoadoutManager.Instance.selectedWeapon == null)
            {
                Debug.Log("[LoadoutApplier] Kein Loadout gewaehlt - Spieler startet ohne vorkonfigurierte Waffe.");
                return;
            }

            SpawnAndEquip(WeaponLoadoutManager.Instance.selectedWeapon);
        }

        private void SpawnAndEquip(WeaponSO weaponData)
        {
            GameObject instance = weaponData.viewmodelPrefab != null
                ? Instantiate(weaponData.viewmodelPrefab)
                : Instantiate(weaponControllerBasePrefab);

            var controller = instance.GetComponent<WeaponController>();
            if (controller == null) controller = instance.AddComponent<WeaponController>();
            controller.weaponData = weaponData;

            var attachManager = instance.GetComponent<WeaponAttachmentManager>();
            if (attachManager != null)
            {
                controller.attachments = attachManager;
                WeaponLoadoutManager.Instance.ApplyTo(attachManager);
            }

            // WeaponRecoil braucht eine Kamera-Referenz - beim manuellen Platzieren
            // in der Szene hast du das bisher per Hand im Inspector gesetzt, das
            // fehlt hier zur Laufzeit, deshalb explizit nachgeholt:
            var recoil = instance.GetComponent<WeaponRecoil>();
            if (recoil != null && cameraLook != null)
            {
                recoil.cameraLook = cameraLook;
            }

            weaponHolder.PickUpWeapon(controller);
        }
    }
}