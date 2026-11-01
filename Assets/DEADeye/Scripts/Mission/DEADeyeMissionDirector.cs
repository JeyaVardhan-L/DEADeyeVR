using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DEADeye
{
    /// <summary>
    /// Authoritative mission controller managing hostile waves, timer countdown,
    /// objective tracking, and mission completion/failure evaluation.
    /// </summary>
    public class DEADeyeMissionDirector : MonoBehaviour
    {
        public static DEADeyeMissionDirector Instance { get; private set; }

        [Header("Configuration")]
        [SerializeField] private DEADeyeMissionObjective missionConfig;
        [SerializeField] private float missionDuration = 120f;
        [SerializeField] private int maxEscapesAllowed = 3;
        [SerializeField] private int totalHostilesToSpawn = 8;
        [SerializeField] private float spawnInterval = 8.0f;

        [Header("Waypoints & Escape Corridors")]
        [SerializeField] private List<Transform> spawnPoints = new List<Transform>();
        [SerializeField] private List<Transform> waypoints = new List<Transform>();
        [SerializeField] private List<Transform> escapePoints = new List<Transform>();

        [Header("Runtime State")]
        [SerializeField] private float timeRemaining = 120f;
        [SerializeField] private bool isMissionActive = false;
        [SerializeField] private int hostilesSpawnedCount = 0;
        [SerializeField] private int hostilesEliminatedCount = 0;
        [SerializeField] private int hostilesEscapedCount = 0;

        public float TimeRemaining => timeRemaining;
        public float MissionDuration => missionDuration;
        public bool IsMissionActive => isMissionActive;
        public int HostilesRemaining => Mathf.Max(0, totalHostilesToSpawn - (hostilesEliminatedCount + hostilesEscapedCount));
        public int HostilesEliminated => hostilesEliminatedCount;
        public int HostilesEscaped => hostilesEscapedCount;
        public int MaxEscapesAllowed => maxEscapesAllowed;

        public event Action OnMissionStarted;
        public event Action<bool> OnMissionEnded; // true = victory, false = defeat
        public event Action<float> OnTimerTick;

        private Coroutine missionLoopCoroutine;
        private Coroutine spawnLoopCoroutine;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (missionConfig != null)
            {
                missionDuration = missionConfig.missionDurationSeconds;
                totalHostilesToSpawn = missionConfig.initialHostileTargetCount;
                maxEscapesAllowed = missionConfig.maxAllowedEscapes;
            }
            timeRemaining = missionDuration;
        }

        private void Start()
        {
            // Auto-start mission if game state is MissionActive
            if (DEADeyeGameManager.Instance != null && DEADeyeGameManager.Instance.CurrentState == DEADeyeGameState.MissionActive)
            {
                StartMission();
            }
        }

        public void SetNavigationNodes(List<Transform> spawns, List<Transform> wps, List<Transform> escapes)
        {
            spawnPoints = spawns;
            waypoints = wps;
            escapePoints = escapes;
        }

        public void SetSpawnPoints(List<Transform> spawns, List<Transform> wps, List<Transform> escapes)
        {
            SetNavigationNodes(spawns, wps, escapes);
        }

        public void StartMission()
        {
            if (isMissionActive) return;

            isMissionActive = true;
            timeRemaining = missionDuration;
            hostilesSpawnedCount = 0;
            hostilesEliminatedCount = 0;
            hostilesEscapedCount = 0;

            if (DEADeyeScoreManager.Instance != null)
            {
                DEADeyeScoreManager.Instance.ResetMetrics();
            }

            if (DEADeyeAudioManager.Instance != null)
            {
                DEADeyeAudioManager.Instance.PlayMissionStart();
                DEADeyeAudioManager.Instance.StartAmbientCity();
            }

            OnMissionStarted?.Invoke();
            missionLoopCoroutine = StartCoroutine(MissionTimerRoutine());
            spawnLoopCoroutine = StartCoroutine(HostileSpawnerRoutine());
        }

        private IEnumerator MissionTimerRoutine()
        {
            while (timeRemaining > 0f && isMissionActive)
            {
                yield return new WaitForSeconds(1.0f);
                timeRemaining = Mathf.Max(0f, timeRemaining - 1.0f);
                OnTimerTick?.Invoke(timeRemaining);
            }

            if (isMissionActive)
            {
                // Timer elapsed -> assess outcome
                bool victory = hostilesEscapedCount < maxEscapesAllowed;
                EndMission(victory);
            }
        }

        private IEnumerator HostileSpawnerRoutine()
        {
            while (isMissionActive && hostilesSpawnedCount < totalHostilesToSpawn)
            {
                SpawnNextHostile();
                yield return new WaitForSeconds(spawnInterval);
            }
        }

        public void SpawnNextHostile()
        {
            if (spawnPoints == null || spawnPoints.Count == 0) return;

            Transform spawnNode = spawnPoints[hostilesSpawnedCount % spawnPoints.Count];
            GameObject hostileGO = new GameObject($"Hostile_{hostilesSpawnedCount + 1}");
            hostileGO.transform.position = spawnNode.position;
            hostileGO.transform.rotation = spawnNode.rotation;

            DEADeyeHostileAI ai;
            // Alternate between Criminal and Monster
            if (hostilesSpawnedCount % 3 == 2)
            {
                ai = hostileGO.AddComponent<DEADeyeHostileMonster>();
            }
            else
            {
                ai = hostileGO.AddComponent<DEADeyeHostileCriminal>();
            }

            Transform escapeTarget = (escapePoints != null && escapePoints.Count > 0)
                ? escapePoints[UnityEngine.Random.Range(0, escapePoints.Count)]
                : null;

            ai.SetNavigationRoute(waypoints, escapeTarget);
            ai.OnEliminated += HandleHostileEliminated;
            ai.OnEscaped += HandleHostileEscaped;

            hostilesSpawnedCount++;
        }

        private void HandleHostileEliminated(DEADeyeHostileAI hostile)
        {
            hostilesEliminatedCount++;
            CheckMissionCompletion();
        }

        private void HandleHostileEscaped(DEADeyeHostileAI hostile)
        {
            hostilesEscapedCount++;
            if (hostilesEscapedCount >= maxEscapesAllowed)
            {
                // Too many hostiles escaped! Immediate failure
                EndMission(false);
            }
            else
            {
                CheckMissionCompletion();
            }
        }

        private void CheckMissionCompletion()
        {
            if (!isMissionActive) return;

            // All scheduled hostiles resolved
            if (hostilesEliminatedCount + hostilesEscapedCount >= totalHostilesToSpawn)
            {
                bool victory = hostilesEscapedCount < maxEscapesAllowed;
                EndMission(victory);
            }
        }

        public void EndMission(bool isVictory)
        {
            if (!isMissionActive) return;
            isMissionActive = false;

            if (missionLoopCoroutine != null) StopCoroutine(missionLoopCoroutine);
            if (spawnLoopCoroutine != null) StopCoroutine(spawnLoopCoroutine);

            if (DEADeyeAudioManager.Instance != null)
            {
                DEADeyeAudioManager.Instance.StopAmbientCity();
                if (isVictory) DEADeyeAudioManager.Instance.PlayMissionComplete();
                else DEADeyeAudioManager.Instance.PlayMissionFail();
            }

            if (DEADeyeGameManager.Instance != null)
            {
                DEADeyeGameManager.Instance.SetGameState(DEADeyeGameState.MissionResults);
            }

            OnMissionEnded?.Invoke(isVictory);
            Debug.Log($"[DEADeyeMissionDirector] Mission Finished! Result: {(isVictory ? "VICTORY" : "DEFEAT")} - Eliminated: {hostilesEliminatedCount}, Escaped: {hostilesEscapedCount}");
        }
    }
}
