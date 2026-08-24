using UnityEngine;
using WAD.Weapons;
using WAD.Core;
using WAD.Inventory;

namespace WAD.Player
{
    /// <summary>
    /// Macht den Spieler zu einem gueltigen IDamageable-Ziel (Gegner koennen
    /// ApplyDamage aufrufen). Bei 0 HP: Tod -> RunStateManager.OnDeath()
    /// (Loot-Verlust) + Inventar leeren.
    ///
    /// WICHTIG: Das Player-Root-Objekt braucht den Tag "Player" (siehe
    /// CutsceneTrigger, EnemyController-Erkennung ueber Layer statt Tag -
    /// aber fuer andere Systeme spaeter nuetzlich).
    /// </summary>
    public class PlayerHealth : MonoBehaviour, IDamageable
    {
        [Header("Stats")]
        public float maxHealth = 100f;
        private float currentHealth;

        [Header("Referenzen")]
        public InventoryManager inventory;
        public TarkovMovementController movementController;

        public float CurrentHealth => currentHealth;
        public float MaxHealth => maxHealth;
        public float HealthPercent01 => Mathf.Clamp01(currentHealth / maxHealth);
        public bool IsDead { get; private set; }

        public event System.Action<float> OnDamaged;   // Schadensbetrag
        public event System.Action<float> OnHealed;    // Heilbetrag
        public event System.Action OnDied;

        private void Awake()
        {
            currentHealth = maxHealth;
        }

        public void ApplyDamage(float amount, Vector3 hitPoint, Vector3 hitDirection)
        {
            if (IsDead) return;

            currentHealth -= amount;
            currentHealth = Mathf.Max(0f, currentHealth);
            OnDamaged?.Invoke(amount);

            if (currentHealth <= 0f)
            {
                Die();
            }
        }

        public void Heal(float amount)
        {
            if (IsDead) return;

            currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
            OnHealed?.Invoke(amount);
        }

        private void Die()
        {
            IsDead = true;
            OnDied?.Invoke();

            if (movementController != null) movementController.enabled = false;
            if (inventory != null) inventory.ClearOnDeath();

            RunStateManager.Instance?.OnDeath();

            // TODO: Death-Screen/Respawn-Logik, sobald das UI dafuer steht
        }

        /// <summary> Fuer Respawn/neuen Run: HP zuruecksetzen. </summary>
        public void ResetHealth()
        {
            currentHealth = maxHealth;
            IsDead = false;
            if (movementController != null) movementController.enabled = true;
        }
    }
}