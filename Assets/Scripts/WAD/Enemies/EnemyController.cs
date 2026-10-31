using System.Collections.Generic;
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
        [Tooltip("Wie lange der Gegner die letzte bekannte Spielerposition noch verfolgt, bevor er aufgibt")]
        public float loseTargetSeconds = 4f;
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

        // Letzte Position, an der der Spieler tatsaechlich gesehen wurde.
        // Verhindert, dass der Gegner den Spieler durch Waende hindurch "spuert".
        private Vector3 lastKnownPlayerPosition;
        private float lastSeenTime = -999f;

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
            if (limbHealth != null)
            {
                limbHealth.OnDied += HandleDied;
                limbHealth.OnLimbDamaged += HandleLimbDamaged;
                limbHealth.OnLimbDestroyed += HandleLimbDestroyed;
            }

            // Ersten Pfad sofort anfordern: ohne Ziel ist 'remainingDistance'
            // unendlich, wodurch die Ankunftspruefung in TickPatrol/TickWander
            // nie wahr wurde und der Gegner nie einen ersten Patrouillenpunkt
            // anlief (er stand still, bis er den Spieler entdeckte).
            if (agent != null && agent.isOnNavMesh && state != EnemyState.Dead)
            {
                RequestPatrolDestination();
            }
        }

        private void OnDisable()
        {
            if (limbHealth != null)
            {
                limbHealth.OnDied -= HandleDied;
                limbHealth.OnLimbDamaged -= HandleLimbDamaged;
                limbHealth.OnLimbDestroyed -= HandleLimbDestroyed;
            }
        }

        /// <summary> Fordert sofort ein (erstes) Patrouillen-/Wanderziel an. </summary>
        private void RequestPatrolDestination()
        {
            if (patrolPoints != null && patrolPoints.Length > 0)
            {
                Transform point = patrolPoints[currentPatrolIndex];
                if (point != null) SetDestinationSafe(point.position);
                return;
            }

            Vector3 randomDirection = Random.insideUnitSphere * wanderRadius;
            randomDirection += transform.position;

            if (NavMesh.SamplePosition(randomDirection, out NavMeshHit hit, wanderRadius, NavMesh.AllAreas))
            {
                SetDestinationSafe(hit.position);
            }
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

            // Kein Spieler in Reichweite ODER keine Sichtlinie: Ziel nach einer
            // Gnadenfrist aufgeben. Vorher hat ein einzelner Fehlschlag den
            // Zustand NICHT zurueckgesetzt - der Gegner verfolgte den Spieler
            // dadurch mit perfektem Wissen durch Waende hindurch.
            if (candidate == null || !HasLineOfSight(candidate))
            {
                if (IsEngagingPlayer && Time.time - lastSeenTime > loseTargetSeconds)
                {
                    state = EnemyState.Patrol;
                    playerTransform = null;
                }
                return;
            }

            bool wasEngaging = IsEngagingPlayer;

            playerTransform = candidate;
            lastKnownPlayerPosition = candidate.position;
            lastSeenTime = Time.time;

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

        /// <summary>
        /// Erreicht der Agent sein Ziel? WICHTIG: 'remainingDistance' ist
        /// UNENDLICH, solange kein Pfad berechnet wurde (Unity-Doku), und der
        /// Agent bleibt innerhalb seiner eigenen 'stoppingDistance' stehen.
        /// Ein fester Vergleich wie '&lt; 0.5f' ist deshalb NIE erfuellbar, wenn
        /// stoppingDistance >= 0.5 ist (im Projekt: 1.5).
        /// </summary>
        private bool HasArrived()
        {
            if (agent.pathPending) return false;
            if (!agent.hasPath) return true;                       // kein Pfad -> neues Ziel anfordern
            if (agent.pathStatus != NavMeshPathStatus.PathComplete) return true; // unerreichbar -> neues Ziel

            return agent.remainingDistance <= agent.stoppingDistance + 0.25f;
        }

        /// <summary> Setzt ein Ziel nur, wenn der Agent gueltig auf dem NavMesh liegt. </summary>
        private bool SetDestinationSafe(Vector3 destination)
        {
            if (!agent.isOnNavMesh || !agent.isActiveAndEnabled) return false;

            return agent.SetDestination(destination);
        }

        // ---- Patrol ----
        private void TickPatrol()
        {
            if (patrolPoints == null || patrolPoints.Length == 0)
            {
                TickWander();
                return;
            }

            if (!HasArrived()) return;

            patrolWaitTimer += Time.deltaTime;
            if (patrolWaitTimer < patrolWaitSeconds) return;

            patrolWaitTimer = 0f;
            currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;

            Transform point = patrolPoints[currentPatrolIndex];
            if (point != null) SetDestinationSafe(point.position);
        }

        private void TickWander()
        {
            if (!HasArrived()) return;

            wanderWaitTimer += Time.deltaTime;
            if (wanderWaitTimer < wanderWaitSeconds) return;

            wanderWaitTimer = 0f;
            Vector3 randomDirection = Random.insideUnitSphere * wanderRadius;
            randomDirection += transform.position;

            if (NavMesh.SamplePosition(randomDirection, out NavMeshHit hit, wanderRadius, NavMesh.AllAreas))
            {
                SetDestinationSafe(hit.position);
            }
        }

        // ---- Chase ----
        private void TickChase()
        {
            if (playerTransform == null) { state = EnemyState.Patrol; return; }

            // Bei aktivem Cover-AI besitzt EnemyCombatAI das Ziel des Agenten -
            // sonst ueberschreiben sich beide Skripte jeden Frame.
            if (useCoverAI) return;

            // Nur die zuletzt GESEHENE Position anlaufen, nicht die Live-Position.
            Vector3 destination = Time.time - lastSeenTime <= loseTargetSeconds
                ? lastKnownPlayerPosition
                : playerTransform.position;

            SetDestinationSafe(destination);
        }

        // ---- Attack ----
        private void TickAttack()
        {
            if (useCoverAI) return;

            if (playerTransform == null) { state = EnemyState.Patrol; return; }

            // Nicht durch Waende schiessen: ohne aktuelle Sichtlinie kein Angriff.
            if (!HasLineOfSight(playerTransform)) return;

            if (requireGroupToAttack && !HasEnoughGroupNearby())
            {
                SetDestinationSafe(transform.position);
                return;
            }

            SetDestinationSafe(transform.position);
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

        /// <summary>
        /// Zaehlt ANDERE Gegner in der Naehe. Vorher wurden schlicht die
        /// Collider der Abfrage gezaehlt - dazu gehoerten die eigenen
        /// Hitbox-Collider (alle auf dem Enemy-Layer), wodurch die Pruefung
        /// mit minGroupSize = 1 immer wahr war und die Gruppen-Anforderung
        /// der "Death Insurgency" nie etwas bewirkt hat.
        /// </summary>
        private bool HasEnoughGroupNearby()
        {
            Collider[] nearby = Physics.OverlapSphere(transform.position, groupCheckRadius, sameTypeLayer);

            var others = new HashSet<EnemyController>();
            foreach (var col in nearby)
            {
                var other = col.GetComponentInParent<EnemyController>();
                if (other != null && other != this) others.Add(other);
            }

            return others.Count >= minGroupSize;
        }

        /// <summary>
        /// Nahkampf-/einfacher Angriff trifft pauschal den Thorax (kein Raycast
        /// gegen konkrete Hitboxen noetig fuer diese Art von Angriff).
        /// Die Reichweite wird hier ERNEUT geprueft: EnemyCombatAI ruft
        /// ExecuteAttack() aus der Deckung heraus auf, ohne selbst eine
        /// Distanzpruefung zu machen - ohne diesen Guard traf der Gegner
        /// ueber die gesamte Karte.
        /// </summary>
        protected virtual void PerformAttack()
        {
            if (playerTransform == null) return;

            if (Vector3.Distance(transform.position, playerTransform.position) > attackRange)
            {
                return;
            }

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
            if (state == EnemyState.Dead) return;
            state = EnemyState.Dead;

            if (agent != null && agent.isOnNavMesh) agent.isStopped = true;

            // Kill-Zaehler fuer Taskmaster-Aufgaben fuehren
            WAD.Core.RunStateManager.Instance?.RegisterKill();

            OnDeath?.Invoke(this);

            SpawnLootableCorpse();

            // Destroy(this) hat zuvor EnemyCombatAI/EnemyAudio/SuppressionController
            // mit einer zerstoerten EnemyController-Referenz weiterlaufen lassen
            // (und EnemyCombatAI gab seine Deckung nie frei).
            var combatAI = GetComponent<EnemyCombatAI>();
            if (combatAI != null) combatAI.enabled = false;

            var suppression = GetComponent<EnemySuppressionController>();
            if (suppression != null) suppression.enabled = false;

            if (agent != null) agent.enabled = false;
            enabled = false;
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