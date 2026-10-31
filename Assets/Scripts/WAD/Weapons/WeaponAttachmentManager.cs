using System.Collections.Generic;
using UnityEngine;

namespace WAD.Weapons.Attachments
{
    /// <summary>
    /// Liegt auf demselben Objekt wie WeaponController. Verwaltet montierte
    /// Attachments PRO RAIL und stellt aggregierte Stat-Multiplikatoren bereit.
    /// </summary>
    public class WeaponAttachmentManager : MonoBehaviour
    {
        [Header("Rail-Mounts (physische Befestigungspunkte)")]
        public List<RailMount> railMounts = new List<RailMount>();

        private readonly Dictionary<string, AttachmentSO> equippedByRail = new Dictionary<string, AttachmentSO>();
        private readonly Dictionary<string, GameObject> spawnedVisuals = new Dictionary<string, GameObject>();

        public event System.Action OnAttachmentsChanged;

        public RailMount GetRail(string railId) => railMounts.Find(r => r.railId == railId);
        public AttachmentSO GetEquipped(string railId) => equippedByRail.TryGetValue(railId, out var a) ? a : null;

        public bool EquipAttachment(string railId, AttachmentSO attachment)
        {
            var rail = GetRail(railId);
            if (rail == null)
            {
                Debug.LogWarning($"[WeaponAttachmentManager:{gameObject.name}] Rail '{railId}' existiert nicht.");
                return false;
            }

            if (attachment != null && !rail.Accepts(attachment.category))
            {
                Debug.LogWarning($"[WeaponAttachmentManager:{gameObject.name}] Rail '{railId}' akzeptiert keine Kategorie '{attachment.category}'.");
                return false;
            }

            // Waffen-Kompatibilitaet pruefen (falls im Attachment eingeschraenkt)
            if (attachment != null)
            {
                var weapon = GetComponent<WeaponController>();
                if (weapon != null && weapon.weaponData != null && !attachment.IsCompatibleWith(weapon.weaponData))
                {
                    Debug.LogWarning($"[WeaponAttachmentManager:{gameObject.name}] '{attachment.displayName}' ist nicht kompatibel mit '{weapon.weaponData.displayName}'.");
                    return false;
                }
            }

            RemoveAttachmentInternal(railId);

            if (attachment == null)
            {
                OnAttachmentsChanged?.Invoke();
                return true;
            }

            equippedByRail[railId] = attachment;

            if (attachment.visualPrefab != null)
            {
                Vector3 pos = rail.mountTransform != null ? rail.mountTransform.position : transform.position;
                Quaternion rot = rail.mountTransform != null ? rail.mountTransform.rotation : transform.rotation;
                Transform parent = rail.mountTransform != null ? rail.mountTransform : transform;

                if (rail.mountTransform == null)
                {
                    Debug.LogWarning($"[WeaponAttachmentManager:{gameObject.name}] Rail '{railId}' hat kein Mount Transform - " +
                                     $"'{attachment.displayName}' wird an der Waffenwurzel befestigt und sitzt dadurch vermutlich falsch. " +
                                     "Bitte im Waffen-Prefab einen Befestigungspunkt zuweisen.");
                }

                GameObject visual = Instantiate(attachment.visualPrefab, pos, rot, parent);
                spawnedVisuals[railId] = visual;
            }
            else
            {
                Debug.LogWarning($"[WeaponAttachmentManager:{gameObject.name}] '{attachment.displayName}' hat kein Visual Prefab.");
            }

            OnAttachmentsChanged?.Invoke();
            return true;
        }

        public void RemoveAttachment(string railId)
        {
            RemoveAttachmentInternal(railId);
            OnAttachmentsChanged?.Invoke();
        }

