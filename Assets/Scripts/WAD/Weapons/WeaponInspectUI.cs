using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using WAD.Weapons;

namespace WAD.Weapons.UI
{
    /// <summary>
    /// Zeigt Munitionsinfo NUR waehrend 'V' gehalten wird.
    ///
    /// Zeigt:
    ///  - geladenes Magazin (Typ, Fuellstand, Kapazitaet)
    ///  - ALLE Reserve-Magazine einzeln mit Typ-Namen und Fuellstand
    ///  - optional den kompletten Magazin-Katalog (alle im Spiel existierenden
    ///    Magazin-Typen) in einem scrollbaren Fenster
    ///
    /// Voraussetzung fuer die Liste: ein 'content' Transform mit LayoutGroup
    /// und ein optionales Zeilen-Prefab. Ohne Prefab werden einfache Textzeilen
    /// automatisch erzeugt.
    /// </summary>
    public class WeaponInspectUI : MonoBehaviour
    {
        [Header("Referenzen")]
        public PlayerWeaponHolder weaponHolder;
        public GameObject panelRoot;
        public Text weaponNameText;
        public Text ammoTypeText;
        public Text magazineFillText;
        public Text reserveMagazinesText;
        public KeyCode inspectKey = KeyCode.V;

        [Header("Reserve-Magazin-Liste (scrollbar)")]
        [Tooltip("Parent INNERHALB der ScrollView, mit VerticalLayoutGroup + ContentSizeFitter")]
        public Transform reserveListContent;
        [Tooltip("Optionales Zeilen-Prefab. Leer = Textzeilen werden automatisch erzeugt.")]
        public GameObject reserveEntryPrefab;
        [Tooltip("Nur die Liste neu aufbauen, wenn sich die Waffe aendert (spart Arbeit)")]
        public bool rebuildOnlyOnWeaponChange = true;

        [Header("Magazin-Katalog (optional)")]
        [Tooltip("Katalog aller Magazin-Typen im Spiel. Leer = Katalogbereich bleibt leer.")]
        public MagazineCatalogSO magazineCatalog;
        [Tooltip("Parent fuer die Katalog-Liste (eigene ScrollView)")]
        public Transform catalogListContent;
        [Tooltip("Nur Magazin-Typen zeigen, die zur aktuellen Waffe passen")]
        public bool filterCatalogToWeapon = true;

        [Header("Textvorlagen")]
        public string noMagazineLabel = "Kein Magazin geladen";
        public string noReserveLabel = "Keine Reserve-Magazine";
        public string capacityUnknown = "-";

        private readonly List<GameObject> spawnedRows = new List<GameObject>();
        private readonly List<GameObject> spawnedCatalogRows = new List<GameObject>();
        private WeaponController lastBuiltFor;

        private void Start()
        {
            ResolveMissingReferences();
            SetPanelVisible(false);
        }

        private void OnDestroy()
        {
            ClearRows(spawnedRows);
            ClearRows(spawnedCatalogRows);
        }

        /// <summary>
        /// Dieses Skript sitzt auf dem Spieler-Prefab, die Anzeige-Texte liegen
        /// aber im Canvas-Prefab. Eine solche Cross-Prefab-Referenz kann Unity
        /// nicht speichern - deshalb blieben alle Felder leer und 'V' zeigte
        /// nichts an. Hier wird zur Laufzeit im Canvas gesucht.
        /// </summary>
        private void ResolveMissingReferences()
        {
            if (weaponHolder == null) weaponHolder = FindFirstObjectByType<PlayerWeaponHolder>();

            if (weaponNameText == null) weaponNameText = FindTextByName("Text_WeaponName");
            if (ammoTypeText == null) ammoTypeText = FindTextByName("Text_AmmoType");
            if (magazineFillText == null) magazineFillText = FindTextByName("Text_MagFill");
            if (reserveMagazinesText == null) reserveMagazinesText = FindTextByName("Text_ReserveMags");

            // Panel automatisch bestimmen - aber NIE das eigene GameObject,
            // sonst deaktiviert SetPanelVisible(false) dieses Skript selbst.
            if (panelRoot == null && weaponNameText != null)
            {
                Transform root = weaponNameText.transform;
                while (root.parent != null &&
                       !root.name.StartsWith("InspectPanel", System.StringComparison.OrdinalIgnoreCase))
                {
                    root = root.parent;
                }

                if (root.gameObject != gameObject) panelRoot = root.gameObject;
            }

            if (weaponNameText == null)
            {
                Debug.LogWarning($"[WeaponInspectUI:{name}] Keine HUD-Texte gefunden - 'V' zeigt nichts an. " +
                                 "Erwartet werden Text-Elemente namens Text_WeaponName/Text_AmmoType/Text_MagFill/Text_ReserveMags.");
            }
        }

        private void Update()
        {
            bool holdingInspect = Input.GetKey(inspectKey);

            if (holdingInspect)
            {
                SetPanelVisible(true);
                RefreshDisplay();
            }
            else
            {
                SetPanelVisible(false);
            }
        }

        private void SetPanelVisible(bool visible)
        {
            if (panelRoot != null && panelRoot.activeSelf != visible) panelRoot.SetActive(visible);
        }

