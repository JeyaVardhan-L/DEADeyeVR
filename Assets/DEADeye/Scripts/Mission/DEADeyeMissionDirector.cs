using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DEADeye
{
    public enum OperationPhase
    {
        Reconnaissance,     // Phase 1: Reticle dwell spotting jammer, Elena, Marcus, Commander
        Disruption,         // Phase 2: Disable jammer via power junction, direct shot, or distraction
        TechnicianAdvance,  // Phase 3: Elena & Marcus move to console, repair 0-100%, take cover under fire
        NeutralizeCommander,// Phase 4: Commander Vance arrives with security detail, heading for exfil
        ConvoyExtraction,   // Phase 5: Clear final ambush at Transport Interchange, convoy extracts team
        Debriefing          // Debriefing screen with S/A/B/C/F grade
    }

    /// <summary>
    /// Authoritative mission controller managing hostile waves, timer countdown,
    /// dynamic tactical objectives, VIP protection, civilian presence, and mission completion.
    /// Drives the 5 sequential phases of OPERATION DEAD AIR (The Blackout Protocol).
    /// </summary>
    public class DEADeyeMissionDirector : MonoBehaviour
    {
        public static DEADeyeMissionDirector Instance { get; private set; }

        [Header("Active Mission Definition")]
        [SerializeField] private DEADeyeMissionDefinition activeMission;
        [SerializeField] private float missionDuration = 150f;
        [SerializeField] private int maxEscapesAllowed = 2;
        [SerializeField] private int totalHostilesToSpawn = 7;
        [SerializeField] private float spawnInterval = 6.0f;

        [Header("Operation DEAD AIR State")]
        [SerializeField] private OperationPhase currentPhase = OperationPhase.Reconnaissance;
        [SerializeField] private bool disabledViaPowerJunction = false;
        [SerializeField] private bool alarmSilenced = false;
        [SerializeField] private bool securityGateOpened = false;
        [SerializeField] private bool isCommanderSpawned = false;
        [SerializeField] private bool isCommanderEliminated = false;
        [SerializeField] private int spottedKeyTargetCount = 0;

        [Header("Character Prefabs")]
        [SerializeField] private GameObject[] hostilePrefabs;
        [SerializeField] private GameObject commanderPrefab;
        [SerializeField] private GameObject technicianPrefab;
        [SerializeField] private GameObject civilianPrefab;
        [SerializeField] private GameObject securityEscortPrefab;

        [Header("Waypoints & Escape Corridors")]
        [SerializeField] private List<Transform> spawnPoints = new List<Transform>();
        [SerializeField] private List<Transform> waypoints = new List<Transform>();
        [SerializeField] private List<Transform> escapePoints = new List<Transform>();

        [Header("Runtime State")]
        [SerializeField] private float timeRemaining = 150f;
        [SerializeField] private bool isMissionActive = false;
        [SerializeField] private int hostilesSpawnedCount = 0;
        [SerializeField] private int hostilesEliminatedCount = 0;
        [SerializeField] private int hostilesEscapedCount = 0;
        [SerializeField] private float commsRepairProgress = 0f;

        private List<GameObject> activeSpawnedEntities = new List<GameObject>();
        private List<DEADeyeFriendlyAI> activeTechnicians = new List<DEADeyeFriendlyAI>();
        private List<DEADeyeCivilianAI> activeCivilians = new List<DEADeyeCivilianAI>();
        private DEADeyeFriendlyAI techMarcus;
        private DEADeyeFriendlyAI techElena;
        private DEADeyeHostileAI commanderVance;

        public DEADeyeMissionDefinition ActiveMission => activeMission;
        public OperationPhase CurrentPhase => currentPhase;
        public float TimeRemaining => timeRemaining;
        public float MissionDuration => missionDuration;
        public bool IsMissionActive => isMissionActive;
        public int HostilesRemaining => Mathf.Max(0, totalHostilesToSpawn - (hostilesEliminatedCount + hostilesEscapedCount));
        public int HostilesEliminated => hostilesEliminatedCount;
        public int HostilesEscaped => hostilesEscapedCount;
        public int MaxEscapesAllowed => maxEscapesAllowed;
        public float CommsRepairProgress => commsRepairProgress;
        public bool DisabledViaPowerJunction => disabledViaPowerJunction;
        public bool AlarmSilenced => alarmSilenced;
        public bool SecurityGateOpened => securityGateOpened;

        public event Action OnMissionStarted;
        public event Action<bool> OnMissionEnded; // true = victory, false = defeat
        public event Action<float> OnTimerTick;
        public event Action<float> OnCommsProgressUpdated;
        public event Action<OperationPhase> OnPhaseChanged;

        private Coroutine missionLoopCoroutine;
        private Coroutine phaseCheckCoroutine;

        private void Awake()
        {
            Instance = this;
            LoadPrefabsIfMissing();

            if (DEADeyeSpottingSystem.Instance == null && FindFirstObjectByType<DEADeyeSpottingSystem>() == null)
            {
                GameObject spotSysGO = new GameObject("DEADeyeSpottingSystem");
                spotSysGO.AddComponent<DEADeyeSpottingSystem>();
            }
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

            if (activeMission == null)
            {
                activeMission = Resources.Load<DEADeyeMissionDefinition>("Missions/Mission_01_BrokenSignal");
#if UNITY_EDITOR
                if (activeMission == null)
                {
                    activeMission = UnityEditor.AssetDatabase.LoadAssetAtPath<DEADeyeMissionDefinition>("Assets/DEADeye/Data/Missions/Mission_01_BrokenSignal.asset");
                }
#endif
            }

            bool terminalOpen = DEADeyeTacticalTerminalUI.Instance != null && DEADeyeTacticalTerminalUI.Instance.IsTerminalActive;
            if (terminalOpen)
            {
                if (DEADeyeGameManager.Instance != null)
                {
                    DEADeyeGameManager.Instance.SetGameState(DEADeyeGameState.MissionBriefing);
                }
                // Do not auto-launch in background! Wait until player clicks DEPLOY INTO MISSION
            }
            else
            {
                if (DEADeyeGameManager.Instance != null)
                {
                    DEADeyeGameManager.Instance.SetGameState(DEADeyeGameState.MissionActive);
                }

                if (activeMission != null)
                {
                    LaunchMission(activeMission);
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
                if (list.Count > 0) hostilePrefabs = list.ToArray();
                if (h3 != null) commanderPrefab = h3;

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
            if (phaseCheckCoroutine != null) StopCoroutine(phaseCheckCoroutine);

            ClearActiveEntities();

            isMissionActive = true;
            timeRemaining = missionDuration;
            hostilesSpawnedCount = 0;
            hostilesEliminatedCount = 0;
            hostilesEscapedCount = 0;
            commsRepairProgress = 0f;
            disabledViaPowerJunction = false;
            alarmSilenced = false;
            securityGateOpened = false;
            isCommanderSpawned = false;
            isCommanderEliminated = false;
            spottedKeyTargetCount = 0;
            currentPhase = OperationPhase.Reconnaissance;

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

            // Spawn initial hostile patrol in Commercial Plaza
            SpawnNextHostile(new Vector3(18f, 0.15f, 32f));
            SpawnNextHostile(new Vector3(25f, 0.15f, 42f));

            // Hook Spotting System for reconnaissance phase
            if (DEADeyeSpottingSystem.Instance != null)
            {
                DEADeyeSpottingSystem.Instance.OnTargetSpotted += HandleTargetSpotted;
            }

            OnMissionStarted?.Invoke();
            missionLoopCoroutine = StartCoroutine(MissionTimerRoutine());
            phaseCheckCoroutine = StartCoroutine(PhaseManagementRoutine());

            // Initial Phase 1 Briefing
            StartCoroutine(InitialRadioBriefingRoutine());

            Debug.Log($"[DEADeyeMissionDirector] OPERATION DEAD AIR started! Duration: {missionDuration}s, Hostiles: {totalHostilesToSpawn}");
        }

        private IEnumerator InitialRadioBriefingRoutine()
        {
            yield return new WaitForSeconds(1.0f);

            if (DEADeyeMissionHUD.Instance != null)
            {
                DEADeyeMissionHUD.Instance.UpdateObjectiveTracker(
                    1, 6,
                    "RECON & SPOT",
                    "Dwell optical scope reticle on Elena, Marcus, and the broadcast tower to confirm tactical status.",
                    $"TARGETS SPOTTED: {spottedKeyTargetCount} / 3"
                );
                DEADeyeMissionHUD.Instance.ShowRadioDialogue(
                    "CONTROL",
                    "Overwatch, this is Control. Sector is dark. Hostiles have deployed a military jamming array in Central Plaza. Technicians Elena and Marcus are pinned near the fountain. Spot the jammer and team before engaging."
                );
            }
        }

        private void ClearActiveEntities()
        {
            if (DEADeyeSpottingSystem.Instance != null)
            {
                DEADeyeSpottingSystem.Instance.OnTargetSpotted -= HandleTargetSpotted;
            }

            foreach (var go in activeSpawnedEntities)
            {
                if (go != null) Destroy(go);
            }
            activeSpawnedEntities.Clear();
            activeTechnicians.Clear();
            activeCivilians.Clear();
            techMarcus = null;
            techElena = null;
            commanderVance = null;
        }

        private void SpawnMissionSpecificEntities()
        {
            // Spawn 2 Friendly Comms Technicians at initial fountain cover in Central Plaza
            Vector3 tech1Pos = new Vector3(12.5f, 0.15f, 26f);
            Vector3 tech2Pos = new Vector3(14.0f, 0.15f, 25.5f);

            techMarcus = SpawnTechnician(tech1Pos, "Lead Technician Marcus");
            techElena = SpawnTechnician(tech2Pos, "Systems Tech Elena");

            // Civilians in residential district if configured
            if (activeMission != null && activeMission.hasCivilians)
            {
                Vector3[] civSpawns = new Vector3[]
                {
                    new Vector3(-8.5f, 0.15f, 22f),
                    new Vector3(-8.5f, 0.15f, 35f),
                    new Vector3(-20f, 0.15f, 38f)
                };

                for (int i = 0; i < civSpawns.Length; i++)
                {
                    SpawnCivilian(civSpawns[i], $"Civilian_{i + 1}");
                }
            }
        }

        private DEADeyeFriendlyAI SpawnTechnician(Vector3 pos, string techName)
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
                friendlyAI.SetPerformsWork(false, 3.5f);
                friendlyAI.OnProgressUpdated += HandleCommsProgress;
                friendlyAI.OnVipKilled += HandleTechnicianKilled;
            }

            return friendlyAI;
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

        public void HandleTargetSpotted(DEADeyeSpotable target)
        {
            if (target == null) return;
            spottedKeyTargetCount++;

            if (DEADeyeMissionHUD.Instance != null)
            {
                DEADeyeMissionHUD.Instance.NotifyTargetSpotted();
                if (currentPhase == OperationPhase.Reconnaissance)
                {
                    DEADeyeMissionHUD.Instance.UpdateObjectiveProgress($"TARGETS SPOTTED: {spottedKeyTargetCount} / 3");
                }
            }

            // If in Reconnaissance phase and key assets spotted, advise player to proceed to Disruption
            if (currentPhase == OperationPhase.Reconnaissance)
            {
                if (target.Classification == SpotClassification.Objective || target.Classification == SpotClassification.Environmental || spottedKeyTargetCount >= 2)
                {
                    TransitionToPhase(OperationPhase.Disruption);
                }
            }
        }

        public void TransitionToPhase(OperationPhase newPhase)
        {
            if (currentPhase == newPhase) return;
            currentPhase = newPhase;
            OnPhaseChanged?.Invoke(newPhase);

            Debug.Log($"[DEADeyeMissionDirector] Transitioning to {newPhase}");

            switch (newPhase)
            {
                case OperationPhase.Disruption:
                    if (DEADeyeMissionHUD.Instance != null)
                    {
                        DEADeyeMissionHUD.Instance.UpdateObjectiveTracker(
                            2, 6,
                            "DISRUPT COMMUNICATIONS",
                            "Shoot high-voltage power junction on utility pole to cut power to syndicate jammer.",
                            "JAMMER STATUS: ACTIVE [TARGET POWER JUNCTION]"
                        );
                        DEADeyeMissionHUD.Instance.ShowRadioDialogue(
                            "CONTROL",
                            "Overwatch: Disrupt the jammer. Snipe the power junction on the tower, hit the jammer dish directly, or trigger the transformer distraction."
                        );
                    }
                    break;

                case OperationPhase.TechnicianAdvance:
                    if (DEADeyeMissionHUD.Instance != null)
                    {
                        DEADeyeMissionHUD.Instance.UpdateObjectiveTracker(
                            3, 6,
                            "PROVIDE OVERWATCH",
                            "Protect Elena and Marcus from hostile assault waves while they repair the broadcast array.",
                            $"REPAIR PROGRESS: {commsRepairProgress:F0}% | HOSTILES: {HostilesRemaining}"
                        );
                    }

                    // Command technicians to advance across plaza to the communications console
                    Vector3 marcusConsole = new Vector3(21.0f, 0.15f, 37.0f);
                    Vector3 elenaConsole = new Vector3(22.5f, 0.15f, 38.0f);

                    if (techMarcus != null)
                    {
                        techMarcus.SetPerformsWork(true, 3.5f);
                        techMarcus.MoveToDestination(marcusConsole);
                    }
                    if (techElena != null)
                    {
                        techElena.SetPerformsWork(true, 3.5f);
                        techElena.MoveToDestination(elenaConsole);
                    }

                    // Spawn hostile reinforcement patrol moving towards plaza
                    SpawnNextHostile(new Vector3(0f, 0.15f, 50f));
                    SpawnNextHostile(new Vector3(-12f, 0.15f, 42f));
                    break;

                case OperationPhase.NeutralizeCommander:
                    if (DEADeyeMissionHUD.Instance != null)
                    {
                        DEADeyeMissionHUD.Instance.UpdateObjectiveTracker(
                            4, 6,
                            "NEUTRALIZE COMMANDER VANCE",
                            "Identify and eliminate Commander Vance before his armored security detail reaches exfil.",
                            "COMMANDER STATUS: ADVANCING TO EXFIL"
                        );
                        DEADeyeMissionHUD.Instance.ShowRadioDialogue(
                            "CONTROL",
                            "Commander Vance has deployed to Sector 3 with an armed exfil escort! He's heading towards the transport interchange. Do not let him escape!"
                        );
                    }

                    SpawnCommanderVance();
                    break;

                case OperationPhase.ConvoyExtraction:
                    if (DEADeyeMissionHUD.Instance != null)
                    {
                        DEADeyeMissionHUD.Instance.UpdateObjectiveTracker(
                            5, 6,
                            "ESCORT EVACUATION",
                            "Cover the technicians' withdrawal across the boulevard to the extraction vehicle.",
                            "EXTRACTION CONVOY: STAGING AT BUS TERMINAL"
                        );
                        DEADeyeMissionHUD.Instance.ShowRadioDialogue(
                            "CONTROL",
                            "Hostile command neutralized! Extraction convoy is staging at the Transport Interchange bus terminal. Cover Elena and Marcus as they exfil!"
                        );
                    }

                    // Technicians move north towards Transport Interchange extraction zone
                    Vector3 evacZone = new Vector3(3f, 0.15f, 75f);
                    if (techMarcus != null)
                    {
                        techMarcus.SetPerformsWork(false);
                        techMarcus.MoveToDestination(evacZone);
                    }
                    if (techElena != null)
                    {
                        techElena.SetPerformsWork(false);
                        techElena.MoveToDestination(evacZone + new Vector3(1.5f, 0f, 0f));
                    }

                    // Spawn final ambush squad at transport interchange
                    SpawnNextHostile(new Vector3(8f, 0.15f, 82f));
                    SpawnNextHostile(new Vector3(-6f, 0.15f, 85f));
                    break;

                case OperationPhase.Debriefing:
                    EndMission(true);
                    break;
            }
        }

        private IEnumerator PhaseManagementRoutine()
        {
            // Auto advance Reconnaissance after 14 seconds if player hasn't spotted yet
            if (currentPhase == OperationPhase.Reconnaissance)
            {
                float timer = 14f;
                while (timer > 0f && currentPhase == OperationPhase.Reconnaissance)
                {
                    yield return new WaitForSeconds(1.0f);
                    timer -= 1.0f;
                }
                if (currentPhase == OperationPhase.Reconnaissance)
                {
                    TransitionToPhase(OperationPhase.Disruption);
                }
            }

            while (isMissionActive)
            {
                yield return new WaitForSeconds(1.0f);

                // Check Phase 3 -> Phase 4 transition (repair completed)
                if (currentPhase == OperationPhase.TechnicianAdvance && commsRepairProgress >= 100f)
                {
                    TransitionToPhase(OperationPhase.NeutralizeCommander);
                }

                // Check Phase 4 -> Phase 5 transition (commander eliminated)
                if (currentPhase == OperationPhase.NeutralizeCommander && isCommanderEliminated)
                {
                    TransitionToPhase(OperationPhase.ConvoyExtraction);
                }

                // Check Phase 5 -> Debriefing (technicians reached extraction and ambush hostiles eliminated)
                if (currentPhase == OperationPhase.ConvoyExtraction)
                {
                    bool techClose = (techMarcus == null || Vector3.Distance(techMarcus.transform.position, new Vector3(3f, 0.15f, 75f)) < 6f);
                    if (techClose && hostilesEliminatedCount >= (totalHostilesToSpawn - 1))
                    {
                        TransitionToPhase(OperationPhase.Debriefing);
                    }
                }
            }
        }

        public void HandlePowerJunctionDisabled()
        {
            disabledViaPowerJunction = true;
            Debug.Log("[DEADeyeMissionDirector] Power Junction disabled! Silent disruption achieved (+Secondary Objective).");

            // Disable Jammer visuals/rotation in plaza
            DisableJammerArray();

            if (currentPhase == OperationPhase.Reconnaissance || currentPhase == OperationPhase.Disruption)
            {
                TransitionToPhase(OperationPhase.TechnicianAdvance);
            }
        }

        public void HandleJammerDishHit()
        {
            Debug.Log("[DEADeyeMissionDirector] Jammer dish shot directly! Hostiles alerted.");

            DisableJammerArray();

            // Alert nearby hostiles
            AlertHostilesInSector(new Vector3(22.5f, 0f, 40f), 45f);

            if (DEADeyeMissionHUD.Instance != null)
            {
                DEADeyeMissionHUD.Instance.ShowRadioDialogue(
                    "CONTROL",
                    "Jammer array destroyed! Loud impact has alerted the perimeter guards! Elena, Marcus, move under sniper cover!"
                );
            }

            if (currentPhase == OperationPhase.Reconnaissance || currentPhase == OperationPhase.Disruption)
            {
                TransitionToPhase(OperationPhase.TechnicianAdvance);
            }
        }

        public void HandleAlarmSilenced()
        {
            alarmSilenced = true;
            Debug.Log("[DEADeyeMissionDirector] District Emergency Alarm silenced.");
        }

        public void HandleSecurityGateOpened()
        {
            securityGateOpened = true;
            Debug.Log("[DEADeyeMissionDirector] Security alley gate opened.");
        }

        private void DisableJammerArray()
        {
            var jammerObj = GameObject.Find("CommunicationsJammer");
            if (jammerObj != null)
            {
                var dish = jammerObj.transform.Find("JammerDish");
                if (dish != null)
                {
                    dish.localRotation = Quaternion.Euler(0f, 0f, 85f); // Dish droops down powered off
                }
            }
        }

        private void AlertHostilesInSector(Vector3 pos, float radius)
        {
            DEADeyeHostileAI[] allHostiles = FindObjectsByType<DEADeyeHostileAI>(FindObjectsSortMode.None);
            foreach (var h in allHostiles)
            {
                if (h != null && !h.IsEliminated)
                {
                    if (Vector3.Distance(h.transform.position, pos) <= radius)
                    {
                        h.ReactToGunfire(pos);
                    }
                }
            }
        }

        private void SpawnCommanderVance()
        {
            if (isCommanderSpawned) return;
            isCommanderSpawned = true;

            Vector3 spawnPos = new Vector3(-6f, 0.15f, 48f);
            GameObject cmdGO = null;

            if (commanderPrefab != null)
            {
                cmdGO = Instantiate(commanderPrefab, spawnPos, Quaternion.Euler(0f, 0f, 0f));
            }
            else if (hostilePrefabs != null && hostilePrefabs.Length > 0)
            {
                cmdGO = Instantiate(hostilePrefabs[0], spawnPos, Quaternion.identity);
            }
            else
            {
                cmdGO = new GameObject("Hostile_Commander_Vance");
                cmdGO.transform.position = spawnPos;
                var humanoidAI = cmdGO.AddComponent<DEADeyeHostileHumanoid>();
                humanoidAI.InitializeVariant(HumanoidVariant.Commander);
            }

            cmdGO.name = "Commander_Vance";
            activeSpawnedEntities.Add(cmdGO);

            commanderVance = cmdGO.GetComponent<DEADeyeHostileAI>();
            if (commanderVance != null)
            {
                var spotable = cmdGO.GetComponent<DEADeyeSpotable>();
                if (spotable == null) spotable = cmdGO.AddComponent<DEADeyeSpotable>();
                spotable.SetTargetInfo("COMMANDER VANCE", SpotClassification.HostileCommander, "Syndicate tactical commander overseeing Operation Blackout. Target must be neutralized before exfiltration.", 200);

                // Vance flees towards the northern highway escape point
                Transform escapeTarget = (escapePoints != null && escapePoints.Count > 0) ? escapePoints[0] : null;
                commanderVance.SetNavigationRoute(waypoints, escapeTarget);
                commanderVance.ChangeState(HostileStateType.Escape);

                commanderVance.OnEliminated += (ai) =>
                {
                    isCommanderEliminated = true;
                    Debug.Log("[DEADeyeMissionDirector] COMMANDER VANCE NEUTRALIZED!");
                    if (DEADeyeMissionHUD.Instance != null)
                    {
                        DEADeyeMissionHUD.Instance.ShowRadioDialogue("CONTROL", "Target confirmed neutralized! Commander Vance is down!");
                    }
                };

                commanderVance.OnEscaped += (ai) =>
                {
                    Debug.LogError("[DEADeyeMissionDirector] MISSION FAILED: Commander Vance escaped!");
                    EndMission(false);
                };
            }

            hostilesSpawnedCount++;
        }

        private void HandleCommsProgress(float progress)
        {
            commsRepairProgress = progress;
            OnCommsProgressUpdated?.Invoke(progress);

            if (currentPhase == OperationPhase.TechnicianAdvance && DEADeyeMissionHUD.Instance != null)
            {
                int barCount = (int)(progress / 10f);
                string bar = new string('=', barCount).PadRight(10, ' ');
                DEADeyeMissionHUD.Instance.UpdateObjectiveProgress($"REPAIR: [{bar}] {progress:F0}% | HOSTILES: {HostilesRemaining}");

                if (progress >= 50f && progress < 55f)
                {
                    DEADeyeMissionHUD.Instance.ShowRadioDialogue("MARCUS", "Handshake signal connected! Halfway through the bypass, keep covering us!");
                }
            }
        }

        private void HandleTechnicianKilled(DEADeyeFriendlyAI tech)
        {
            Debug.LogError($"[DEADeyeMissionDirector] MISSION FAILED: Comms Technician {tech.name} was eliminated!");
            if (DEADeyeMissionHUD.Instance != null)
            {
                DEADeyeMissionHUD.Instance.ShowRadioDialogue("CONTROL", "Critical friendly casualty! Mission is compromised!");
            }
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
                bool victory = (techMarcus != null && !techMarcus.IsEliminated) && (techElena != null && !techElena.IsEliminated) && isCommanderEliminated;
                EndMission(victory);
            }
        }

        public void SpawnNextHostile(Vector3? customPos = null)
        {
            CollectNavigationNodes();

            Vector3 spawnPos;
            Quaternion spawnRot = Quaternion.identity;

            if (customPos.HasValue)
            {
                spawnPos = customPos.Value;
            }
            else
            {
                if (spawnPoints == null || spawnPoints.Count == 0)
                {
                    Vector3 fallbackPos = new Vector3(0f, 0.1f, 20f + (hostilesSpawnedCount * 8f));
                    GameObject fallbackNode = new GameObject($"FallbackSpawn_{hostilesSpawnedCount + 1}");
                    fallbackNode.transform.position = fallbackPos;
                    spawnPoints = new List<Transform> { fallbackNode.transform };
                }

                Transform spawnNode = spawnPoints[hostilesSpawnedCount % spawnPoints.Count];
                spawnPos = spawnNode.position;
                spawnRot = spawnNode.rotation;
            }

            if (UnityEngine.AI.NavMesh.SamplePosition(spawnPos, out UnityEngine.AI.NavMeshHit navHit, 6f, UnityEngine.AI.NavMesh.AllAreas))
            {
                spawnPos = navHit.position;
            }

            GameObject hostileGO = null;
            DEADeyeHostileAI ai = null;

            if (hostilePrefabs != null && hostilePrefabs.Length > 0)
            {
                int prefabIndex = hostilesSpawnedCount % hostilePrefabs.Length;
                GameObject prefab = hostilePrefabs[prefabIndex];
                if (prefab != null)
                {
                    hostileGO = Instantiate(prefab, spawnPos, spawnRot);
                    hostileGO.name = $"Hostile_{hostilesSpawnedCount + 1}_{prefab.name}";
                    ai = hostileGO.GetComponent<DEADeyeHostileAI>();
                }
            }

            if (hostileGO == null)
            {
                hostileGO = new GameObject($"Hostile_{hostilesSpawnedCount + 1}");
                hostileGO.transform.position = spawnPos;
                hostileGO.transform.rotation = spawnRot;

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

            if (currentPhase == OperationPhase.ConvoyExtraction && hostilesEliminatedCount >= (totalHostilesToSpawn - 1))
            {
                TransitionToPhase(OperationPhase.Debriefing);
            }
        }

        public void EndMission(bool isVictory)
        {
            if (!isMissionActive) return;
            isMissionActive = false;

            if (missionLoopCoroutine != null) StopCoroutine(missionLoopCoroutine);
            if (phaseCheckCoroutine != null) StopCoroutine(phaseCheckCoroutine);

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

            // Show debriefing on tactical terminal
            if (DEADeyeTacticalTerminalUI.Instance != null)
            {
                DEADeyeTacticalTerminalUI.Instance.ShowDebriefing(
                    isVictory,
                    score,
                    hostilesEliminatedCount,
                    headshots,
                    accuracy,
                    civCasualties,
                    hostilesEscapedCount
                );
            }

            OnMissionEnded?.Invoke(isVictory);
            Debug.Log($"[DEADeyeMissionDirector] Mission Finished! Result: {(isVictory ? "VICTORY" : "DEFEAT")} - Eliminated: {hostilesEliminatedCount}, Escaped: {hostilesEscapedCount}, Score: {score}, JunctionDisruption: {disabledViaPowerJunction}");
        }
    }
}
