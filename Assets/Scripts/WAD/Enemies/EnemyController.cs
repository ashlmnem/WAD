using UnityEngine;
using UnityEngine.AI;
using WAD.Weapons;
using WAD.Combat;

namespace WAD.Enemies
{
    public enum EnemyState { Patrol, Chase, Attack, Dead }

    /// <summary>
    /// Generisches Gegner-Grundgeruest, wiederverwendbar fuer alle Gegnertypen.
    /// Gesundheit laeuft jetzt komplett ueber LimbHealthSystem (Punkt 6) -
    /// dieses Skript implementiert selbst KEIN IDamageable mehr, sondern
    /// reagiert auf die Events des LimbHealthSystem.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(LimbHealthSystem))]
    public class EnemyController : MonoBehaviour
    {
        [Header("Wahrnehmung")]
        public float sightRange = 25f;
        public float attackRange = 15f;
        public LayerMask playerLayer;
        public LayerMask obstructionLayers;

        [Header("Patrol")]
        public Transform[] patrolPoints;
        private int currentPatrolIndex;
        public float patrolWaitSeconds = 3f;
        private float patrolWaitTimer;

        [Header("Angriff")]
        public float damagePerHit = 15f;
        public float attackCooldown = 1.2f;
        private float lastAttackTime = -999f;

        [Header("Erweiterte KI (Punkt 9)")]
        public bool useCoverAI = false;
        public WAD.Procedural.LootTableSO deathLootTable;
        public WeaponSO droppedWeapon;
        [Range(0f, 1f)] public float droppedWeaponChance = 0.5f;
        public GameObject corpseLootPrefab;

        [Header("Gruppen-Anforderung (z.B. Death Insurgency)")]
        public bool requireGroupToAttack = false;
        public int minGroupSize = 2;
        public float groupCheckRadius = 15f;
        public LayerMask sameTypeLayer;

        [Header("Limb-Effekte")]
        [Tooltip("Geschwindigkeits-Multiplikator, wenn ein Bein zerstoert ist")]
        [Range(0.1f, 1f)] public float legDestroyedSpeedMultiplier = 0.4f;

        private NavMeshAgent agent;
        private LimbHealthSystem limbHealth;
        private Transform playerTransform;
        private EnemyState state = EnemyState.Patrol;
        private float baseAgentSpeed;

        [Header("Wander-Fallback (falls keine Patrol Points gesetzt sind)")]
        public float wanderRadius = 15f;
        public float wanderWaitSeconds = 4f;
        private float wanderWaitTimer;

        public event System.Action<EnemyController> OnDeath;
        public event System.Action<EnemyController> OnSpottedPlayer;
        public event System.Action<EnemyController, float> OnDamaged; // (this, damageAmount)

        public EnemyState CurrentState => state;
        public bool IsEngagingPlayer => state == EnemyState.Chase || state == EnemyState.Attack;
        public Transform PlayerTransform => playerTransform;

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            limbHealth = GetComponent<LimbHealthSystem>();
            baseAgentSpeed = agent.speed;
        }

        private void OnEnable()
        {
            limbHealth.OnDied += HandleDied;
            limbHealth.OnLimbDamaged += HandleLimbDamaged;
            limbHealth.OnLimbDestroyed += HandleLimbDestroyed;
        }

        private void OnDisable()
        {
            limbHealth.OnDied -= HandleDied;
            limbHealth.OnLimbDamaged -= HandleLimbDamaged;
            limbHealth.OnLimbDestroyed -= HandleLimbDestroyed;
        }

        private void Update()
        {
            if (state == EnemyState.Dead) return;

            UpdatePlayerDetection();

            switch (state)
            {
                case EnemyState.Patrol: TickPatrol(); break;
                case EnemyState.Chase: TickChase(); break;
                case EnemyState.Attack: TickAttack(); break;
            }
        }

