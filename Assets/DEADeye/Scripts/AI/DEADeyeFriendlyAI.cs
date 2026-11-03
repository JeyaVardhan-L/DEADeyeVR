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
        Working,      // E.g., repairing communications antenna
        UnderFire,    // Taking cover while working
        Eliminated
    }

    /// <summary>
    /// AI controller for friendly and VIP personnel (e.g., Comms Technicians, Security Escorts).
    /// Tracks objective work progress (0-100%), takes damage, and triggers objective failure if lost.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public class DEADeyeFriendlyAI : MonoBehaviour
    {
        [Header("Classification")]
        [SerializeField] private string classificationName = "FRIENDLY VIP";
        [SerializeField] private int maxHealth = 100;
        [SerializeField] private int currentHealth = 100;

        [Header("Objective Work (e.g. Comms Repair)")]
        [SerializeField] private bool performsObjectiveWork = true;
        [SerializeField] private float workProgress = 0f; // 0 to 100%
        [SerializeField] private float repairSpeedPerSecond = 2.5f; // ~40s to repair

        [Header("State")]
        [SerializeField] private FriendlyState currentState = FriendlyState.Working;

        private NavMeshAgent agent;
        private bool isEliminated = false;

        public string ClassificationName => classificationName;
        public int CurrentHealth => currentHealth;
        public int MaxHealth => maxHealth;
        public bool IsEliminated => isEliminated;
        public float WorkProgress => workProgress;
        public FriendlyState CurrentState => currentState;

        public event Action<float> OnProgressUpdated; // 0..100
        public event Action<DEADeyeFriendlyAI> OnVipDamaged;
        public event Action<DEADeyeFriendlyAI> OnVipKilled;

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            currentHealth = maxHealth;
        }

        private void Update()
        {
            if (isEliminated) return;

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
        }

        public void SetPerformsWork(bool performs, float speed = 2.5f)
        {
            performsObjectiveWork = performs;
            repairSpeedPerSecond = speed;
        }

        public void TakeDamage(int damage, Vector3 hitPoint, Vector3 hitNormal, bool isHeadshot)
        {
            if (isEliminated) return;

            currentHealth -= damage;
            currentState = FriendlyState.UnderFire;
            OnVipDamaged?.Invoke(this);

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