        private void RefreshDisplay()
        {
            WeaponController active = weaponHolder != null ? weaponHolder.GetActiveWeapon() : null;

            if (active == null || active.weaponData == null)
            {
                if (weaponNameText != null) weaponNameText.text = "";
                if (ammoTypeText != null) ammoTypeText.text = noMagazineLabel;
                if (magazineFillText != null) magazineFillText.text = capacityUnknown;
                if (reserveMagazinesText != null) reserveMagazinesText.text = noReserveLabel;
                ClearRows(spawnedRows);
                lastBuiltFor = null;
                return;
            }

            if (weaponNameText != null) weaponNameText.text = active.weaponData.displayName;

            // --- Geladenes Magazin ---
            if (active.loadedMagazine != null && active.loadedMagazine.magazineType != null)
            {
                var mt = active.loadedMagazine.magazineType;
                if (ammoTypeText != null) ammoTypeText.text = mt.displayName;
                if (magazineFillText != null)
                    magazineFillText.text = $"{active.loadedMagazine.currentRounds} / {mt.baseCapacity}";
            }
            else if (active.loadedMagazine != null)
            {
                if (ammoTypeText != null) ammoTypeText.text = "(Magazin-Typ fehlt!)";
                if (magazineFillText != null)
                    magazineFillText.text = $"{active.loadedMagazine.currentRounds} / {capacityUnknown}";
            }
            else
            {
                if (ammoTypeText != null) ammoTypeText.text = noMagazineLabel;
                if (magazineFillText != null) magazineFillText.text = capacityUnknown;
            }

            // --- Reserve als einfacher Text ---
            if (reserveMagazinesText != null)
            {
                if (active.reserveMagazines.Count == 0)
                {
                    reserveMagazinesText.text = noReserveLabel;
                }
                else
                {
                    string list = "";
                    foreach (var mag in active.reserveMagazines)
                    {
                        if (mag == null) continue;
                        string magName = mag.magazineType != null ? mag.magazineType.displayName : "(Typ fehlt)";
                        int cap = mag.magazineType != null ? mag.magazineType.baseCapacity : 0;
                        list += $"{magName}: {mag.currentRounds}/{cap}\n";
                    }
                    reserveMagazinesText.text = list.TrimEnd();
                }
            }

            // --- Scrollbare Liste ---
            if (reserveListContent != null)
            {
                if (!rebuildOnlyOnWeaponChange || lastBuiltFor != active)
                {
                    BuildReserveList(active);
                    BuildCatalogList(active);
                    lastBuiltFor = active;
                }
            }
        }

        private void BuildReserveList(WeaponController active)
        {
            ClearRows(spawnedRows);

            if (active == null) return;

            foreach (var mag in active.reserveMagazines)
            {
                if (mag == null) continue;

                string magName = mag.magazineType != null ? mag.magazineType.displayName : "(Typ fehlt)";
                int cap = mag.magazineType != null ? mag.magazineType.baseCapacity : 0;
                string weight = mag.magazineType != null ? $"{mag.GetWeightKg():F2} kg" : "";
                Color color = mag.currentRounds <= 0 ? new Color(0.75f, 0.35f, 0.35f) : Color.white;

                SpawnRow(reserveListContent, spawnedRows, $"{magName}  {mag.currentRounds}/{cap}  {weight}", color);
            }
        }

        private void BuildCatalogList(WeaponController active)
        {
            ClearRows(spawnedCatalogRows);

            if (catalogListContent == null || magazineCatalog == null || active == null) return;

            List<MagazineTypeSO> entries = filterCatalogToWeapon
                ? magazineCatalog.GetMagazinesForWeapon(active.weaponData)
                : magazineCatalog.allMagazineTypes;

            foreach (var mt in entries)
            {
                if (mt == null) continue;

                // Markieren, ob dieser Typ gerade im Inventar ist
                bool inReserve = false;
                foreach (var held in active.reserveMagazines)
                {
                    if (held != null && held.magazineType == mt) { inReserve = true; break; }
                }

                string label = $"{mt.displayName}  ({mt.baseCapacity})  [{mt.magazineClass}]";
                Color color = inReserve ? Color.white : new Color(0.6f, 0.6f, 0.6f);

                SpawnRow(catalogListContent, spawnedCatalogRows, label, color);
            }
        }

        private void SpawnRow(Transform parent, List<GameObject> registry, string text, Color color)
        {
            if (parent == null) return;

            GameObject row;
            if (reserveEntryPrefab != null)
            {
                row = Instantiate(reserveEntryPrefab, parent);
                var label = row.GetComponentInChildren<Text>();
                if (label != null) { label.text = text; label.color = color; }
            }
            else
            {
                // Ohne Prefab: einfache Textzeile automatisch erzeugen
                row = new GameObject("Row", typeof(RectTransform), typeof(Text));
                row.transform.SetParent(parent, false);

                var t = row.GetComponent<Text>();
                t.text = text;
                t.color = color;
                t.fontSize = 16;
                t.alignment = TextAnchor.MiddleLeft;

                var rect = row.GetComponent<RectTransform>();
                rect.sizeDelta = new Vector2(0f, 22f);
            }

            registry.Add(row);
        }

        private static void ClearRows(List<GameObject> registry)
        {
            foreach (var row in registry)
            {
                if (row != null) Destroy(row);
            }
            registry.Clear();
        }

        /// <summary> Sucht ein Text-Element, dessen GameObject so heisst (auch inaktive). </summary>
        private static Text FindTextByName(string exactName)
        {
            foreach (var text in FindObjectsByType<Text>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (text != null && string.Equals(text.name, exactName, System.StringComparison.OrdinalIgnoreCase))
                {
                    return text;
                }
            }
            return null;
        }
    }
}