        /// <summary>
        /// Entfernt ein Attachment OHNE das Changed-Event auszuloesen.
        /// Wird intern benutzt, damit ein Wechsel das Event nur EINMAL feuert -
        /// vorher lief WeaponController.ApplyMagazineTypeOverride zweimal und
        /// konnte dabei Patronen verlieren.
        /// </summary>
        private void RemoveAttachmentInternal(string railId)
        {
            if (spawnedVisuals.TryGetValue(railId, out var visual) && visual != null)
            {
                Destroy(visual);
            }
            spawnedVisuals.Remove(railId);
            equippedByRail.Remove(railId);
        }

        public float GetRecoilMultiplier() => Aggregate(a => a.recoilMultiplier);
        public float GetSpreadMultiplier() => Aggregate(a => a.spreadMultiplier);
        public float GetADSSpeedMultiplier() => Aggregate(a => a.adsSpeedMultiplier);

        public float GetADSFOVOverride()
        {
            foreach (var a in equippedByRail.Values)
            {
                if (a.category == AttachmentCategory.Optic && a.adsFOVOverride > 0f) return a.adsFOVOverride;
            }
            return 0f;
        }

        public MagazineTypeSO GetMagazineTypeOverride()
        {
            foreach (var a in equippedByRail.Values)
            {
                if (a.category == AttachmentCategory.Magazine && a.magazineTypeOverride != null) return a.magazineTypeOverride;
            }
            return null;
        }

        /// <summary>
        /// Findet den "ADS_AimPoint" der aktuell montierten Optik - auch wenn er
        /// TIEFER in der Optik-Hierarchie liegt.
        ///
        /// WICHTIG: Die Optik-Prefabs sind selbst Prefab-Instanzen (z.B. T-1 mit
        /// Riser). Deren Wurzel-Transform ist beim Instanziieren "stripped",
        /// wodurch das eigentliche Optik-Modell eine Ebene UNTER dem Visual-Root
        /// liegt und "ADS_AimPoint" erst in der zweiten Ebene haengt.
        /// Transform.Find() sucht nur DIREKTE Kinder und lieferte deshalb NULL -
        /// die Waffe fiel auf die offene Visierung zurueck und man zielte durch
        /// Kimme/Korn statt durch die Optik.
        /// </summary>
        public Transform GetOpticAimPoint()
        {
            string opticRailId = FindOpticRailId();
            if (opticRailId == null) return null; // keine Optik montiert - kein Fehler, einfach kein Aim Point

            if (!spawnedVisuals.TryGetValue(opticRailId, out var visual) || visual == null) return null;

            Transform aimPoint = FindDescendant(visual.transform, "ADS_AimPoint");

            if (aimPoint == null)
            {
                GameObject prefab = equippedByRail.TryGetValue(opticRailId, out var so) ? (so != null ? so.visualPrefab : null) : null;
                Debug.LogWarning($"[WeaponAttachmentManager:{gameObject.name}] Optik '{(prefab != null ? prefab.name : opticRailId)}' hat keinen Kindpunkt 'ADS_AimPoint' - " +
                                 "ADS richtet sich weiter an der offenen Visierung aus. Bitte im Optik-Prefab einen Punkt mit exakt diesem Namen anlegen.");
            }

            return aimPoint;
        }

        /// <summary> Rekursive Suche nach einem Kindpunkt (Transform.Find sucht nur direkte Kinder). </summary>
        private static Transform FindDescendant(Transform root, string name)
        {
            if (root == null) return null;
            if (root.name == name) return root;

            for (int i = 0; i < root.childCount; i++)
            {
                Transform found = FindDescendant(root.GetChild(i), name);
                if (found != null) return found;
            }
            return null;
        }

        private string FindOpticRailId()
        {
            foreach (var kvp in equippedByRail)
            {
                if (kvp.Value.category == AttachmentCategory.Optic) return kvp.Key;
            }
            return null;
        }

        public float GetTotalWeightBonusKg()
        {
            float total = 0f;
            foreach (var a in equippedByRail.Values) total += a.weightKg;
            return total;
        }

        private float Aggregate(System.Func<AttachmentSO, float> selector)
        {
            float result = 1f;
            foreach (var a in equippedByRail.Values) result *= selector(a);
            return result;
        }
    }
}
