using System.Collections.Generic;
using UnityEngine;

namespace WAD.Weapons
{
    /// <summary>
    /// Zentraler Katalog aller Magazin-Typen im Spiel, gruppiert nach Kaliber.
    /// Wird von Loot-Spawnern und vom Inspect-Fenster (V) benutzt.
    ///
    /// Neues Magazin hinzufuegen: Asset vom Typ MagazineTypeSO erstellen und
    /// hier in die Liste ziehen. Danach ist es automatisch Katalog-, Loot- und
    /// UI-Inhalt. Kein Code noetig.
    ///
    /// Erstelle im Editor via: Assets > Create > WAD > Magazine Catalog
    /// </summary>
    [CreateAssetMenu(fileName = "MagazineCatalog", menuName = "WAD/Magazine Catalog")]
    public class MagazineCatalogSO : ScriptableObject
    {
        [Tooltip("Alle Magazin-Typen im Spiel. Kaliber wird aus dem AmmoTypeSO des jeweiligen Magazins gelesen.")]
        public List<MagazineTypeSO> allMagazineTypes = new List<MagazineTypeSO>();

        /// <summary>
        /// Zuletzt geladener Katalog. ScriptableObjects sind keine
        /// Szenen-Objekte, deshalb kann man sie nicht per FindFirstObjectByType
        /// finden - dieser Zeiger erlaubt Systemen wie dem Loot-Spawner den
        /// Zugriff, ohne dass manuell verdrahtet werden muss.
        /// </summary>
        public static MagazineCatalogSO Instance { get; private set; }

        private void OnEnable()
        {
            Instance = this;
        }

        private void OnDisable()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary> Alle Magazine, die zu diesem Kaliber passen. </summary>
        public List<MagazineTypeSO> GetMagazinesForCaliber(AmmoTypeSO ammoType)
        {
            var result = new List<MagazineTypeSO>();
            if (ammoType == null) return result;

            foreach (var mag in allMagazineTypes)
            {
                if (mag != null && mag.ammoType == ammoType) result.Add(mag);
            }
            return result;
        }

        /// <summary> Alle Magazine, die diese Waffe laden kann (alle Kaliber der Waffe). </summary>
        public List<MagazineTypeSO> GetMagazinesForWeapon(WeaponSO weapon)
        {
            var result = new List<MagazineTypeSO>();
            if (weapon == null) return result;

            foreach (var mag in allMagazineTypes)
            {
                if (mag != null && weapon.AcceptsMagazine(mag) && !result.Contains(mag)) result.Add(mag);
            }
            return result;
        }

        /// <summary> Alle Magazine einer Grobklasse (z.B. nur Trommelmagazine). </summary>
        public List<MagazineTypeSO> GetMagazinesOfClass(MagazineClass magazineClass)
        {
            var result = new List<MagazineTypeSO>();
            foreach (var mag in allMagazineTypes)
            {
                if (mag != null && mag.magazineClass == magazineClass) result.Add(mag);
            }
            return result;
        }

        /// <summary> Alle im Katalog vorkommenden Kaliber (fuer Filter-Dropdowns). </summary>
        public List<AmmoTypeSO> GetAllCalibers()
        {
            var result = new List<AmmoTypeSO>();
            foreach (var mag in allMagazineTypes)
            {
                if (mag != null && mag.ammoType != null && !result.Contains(mag.ammoType)) result.Add(mag.ammoType);
            }
            return result;
        }

        /// <summary> Zufaelliges Magazin, optional auf ein Kaliber eingeschraenkt (fuer Loot). </summary>
        public MagazineTypeSO GetRandomMagazine(AmmoTypeSO ammoTypeFilter = null, MagazineClass? classFilter = null)
        {
            var pool = new List<MagazineTypeSO>();
            foreach (var mag in allMagazineTypes)
            {
                if (mag == null) continue;
                if (ammoTypeFilter != null && mag.ammoType != ammoTypeFilter) continue;
                if (classFilter.HasValue && mag.magazineClass != classFilter.Value) continue;
                pool.Add(mag);
            }

            if (pool.Count == 0) return null;
            return pool[Random.Range(0, pool.Count)];
        }
    }
}
