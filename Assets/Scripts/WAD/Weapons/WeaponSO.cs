using UnityEngine;

namespace WAD.Weapons
{
    public enum FireMode
    {
        Semi,       // ein Schuss pro Klick (z.B. M27, Entity-001srp nach Transformation)
        FullAuto,   // Dauerfeuer (z.B. Staccato-9 falls automatisch)
        BoltAction  // muss zwischen Schuessen manuell durchgeladen werden (Entity-003mn)
    }

    /// <summary>
    /// Waffen-Datenobjekt. Erstelle im Editor via:
    /// Assets > Create > WAD > Weapon
    /// </summary>
    [CreateAssetMenu(fileName = "Weapon_", menuName = "WAD/Weapon")]
    public class WeaponSO : ScriptableObject
    {
        [Header("Identifikation")]
        public string weaponId;
        public string displayName;
        public GameObject viewmodelPrefab; // Waffe in der Hand (First-Person)
        public GameObject worldModelPrefab; // Waffe als Loot am Boden

        [Header("Feuerverhalten")]
        public FireMode fireMode = FireMode.Semi;
        [Tooltip("Schuesse pro Minute (fuer FullAuto/Semi relevant)")]
        public float roundsPerMinute = 600f;
        [Tooltip("Zeit in Sekunden, die der Bolt-Cycle bei BoltAction dauert")]
        public float boltActionCycleTime = 1.2f;

        [Header("Magazin")]
        public int magazineCapacity = 15;
        public AmmoTypeSO compatibleAmmoType;
        public float reloadTimeSeconds = 2.2f;
        [Tooltip("Falls true: muss vor dem ersten Schuss nach dem Nachladen manuell durchgeladen werden")]
        public bool requiresChamberAfterReload = false;

        [Header("Genauigkeit & Recoil")]
        public float baseSpreadDegrees = 1.5f;
        public float adsSpreadDegrees = 0.3f;
        [Tooltip("Vertikaler Rueckstoss pro Schuss in Grad")]
        public float recoilPerShot = 1.8f;
        [Tooltip("Zufaelliger horizontaler Rueckstoss pro Schuss in Grad (+/-)")]
        public float horizontalRecoilPerShot = 0.4f;
        [Tooltip("Wie schnell sich die Waffe nach Rueckstoss wieder beruhigt")]
        public float recoilRecoverySpeed = 4f;

        [Header("Reichweite")]
        public float maxRange = 300f;
        [Tooltip("Kurve: X = Distanz in m, Y = Schaden-Multiplikator (Falloff)")]
        public AnimationCurve damageFalloff = AnimationCurve.Linear(0f, 1f, 300f, 0.4f);

        [Header("Gewicht (fuer Inventar/Bewegungssystem)")]
        public float weightKg = 3.2f;

        [Header("VFX & Sound")]
        public GameObject muzzleFlashPrefab;
        public AudioClip fireSound;
        public AudioClip dryFireSound;
        public AudioClip reloadSound;

        [Header("Magazin-Weltmodell (Punkt 4)")]
        [Tooltip("Wie das Magazin dieser Waffe aussieht, wenn es lose in der Welt liegt")]
        public GameObject magazineGroundModelPrefab;

        [Header("ADS")]
        public float adsFOV = 45f;
        public float adsTransitionSpeed = 8f;

        public float SecondsBetweenShots => 60f / Mathf.Max(1f, roundsPerMinute);
    }
}