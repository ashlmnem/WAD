using UnityEngine;

namespace WAD.Weapons
{
    /// <summary>
    /// Grobklasse eines Magazins. Nur fuer UI/Katalog-Filter und zum Gruppieren
    /// im Inspector - die tatsaechliche Funktion haengt an baseCapacity und
    /// ammoType.
    /// </summary>
    public enum MagazineClass
    {
        Standard,
        Short,
        Extended,
        Drum,
        Box,
        Belt,
        Speedloader,
        Internal
    }

    /// <summary>
    /// Ein konkreter MAGAZIN-TYP (nicht ein Munitionstyp!) - z.B. "Glock 17 Mag",
    /// "STANAG 30rd", "Drum Mag 75rd". Mehrere Waffen koennen denselben
    /// MagazineTypeSO als kompatibel listen.
    ///
    /// Fuer ein neues Magazin (Trommel, Kurz, Lang...) einfach ein weiteres
    /// Asset dieses Typs anlegen und im MagazineCatalogSO eintragen - es ist
    /// danach automatisch als Weltloot, Reserve-Magazin und Magazin-Attachment
    /// verwendbar. Kein Code noetig.
    ///
    /// Erstelle im Editor via: Assets > Create > WAD > Magazine Type
    /// </summary>
    [CreateAssetMenu(fileName = "MagType_", menuName = "WAD/Magazine Type")]
    public class MagazineTypeSO : ScriptableObject
    {
        [Header("Identifikation")]
        public string magazineTypeId;
        public string displayName;
        public Sprite icon;

        [TextArea(2, 4)]
        [Tooltip("Kurzbeschreibung fuer das Inspect-Fenster (V)")]
        public string description;

        [Header("Einordnung")]
        public MagazineClass magazineClass = MagazineClass.Standard;

        [Header("Munition & Kapazitaet")]
        [Tooltip("Bestimmt das Kaliber. Waffen, die dieses Kaliber laden, akzeptieren das Magazin automatisch.")]
        public AmmoTypeSO ammoType;
        [Min(1)] public int baseCapacity = 15;

        [Header("Gewicht")]
        [Tooltip("Leergewicht des Magazins in kg (ohne Patronen)")]
        public float emptyWeightKg = 0.15f;

        [Header("Handling-Auswirkung (optional)")]
        [Tooltip("Multiplikator fuer die Nachladedauer. Trommelmagazine z.B. 1.3 (langsamer).")]
        [Min(0.1f)] public float reloadTimeMultiplier = 1f;
        [Tooltip("Multiplikator fuer die Waffenstreuung durch das Gewicht. Schwere Magazine z.B. 1.05.")]
        [Min(0.1f)] public float spreadMultiplier = 1f;

        [Header("Weltmodell")]
        [Tooltip("Wie dieses Magazin aussieht, wenn es lose in der Welt liegt")]
        public GameObject groundModelPrefab;

        [Header("Als Waffen-Attachment (MagPoint-Rail)")]
        [Tooltip("Wenn gesetzt, kann dieses Magazin zusaetzlich als Attachment montiert werden (AttachmentSO, Kategorie Magazine).")]
        public GameObject attachmentVisualPrefab;

        /// <summary> Passt dieses Magazin zum angegebenen Kaliber? </summary>
        public bool IsSameCaliber(AmmoTypeSO other) => other != null && ammoType == other;

        /// <summary> Anzeigename inkl. Kapazitaet, fuer Listen im UI. </summary>
        public string DisplayNameWithCapacity =>
            string.IsNullOrEmpty(displayName) ? $"{name} ({baseCapacity})" : $"{displayName} ({baseCapacity})";
    }
}
