using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using WAD.Weapons;
using WAD.Weapons.Attachments;

namespace WAD.Menu
{
    [System.Serializable]
    public class AttachmentSlotUIBinding
    {
        [Tooltip("Muss exakt der 'Rail Id' auf dem Waffen-Prefab entsprechen, z.B. 'TopRail'")]
        public string railId;
        public AttachmentSO[] options;
        public Text label;
    }

    /// <summary>
    /// Steuert den Loadout-Bildschirm im Main Menu: Waffe + Attachments pro
    /// Rail durchklicken, 3D-Vorschau aktualisiert sich live. Bei Bestaetigung
    /// wird die Auswahl in WeaponLoadoutManager gespeichert.
    /// </summary>
    public class LoadoutMenuController : MonoBehaviour
    {
        [Header("Verfuegbare Waffen")]
        public WeaponSO[] availableWeapons;
        private int currentWeaponIndex;

        [Header("3D-Vorschau")]
        public Transform previewPedestal;
        [Tooltip("Waehle hier GENAU EINEN Layer aus (wie bei Culling Mask) - die Vorschau-Kamera sollte NUR diesen Layer rendern")]
        public LayerMask previewLayer;
        private GameObject currentPreviewInstance;
        private WeaponAttachmentManager previewAttachmentManager;

        [Header("UI")]
        public Text weaponNameText;
        public Text weaponStatsText;

        [Header("Attachment-Rail-UI (pro Rail ein Eintrag)")]
        public List<AttachmentSlotUIBinding> attachmentSlotBindings = new List<AttachmentSlotUIBinding>();

        private void Start()
        {
            ShowWeapon(0);
        }

        public void NextWeapon() => ShowWeapon((currentWeaponIndex + 1) % availableWeapons.Length);
        public void PreviousWeapon() => ShowWeapon((currentWeaponIndex - 1 + availableWeapons.Length) % availableWeapons.Length);

        private void ShowWeapon(int index)
        {
            if (availableWeapons == null || availableWeapons.Length == 0) return;
            currentWeaponIndex = index;
            WeaponSO weapon = availableWeapons[currentWeaponIndex];

            if (currentPreviewInstance != null) Destroy(currentPreviewInstance);

            GameObject prefabToShow = weapon.worldModelPrefab != null ? weapon.worldModelPrefab : weapon.viewmodelPrefab;
            if (prefabToShow != null && previewPedestal != null)
            {
                currentPreviewInstance = Instantiate(prefabToShow, previewPedestal.position, previewPedestal.rotation, previewPedestal);
                previewAttachmentManager = currentPreviewInstance.GetComponent<WeaponAttachmentManager>();
                if (previewAttachmentManager == null)
                {
                    Debug.LogWarning($"[LoadoutMenu] {weapon.displayName}: Prefab hat keinen WeaponAttachmentManager mit konfigurierten Rail Mounts.");
                }

                SetLayerRecursively(currentPreviewInstance, LayerMaskToLayerIndex(previewLayer));
            }
            else if (prefabToShow == null)
            {
                Debug.LogWarning($"[LoadoutMenu] {weapon.displayName}: weder World Model Prefab noch Viewmodel Prefab gesetzt - keine 3D-Vorschau m glich.");
            }

            if (weaponNameText != null) weaponNameText.text = weapon.displayName;
            if (weaponStatsText != null)
            {
                weaponStatsText.text = $"RPM: {weapon.roundsPerMinute}   Magazin: {weapon.magazineCapacity}   Gewicht: {weapon.weightKg:F1}kg";
            }

            if (WeaponLoadoutManager.Instance != null && previewAttachmentManager != null)
            {
                foreach (var binding in attachmentSlotBindings)
                {
                    var saved = WeaponLoadoutManager.Instance.GetAttachment(binding.railId);
                    if (saved != null && saved.IsCompatibleWith(weapon))
                    {
                        previewAttachmentManager.EquipAttachment(binding.railId, saved);
                    }
                    RefreshSlotLabel(binding);
                }
            }
        }
        // ---- Attachment-Wechsel pro Rail ----
        public void CycleAttachment(string railId, int direction)
        {
            var binding = attachmentSlotBindings.Find(b => b.railId == railId);
            if (binding == null)
            {
                Debug.LogWarning($"[LoadoutMenu] Kein Attachment Slot Binding fuer Rail '{railId}' gefunden - Eintrag im Inspector anlegen.");
                return;
            }
            if (binding.options == null || binding.options.Length == 0)
            {
                Debug.LogWarning($"[LoadoutMenu] Binding fuer Rail '{railId}' hat keine 'Options' zugewiesen.");
                return;
            }
            if (WeaponLoadoutManager.Instance == null)
            {
                Debug.LogWarning("[LoadoutMenu] WeaponLoadoutManager.Instance ist null - ist das Manager-Objekt als Root-Objekt in der Szene vorhanden?");
                return;
            }

            WeaponSO currentWeapon = availableWeapons[currentWeaponIndex];
            List<AttachmentSO> compatible = new List<AttachmentSO> { null }; // "kein Attachment"
            foreach (var option in binding.options)
            {
                if (option.IsCompatibleWith(currentWeapon)) compatible.Add(option);
            }

            AttachmentSO current = WeaponLoadoutManager.Instance.GetAttachment(railId);
            int currentIndex = compatible.IndexOf(current);
            int nextIndex = (currentIndex + direction + compatible.Count) % compatible.Count;
            AttachmentSO next = compatible[nextIndex];

            WeaponLoadoutManager.Instance.SetAttachment(railId, next);

            if (previewAttachmentManager != null)
            {
                previewAttachmentManager.EquipAttachment(railId, next);
            }

            RefreshSlotLabel(binding);
        }

        private static int LayerMaskToLayerIndex(LayerMask mask)
        {
            int value = mask.value;
            if (value == 0) return -1;
            for (int i = 0; i < 32; i++)
            {
                if ((value & (1 << i)) != 0) return i;
            }
            return -1;
        }

        private static void SetLayerRecursively(GameObject obj, int layer)
        {
            if (layer < 0) return;
            obj.layer = layer;
            foreach (Transform child in obj.transform)
            {
                SetLayerRecursively(child.gameObject, layer);
            }
        }

        private void RefreshSlotLabel(AttachmentSlotUIBinding binding)
        {
            if (binding.label == null) return;
            var current = WeaponLoadoutManager.Instance?.GetAttachment(binding.railId);
            binding.label.text = current != null ? current.displayName : "-- leer --";
        }

        // ---- Wrapper fuer Button-OnClick() (max. 1 Parameter im Inspector moeglich) ----
        public void CycleUpperReceiverRail(int direction) => CycleAttachment("UpperReceiverRail", direction);
        public void CycleMuzzlePoint(int direction) => CycleAttachment("MuzzlePoint", direction);
        public void CycleStockPoint(int direction) => CycleAttachment("StockPoint", direction);
        public void CycleMagPoint(int direction) => CycleAttachment("MagPoint", direction);
        public void CycleHandguardLowerRail(int direction) => CycleAttachment("HandguardLowerRail", direction);
        public void CycleHandguardUpperRail(int direction) => CycleAttachment("HandguardUpperRail", direction);

        // ---- Bestaetigen ----
        public void ConfirmSelection()
        {
            if (WeaponLoadoutManager.Instance == null) return;
            WeaponLoadoutManager.Instance.selectedWeapon = availableWeapons[currentWeaponIndex];
            Debug.Log($"[LoadoutMenu] Ausgewaehlt: {availableWeapons[currentWeaponIndex].displayName}");
        }
    }
}