using System.Collections.Generic;
using UnityEngine;
using WAD.Player;

namespace WAD.Weapons
{
    /// <summary>
    /// Verwaltet die aktuell ausgeruesteten Waffen (max. 2 Slots).
    /// Setzt konsequent WeaponController.SetEquipped(), damit lose in der
    /// Welt liegende Waffen NICHT auf Spieler-Eingaben reagieren, bevor sie
    /// tatsaechlich aufgehoben wurden.
    /// </summary>
    public class PlayerWeaponHolder : MonoBehaviour
    {
        [Header("Referenzen")]
        public Transform weaponSocket;
        public TarkovMovementController movementController;
        public Camera playerCamera;
        public FirstPersonCameraLook cameraLook;

        [Header("Slots")]
        public int maxWeaponSlots = 2;

        [Header("Ausgeruestete Waffen")]
        public List<WeaponController> equippedWeapons = new List<WeaponController>();
        private int activeWeaponIndex = -1;

        private void Start()
        {
            // Faengt auch manuell im Inspector vorbefuellte 'Equipped Weapons'-
            // Eintraege ab (z.B. zum schnellen Testen) - ohne das wuerden die
            // nie SetEquipped(true) erhalten und komplett auf Eingaben taub bleiben.
            foreach (var weapon in equippedWeapons)
            {
                if (weapon != null) weapon.SetEquipped(true);
            }
        }

        private void Update()
        {
            HandleWeaponSwitchInput();
        }

        private void HandleWeaponSwitchInput()
        {
            if (Input.GetKeyDown(KeyCode.Alpha1)) EquipWeaponAtIndex(0);
            if (Input.GetKeyDown(KeyCode.Alpha2)) EquipWeaponAtIndex(1);
        }

        public void EquipWeaponAtIndex(int index)
        {
            if (index < 0 || index >= equippedWeapons.Count) return;
            if (index == activeWeaponIndex) return;

            foreach (var weapon in equippedWeapons)
            {
                if (weapon == null) continue;
                weapon.gameObject.SetActive(false);
            }

            activeWeaponIndex = index;
            var active = equippedWeapons[activeWeaponIndex];
            if (active == null)
            {
                Debug.LogWarning($"[PlayerWeaponHolder] Leerer Eintrag in 'Equipped Weapons' an Index {index} - bitte im Inspector bereinigen.");
                return;
            }
            active.gameObject.SetActive(true);
            active.playerCamera = playerCamera;
            active.movementController = movementController;
        }

        /// <summary>
        /// Fuegt eine neu gefundene/aufgenommene Waffe hinzu. Ist bereits die
        /// maximale Slot-Anzahl erreicht, wird zuerst die aktuell aktive
        /// Waffe am Boden vor dem Spieler abgelegt (bleibt aufsammelbar).
        /// </summary>
        public void PickUpWeapon(WeaponController weapon)
        {
            if (weapon == null) return;

            if (equippedWeapons.Count >= maxWeaponSlots)
            {
                int dropIndex = activeWeaponIndex >= 0 ? activeWeaponIndex : 0;
                DropWeapon(dropIndex);
            }

            weapon.transform.SetParent(weaponSocket);
            weapon.transform.localPosition = Vector3.zero;
            weapon.transform.localRotation = Quaternion.identity;
            weapon.gameObject.SetActive(false);
            weapon.SetEquipped(true); // ab jetzt reagiert sie auf Eingaben (sobald aktiv geschaltet)

            WireRuntimeReferences(weapon);

            equippedWeapons.Add(weapon);
            EquipWeaponAtIndex(equippedWeapons.Count - 1);
        }

        /// <summary>
        /// Verknuepft alle Referenzen, die NICHT im Waffen-Prefab selbst
        /// gespeichert werden koennen, weil sie auf spezifische Szenen-Objekte
        /// (Spieler-Kamera etc.) zeigen - Prefab-Assets duerfen so etwas nicht
        /// dauerhaft referenzieren. Wird bei JEDEM Ausruesten automatisch
        /// aufgerufen, egal ob die Waffe frisch gefunden, aus dem Inventar
        /// benutzt oder durch Entity-001srp-Transformation entstanden ist.
        /// </summary>
        private void WireRuntimeReferences(WeaponController weapon)
        {
            var recoil = weapon.GetComponent<WAD.Weapons.WeaponRecoil>();
            if (recoil != null && recoil.cameraLook == null)
            {
                recoil.cameraLook = cameraLook;
            }

            var entitySrp = weapon.GetComponent<WAD.Entities.Entity001srp>();
            if (entitySrp != null)
            {
                entitySrp.AutoWireReferences(this);
            }
        }

        /// <summary> Legt eine ausgeruestete Waffe am Boden ab - bleibt in der Welt aufsammelbar, reagiert danach wieder NICHT auf Eingaben. </summary>
        public void DropWeapon(int index)
        {
            if (index < 0 || index >= equippedWeapons.Count) return;

            var weapon = equippedWeapons[index];
            equippedWeapons.RemoveAt(index);

            if (weapon == null) return;

            weapon.transform.SetParent(null);
            weapon.transform.position = transform.position + transform.forward * 1f;
            weapon.gameObject.SetActive(true);
            weapon.SetEquipped(false); // liegt jetzt nur noch als Loot rum, reagiert nicht mehr auf Eingaben

            if (weapon.GetComponent<Collider>() == null)
            {
                var col = weapon.gameObject.AddComponent<BoxCollider>();
                col.size = Vector3.one * 0.2f;
            }

            activeWeaponIndex = -1;
            if (equippedWeapons.Count > 0)
            {
                EquipWeaponAtIndex(0);
            }
        }

        public WeaponController GetActiveWeapon()
        {
            if (activeWeaponIndex < 0 || activeWeaponIndex >= equippedWeapons.Count) return null;
            return equippedWeapons[activeWeaponIndex];
        }

        public float GetTotalEquippedWeaponsWeightKg()
        {
            float total = 0f;
            foreach (var weapon in equippedWeapons)
            {
                if (weapon == null) continue;
                total += weapon.GetTotalWeightKg();
            }
            return total;
        }
    }
}