        // ---- Wahrnehmung ----
        private void UpdatePlayerDetection()
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, sightRange, playerLayer);

            Transform candidate = null;
            foreach (var h in hits)
            {
                if (h.transform.root == transform.root) continue; // niemals sich selbst als "Spieler" erkennen
                if (h.GetComponentInParent<EnemyController>() != null) continue; // niemals andere Gegner als "Spieler" erkennen
                candidate = h.transform;
                break;
            }

            if (candidate == null)
            {
                if (state == EnemyState.Chase || state == EnemyState.Attack) state = EnemyState.Patrol;
                return;
            }

            if (!HasLineOfSight(candidate)) return;

            bool wasEngaging = IsEngagingPlayer;

            playerTransform = candidate;
            float distance = Vector3.Distance(transform.position, playerTransform.position);

            state = distance <= attackRange ? EnemyState.Attack : EnemyState.Chase;

            if (!wasEngaging) OnSpottedPlayer?.Invoke(this);
        }

        private bool HasLineOfSight(Transform target)
        {
            Vector3 origin = transform.position + Vector3.up * 1.5f;
            Vector3 targetPos = target.position + Vector3.up * 1.5f;
            Vector3 direction = targetPos - origin;

            if (Physics.Raycast(origin, direction.normalized, out RaycastHit hit, direction.magnitude, obstructionLayers))
            {
                return false;
            }
            return true;
        }

        // ---- Patrol ----
        private void TickPatrol()
        {
            if (patrolPoints == null || patrolPoints.Length == 0)
            {
                TickWander();
                return;
            }

            if (!agent.pathPending && agent.remainingDistance < 0.5f)
            {
                patrolWaitTimer += Time.deltaTime;
                if (patrolWaitTimer >= patrolWaitSeconds)
                {
                    patrolWaitTimer = 0f;
                    currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
                    agent.SetDestination(patrolPoints[currentPatrolIndex].position);
                }
            }
        }

        private void TickWander()
        {
            if (!agent.pathPending && agent.remainingDistance < 0.5f)
            {
                wanderWaitTimer += Time.deltaTime;
                if (wanderWaitTimer >= wanderWaitSeconds)
                {
                    wanderWaitTimer = 0f;
                    Vector3 randomDirection = Random.insideUnitSphere * wanderRadius;
                    randomDirection += transform.position;

                    if (NavMesh.SamplePosition(randomDirection, out NavMeshHit hit, wanderRadius, NavMesh.AllAreas))
                    {
                        agent.SetDestination(hit.position);
                    }
                }
            }
        }

        // ---- Chase ----
        private void TickChase()
        {
            if (playerTransform == null) { state = EnemyState.Patrol; return; }
            agent.SetDestination(playerTransform.position);
        }

        // ---- Attack ----
        private void TickAttack()
        {
            if (useCoverAI) return;

            if (playerTransform == null) { state = EnemyState.Patrol; return; }

            if (requireGroupToAttack && !HasEnoughGroupNearby())
            {
                agent.SetDestination(transform.position);
                return;
            }

            agent.SetDestination(transform.position);
            transform.LookAt(new Vector3(playerTransform.position.x, transform.position.y, playerTransform.position.z));

            if (Time.time - lastAttackTime >= attackCooldown)
            {
                lastAttackTime = Time.time;
                PerformAttack();
            }
        }

        public void ExecuteAttack()
        {
            if (Time.time - lastAttackTime < attackCooldown) return;
            lastAttackTime = Time.time;
            PerformAttack();
        }

        /// <summary> Von EnemySuppressionController: versetzt den Gegner sofort in Kampfbereitschaft (Punkt 8). </summary>
        public void ForceEngagement()
        {
            if (state == EnemyState.Patrol) state = EnemyState.Chase;
        }

        private bool HasEnoughGroupNearby()
        {
            Collider[] nearby = Physics.OverlapSphere(transform.position, groupCheckRadius, sameTypeLayer);
            return nearby.Length >= minGroupSize;
        }

        /// <summary>
        /// Nahkampf-/einfacher Angriff trifft pauschal den Thorax (kein Raycast
        /// gegen konkrete Hitboxen noetig fuer diese Art von Angriff).
        /// </summary>
        protected virtual void PerformAttack()
        {
            var targetLimbSystem = playerTransform.GetComponentInParent<LimbHealthSystem>();
            targetLimbSystem?.ApplyDamageToLimb(LimbType.Thorax, damagePerHit);
        }

        // ---- Limb-System-Events ----
        private void HandleLimbDamaged(LimbType limb, float amount)
        {
            OnDamaged?.Invoke(this, amount);

            if (state == EnemyState.Patrol)
            {
                state = EnemyState.Chase;
            }
        }

        private void HandleLimbDestroyed(LimbType limb)
        {
            if (limb == LimbType.LeftLeg || limb == LimbType.RightLeg)
            {
                agent.speed = baseAgentSpeed * legDestroyedSpeedMultiplier;
            }
        }

        private void HandleDied()
        {
            state = EnemyState.Dead;
            agent.isStopped = true;
            OnDeath?.Invoke(this);

            SpawnLootableCorpse();

            Destroy(this);
            Destroy(agent);
        }

        private void SpawnLootableCorpse()
        {
            GameObject corpseObj = corpseLootPrefab != null
                ? Instantiate(corpseLootPrefab, transform.position, transform.rotation)
                : gameObject;

            var searchable = corpseObj.GetComponent<WAD.Inventory.CorpseSearchable>();
            if (searchable == null) searchable = corpseObj.AddComponent<WAD.Inventory.CorpseSearchable>();

            searchable.lootTable = deathLootTable;
            searchable.droppedWeapon = droppedWeapon;
            searchable.droppedWeaponChance = droppedWeaponChance;
            searchable.RollLoot();
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, sightRange);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackRange);
        }
    }
}