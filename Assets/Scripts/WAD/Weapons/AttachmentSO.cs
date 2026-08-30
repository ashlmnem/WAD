using UnityEngine;
using WAD.Weapons;

namespace WAD.Weapons.Attachments
{
    /// <summary>
    /// Ein Waffen-Anbauteil. Erstelle im Editor via:
    /// Assets > Create > WAD > Attachment
    /// </summary>
    [CreateAssetMenu(fileName = "Attachment_", menuName = "WAD/Attachment")]
    public class AttachmentSO : ScriptableObject
    {
        [Header("Identifikation")]
        public string attachmentId;
        public string displayName;
        public Sprite icon;
        public AttachmentCategory category;

        [Header("Visuals")]
        public GameObject visualPrefab;

        [Header("Stat-Modifikatoren (multiplikativ, 1 = keine Aenderung)")]
        public float recoilMultiplier = 1f;
        public float spreadMultiplier = 1f;
        public float adsSpeedMultiplier = 1f;
        public float adsFOVOverride = 0f;

        [Header("Magazin-spezifisch (nur bei Category = Magazine, Punkt 8)")]
        [Tooltip("Montieren dieses Attachments wechselt den aktuell geladenen Magazin-TYP (z.B. Trommelmagazin statt Standard) - leer lassen fuer nicht-Magazin-Attachments")]
        public MagazineTypeSO magazineTypeOverride;

        [Header("Gewicht")]
        public float weightKg = 0.2f;

        [Header("Kompatibilitaet")]
        [Tooltip("Leer = kompatibel mit allen Waffen. Sonst nur mit den hier gelisteten.")]
        public WeaponSO[] compatibleWeapons;

        public bool IsCompatibleWith(WeaponSO weapon)
        {
            if (compatibleWeapons == null || compatibleWeapons.Length == 0) return true;
            foreach (var w in compatibleWeapons)
            {
                if (w == weapon) return true;
            }
            return false;
        }
    }
}