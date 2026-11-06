using System.Collections.Generic;
using UnityEngine;
using WAD.Weapons;
using WAD.Weapons.Attachments;

namespace WAD.Enemies
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(EnemyController))]
    public class EnemyWeaponLoadout : MonoBehaviour
    {
        [Header("Weapon Pool")]
        public WeaponSO[] weaponOptions;
        public Transform weaponMount;
        public Vector3 weaponLocalPosition = new Vector3(0.25f, 0.45f, 0.05f);
        public Vector3 weaponLocalEulerAngles = new Vector3(-90f, 180f, 0f);
        [Tooltip("Weapon model axis that points along the barrel. The current weapon meshes use local +Y.")]
        public Vector3 weaponForwardAxis = Vector3.up;

        [Header("Random Attachments")]
        public AttachmentSO[] attachmentOptions;
        [Min(0)] public int minAttachments = 1;
        [Min(0)] public int maxAttachments = 2;

        [Header("AI Accuracy")]
        [Min(0f)] public float spreadMultiplier = 12f;

        private WeaponController weapon;
        private EnemyController enemy;

        public bool HasWeapon => weapon != null && weapon.weaponData != null;
        public WeaponSO SelectedWeapon => weapon != null ? weapon.weaponData : null;

        private struct AttachmentChoice
        {
            public string railId;
            public AttachmentSO attachment;
        }

        private void Awake()
        {
            enemy = GetComponent<EnemyController>();
            EquipRandomWeapon();
        }

        private void LateUpdate()
        {
            if (HasWeapon && enemy != null && enemy.IsEngagingPlayer && enemy.PlayerTransform != null)
            {
                AimAt(enemy.GetPlayerAimPoint());
            }
        }

        public bool FireAt(Vector3 targetPoint, float additionalSpreadMultiplier = 1f)
        {
            if (!HasWeapon) return false;

            AimAt(targetPoint);

            if (weapon.loadedMagazine == null || weapon.loadedMagazine.IsEmpty)
            {
                weapon.TryReload();
                return false;
            }

            return weapon.TryFireAt(targetPoint, spreadMultiplier * Mathf.Max(0f, additionalSpreadMultiplier));
        }

        private void AimAt(Vector3 targetPoint)
        {
            if (weaponForwardAxis.sqrMagnitude < 0.0001f) return;

            Transform muzzle = weapon.muzzlePoint;
            Vector3 origin = muzzle != null ? muzzle.position : weapon.transform.position;
            Vector3 direction = targetPoint - origin;
            if (direction.sqrMagnitude < 0.0001f) return;

            Vector3 barrelDirection = weapon.transform.TransformDirection(weaponForwardAxis.normalized);
            Quaternion correction = Quaternion.FromToRotation(barrelDirection, direction.normalized);
            weapon.transform.rotation = correction * weapon.transform.rotation;
        }

        private void EquipRandomWeapon()
        {
            WeaponSO selected = SelectRandomWeapon();
            if (selected == null)
            {
                Debug.LogWarning($"[EnemyWeaponLoadout:{name}] No valid weapon with a world prefab and WeaponController is configured.", this);
                return;
            }

            Transform mount = weaponMount != null ? weaponMount : transform;
            GameObject weaponObject = Instantiate(selected.worldModelPrefab, mount, false);
            weaponObject.name = $"{selected.displayName} (Enemy)";
            weaponObject.transform.localPosition = weaponLocalPosition;
            weaponObject.transform.localRotation = Quaternion.Euler(weaponLocalEulerAngles);

            weapon = weaponObject.GetComponentInChildren<WeaponController>(true);
            if (weapon == null)
            {
                Destroy(weaponObject);
                Debug.LogWarning($"[EnemyWeaponLoadout:{name}] Weapon prefab '{selected.worldModelPrefab.name}' has no WeaponController.", this);
                return;
            }

            weapon.weaponData = selected;
            weapon.SetEquipped(false);
            weapon.SetAttachmentManager(weapon.GetComponent<WeaponAttachmentManager>());
            weapon.casingEjector = null;
            EnsureAmmunition(selected);
            DisableHeldWeaponPhysics(weaponObject);

            var magazineCheat = weaponObject.GetComponentInChildren<MagazinesCreator>(true);
            if (magazineCheat != null) magazineCheat.enabled = false;

            ApplyRandomAttachments();
        }

        private WeaponSO SelectRandomWeapon()
        {
            if (weaponOptions == null) return null;

            WeaponSO selected = null;
            int validCount = 0;

            foreach (WeaponSO option in weaponOptions)
            {
                if (option == null || option.worldModelPrefab == null ||
                    option.worldModelPrefab.GetComponentInChildren<WeaponController>(true) == null)
                {
                    continue;
                }

                validCount++;
                if (Random.Range(0, validCount) == 0) selected = option;
            }

            return selected;
        }

        private void EnsureAmmunition(WeaponSO weaponData)
        {
            MagazineTypeSO defaultMagazine = weaponData.DefaultMagazineType;
            if (weapon.loadedMagazine == null || weapon.loadedMagazine.magazineType == null ||
                weapon.loadedMagazine.ammoType == null)
            {
                weapon.loadedMagazine = defaultMagazine != null ? new Magazine(defaultMagazine) : null;
            }

            if (weapon.reserveMagazines == null) weapon.reserveMagazines = new List<Magazine>();
            weapon.reserveMagazines.RemoveAll(magazine => magazine == null);

            bool hasReserve = false;
            foreach (Magazine magazine in weapon.reserveMagazines)
            {
                if (magazine != null && !magazine.IsEmpty && weaponData.AcceptsMagazine(magazine.magazineType))
                {
                    hasReserve = true;
                    break;
                }
            }

            if (!hasReserve && defaultMagazine != null)
            {
                weapon.reserveMagazines.Add(new Magazine(defaultMagazine));
            }
        }

        private void ApplyRandomAttachments()
        {
            WeaponAttachmentManager manager = weapon.GetComponent<WeaponAttachmentManager>();
            if (manager == null || manager.railMounts == null || attachmentOptions == null) return;

            var choices = new List<AttachmentChoice>(manager.railMounts.Count);
            foreach (RailMount rail in manager.railMounts)
            {
                if (rail == null || string.IsNullOrEmpty(rail.railId)) continue;

                AttachmentSO attachment = SelectRandomAttachment(rail);
                if (attachment == null) continue;

                choices.Add(new AttachmentChoice { railId = rail.railId, attachment = attachment });
            }

            int minimum = Mathf.Clamp(minAttachments, 0, choices.Count);
            int maximum = Mathf.Clamp(maxAttachments, minimum, choices.Count);
            int count = Random.Range(minimum, maximum + 1);

            for (int i = 0; i < count; i++)
            {
                int selectedIndex = Random.Range(i, choices.Count);
                (choices[i], choices[selectedIndex]) = (choices[selectedIndex], choices[i]);

                AttachmentChoice choice = choices[i];
                manager.EquipAttachment(choice.railId, choice.attachment);
            }
        }

        private AttachmentSO SelectRandomAttachment(RailMount rail)
        {
            AttachmentSO selected = null;
            int compatibleCount = 0;

            foreach (AttachmentSO option in attachmentOptions)
            {
                if (option == null || option.visualPrefab == null || !rail.Accepts(option.category) ||
                    !option.IsCompatibleWith(weapon.weaponData))
                {
                    continue;
                }

                compatibleCount++;
                if (Random.Range(0, compatibleCount) == 0) selected = option;
            }

            return selected;
        }

        private static void DisableHeldWeaponPhysics(GameObject weaponObject)
        {
            foreach (Collider collider in weaponObject.GetComponentsInChildren<Collider>(true))
            {
                collider.enabled = false;
            }

            foreach (Rigidbody body in weaponObject.GetComponentsInChildren<Rigidbody>(true))
            {
                body.isKinematic = true;
                body.useGravity = false;
                body.detectCollisions = false;
            }
        }
    }
}