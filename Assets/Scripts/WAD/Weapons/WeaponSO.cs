using UnityEngine;
using System.Collections.Generic;

namespace WAD.Weapons
{
    public enum FireMode
    {
        Semi,
        FullAuto,
        BoltAction
    }

    /// <summary>
    /// Waffen-Datenobjekt. Erstelle im Editor via: Assets > Create > WAD > Weapon
    /// </summary>
    [CreateAssetMenu(fileName = "Weapon_", menuName = "WAD/Weapon")]
    public class WeaponSO : ScriptableObject
    {
        [Header("Identifikation")]
        public string weaponId;
        public string displayName;
        public GameObject viewmodelPrefab;
        public GameObject worldModelPrefab;

        [Header("Feuerverhalten")]
        public FireMode fireMode = FireMode.Semi;
        public float roundsPerMinute = 600f;
        public float boltActionCycleTime = 1.2f;

        [Header("Kompatible Magazine")]
        [Tooltip("Liste aller Magazin-Typen, die diese Waffe laden kann - der erste Eintrag ist der Standard beim Erstausruesten. Mehrere Waffen koennen denselben MagazineTypeSO in ihrer Liste haben (z.B. Staccato-9 + M27 teilen sich das gleiche Magazin).")]
        public List<MagazineTypeSO> compatibleMagazineTypes = new List<MagazineTypeSO>();
        public MagazineTypeSO DefaultMagazineType => compatibleMagazineTypes.Count > 0 ? compatibleMagazineTypes[0] : null;

        /// <summary>
        /// Nimmt diese Waffe das Magazin an?
        /// Zuerst die explizite Liste (Sonderfaelle wie geteilte Magazine),
        /// danach das Kaliber - so funktionieren neue Magazin-Typen
        /// (Trommel, Kurz, Lang) automatisch, ohne sie hier einzutragen.
        /// </summary>
        public bool AcceptsMagazine(MagazineTypeSO type)
        {
            if (type == null) return false;
            if (compatibleMagazineTypes.Contains(type)) return true;

            // Kaliber-Abgleich: gleiches AmmoTypeSO = kompatibel
            foreach (var compatible in compatibleMagazineTypes)
            {
                if (compatible != null && compatible.ammoType != null && compatible.ammoType == type.ammoType)
                {
                    return true;
                }
            }
            return false;
        }

        [Header("Nachladen")]
        public float reloadTimeSeconds = 2.2f;
        public bool requiresChamberAfterReload = false;

        [Header("Genauigkeit & Recoil")]
        public float baseSpreadDegrees = 1.5f;
        public float adsSpreadDegrees = 0.3f;
        public float recoilPerShot = 1.8f;
        public float horizontalRecoilPerShot = 0.4f;
        public float recoilRecoverySpeed = 4f;

        [Header("Reichweite")]
        public float maxRange = 300f;
        public AnimationCurve damageFalloff = AnimationCurve.Linear(0f, 1f, 300f, 0.4f);

        [Header("Gewicht")]
        public float weightKg = 3.2f;

        [Header("ADS")]
        public float adsFOV = 45f;
        public float adsTransitionSpeed = 8f;

        [Header("VFX & Sound")]
        public GameObject muzzleFlashPrefab;
        public AudioClip fireSound;
        public AudioClip dryFireSound;
        public AudioClip reloadSound;

        public float SecondsBetweenShots => 60f / Mathf.Max(1f, roundsPerMinute);
    }
}

