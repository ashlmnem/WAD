using UnityEngine;

namespace WAD.Weapons
{
    /// <summary>
    /// Ein konkreter MAGAZIN-TYP (nicht ein Munitionstyp!) - z.B. "Glock 17 Mag",
    /// "STANAG 30rd", "Drum Mag 75rd". Mehrere Waffen koennen denselben
    /// MagazineTypeSO als kompatibel listen (Punkt 7: Staccato-9 akzeptiert
    /// M27-Magazine). Verschiedene Kapazitaeten/Modelle desselben Kalibers
    /// (Punkt 8: Trommel-/Kurzmagazine) sind einfach weitere MagazineTypeSO-
    /// Assets mit demselben AmmoType.
    ///
    /// Erstelle im Editor via: Assets > Create > WAD > Magazine Type
    /// </summary>
    [CreateAssetMenu(fileName = "MagType_", menuName = "WAD/Magazine Type")]
    public class MagazineTypeSO : ScriptableObject
    {
        [Header("Identifikation")]
        public string magazineTypeId;
        public string displayName; // z.B. "M27 Standardmagazin", "M27 Trommelmagazin"
        public Sprite icon; // fuer Inspect-UI (Punkt 8)

        [Header("Munition & Kapazitaet")]
        public AmmoTypeSO ammoType;
        public int baseCapacity = 15;

        [Header("Gewicht")]
        public float emptyWeightKg = 0.15f;

        [Header("Weltmodell")]
        [Tooltip("Wie dieses Magazin aussieht, wenn es lose in der Welt liegt (Punkt 4 von frueher)")]
        public GameObject groundModelPrefab;
    }
}
