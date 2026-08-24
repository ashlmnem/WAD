using UnityEngine;
using WAD.Weapons;

namespace WAD.Combat
{
    /// <summary>
    /// Liegt auf JEDEM einzelnen Koerperteil-Collider (Kind-Objekt von Spieler
    /// oder Gegner). Ersetzt die bisherige direkte IDamageable-Implementierung
    /// auf EnemyController/PlayerHealth - jetzt muss jeder Treffer eine
    /// konkrete Hitbox treffen, damit Schaden entsteht (echtes Hit-System).
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class BodyPartHitbox : MonoBehaviour, IDamageable
    {
        public LimbType limbType;
        [Tooltip("Kopf z.B. 3x, Torso 1x, Arme/Beine 0.7x - stellt Headshot-Bonus etc. ein")]
        public float damageMultiplier = 1f;

        [Tooltip("Falls leer, wird automatisch im Parent gesucht")]
        public LimbHealthSystem limbHealthSystem;

        private void Awake()
        {
            if (limbHealthSystem == null)
            {
                limbHealthSystem = GetComponentInParent<LimbHealthSystem>();
            }
        }

        public void ApplyDamage(float amount, Vector3 hitPoint, Vector3 hitDirection)
        {
            limbHealthSystem?.ApplyDamageToLimb(limbType, amount * damageMultiplier);
        }
    }
}
