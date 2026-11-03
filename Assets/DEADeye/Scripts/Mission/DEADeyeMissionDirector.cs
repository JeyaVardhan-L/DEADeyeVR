using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DEADeye
{
    /// <summary>
    /// Authoritative mission controller managing hostile waves, timer countdown,
    /// dynamic tactical objectives, VIP protection, civilian presence, and mission completion.
    /// Supports the full 4-operation urban campaign.
    /// </summary>
    public class DEADeyeMissionDirector : MonoBehaviour
    {
        public static DEADeyeMissionDirector Instance { get; private set; }

        [Header("Active Mission Definition")]
        [SerializeField] private DEADeyeMissionDefinition activeMission;
        [SerializeField] private float missionDuration = 120f;
        [SerializeField] private int maxEscapesAllowed = 2;
        [SerializeField] private int totalHostilesToSpawn = 6;
        [SerializeField] private float spawnInterval = 6.5f;

        [Header("Character Prefabs")]
        [SerializeField] private GameObject[] hostilePrefabs;
        [SerializeField] private GameObject technicianPrefab;
        [SerializeField] private GameObject civilianPrefab;
        [SerializeField] private GameObject securityEscortPrefab;

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
        [SerializeField] private float commsRepairProgress = 0f;

        private List<GameObject> activeSpawnedEntities = new List<GameObject>();
        private List<DEADeyeFriendlyAI> activeTechnicians = new List<DEADeyeFriendlyAI>();
        private List<DEADeyeCivilianAI> activeCivilians = new List<DEADeyeCivilianAI>();

        public DEADeyeMissionDefinition ActiveMission => activeMission;
        public float TimeRemaining => timeRemaining;
        public float MissionDuration => missionDuration;
        public bool IsMissionActive => isMissionActive;
        public int HostilesRemaining => Mathf.Max(0, totalHostilesToSpawn - (hostilesEliminatedCount + hostilesEscapedCount));
        public int HostilesEliminated => hostilesEliminatedCount;
        public int HostilesEscaped => hostilesEscapedCount;
        public int MaxEscapesAllowed => maxEscapesAllowed;
        public float CommsRepairProgress => commsRepairProgress;

        public event Action OnMissionStarted;
        public event Action<bool> OnMissionEnded; // true = victory, false = defeat
        public event Action<float> OnTimerTick;
        public event Action<float> OnCommsProgressUpdated;

        private Coroutine missionLoopCoroutine;
        private Coroutine spawnLoopCoroutine;

        private void Awake()
        {
            Instance = this;
            LoadPrefabsIfMissing();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void Start()
        {
            CollectNavigationNodes();

            if (DEADeyeGameManager.Instance != null)
            {
                DEADeyeGameManager.Instance.SetGameState(DEADeyeGameState.MissionActive);
            }

            // If an active mission is assigned, launch it.
            // Otherwise, load default Mission 01: BROKEN SIGNAL.
            if (activeMission != null)
            {
                LaunchMission(activeMission);
            }
            else
            {
                var defaultMission = Resources.Load<DEADeyeMissionDefinition>("Missions/Mission_01_BrokenSignal");
#if UNITY_EDITOR
                if (defaultMission == null)
                {
                    defaultMission = UnityEditor.AssetDatabase.LoadAssetAtPath<DEADeyeMissionDefinition>("Assets/DEADeye/Data/Missions/Mission_01_BrokenSignal.asset");
                }
#endif
                if (defaultMission != null)
                {
                    LaunchMission(defaultMission);
                }
                else
                {
                    StartMission();
                }
            }
        }

        public void LoadPrefabsIfMissing()
        {
            if (hostilePrefabs == null || hostilePrefabs.Length == 0)
            {
#if UNITY_EDITOR
                var h1 = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/DEADeye/Prefabs/Characters/Hostile_Tactical_Rifleman.prefab");
                var h2 = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/DEADeye/Prefabs/Characters/Hostile_Sniper_Scout.prefab");
                var h3 = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/DEADeye/Prefabs/Characters/Hostile_Commander.prefab");
                List<GameObject> list = new List<GameObject>();
                if (h1 != null) list.Add(h1);
                if (h2 != null) list.Add(h2);
                if (h3 != null) list.Add(h3);
                if (list.Count > 0) hostilePrefabs = list.ToArray();

                if (technicianPrefab == null)
                    technicianPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/DEADeye/Prefabs/Characters/Friendly_Technician.prefab");
                if (securityEscortPrefab == null)
                    securityEscortPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/DEADeye/Prefabs/Characters/Friendly_Security_Escort.prefab");
                if (civilianPrefab == null)
                    civilianPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/DEADeye/Prefabs/Characters/Civilian_Male_Casual.prefab");
#endif
            }
        }

        public void CollectNavigationNodes()
        {
            if (spawnPoints == null || spawnPoints.Count == 0)
            {
                var cityBuilder = FindFirstObjectByType<DEADeyeUrbanCityBuilder>();
                if (cityBuilder != null)
                {
                    SetSpawnPoints(cityBuilder.SpawnPoints, cityBuilder.Waypoints, cityBuilder.EscapePoints);
                }
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

        public void SetHostilePrefabs(GameObject[] prefabs)
        {
            hostilePrefabs = prefabs;
        }

        public void LaunchMission(DEADeyeMissionDefinition mission)
        {
            activeMission = mission;
            if (mission != null)
            {
                missionDuration = mission.timeLimitSeconds;
                totalHostilesToSpawn = mission.hostileCount;
                maxEscapesAllowed = mission.maxEscapesAllowed;
                spawnInterval = mission.hostileSpawnInterval;
            }

            StartMission();
        }

        public void StartMission()
        {
            if (missionLoopCoroutine != null) StopCoroutine(missionLoopCoroutine);
            if (spawnLoopCoroutine != null) StopCoroutine(spawnLoopCoroutine);

            // Clean up previous mission entities
            ClearActiveEntities();

            isMissionActive = true;
            timeRemaining = missionDuration;
            hostilesSpawnedCount = 0;
            hostilesEliminatedCount = 0;
            hostilesEscapedCount = 0;
            commsRepairProgress = 0f;

            if (DEADeyeScoreManager.Instance != null)
            {
                DEADeyeScoreManager.Instance.ResetMetrics();
            }

            if (DEADeyeWeaponManager.Instance != null)
            {
                DEADeyeWeaponManager.Instance.ResetAmmo();
            }

            if (DEADeyeAudioManager.Instance != null)
            {
                DEADeyeAudioManager.Instance.PlayMissionStart();
                DEADeyeAudioManager.Instance.StartAmbientCity();
            }

            CollectNavigationNodes();

            // Spawn Mission-specific assets
            SpawnMissionSpecificEntities();

            // Spawn initial hostile wave immediately
            int initialWave = Mathf.Min(3, totalHostilesToSpawn);
            for (int i = 0; i < initialWave; i++)
            {
                SpawnNextHostile();
            }

            OnMissionStarted?.Invoke();
            missionLoopCoroutine = StartCoroutine(MissionTimerRoutine());
            spawnLoopCoroutine = StartCoroutine(HostileSpawnerRoutine());

            Debug.Log($"[DEADeyeMissionDirector] Mission '{ (activeMission != null ? activeMission.operationTitle : "OPERATION") }' started! Duration: {missionDuration}s, Hostiles: {totalHostilesToSpawn}");
        }

        private void ClearActiveEntities()
        {
            foreach (var go in activeSpawnedEntities)
            {
                if (go != null) Destroy(go);
            }
            activeSpawnedEntities.Clear();
            activeTechnicians.Clear();
            activeCivilians.Clear();
        }

        private void SpawnMissionSpecificEntities()
        {
            // 1. Mission 01: BROKEN SIGNAL -> Spawn 2 Friendly Comms Technicians at the Comms Tower in East Plaza
            if (activeMission == null || activeMission.hasTechnicians)
            {
                Vector3 tech1Pos = new Vector3(21.5f, 0.15f, 37.5f);
                Vector3 tech2Pos = new Vector3(23.2f, 0.15f, 38.5f);

                SpawnTechnician(tech1Pos, "Lead Technician Marcus");
                SpawnTechnician(tech2Pos, "Systems Tech Elena");
            }

            // 2. Mission 03: NO CLEAR SHOT -> Spawn Civilians walking sidewalks
            if (activeMission != null && activeMission.hasCivilians)
            {
                Vector3[] civSpawns = new Vector3[]
                {
                    new Vector3(-8.5f, 0.15f, 22f),
                    new Vector3(-8.5f, 0.15f, 35f),
                    new Vector3(-8.5f, 0.15f, 52f),
                    new Vector3(-20f, 0.15f, 38f)
                };

                for (int i = 0; i < civSpawns.Length; i++)
                {
                    SpawnCivilian(civSpawns[i], $"Civilian_{i + 1}");
                }
            }
        }

        private void SpawnTechnician(Vector3 pos, string techName)
        {
            GameObject go = null;
            if (technicianPrefab != null)
            {
                go = Instantiate(technicianPrefab, pos, Quaternion.Euler(0f, -90f, 0f));
            }
            else
            {
                go = new GameObject(techName);
                go.transform.position = pos;
                go.AddComponent<DEADeyeFriendlyAI>();
            }

            go.name = techName;
            activeSpawnedEntities.Add(go);

            var friendlyAI = go.GetComponent<DEADeyeFriendlyAI>();
            if (friendlyAI != null)
            {
                activeTechnicians.Add(friendlyAI);
                friendlyAI.SetClassification(techName, 100);
                friendlyAI.SetPerformsWork(true, 2.5f); // ~40s total work
                friendlyAI.OnProgressUpdated += HandleCommsProgress;
                friendlyAI.OnVipKilled += HandleTechnicianKilled;
            }
        }

        private void SpawnCivilian(Vector3 pos, string civName)
        {
            GameObject go = null;
            if (civilianPrefab != null)
            {
                go = Instantiate(civilianPrefab, pos, Quaternion.identity);
            }
            else
            {
                go = new GameObject(civName);
                go.transform.position = pos;
                go.AddComponent<DEADeyeCivilianAI>();
            }

            go.name = civName;
            activeSpawnedEntities.Add(go);

            var civAI = go.GetComponent<DEADeyeCivilianAI>();
            if (civAI != null)
            {
                activeCivilians.Add(civAI);
                civAI.SetPatrolRoute(waypoints);
            }
        }

        private void HandleCommsProgress(float progress)
        {
            commsRepairProgress = progress;
            OnCommsProgressUpdated?.Invoke(progress);
        }

        private void HandleTechnicianKilled(DEADeyeFriendlyAI tech)
        {
            Debug.LogError($"[DEADeyeMissionDirector] MISSION FAILED: Comms Technician {tech.name} was eliminated!");
            EndMission(false);
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
                // Timer elapsed -> check if mission objectives were met
                bool victory = hostilesEscapedCount < maxEscapesAllowed && hostilesEliminatedCount >= (totalHostilesToSpawn / 2);
                EndMission(victory);
            }
        }

        private IEnumerator HostileSpawnerRoutine()
        {
            while (isMissionActive && hostilesSpawnedCount < totalHostilesToSpawn)
            {
                yield return new WaitForSeconds(spawnInterval);
                if (isMissionActive)
                {
                    SpawnNextHostile();
                }
            }
        }

        public void SpawnNextHostile()
        {
            CollectNavigationNodes();

            if (spawnPoints == null || spawnPoints.Count == 0)
            {
                Vector3 fallbackPos = new Vector3(0f, 0.1f, 20f + (hostilesSpawnedCount * 8f));
                GameObject fallbackNode = new GameObject($"FallbackSpawn_{hostilesSpawnedCount + 1}");
                fallbackNode.transform.position = fallbackPos;
                spawnPoints = new List<Transform> { fallbackNode.transform };
            }

            Transform spawnNode = spawnPoints[hostilesSpawnedCount % spawnPoints.Count];
            Vector3 spawnPos = spawnNode.position;
            if (UnityEngine.AI.NavMesh.SamplePosition(spawnPos, out UnityEngine.AI.NavMeshHit navHit, 6f, UnityEngine.AI.NavMesh.AllAreas))
            {
                spawnPos = navHit.position;
            }

            GameObject hostileGO = null;
            DEADeyeHostileAI ai = null;

            // Spawn from configured clothed Humanoid Prefabs (Rifleman, Scout, Commander)
            if (hostilePrefabs != null && hostilePrefabs.Length > 0)
            {
                int prefabIndex = hostilesSpawnedCount % hostilePrefabs.Length;
                GameObject prefab = hostilePrefabs[prefabIndex];
                if (prefab != null)
                {
                    hostileGO = Instantiate(prefab, spawnPos, spawnNode.rotation);
                    hostileGO.name = $"Hostile_{hostilesSpawnedCount + 1}_{prefab.name}";
                    ai = hostileGO.GetComponent<DEADeyeHostileAI>();
                }
            }

            // Fallback instantiation if no prefabs bound
            if (hostileGO == null)
            {
                hostileGO = new GameObject($"Hostile_{hostilesSpawnedCount + 1}");
                hostileGO.transform.position = spawnPos;
                hostileGO.transform.rotation = spawnNode.rotation;

                DEADeyeHostileHumanoid humanoidAI = hostileGO.AddComponent<DEADeyeHostileHumanoid>();
                HumanoidVariant variant = (HumanoidVariant)(hostilesSpawnedCount % 3);
                humanoidAI.InitializeVariant(variant);
                ai = humanoidAI;
            }

            activeSpawnedEntities.Add(hostileGO);

            if (ai.Agent != null && ai.Agent.isOnNavMesh)
            {
                ai.Agent.Warp(spawnPos);
            }

            Transform escapeTarget = (escapePoints != null && escapePoints.Count > 0)
                ? escapePoints[UnityEngine.Random.Range(0, escapePoints.Count)]
                : null;

            ai.SetNavigationRoute(waypoints, escapeTarget);
            ai.OnEliminated += HandleHostileEliminated;
            ai.OnEscaped += HandleHostileEscaped;

            hostilesSpawnedCount++;
            Debug.Log($"[DEADeyeMissionDirector] Spawned hostile {hostileGO.name} at {spawnPos}");
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

            // Retrieve score metrics
            int score = DEADeyeScoreManager.Instance != null ? DEADeyeScoreManager.Instance.CurrentScore : 0;
            int headshots = DEADeyeScoreManager.Instance != null ? DEADeyeScoreManager.Instance.Headshots : 0;
            float accuracy = DEADeyeScoreManager.Instance != null ? DEADeyeScoreManager.Instance.AccuracyPercentage : 0f;
            int civCasualties = DEADeyeScoreManager.Instance != null ? DEADeyeScoreManager.Instance.CivilianCasualties : 0;

            // Show debrief on tactical terminal
            if (DEADeyeTacticalTerminalUI.Instance != null)
            {
                DEADeyeTacticalTerminalUI.Instance.ShowDebrief(
                    isVictory,
                    score,
                    headshots,
                    accuracy,
                    hostilesEliminatedCount,
                    hostilesEscapedCount,
                    civCasualties
                );
            }

            OnMissionEnded?.Invoke(isVictory);
            Debug.Log($"[DEADeyeMissionDirector] Mission Finished! Result: {(isVictory ? "VICTORY" : "DEFEAT")} - Eliminated: {hostilesEliminatedCount}, Escaped: {hostilesEscapedCount}, Score: {score}");
        }
    }
}
