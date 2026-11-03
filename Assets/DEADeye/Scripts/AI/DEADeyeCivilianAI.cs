using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace DEADeye
{
    public enum CivilianState
    {
        Wandering,
        Browsing,
        Panicked,
        Cowering,
        Eliminated
    }

    /// <summary>
    /// AI controller for urban civilians.
    /// Walks sidewalks, flees or cowers on gunfire, and penalizes friendly fire.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public class DEADeyeCivilianAI : MonoBehaviour
    {
        [Header("Classification")]
        [SerializeField] private string classificationName = "CIVILIAN";
        [SerializeField] private int maxHealth = 50;
        [SerializeField] private int currentHealth = 50;
        [SerializeField] private float walkSpeed = 1.2f;
        [SerializeField] private float panicSpeed = 3.2f;

        [Header("State")]
        [SerializeField] private CivilianState currentState = CivilianState.Wandering;
        [SerializeField] private List<Transform> patrolWaypoints = new List<Transform>();
        [SerializeField] private int currentWaypointIndex = 0;

        private NavMeshAgent agent;
        private bool isEliminated = false;

        public string ClassificationName => classificationName;
        public int CurrentHealth => currentHealth;
        public bool IsEliminated => isEliminated;
        public CivilianState CurrentState => currentState;

        public event Action<DEADeyeCivilianAI> OnCivilianCasualty;

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            currentHealth = maxHealth;
        }

        private void Start()
        {
            if (patrolWaypoints.Count > 0)
            {
                NavigateToNextPoint();
            }
        }

        private void Update()
        {
            if (isEliminated || agent == null || !agent.isOnNavMesh) return;

            if (currentState == CivilianState.Wandering)
            {
                if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.3f)
                {
                    NavigateToNextPoint();
                }
            }
            else if (currentState == CivilianState.Panicked)
            {
                if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.3f)
                {
                    currentState = CivilianState.Cowering;
                    agent.isStopped = true;
                }
            }
        }

        public void SetClassification(string name, int health)
        {
            classificationName = name;
            maxHealth = health;
            currentHealth = health;
        }

        public void SetPatrolRoute(List<Transform> waypoints)
        {
            patrolWaypoints = waypoints != null ? new List<Transform>(waypoints) : new List<Transform>();
            currentWaypointIndex = 0;
            if (currentState == CivilianState.Wandering)
            {
                NavigateToNextPoint();
            }
        }

        private void NavigateToNextPoint()
        {
            if (patrolWaypoints.Count == 0 || agent == null || !agent.isOnNavMesh) return;
            agent.speed = walkSpeed;
            agent.SetDestination(patrolWaypoints[currentWaypointIndex].position);
            currentWaypointIndex = (currentWaypointIndex + 1) % patrolWaypoints.Count;
        }

        public void TriggerPanic(Vector3 dangerSource)
        {
            if (isEliminated || currentState == CivilianState.Panicked) return;

            currentState = CivilianState.Panicked;
            if (agent != null && agent.isOnNavMesh)
            {
                agent.speed = panicSpeed;
                agent.isStopped = false;
                // Run away from danger
                Vector3 fleeDir = (transform.position - dangerSource).normalized;
                Vector3 target = transform.position + fleeDir * 18f;
                NavMeshHit hit;
                if (NavMesh.SamplePosition(target, out hit, 10f, NavMesh.AllAreas))
                {
                    agent.SetDestination(hit.position);
                }
            }
        }

        public void TakeDamage(int damage, Vector3 hitPoint, Vector3 hitNormal, bool isHeadshot)
        {
            if (isEliminated) return;

            currentHealth -= damage;
            TriggerPanic(hitPoint);

            if (currentHealth <= 0)
            {
                Eliminate();
            }
        }

        private void Eliminate()
        {
            isEliminated = true;
            currentState = CivilianState.Eliminated;

            if (agent != null && agent.isOnNavMesh)
            {
                agent.isStopped = true;
                agent.enabled = false;
            }

            // Fallback ragdoll or tip over
            transform.Rotate(Vector3.forward, 85f);
            transform.position += Vector3.down * 0.4f;

            // Notify mission and score manager of civilian casualty
            if (DEADeyeScoreManager.Instance != null)
            {
                DEADeyeScoreManager.Instance.RegisterCivilianCasualty();
            }

            OnCivilianCasualty?.Invoke(this);
            Debug.LogWarning($"[DEADeyeCivilianAI] Civilian casualty recorded: {name}");
        }
    }
}
