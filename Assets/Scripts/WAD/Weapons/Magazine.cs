using UnityEngine;

namespace WAD.Weapons
{
    /// <summary>
    /// Laufzeit-Instanz eines konkreten, getragenen Magazins. Referenziert
    /// jetzt einen MagazineTypeSO statt direkt AmmoType+Capacity - dadurch
    /// bestimmt einzig der Typ, welche Munition/Kapazitaet gilt (Punkt 6+7+8).
    /// </summary>
    [System.Serializable]
    public class Magazine
    {
        public MagazineTypeSO magazineType;
        public int currentRounds;

        public AmmoTypeSO ammoType => magazineType != null ? magazineType.ammoType : null;
        public int capacity => magazineType != null ? magazineType.baseCapacity : 0;

        public Magazine(MagazineTypeSO type, int startingRounds = -1)
        {
            magazineType = type;
            currentRounds = startingRounds < 0 ? capacity : Mathf.Clamp(startingRounds, 0, capacity);
        }

        public bool IsEmpty => currentRounds <= 0;
        public bool IsFull => currentRounds >= capacity;

        public bool TryConsumeRound()
        {
            if (IsEmpty) return false;
            currentRounds--;
            return true;
        }

        public float GetWeightKg()
        {
            if (magazineType == null) return 0f;
            float ammoWeight = ammoType != null ? (currentRounds * ammoType.weightPerRoundGrams) / 1000f : 0f;
            return magazineType.emptyWeightKg + ammoWeight;
        }
    }
}