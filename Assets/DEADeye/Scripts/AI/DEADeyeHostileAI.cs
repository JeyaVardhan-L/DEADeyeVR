using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace DEADeye
{
    public enum HostileType
    {
        Criminal,
        Monster
    }

    /// <summary>
    /// Base class for all urban hostile AI units.
    /// Operates via a formal state machine (Spawn, Patrol, Suspicious, Alerted, Escape, Eliminated)
    /// driven by NavMeshAgent pathfinding.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public class DEADeyeHostileAI : MonoBehaviour
    {
        [Header("Classification")]
        [SerializeField] protected HostileType hostileType = HostileType.Criminal;
        [SerializeField] protected string classificationName = "HOSTILE";
        [SerializeField] protected int basePoints = 100;
        [SerializeField] protected int maxHealth = 100;
        [SerializeField] protected int currentHealth;

        [Header("Movement Speeds")]
        [SerializeField] protected float walkSpeed = 1.6f;
        [SerializeField] protected float alertSpeed = 2.8f;
        [SerializeField] protected float escapeSpeed = 3.6f;

        [Header("Navigation")]
        [SerializeField] protected List<Transform> waypoints = new List<Transform>();
        [SerializeField] protected Transform escapeTarget;
        [SerializeField] protected int currentWaypointIndex = 0;

        protected NavMeshAgent agent;
        protected IEnemyState currentState;
        protected HostileStateType currentStateType = HostileStateType.Spawn;
        protected DEADeyeHostileMarker hostileMarker;
        protected bool isEliminated = false;
        protected bool hasEscaped = false;

        public HostileType HostileType => hostileType;
        public string ClassificationName => classificationName;
        public int BasePoints => basePoints;
        public int CurrentHealth => currentHealth;
        public bool IsEliminated => isEliminated;
        public bool HasEscaped => hasEscaped;
        public NavMeshAgent Agent => agent;
        public HostileStateType CurrentStateType => currentStateType;

        public float WalkSpeed => walkSpeed * GetDifficultyMultiplier();
        public float AlertSpeed => alertSpeed * GetDifficultyMultiplier();
        public float EscapeSpeed => escapeSpeed * GetDifficultyMultiplier();

        public event Action<DEADeyeHostileAI> OnEliminated;
        public event Action<DEADeyeHostileAI> OnEscaped;

        protected virtual void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            currentHealth = maxHealth;

            // Ensure Hostile Marker
            hostileMarker = GetComponent<DEADeyeHostileMarker>();
            if (hostileMarker == null)
            {
                hostileMarker = gameObject.AddComponent<DEADeyeHostileMarker>();
            }

            // Ensure Hitbox on root if none exists
            if (GetComponent<Collider>() == null)
            {
                CapsuleCollider col = gameObject.AddComponent<CapsuleCollider>();
                col.center = new Vector3(0f, 0.9f, 0f);
                col.height = 1.8f;
                col.radius = 0.35f;
            }
        }

        protected virtual void Start()
        {
            if (DEADeyeEnemyRegistry.Instance != null)
            {
                DEADeyeEnemyRegistry.Instance.RegisterEnemy(this);
            }

            ChangeState(HostileStateType.Spawn);
        }

        protected virtual void Update()
        {
            if (currentState != null && !isEliminated && !hasEscaped)
            {
                currentState.UpdateState(this);
            }
        }

        public void SetNavigationRoute(List<Transform> pathWaypoints, Transform escapeZone)
        {
            waypoints = pathWaypoints != null ? new List<Transform>(pathWaypoints) : new List<Transform>();
            escapeTarget = escapeZone;
            currentWaypointIndex = 0;
        }

        public bool NavigateToNextWaypoint()
        {
            if (waypoints == null || waypoints.Count == 0 || currentWaypointIndex >= waypoints.Count)
            {
                return false;
            }

            Transform targetWP = waypoints[currentWaypointIndex];
            currentWaypointIndex++;

            if (targetWP != null && agent != null && agent.isOnNavMesh)
            {
                agent.SetDestination(targetWP.position);
                return true;
            }

            return false;
        }

        public void NavigateToEscapeZone()
        {
            if (escapeTarget != null && agent != null && agent.isOnNavMesh)
            {
                agent.SetDestination(escapeTarget.position);
            }
        }

        public virtual void ChangeState(HostileStateType nextStateType)
        {
            if (isEliminated || hasEscaped) return;

            currentState?.Exit(this);
            currentStateType = nextStateType;

            switch (nextStateType)
            {
                case HostileStateType.Spawn:
                    currentState = new HostileSpawnState();
                    break;
                case HostileStateType.Patrol:
                    currentState = new HostilePatrolState();
                    break;
                case HostileStateType.Suspicious:
                    currentState = new HostileSuspiciousState();
                    break;
                case HostileStateType.Alerted:
                    currentState = new HostileAlertedState();
                    break;
                case HostileStateType.Escape:
                    currentState = new HostileEscapeState();
                    break;
                case HostileStateType.Eliminated:
                    currentState = new HostileEliminatedState();
                    break;
            }

            currentState?.Enter(this);
        }

        public virtual void TakeDamage(int damage, Vector3 hitPoint, Vector3 hitNormal, bool isHeadshot)
        {
            if (isEliminated || hasEscaped) return;

            int effectiveDamage = isHeadshot ? damage * 3 : damage;
            currentHealth -= effectiveDamage;

            // Hit feedback
            if (DEADeyeScoreManager.Instance != null)
            {
                DEADeyeScoreManager.Instance.RegisterHit(isHeadshot, basePoints);
            }

            if (DEADeyeAudioManager.Instance != null)
            {
                DEADeyeAudioManager.Instance.PlayHitConfirm();
            }

            if (currentHealth <= 0)
            {
                Eliminate(hitPoint, hitNormal, isHeadshot);
            }
            else
            {
                // If wounded, instantly become alerted and sprint
                ChangeState(HostileStateType.Alerted);
            }
        }

        protected virtual void Eliminate(Vector3 hitPoint, Vector3 hitNormal, bool isHeadshot)
        {
            isEliminated = true;
            ChangeState(HostileStateType.Eliminated);

            if (hostileMarker != null)
            {
                hostileMarker.SetVisible(false);
            }

            if (DEADeyeEnemyRegistry.Instance != null)
            {
                DEADeyeEnemyRegistry.Instance.UnregisterEnemy(this);
            }

            if (DEADeyeScoreManager.Instance != null)
            {
                DEADeyeScoreManager.Instance.RegisterElimination();
            }

            OnEliminated?.Invoke(this);

            // Procedural death reaction (collapse/sink and destroy)
            StartCoroutine(EliminationRoutine());
        }

        public virtual void OnReachedEscapeZone(DEADeyeEscapeZone zone)
        {
            if (isEliminated || hasEscaped) return;

            hasEscaped = true;
            Debug.Log($"[DEADeyeHostileAI] Hostile {name} escaped via {zone.ZoneName}!");

            if (hostileMarker != null)
            {
                hostileMarker.SetVisible(false);
            }

            if (DEADeyeEnemyRegistry.Instance != null)
            {
                DEADeyeEnemyRegistry.Instance.UnregisterEnemy(this);
            }

            if (DEADeyeScoreManager.Instance != null)
            {
                DEADeyeScoreManager.Instance.RegisterEscape();
            }

            OnEscaped?.Invoke(this);
            Destroy(gameObject, 0.5f);
        }

        private IEnumerator EliminationRoutine()
        {
            float elapsed = 0f;
            Vector3 startPos = transform.position;
            Vector3 endPos = startPos + Vector3.down * 0.8f;
            Quaternion startRot = transform.rotation;
            Quaternion endRot = startRot * Quaternion.Euler(75f, 0f, 0f);

            while (elapsed < 1.0f)
            {
                elapsed += Time.deltaTime;
                transform.position = Vector3.Lerp(startPos, endPos, elapsed);
                transform.rotation = Quaternion.Slerp(startRot, endRot, elapsed);
                yield return null;
            }

            yield return new WaitForSeconds(1.5f);
            Destroy(gameObject);
        }

        private float GetDifficultyMultiplier()
        {
            if (DEADeyeGameManager.Instance == null) return 1.0f;
            switch (DEADeyeGameManager.Instance.Difficulty)
            {
                case MissionDifficulty.Recruit: return 0.8f;
                case MissionDifficulty.Elite: return 1.35f;
                case MissionDifficulty.Operative:
                default: return 1.0f;
            }
        }
    }
}

