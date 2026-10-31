using UnityEngine;
using WAD.Weapons;
using WAD.Core;
using WAD.Combat;
using WAD.Inventory;

namespace WAD.Player
{
    /// <summary>
    /// Macht den Spieler zu einem gueltigen IDamageable-Ziel und ist die
    /// EINZIGE Stelle, die den Tod des Spielers ausloest.
    ///
    /// WICHTIG: Der eigentliche Treffer-Code laeuft ueber LimbHealthSystem
    /// (BodyPartHitbox-Collider). EnemyController ruft
    /// LimbHealthSystem.ApplyDamageToLimb(...) auf - NICHT PlayerHealth.
    /// Vorher waren beide Systeme komplett entkoppelt: der Spieler konnte
    /// "sterben" (LimbHealthSystem.IsDead) ohne dass etwas passierte, und
    /// PlayerHealth.Die() wurde nie erreicht. Diese Klasse uebernimmt deshalb
    /// die Fuehrung: LimbHealthSystem ist die Quelle der Wahrheit fuer HP,
    /// PlayerHealth spiegelt sie und feuert den Todespfad.
    /// </summary>
    public class PlayerHealth : MonoBehaviour, IDamageable
    {
        [Header("Referenzen")]
        public InventoryManager inventory;
        public TarkovMovementController movementController;
        [Tooltip("Wird automatisch gesucht, falls leer. Quelle der Wahrheit fuer die HP.")]
        public LimbHealthSystem limbHealthSystem;

        [Header("Fallback (nur ohne LimbHealthSystem)")]
        public float maxHealth = 100f;
        private float currentHealth;

        public float CurrentHealth => currentHealth;
        public float MaxHealth => maxHealth;
        public float HealthPercent01 => Mathf.Clamp01(currentHealth / maxHealth);
        public bool IsDead { get; private set; }

        public event System.Action<float> OnDamaged;   // Schadensbetrag
        public event System.Action<float> OnHealed;    // Heilbetrag
        public event System.Action OnDied;

        private void Awake()
        {
            if (limbHealthSystem == null) limbHealthSystem = GetComponent<LimbHealthSystem>();
            currentHealth = maxHealth;
        }

        private void OnEnable()
        {
            if (limbHealthSystem == null) return;

            limbHealthSystem.OnLimbDamaged += HandleLimbDamaged;
            limbHealthSystem.OnHealed += HandleLimbHealed;
            limbHealthSystem.OnDied += Die;

            SyncFromLimbs();
        }

        private void OnDisable()
        {
            if (limbHealthSystem == null) return;

            limbHealthSystem.OnLimbDamaged -= HandleLimbDamaged;
            limbHealthSystem.OnHealed -= HandleLimbHealed;
            limbHealthSystem.OnDied -= Die;
        }

        /// <summary>
        /// Spiegelt den Gesamtzustand der Koerperteile auf die flachen HP.
        /// Dadurch funktionieren HUD/UI und die Low-Health-Musik weiterhin,
        /// obwohl die Treffer am Limb-System ankommen.
        /// </summary>
        private void SyncFromLimbs()
        {
            if (limbHealthSystem == null) return;
            currentHealth = limbHealthSystem.OverallHealthPercent01 * maxHealth;
        }

        private void HandleLimbDamaged(LimbType limb, float amount)
        {
            SyncFromLimbs();
            OnDamaged?.Invoke(amount);
        }

        private void HandleLimbHealed(float amount)
        {
            SyncFromLimbs();
            OnHealed?.Invoke(amount);
        }

        /// <summary>
        /// Direkter Schaden. Wird zurzeit nur vom Waffen-Raycast gegen Ziele
        /// aufgerufen, die IDamageable implementieren. Mit vorhandenem
        /// LimbHealthSystem wird der Schaden in den Thorax geleitet, damit es
        /// weiterhin GENAU EINEN HP-Pfad gibt; ohne Limb-System greift der
        /// flache Fallback.
        /// </summary>
        public void ApplyDamage(float amount, Vector3 hitPoint, Vector3 hitDirection)
        {
            if (IsDead) return;

            if (limbHealthSystem != null)
            {
                limbHealthSystem.ApplyDamageToLimb(LimbType.Thorax, amount);
                return;
            }

            currentHealth = Mathf.Max(0f, currentHealth - amount);
            OnDamaged?.Invoke(amount);

            if (currentHealth <= 0f) Die();
        }

        public void Heal(float amount)
        {
            if (IsDead) return;

            if (limbHealthSystem != null)
            {
                limbHealthSystem.HealAll(amount);
                return;
            }

            currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
            OnHealed?.Invoke(amount);
        }

        private void Die()
        {
            if (IsDead) return; // LimbHealthSystem und ApplyDamage koennen beide ausloesen
            IsDead = true;

            Debug.Log("[PlayerHealth] Spieler gestorben.");
            OnDied?.Invoke();

            if (movementController != null) movementController.enabled = false;
            if (inventory != null) inventory.ClearOnDeath();

            RunStateManager.Instance?.OnDeath();

            // TODO: Death-Screen/Respawn-Logik, sobald das UI dafuer steht
        }

        /// <summary> Fuer Respawn/neuen Run: HP zuruecksetzen. </summary>
        public void ResetHealth()
        {
            if (limbHealthSystem != null) limbHealthSystem.ResetAll();

            currentHealth = maxHealth;
            IsDead = false;
            if (movementController != null) movementController.enabled = true;
        }
    }
}
