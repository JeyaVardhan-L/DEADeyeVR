using UnityEngine;

namespace DEADeye
{
    public class HostileSpawnState : IEnemyState
    {
        public HostileStateType StateType => HostileStateType.Spawn;
        private float spawnTimer = 0.5f;

        public void Enter(DEADeyeHostileAI hostile)
        {
            if (hostile.Agent != null && hostile.Agent.isOnNavMesh)
            {
                hostile.Agent.isStopped = true;
            }
            spawnTimer = 0.5f;
        }

        public void UpdateState(DEADeyeHostileAI hostile)
        {
            spawnTimer -= Time.deltaTime;
            if (spawnTimer <= 0f)
            {
                hostile.ChangeState(HostileStateType.Patrol);
            }
        }

        public void Exit(DEADeyeHostileAI hostile)
        {
            if (hostile.Agent != null && hostile.Agent.isOnNavMesh)
            {
                hostile.Agent.isStopped = false;
            }
        }
    }

    public class HostilePatrolState : IEnemyState
    {
        public HostileStateType StateType => HostileStateType.Patrol;

        public void Enter(DEADeyeHostileAI hostile)
        {
            if (hostile.Agent != null && hostile.Agent.isOnNavMesh)
            {
                hostile.Agent.isStopped = false;
                hostile.Agent.speed = hostile.WalkSpeed;
                hostile.NavigateToNextWaypoint();
            }
        }

        public void UpdateState(DEADeyeHostileAI hostile)
        {
            if (hostile.Agent == null || !hostile.Agent.isOnNavMesh) return;

            // Check if arrived near waypoint
            if (!hostile.Agent.pathPending && hostile.Agent.remainingDistance <= hostile.Agent.stoppingDistance + 0.5f)
            {
                bool hasMore = hostile.NavigateToNextWaypoint();
                if (!hasMore)
                {
                    // Route completed -> attempt escape
                    hostile.ChangeState(HostileStateType.Escape);
                }
            }
        }

        public void Exit(DEADeyeHostileAI hostile) { }
    }

    public class HostileSuspiciousState : IEnemyState
    {
        public HostileStateType StateType => HostileStateType.Suspicious;
        private float pauseTimer = 2.0f;

        public void Enter(DEADeyeHostileAI hostile)
        {
            if (hostile.Agent != null && hostile.Agent.isOnNavMesh)
            {
                hostile.Agent.isStopped = true;
            }
            pauseTimer = 2.0f;
        }

        public void UpdateState(DEADeyeHostileAI hostile)
        {
            pauseTimer -= Time.deltaTime;
            if (pauseTimer <= 0f)
            {
                // After investigating, enemy resumes running toward escape
                hostile.ChangeState(HostileStateType.Escape);
            }
        }

        public void Exit(DEADeyeHostileAI hostile)
        {
            if (hostile.Agent != null && hostile.Agent.isOnNavMesh)
            {
                hostile.Agent.isStopped = false;
            }
        }
    }

    public class HostileAlertedState : IEnemyState
    {
        public HostileStateType StateType => HostileStateType.Alerted;

        public void Enter(DEADeyeHostileAI hostile)
        {
            if (hostile.Agent != null && hostile.Agent.isOnNavMesh)
            {
                hostile.Agent.isStopped = false;
                hostile.Agent.speed = hostile.AlertSpeed;
            }

            if (DEADeyeAudioManager.Instance != null)
            {
                DEADeyeAudioManager.Instance.PlayEnemyAlert(hostile.transform.position);
            }

            hostile.NavigateToEscapeZone();
        }

        public void UpdateState(DEADeyeHostileAI hostile)
        {
            if (hostile.Agent == null || !hostile.Agent.isOnNavMesh) return;

            if (!hostile.Agent.pathPending && hostile.Agent.remainingDistance <= hostile.Agent.stoppingDistance + 0.8f)
            {
                hostile.ChangeState(HostileStateType.Escape);
            }
        }

        public void Exit(DEADeyeHostileAI hostile) { }
    }

    public class HostileEscapeState : IEnemyState
    {
        public HostileStateType StateType => HostileStateType.Escape;

        public void Enter(DEADeyeHostileAI hostile)
        {
            if (hostile.Agent != null && hostile.Agent.isOnNavMesh)
            {
                hostile.Agent.isStopped = false;
                hostile.Agent.speed = hostile.EscapeSpeed;
                hostile.NavigateToEscapeZone();
            }
        }

        public void UpdateState(DEADeyeHostileAI hostile)
        {
            // Escape zone trigger handles actual escape event
        }

        public void Exit(DEADeyeHostileAI hostile) { }
    }

    public class HostileEliminatedState : IEnemyState
    {
        public HostileStateType StateType => HostileStateType.Eliminated;

        public void Enter(DEADeyeHostileAI hostile)
        {
            if (hostile.Agent != null && hostile.Agent.isOnNavMesh)
            {
                hostile.Agent.isStopped = true;
                hostile.Agent.enabled = false;
            }

            // Disable all colliders so deceased body cannot be shot again
            var colliders = hostile.GetComponentsInChildren<Collider>();
            foreach (var col in colliders)
            {
                col.enabled = false;
            }

            if (DEADeyeAudioManager.Instance != null)
            {
                DEADeyeAudioManager.Instance.PlayEnemyEliminated(hostile.transform.position);
            }
        }

        public void UpdateState(DEADeyeHostileAI hostile) { }
        public void Exit(DEADeyeHostileAI hostile) { }
    }
}

