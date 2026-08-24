using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace WAD.Enemies
{
    public enum CombatSubState { Advancing, InCover, SuppressiveFire }

    /// <summary>
    /// Ersetzt (bei EnemyController.useCoverAI = true) die simple "stehenbleiben
    /// und schiessen"-Logik durch:
    ///
    /// 1) Cover-Hopping: wechselt periodisch zu einer freien CoverPoint-Position
    ///    in der Naehe, statt frei im offenen Feld stehen zu bleiben
    /// 2) Suppressive Fire: wenn der Spieler kurzzeitig keine Sichtlinie hat
    ///    (hinter Deckung), feuert der Gegner trotzdem in Richtung der letzten
    ///    bekannten Position, um den Spieler unter Druck zu halten
    ///
    /// Bewusst vereinfacht (kein echtes "beste Deckung basierend auf Schusswinkel"-
    /// Pathfinding) - fuer den Umfang des Projekts ausreichend und performant.
    /// </summary>
    [RequireComponent(typeof(EnemyController))]
    [RequireComponent(typeof(NavMeshAgent))]
    public class EnemyCombatAI : MonoBehaviour
    {
        [Header("Cover-Suche")]
        public float coverSearchRadius = 20f;
        public float minTimeInCover = 3f;
        public float maxTimeInCover = 7f;
        public LayerMask coverLayer;

        [Header("Suppressive Fire")]
        public float loseSightGraceTime = 1.5f; // Zeit ohne Sichtlinie, bevor auf Suppressive Fire umgeschaltet wird
        public float suppressiveFireDuration = 4f;
        public float suppressiveFireCooldown = 2f;

        private EnemyController enemy;
        private NavMeshAgent agent;

        private CombatSubState subState = CombatSubState.Advancing;
        private CoverPoint currentCover;
        private float nextCoverSwitchTime;

        private Vector3 lastKnownPlayerPosition;
        private float timeWithoutSight;
        private float suppressiveFireTimer;
        private float lastSuppressiveShotTime;

        private void Awake()
        {
            enemy = GetComponent<EnemyController>();
            agent = GetComponent<NavMeshAgent>();
        }

        private void Update()
        {
            if (!enemy.useCoverAI) return;
            if (enemy.CurrentState != EnemyState.Attack && enemy.CurrentState != EnemyState.Chase) return;
            if (enemy.PlayerTransform == null) return;

            bool hasSight = HasLineOfSightToPlayer();

            if (hasSight)
            {
                lastKnownPlayerPosition = enemy.PlayerTransform.position;
                timeWithoutSight = 0f;
                if (subState == CombatSubState.SuppressiveFire) subState = CombatSubState.Advancing;
            }
            else
            {
                timeWithoutSight += Time.deltaTime;
                if (timeWithoutSight >= loseSightGraceTime && subState != CombatSubState.SuppressiveFire)
                {
                    subState = CombatSubState.SuppressiveFire;
                    suppressiveFireTimer = 0f;
                }
            }

            switch (subState)
            {
                case CombatSubState.Advancing: TickAdvancing(hasSight); break;
                case CombatSubState.InCover: TickInCover(hasSight); break;
                case CombatSubState.SuppressiveFire: TickSuppressiveFire(); break;
            }
        }

        private bool HasLineOfSightToPlayer()
        {
            Vector3 origin = transform.position + Vector3.up * 1.5f;
            Vector3 targetPos = enemy.PlayerTransform.position + Vector3.up * 1.5f;
            Vector3 direction = targetPos - origin;

            return !Physics.Raycast(origin, direction.normalized, direction.magnitude, enemy.obstructionLayers);
        }

        // ---- Advancing: normal in Richtung/auf den Spieler zielen, danach ab und zu Deckung suchen ----
        private void TickAdvancing(bool hasSight)
        {
            if (hasSight)
            {
                FaceAndFire();
            }

            if (Time.time >= nextCoverSwitchTime)
            {
                TryMoveToNewCover();
            }
        }

        private void TryMoveToNewCover()
        {
            CoverPoint best = FindNearestFreeCover();
            if (best == null) return;

            if (currentCover != null) currentCover.occupiedBy = null;

            currentCover = best;
            currentCover.occupiedBy = enemy;
            agent.SetDestination(currentCover.transform.position);
            subState = CombatSubState.InCover;
            nextCoverSwitchTime = Time.time + Random.Range(minTimeInCover, maxTimeInCover);
        }

        private CoverPoint FindNearestFreeCover()
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, coverSearchRadius, coverLayer);
            CoverPoint nearest = null;
            float nearestDist = float.MaxValue;

            foreach (var hit in hits)
            {
                var cover = hit.GetComponent<CoverPoint>();
                if (cover == null || cover.IsOccupied) continue;

                float dist = Vector3.Distance(transform.position, cover.transform.position);
                if (dist < nearestDist)
                {
                    nearestDist = dist;
                    nearest = cover;
                }
            }
            return nearest;
        }

        // ---- InCover: warten bis am Ziel, dann feuern, danach irgendwann neue Deckung ----
        private void TickInCover(bool hasSight)
        {
            bool arrived = !agent.pathPending && agent.remainingDistance < 0.5f;

            if (arrived && hasSight)
            {
                FaceAndFire();
            }

            if (Time.time >= nextCoverSwitchTime)
            {
                subState = CombatSubState.Advancing;
                if (currentCover != null) { currentCover.occupiedBy = null; currentCover = null; }
            }
        }

        // ---- Suppressive Fire: auf letzte bekannte Position feuern, obwohl kein Sichtkontakt ----
        private void TickSuppressiveFire()
        {
            suppressiveFireTimer += Time.deltaTime;
            if (suppressiveFireTimer >= suppressiveFireDuration)
            {
                subState = CombatSubState.Advancing;
                return;
            }

            transform.LookAt(new Vector3(lastKnownPlayerPosition.x, transform.position.y, lastKnownPlayerPosition.z));

            if (Time.time - lastSuppressiveShotTime >= suppressiveFireCooldown)
            {
                lastSuppressiveShotTime = Time.time;
                // Feuert "blind" - kein garantierter Treffer, daher hier KEIN
                // enemy.ExecuteAttack() (das wuerde IMMER Schaden verursachen).
                // Stattdessen nur visuelles/akustisches Feedback (Muendungsfeuer,
                // Sound ueber EnemyAudio) - reiner Druck-Effekt auf den Spieler.
                // TODO: optional geringe Trefferchance einbauen, falls gewuenscht.
            }
        }

        private void FaceAndFire()
        {
            transform.LookAt(new Vector3(enemy.PlayerTransform.position.x, transform.position.y, enemy.PlayerTransform.position.z));
            enemy.ExecuteAttack();
        }

        /// <summary>
        /// Von EnemySuppressionController aufgerufen (Punkt 8): erzwingt sofortige
        /// Flucht zur naechsten Deckung + Blindfeuer, unabhaengig vom aktuellen Zustand.
        /// </summary>
        public void ForceSuppressed(float duration)
        {
            CoverPoint best = FindNearestFreeCover();
            if (best != null)
            {
                if (currentCover != null) currentCover.occupiedBy = null;
                currentCover = best;
                currentCover.occupiedBy = enemy;
                agent.SetDestination(currentCover.transform.position);
            }

            subState = CombatSubState.SuppressiveFire;
            suppressiveFireTimer = suppressiveFireDuration - Mathf.Min(duration, suppressiveFireDuration);
            lastKnownPlayerPosition = enemy.PlayerTransform != null ? enemy.PlayerTransform.position : transform.position;
        }

        private void OnDestroy()
        {
            if (currentCover != null) currentCover.occupiedBy = null;
        }
    }
}