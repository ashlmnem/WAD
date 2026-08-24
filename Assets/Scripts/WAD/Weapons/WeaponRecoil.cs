using UnityEngine;
using WAD.Player;

namespace WAD.Weapons
{
    /// <summary>
    /// Verbindet WeaponController.OnFired mit FirstPersonCameraLook.ApplyRecoilKick.
    /// Liegt auf demselben Objekt wie WeaponController (dem Waffen-Viewmodel).
    /// </summary>
    [RequireComponent(typeof(WeaponController))]
    public class WeaponRecoil : MonoBehaviour
    {
        public FirstPersonCameraLook cameraLook;

        private WeaponController weaponController;

        private void Awake()
        {
            weaponController = GetComponent<WeaponController>();
        }

        private void OnEnable()
        {
            weaponController.OnFired += HandleFired;
        }

        private void OnDisable()
        {
            weaponController.OnFired -= HandleFired;
        }

        private void HandleFired()
        {
            if (cameraLook == null || weaponController.weaponData == null) return;

            float verticalKick = weaponController.weaponData.recoilPerShot;
            float horizontalKick = Random.Range(
                -weaponController.weaponData.horizontalRecoilPerShot,
                weaponController.weaponData.horizontalRecoilPerShot);

            // Beim Zielen (ADS) ist der Rueckstoss spuerbar kontrollierbarer
            float adsMultiplier = weaponController.IsAiming ? 0.6f : 1f;

            cameraLook.ApplyRecoilKick(verticalKick * adsMultiplier, horizontalKick * adsMultiplier);
        }
    }
}