using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace DEADeye
{
    public enum FriendlyState
    {
        Idle,
        TakingCover,  // Pinned down behind cover
        Moving,       // Advancing between cover points
        Working,      // E.g., repairing communications antenna / console
        UnderFire,    // Actively taking fire
        Eliminated
    }

    /// <summary>
    /// AI controller for friendly and VIP personnel (e.g., Comms Technicians Elena & Marcus).
    /// Supports movement between tactical cover nodes, objective work progress (0-100%),
    /// reactive crouch/cover behavior, spotable recon integration, and mission failure reporting.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public class DEADeyeFriendlyAI : MonoBehaviour
    {
        [Header("Classification")]
        [SerializeField] private string classificationName = "FRIENDLY VIP";
        [SerializeField] private int maxHealth = 100;
        [SerializeField] private int currentHealth = 100;

        [Header("Objective Work (e.g. Comms Repair)")]
        [SerializeField] private bool performsObjectiveWork = false;
        [SerializeField] private float workProgress = 0f; // 0 to 100%
        [SerializeField] private float repairSpeedPerSecond = 3.5f; // ~28s to repair

        [Header("State")]
        [SerializeField] private FriendlyState currentState = FriendlyState.TakingCover;

        [Header("Navigation")]
        [SerializeField] private Vector3 targetDestination;
        [SerializeField] private bool isMovingToTarget = false;

        private NavMeshAgent agent;
        private bool isEliminated = false;
        private float underFireTimer = 0f;
        private Vector3 initialScale;

        public string ClassificationName => classificationName;
        public int CurrentHealth => currentHealth;
        public int MaxHealth => maxHealth;
        public bool IsEliminated => isEliminated;
        public float WorkProgress => workProgress;
        public FriendlyState CurrentState => currentState;
        public bool IsMoving => isMovingToTarget;

        public event Action<float> OnProgressUpdated; // 0..100
        public event Action<DEADeyeFriendlyAI> OnVipDamaged;
        public event Action<DEADeyeFriendlyAI> OnVipKilled;
        public event Action<DEADeyeFriendlyAI> OnReachedDestination;

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            if (agent != null)
            {
                agent.speed = 3.2f;
                agent.acceleration = 8f;
                agent.stoppingDistance = 0.6f;
            }
            currentHealth = maxHealth;
            initialScale = transform.localScale;

            // Ensure Spotable component is present for optical recon
            var spotable = GetComponent<DEADeyeSpotable>();
            if (spotable == null) spotable = gameObject.AddComponent<DEADeyeSpotable>();
            spotable.SetTargetInfo(
                classificationName.ToUpper(),
                SpotClassification.FriendlyVIP,
                "Friendly field communications technician. Priority asset under strict protection protocol."
            );
        }

        private void Update()
        {
            if (isEliminated) return;

            // Handle UnderFire recovery
            if (currentState == FriendlyState.UnderFire)
            {
                underFireTimer -= Time.deltaTime;
                if (underFireTimer <= 0f)
                {
                    if (performsObjectiveWork && Vector3.Distance(transform.position, targetDestination) < 1.8f)
                    {
                        currentState = FriendlyState.Working;
                    }
                    else if (isMovingToTarget)
                    {
                        currentState = FriendlyState.Moving;
                        if (agent != null && agent.isOnNavMesh) agent.isStopped = false;
                    }
                    else
                    {
                        currentState = FriendlyState.TakingCover;
                    }
                    transform.localScale = initialScale; // Return to standing height
                }
            }

            // Handle movement completion
            if (isMovingToTarget && agent != null && agent.isOnNavMesh)
            {
                if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
                {
                    isMovingToTarget = false;
                    if (performsObjectiveWork)
                    {
                        currentState = FriendlyState.Working;
                    }
                    else
                    {
                        currentState = FriendlyState.TakingCover;
                    }
                    OnReachedDestination?.Invoke(this);
                }
            }

            // Handle repair progress
            if (performsObjectiveWork && currentState == FriendlyState.Working)
            {
                if (workProgress < 100f)
                {
                    workProgress = Mathf.Min(100f, workProgress + repairSpeedPerSecond * Time.deltaTime);
                    OnProgressUpdated?.Invoke(workProgress);
                }
            }
        }

        public void SetClassification(string name, int health)
        {
            classificationName = name;
            maxHealth = health;
            currentHealth = health;

            var spotable = GetComponent<DEADeyeSpotable>();
            if (spotable != null)
            {
                spotable.SetTargetInfo(
                    name.ToUpper(),
                    SpotClassification.FriendlyVIP,
                    "Friendly field communications technician. Priority asset under strict protection protocol."
                );
            }
        }

        public void SetPerformsWork(bool performs, float speed = 3.5f)
        {
            performsObjectiveWork = performs;
            repairSpeedPerSecond = speed;
        }

        public void MoveToDestination(Vector3 destination)
        {
            if (isEliminated) return;
            targetDestination = destination;
            isMovingToTarget = true;
            currentState = FriendlyState.Moving;

            if (agent != null)
            {
                if (agent.isOnNavMesh)
                {
                    agent.isStopped = false;
                    agent.SetDestination(destination);
                }
                else
                {
                    // If not on navmesh, warp towards it
                    if (NavMesh.SamplePosition(destination, out NavMeshHit hit, 5f, NavMesh.AllAreas))
                    {
                        agent.Warp(hit.position);
                    }
                }
            }
        }

        public void TakeCoverFromThreat(Vector3 threatPos, float duration = 4.0f)
        {
            if (isEliminated) return;

            currentState = FriendlyState.UnderFire;
            underFireTimer = duration;

            if (agent != null && agent.isOnNavMesh)
            {
                agent.isStopped = true;
            }

            // Crouch visually by scaling Y slightly
            transform.localScale = new Vector3(initialScale.x, initialScale.y * 0.65f, initialScale.z);
        }

        public void TakeDamage(int damage, Vector3 hitPoint, Vector3 hitNormal, bool isHeadshot)
        {
            if (isEliminated) return;

            currentHealth -= damage;
            TakeCoverFromThreat(hitPoint, 5.0f);
            OnVipDamaged?.Invoke(this);

            if (DEADeyeAudioManager.Instance != null)
            {
                DEADeyeAudioManager.Instance.PlayBodyHit();
            }

            if (currentHealth <= 0)
            {
                Eliminate();
            }
        }

        private void Eliminate()
        {
            isEliminated = true;
            currentState = FriendlyState.Eliminated;

            if (agent != null && agent.isOnNavMesh)
            {
                agent.isStopped = true;
                agent.enabled = false;
            }

            // Tip over
            transform.Rotate(Vector3.forward, 85f);
            transform.position += Vector3.down * 0.4f;

            OnVipKilled?.Invoke(this);
            Debug.LogWarning($"[DEADeyeFriendlyAI] CRITICAL VIP KILLED: {name}");
        }
    }
}
