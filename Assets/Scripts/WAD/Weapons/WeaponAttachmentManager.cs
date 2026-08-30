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

            RemoveAttachment(railId);

            if (attachment == null) return true;

            equippedByRail[railId] = attachment;

            if (attachment.visualPrefab != null)
            {
                Vector3 pos = rail.mountTransform != null ? rail.mountTransform.position : transform.position;
                Quaternion rot = rail.mountTransform != null ? rail.mountTransform.rotation : transform.rotation;
                Transform parent = rail.mountTransform != null ? rail.mountTransform : transform;

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
            if (spawnedVisuals.TryGetValue(railId, out var visual) && visual != null)
            {
                Destroy(visual);
            }
            spawnedVisuals.Remove(railId);
            equippedByRail.Remove(railId);
            OnAttachmentsChanged?.Invoke();
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

        /// <summary> Aktives Magazin-Attachment (z.B. Trommelmagazin) - null wenn keins montiert (Punkt 8). </summary>
        public MagazineTypeSO GetMagazineTypeOverride()
        {
            foreach (var a in equippedByRail.Values)
            {
                if (a.category == AttachmentCategory.Magazine && a.magazineTypeOverride != null) return a.magazineTypeOverride;
            }
            return null;
        }
        /// <summary> Findet den "ADS_AimPoint"-Kindpunkt der aktuell montierten Optik, falls vorhanden (Punkt 10/11). </summary>
        public Transform GetOpticAimPoint()
        {
            if (!spawnedVisuals.TryGetValue(FindOpticRailId(), out var visual) || visual == null) return null;
            var aimPoint = visual.transform.Find("ADS_AimPoint");
            return aimPoint;
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