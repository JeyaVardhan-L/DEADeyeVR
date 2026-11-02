using System;
using System.Collections.Generic;
using UnityEngine;

namespace DEADeye
{
    /// <summary>
    /// Central registry tracking all active hostile agents in the mission area.
    /// Decouples weapon aiming and HUD indicators from scene hierarchy lookups.
    /// </summary>
    public class DEADeyeEnemyRegistry : MonoBehaviour
    {
        public static DEADeyeEnemyRegistry Instance { get; private set; }

        private readonly List<DEADeyeHostileAI> activeEnemies = new List<DEADeyeHostileAI>();

        public IReadOnlyList<DEADeyeHostileAI> ActiveEnemies => activeEnemies;
        public int ActiveCount => activeEnemies.Count;

        public event Action<DEADeyeHostileAI> OnEnemyRegistered;
        public event Action<DEADeyeHostileAI> OnEnemyUnregistered;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void RegisterEnemy(DEADeyeHostileAI enemy)
        {
            if (enemy == null || activeEnemies.Contains(enemy)) return;
            activeEnemies.Add(enemy);
            OnEnemyRegistered?.Invoke(enemy);
        }

        public void UnregisterEnemy(DEADeyeHostileAI enemy)
        {
            if (enemy == null || !activeEnemies.Contains(enemy)) return;
            activeEnemies.Remove(enemy);
            OnEnemyUnregistered?.Invoke(enemy);
        }

        public void Clear()
        {
            activeEnemies.Clear();
        }
    }
}

