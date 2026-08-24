using UnityEngine;

namespace WAD.Weapons
{
    /// <summary>
    /// Laufzeit-Instanz eines Magazins. Bewusst KEIN ScriptableObject, da jedes
    /// einzelne Magazin im Inventar einen eigenen, individuellen Fuellstand hat
    /// (Tarkov-typisch: du kannst 3 Magazine mit 30/12/30 Schuss gleichzeitig tragen).
    /// </summary>
    [System.Serializable]
    public class Magazine
    {
        public AmmoTypeSO ammoType;
        public int capacity;
        public int currentRounds;

        public Magazine(AmmoTypeSO ammoType, int capacity, int startingRounds = -1)
        {
            this.ammoType = ammoType;
            this.capacity = capacity;
            this.currentRounds = startingRounds < 0 ? capacity : Mathf.Clamp(startingRounds, 0, capacity);
        }

        public bool IsEmpty => currentRounds <= 0;
        public bool IsFull => currentRounds >= capacity;

        /// <summary> Gibt true zurueck, wenn eine Patrone entnommen werden konnte. </summary>
        public bool TryConsumeRound()
        {
            if (IsEmpty) return false;
            currentRounds--;
            return true;
        }

        /// <summary> Gewicht dieses Magazins inkl. Munition, fuer das Inventar-/Bewegungssystem. </summary>
        public float GetWeightKg(float emptyMagazineWeightKg = 0.15f)
        {
            float ammoWeight = ammoType != null
                ? (currentRounds * ammoType.weightPerRoundGrams) / 1000f
                : 0f;
            return emptyMagazineWeightKg + ammoWeight;
        }
    }
}