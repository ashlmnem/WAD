using System.Collections.Generic;
using UnityEngine;

namespace WAD.Combat
{
    public enum LimbType { Head, Thorax, LeftArm, RightArm, LeftLeg, RightLeg }

    [System.Serializable]
    public class LimbConfig
    {
        public LimbType type;
        public float maxHealth = 50f;
    }

    /// <summary>
    /// Tarkov-/Ready-or-Not-artiges Koerperteile-Hit-System: jeder Limb hat
    /// eigene HP. Kopf/Thorax auf 0 = sofortiger Tod. Arm/Bein-Zerstoerung
    /// loest Effekte aus (Bewegungs-/Genauigkeitsstrafen), toetet aber nicht
    /// direkt - das uebernehmen EnemyController/PlayerHealth per Event.
    ///
    /// Gemeinsam von Spieler UND Gegnern genutzt, damit dasselbe System fuer
    /// beide gilt. Tatsaechliche Treffer kommen von BodyPartHitbox-Collidern.
    /// </summary>
    public class LimbHealthSystem : MonoBehaviour
    {
        [Header("Limb-Konfiguration")]
        public List<LimbConfig> limbConfigs = new List<LimbConfig>
        {
            new LimbConfig { type = LimbType.Head, maxHealth = 35f },
            new LimbConfig { type = LimbType.Thorax, maxHealth = 85f },
            new LimbConfig { type = LimbType.LeftArm, maxHealth = 60f },
            new LimbConfig { type = LimbType.RightArm, maxHealth = 60f },
            new LimbConfig { type = LimbType.LeftLeg, maxHealth = 65f },
            new LimbConfig { type = LimbType.RightLeg, maxHealth = 65f },
        };

        private readonly Dictionary<LimbType, float> currentHealth = new Dictionary<LimbType, float>();
        private readonly Dictionary<LimbType, float> maxHealth = new Dictionary<LimbType, float>();
        private readonly HashSet<LimbType> destroyedLimbs = new HashSet<LimbType>();

        public bool IsDead { get; private set; }

        public event System.Action<LimbType, float> OnLimbDamaged; // (limb, betrag)
        public event System.Action<LimbType> OnLimbDestroyed;
        public event System.Action OnDied;
        public event System.Action<float> OnHealed;

        private void Awake()
        {
            foreach (var config in limbConfigs)
            {
                maxHealth[config.type] = config.maxHealth;
                currentHealth[config.type] = config.maxHealth;
            }
        }

        public void ApplyDamageToLimb(LimbType limb, float amount)
        {
            if (IsDead) return;
            if (!currentHealth.ContainsKey(limb)) return;

            currentHealth[limb] = Mathf.Max(0f, currentHealth[limb] - amount);
            Debug.Log($"[LimbHealthSystem:{gameObject.name}] {limb} getroffen fuer {amount} - verbleibend: {currentHealth[limb]}/{maxHealth[limb]}");
            OnLimbDamaged?.Invoke(limb, amount);

            if (currentHealth[limb] <= 0f && !destroyedLimbs.Contains(limb))
            {
                destroyedLimbs.Add(limb);
                OnLimbDestroyed?.Invoke(limb);
            }

            if ((limb == LimbType.Head || limb == LimbType.Thorax) && currentHealth[limb] <= 0f)
            {
                Die();
            }
        }

        private void Die()
        {
            IsDead = true;
            Debug.Log($"[LimbHealthSystem:{gameObject.name}] TOD ausgeloest.");
            OnDied?.Invoke();
        }

        public float GetLimbHealthPercent(LimbType limb)
        {
            if (!maxHealth.TryGetValue(limb, out float max) || max <= 0f) return 0f;
            return currentHealth[limb] / max;
        }

        public bool IsLimbDestroyed(LimbType limb) => destroyedLimbs.Contains(limb);

        /// <summary> Durchschnittlicher Gesamtzustand ueber alle Limbs (0-1), z.B. fuer Dynamic Music/HUD. </summary>
        public float OverallHealthPercent01
        {
            get
            {
                float totalCurrent = 0f, totalMax = 0f;
                foreach (var kvp in currentHealth)
                {
                    totalCurrent += kvp.Value;
                    totalMax += maxHealth[kvp.Key];
                }
                return totalMax > 0f ? totalCurrent / totalMax : 0f;
            }
        }

        /// <summary> Heilt alle Limbs gleichmaessig (z.B. Medkit) - "entzerstoert" sie ggf. wieder. </summary>
        public void HealAll(float amount)
        {
            if (IsDead) return;

            var keys = new List<LimbType>(currentHealth.Keys);
            foreach (var limb in keys)
            {
                currentHealth[limb] = Mathf.Min(maxHealth[limb], currentHealth[limb] + amount);
                if (currentHealth[limb] > 0f) destroyedLimbs.Remove(limb);
            }
            OnHealed?.Invoke(amount);
        }

        /// <summary> Fuer Respawn/neuen Run. </summary>
        public void ResetAll()
        {
            IsDead = false;
            destroyedLimbs.Clear();
            foreach (var config in limbConfigs)
            {
                currentHealth[config.type] = config.maxHealth;
            }
        }
    }
}