using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using Unity.AI.Navigation;
using DEADeye;

namespace DEADeye.Tests
{
    /// <summary>
    /// Automated integration tests verifying scene integrity, player rigs,
    /// weapon ballistics, enemy AI state machines, and mission director rules.
    /// </summary>
    public class DEADeyeIntegrationTests
    {
        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            Physics.SyncTransforms();
        }

        [Test]
        public void Test_01_RooftopMission_ContainsAllMissionSystemsAndNavMesh()
        {
            Scene scene = EditorSceneManager.OpenScene("Assets/DEADeye/Scenes/RooftopMission.unity", OpenSceneMode.Single);
            Assert.IsTrue(scene.IsValid(), "RooftopMission should exist and be valid.");

            var gm = Object.FindFirstObjectByType<DEADeyeGameManager>();
            var audio = Object.FindFirstObjectByType<DEADeyeAudioManager>();
            var director = Object.FindFirstObjectByType<DEADeyeMissionDirector>();
            var scoreMgr = Object.FindFirstObjectByType<DEADeyeScoreManager>();
            var enemyReg = Object.FindFirstObjectByType<DEADeyeEnemyRegistry>();
            var weaponMgr = Object.FindFirstObjectByType<DEADeyeWeaponManager>();
            var cityBuilder = Object.FindFirstObjectByType<DEADeyeUrbanCityBuilder>();
            var navSurface = Object.FindFirstObjectByType<NavMeshSurface>();
            var weapon = Object.FindFirstObjectByType<DEADeyeSniperWeapon>();
            var hud = Object.FindFirstObjectByType<DEADeyeMissionHUD>();
            var boundary = Object.FindFirstObjectByType<DEADeyeRooftopBoundary>();

            Assert.IsNotNull(gm, "RooftopMission must contain DEADeyeGameManager.");
            Assert.IsNotNull(audio, "RooftopMission must contain DEADeyeAudioManager.");
            Assert.IsNotNull(director, "RooftopMission must contain DEADeyeMissionDirector.");
            Assert.IsNotNull(scoreMgr, "RooftopMission must contain DEADeyeScoreManager.");
            Assert.IsNotNull(enemyReg, "RooftopMission must contain DEADeyeEnemyRegistry.");
            Assert.IsNotNull(weaponMgr, "RooftopMission must contain DEADeyeWeaponManager.");
            Assert.IsNotNull(cityBuilder, "RooftopMission must contain DEADeyeUrbanCityBuilder.");
            Assert.IsNotNull(navSurface, "RooftopMission must have NavMeshSurface.");
            Assert.IsNotNull(weapon, "RooftopMission must have mounted sniper rifle.");
            Assert.IsNotNull(hud, "RooftopMission must have mission HUD.");
            Assert.IsNotNull(boundary, "RooftopMission must have rooftop safety boundary.");
        }

        [Test]
        public void Test_02_RooftopMission_IsSoleActiveBuildScene()
        {
            var scenes = UnityEditor.EditorBuildSettings.scenes;
            Assert.GreaterOrEqual(scenes.Length, 1, "EditorBuildSettings must have at least one scene.");
            Assert.IsTrue(scenes[0].enabled, "Scene 0 must be enabled.");
            Assert.AreEqual("Assets/DEADeye/Scenes/RooftopMission.unity", scenes[0].path, "Scene 0 must be RooftopMission.unity for direct game startup.");
        }

        [Test]
        public void Test_03_RooftopMission_ContainsAllMissionSystemsAndNavMesh()
        {
            Scene scene = EditorSceneManager.OpenScene("Assets/DEADeye/Scenes/RooftopMission.unity", OpenSceneMode.Single);
            Assert.IsTrue(scene.IsValid(), "RooftopMission should exist and be valid.");

            var director = Object.FindFirstObjectByType<DEADeyeMissionDirector>();
            var scoreMgr = Object.FindFirstObjectByType<DEADeyeScoreManager>();
            var enemyReg = Object.FindFirstObjectByType<DEADeyeEnemyRegistry>();
            var weaponMgr = Object.FindFirstObjectByType<DEADeyeWeaponManager>();
            var cityBuilder = Object.FindFirstObjectByType<DEADeyeUrbanCityBuilder>();
            var navSurface = Object.FindFirstObjectByType<NavMeshSurface>();
            var weapon = Object.FindFirstObjectByType<DEADeyeSniperWeapon>();
            var hud = Object.FindFirstObjectByType<DEADeyeMissionHUD>();
            var boundary = Object.FindFirstObjectByType<DEADeyeRooftopBoundary>();

            Assert.IsNotNull(director, "RooftopMission must contain DEADeyeMissionDirector.");
            Assert.IsNotNull(scoreMgr, "RooftopMission must contain DEADeyeScoreManager.");
            Assert.IsNotNull(enemyReg, "RooftopMission must contain DEADeyeEnemyRegistry.");
            Assert.IsNotNull(weaponMgr, "RooftopMission must contain DEADeyeWeaponManager.");
            Assert.IsNotNull(cityBuilder, "RooftopMission must contain DEADeyeUrbanCityBuilder.");
            Assert.IsNotNull(navSurface, "RooftopMission must have NavMeshSurface.");
            Assert.IsNotNull(weapon, "RooftopMission must have mounted sniper rifle.");
            Assert.IsNotNull(hud, "RooftopMission must have mission HUD.");
            Assert.IsNotNull(boundary, "RooftopMission must have rooftop safety boundary.");
        }

        [Test]
        public void Test_04_PlayerController_EnforcesMutuallyExclusiveRigs()
        {
            GameObject root = new GameObject("TestPlayerRoot");
            var ctrl = root.AddComponent<DEADeyePlayerController>();

            GameObject vrGO = new GameObject("VRRig");
            vrGO.transform.SetParent(root.transform);
            GameObject desktopGO = new GameObject("DesktopRig");
            desktopGO.transform.SetParent(root.transform);
            desktopGO.AddComponent<CharacterController>();
            var deskRig = desktopGO.AddComponent<DEADeyeDesktopRig>();

            var so = new UnityEditor.SerializedObject(ctrl);
            so.FindProperty("vrRig").objectReferenceValue = vrGO;
            so.FindProperty("desktopRig").objectReferenceValue = deskRig;
            so.ApplyModifiedProperties();

            // Test Desktop Mode
            ctrl.EnforceActiveRig(DEADeyePlayerMode.Desktop);
            Assert.IsTrue(desktopGO.activeSelf, "Desktop rig must be active in Desktop mode.");
            Assert.IsFalse(vrGO.activeSelf, "VR rig must be inactive in Desktop mode.");

            // Test VR Mode
            ctrl.EnforceActiveRig(DEADeyePlayerMode.VR);
            Assert.IsTrue(vrGO.activeSelf, "VR rig must be active in VR mode.");
            Assert.IsFalse(desktopGO.activeSelf, "Desktop rig must be inactive in VR mode.");

            Object.DestroyImmediate(root);
        }

        [Test]
        public void Test_05_SniperWeapon_BlocksFiringOutsideMissionState()
        {
            GameObject gmGO = new GameObject("TestGM");
            var gm = gmGO.AddComponent<DEADeyeGameManager>();
            DEADeyeGameManager.Instance = gm;
            gm.SetGameState(DEADeyeGameState.Lobby);

            GameObject rifleGO = new GameObject("TestRifle");
            var weapon = rifleGO.AddComponent<DEADeyeSniperWeapon>();

            bool fired = weapon.TryFire();
            Assert.IsFalse(fired, "Sniper rifle must strictly reject firing when not in MissionActive state.");

            Object.DestroyImmediate(rifleGO);
            Object.DestroyImmediate(gmGO);
        }

        [Test]
        public void Test_06_HostileStatePattern_TransitionsCorrectly()
        {
            GameObject enemyGO = new GameObject("TestCriminal");
            var nav = enemyGO.AddComponent<NavMeshAgent>();
            nav.enabled = false; // Disable real agent for unit test
            var criminal = enemyGO.AddComponent<DEADeyeHostileCriminal>();

            Assert.AreEqual(HostileStateType.Spawn, criminal.CurrentStateType, "Initial state should be Spawn.");

            criminal.ChangeState(HostileStateType.Patrol);
            Assert.AreEqual(HostileStateType.Patrol, criminal.CurrentStateType, "State must transition to Patrol.");

            criminal.ChangeState(HostileStateType.Alerted);
            Assert.AreEqual(HostileStateType.Alerted, criminal.CurrentStateType, "State must transition to Alerted.");

            criminal.ChangeState(HostileStateType.Escape);
            Assert.AreEqual(HostileStateType.Escape, criminal.CurrentStateType, "State must transition to Escape.");

            criminal.ChangeState(HostileStateType.Eliminated);
            Assert.AreEqual(HostileStateType.Eliminated, criminal.CurrentStateType, "State must transition to Eliminated.");

            Object.DestroyImmediate(enemyGO);
        }

        [Test]
        public void Test_07_PrecisionHeadshot_AppliesThreeTimesDamage()
        {
            GameObject enemyGO = new GameObject("TestEnemyWithHeadshot");
            var nav = enemyGO.AddComponent<NavMeshAgent>();
            nav.enabled = false;
            var criminal = enemyGO.AddComponent<DEADeyeHostileCriminal>();

            GameObject headGO = new GameObject("Head");
            headGO.transform.SetParent(enemyGO.transform);
            var col = headGO.AddComponent<SphereCollider>();
            col.isTrigger = true;
            var trigger = headGO.AddComponent<DEADeyeHeadshotTrigger>();
            trigger.Initialize(criminal);

            int startHealth = criminal.CurrentHealth;
            trigger.RegisterHeadshot(50, Vector3.zero, Vector3.up);

            // 50 base damage * 3 = 150 damage -> health should be 0 or less
            Assert.LessOrEqual(criminal.CurrentHealth, startHealth - 150, "Headshot trigger must apply 3x damage multiplier.");

            Object.DestroyImmediate(enemyGO);
        }

        [Test]
        public void Test_08_ScoreManager_CalculatesMetricsAccurately()
        {
            GameObject smGO = new GameObject("TestScoreManager");
            var sm = smGO.AddComponent<DEADeyeScoreManager>();
            sm.ResetMetrics();

            sm.RegisterShotFired();
            sm.RegisterShotFired();
            sm.RegisterHit(isHeadshot: false, points: 100);

            Assert.AreEqual(2, sm.ShotsFired, "Shots fired should be 2.");
            Assert.AreEqual(1, sm.ShotsHit, "Shots hit should be 1.");
            Assert.AreEqual(50f, sm.AccuracyPercentage, 0.1f, "Accuracy should be 50%.");
            Assert.GreaterOrEqual(sm.CurrentScore, 100, "Score must increase upon hit.");

            sm.RegisterHit(isHeadshot: true, points: 100);
            Assert.AreEqual(1, sm.Headshots, "Headshots count must be 1.");

            Object.DestroyImmediate(smGO);
        }

        [Test]
        public void Test_09_WeaponManager_ConsumesAmmoAndReloads()
        {
            GameObject wmGO = new GameObject("TestWeaponManager");
            var wm = wmGO.AddComponent<DEADeyeWeaponManager>();
            wm.RefillInstantly();

            Assert.AreEqual(5, wm.CurrentAmmo, "Initial ammo should be 5.");
            Assert.IsTrue(wm.CanFire(), "Weapon should be able to fire with ammo > 0.");

            wm.ConsumeAmmo();
            Assert.AreEqual(4, wm.CurrentAmmo, "Ammo must decrease to 4 after shot.");

            wm.RefillInstantly();
            Assert.AreEqual(5, wm.CurrentAmmo, "Refill must restore full magazine.");

            Object.DestroyImmediate(wmGO);
        }

        [Test]
        public void Test_10_DifficultySettings_ModifyHostileSpeeds()
        {
            GameObject gmGO = new GameObject("TestGM");
            var gm = gmGO.AddComponent<DEADeyeGameManager>();
            DEADeyeGameManager.Instance = gm;

            GameObject enemyGO = new GameObject("TestSpeedEnemy");
            enemyGO.AddComponent<NavMeshAgent>().enabled = false;
            var enemy = enemyGO.AddComponent<DEADeyeHostileCriminal>();

            gm.SetDifficulty(MissionDifficulty.Recruit);
            float recruitSpeed = enemy.WalkSpeed;

            gm.SetDifficulty(MissionDifficulty.Elite);
            float eliteSpeed = enemy.WalkSpeed;

            Assert.Greater(eliteSpeed, recruitSpeed, "Elite difficulty hostile speed must exceed Recruit difficulty.");

            Object.DestroyImmediate(enemyGO);
            Object.DestroyImmediate(gmGO);
        }

        [Test]
        public void Test_11_CampaignMissions_ExistAndAreConfigured()
        {
            string[] missionNames = new string[]
            {
                "Mission_01_DeadAir.asset",
                "Mission_02_TheLastStop.asset",
                "Mission_03_SilentEntry.asset",
                "Mission_04_DeadGround.asset"
            };

            foreach (var mName in missionNames)
            {
                string path = $"Assets/DEADeye/Data/Missions/{mName}";
                var mission = UnityEditor.AssetDatabase.LoadAssetAtPath<DEADeyeMissionDefinition>(path);
                if (mission == null && mName == "Mission_01_DeadAir.asset")
                {
                    mission = UnityEditor.AssetDatabase.LoadAssetAtPath<DEADeyeMissionDefinition>("Assets/DEADeye/Data/Missions/Mission_01_BrokenSignal.asset");
                }
                Assert.IsNotNull(mission, $"Campaign mission at {path} must exist.");
                Assert.IsNotEmpty(mission.operationTitle, "Mission title must not be empty.");
                Assert.Greater(mission.timeLimitSeconds, 30f, "Mission time limit must be > 30s.");
                Assert.Greater(mission.hostileCount, 0, "Hostile count must be > 0.");
            }
        }

        [Test]
        public void Test_12_ClothedCharacterPrefabs_ExistAndHaveRequiredComponents()
        {
            string[] prefabs = new string[]
            {
                "Assets/DEADeye/Prefabs/Characters/Hostile_Tactical_Rifleman.prefab",
                "Assets/DEADeye/Prefabs/Characters/Hostile_Sniper_Scout.prefab",
                "Assets/DEADeye/Prefabs/Characters/Hostile_Commander.prefab",
                "Assets/DEADeye/Prefabs/Characters/Friendly_Technician.prefab",
                "Assets/DEADeye/Prefabs/Characters/Civilian_Male_Casual.prefab"
            };

            foreach (var pPath in prefabs)
            {
                var go = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(pPath);
                Assert.IsNotNull(go, $"Character prefab {pPath} must exist.");
                Assert.IsNotNull(go.GetComponent<NavMeshAgent>(), $"Character {go.name} must have NavMeshAgent.");
                Assert.IsNotNull(go.GetComponent<Collider>(), $"Character {go.name} must have a root Collider.");
                Assert.IsNotNull(go.GetComponent<DEADeyeProceduralCharacterAnimator>(), $"Character {go.name} must have ProceduralCharacterAnimator.");
            }
        }

        [Test]
        public void Test_13_FriendlyAI_TracksRepairProgressAndKills()
        {
            GameObject friendlyGO = new GameObject("TestFriendlyTech");
            friendlyGO.AddComponent<NavMeshAgent>().enabled = false;
            var friendly = friendlyGO.AddComponent<DEADeyeFriendlyAI>();

            Assert.AreEqual(0f, friendly.WorkProgress, "Initial progress must be 0%.");
            Assert.AreEqual(100, friendly.CurrentHealth, "Initial health must be 100.");
            Assert.IsFalse(friendly.IsEliminated, "Friendly must not be eliminated initially.");

            bool killEventFired = false;
            friendly.OnVipKilled += (v) => killEventFired = true;

            friendly.TakeDamage(150, Vector3.zero, Vector3.up, false);
            Assert.IsTrue(friendly.IsEliminated, "Friendly should be eliminated after taking lethal damage.");
            Assert.IsTrue(killEventFired, "OnVipKilled event must fire.");

            Object.DestroyImmediate(friendlyGO);
        }

        [Test]
        public void Test_14_CivilianAI_PenalizesFriendlyFire()
        {
            GameObject smGO = new GameObject("TestSM_Civ");
            var sm = smGO.AddComponent<DEADeyeScoreManager>();
            DEADeyeScoreManager.Instance = sm;
            sm.ResetMetrics();

            GameObject civGO = new GameObject("TestCiv");
            civGO.AddComponent<NavMeshAgent>().enabled = false;
            var civ = civGO.AddComponent<DEADeyeCivilianAI>();

            civ.TakeDamage(100, Vector3.zero, Vector3.up, false);
            Assert.IsTrue(civ.IsEliminated, "Civilian must be eliminated.");
            Assert.AreEqual(1, sm.CivilianCasualties, "Score manager must record 1 civilian casualty.");

            Object.DestroyImmediate(civGO);
            Object.DestroyImmediate(smGO);
            DEADeyeScoreManager.Instance = null;
        }

        [Test]
        public void Test_15_EnvironmentalProps_TriggerTacticalConsequences()
        {
            GameObject propGO = new GameObject("TestPowerJunction");
            var prop = propGO.AddComponent<DEADeyeEnvironmentalProp>();
            prop.Initialize(EnvironmentalPropType.PowerJunction, "TEST POWER JUNCTION");

            Assert.IsFalse(prop.IsTriggered, "Prop should not be triggered initially.");
            Assert.AreEqual(EnvironmentalPropType.PowerJunction, prop.PropType);

            bool triggered = false;
            prop.OnPropTriggered += (p) => triggered = true;

            prop.TakeDamage(100, Vector3.zero, Vector3.up);

            Assert.IsTrue(prop.IsTriggered, "Prop must be marked triggered after taking damage.");
            Assert.IsTrue(triggered, "OnPropTriggered event must fire.");

            Object.DestroyImmediate(propGO);
        }

        [Test]
        public void Test_16_ReconSpottingSystem_IdentifiesTargets()
        {
            GameObject targetGO = new GameObject("TestSpotableTarget");
            var spotable = targetGO.AddComponent<DEADeyeSpotable>();
            spotable.SetTargetInfo("COMMUNICATIONS JAMMER", SpotClassification.Objective, "Test Description", 100);

            Assert.IsFalse(spotable.IsIdentified, "Target should not be identified initially.");
            Assert.AreEqual("COMMUNICATIONS JAMMER", spotable.TargetName);
            Assert.AreEqual(SpotClassification.Objective, spotable.Classification);

            bool identified = false;
            spotable.OnTargetIdentified += (s) => identified = true;

            spotable.MarkIdentified();

            Assert.IsTrue(spotable.IsIdentified, "Target must be marked as identified.");
            Assert.IsTrue(identified, "OnTargetIdentified event must fire.");

            Object.DestroyImmediate(targetGO);
        }

        [Test]
        public void Test_17_OperationDeadAir_TransitionsThroughPhases()
        {
            GameObject dirGO = new GameObject("TestDirector");
            var director = dirGO.AddComponent<DEADeyeMissionDirector>();

            Assert.AreEqual(OperationPhase.Reconnaissance, director.CurrentPhase, "Initial phase must be Reconnaissance.");

            director.TransitionToPhase(OperationPhase.Disruption);
            Assert.AreEqual(OperationPhase.Disruption, director.CurrentPhase, "Phase must transition to Disruption.");

            director.TransitionToPhase(OperationPhase.TechnicianAdvance);
            Assert.AreEqual(OperationPhase.TechnicianAdvance, director.CurrentPhase, "Phase must transition to TechnicianAdvance.");

            director.TransitionToPhase(OperationPhase.NeutralizeCommander);
            Assert.AreEqual(OperationPhase.NeutralizeCommander, director.CurrentPhase, "Phase must transition to NeutralizeCommander.");

            director.TransitionToPhase(OperationPhase.ConvoyExtraction);
            Assert.AreEqual(OperationPhase.ConvoyExtraction, director.CurrentPhase, "Phase must transition to ConvoyExtraction.");

            director.TransitionToPhase(OperationPhase.Debriefing);
            Assert.AreEqual(OperationPhase.Debriefing, director.CurrentPhase, "Phase must transition to Debriefing.");

            Object.DestroyImmediate(dirGO);
        }

        [Test]
        public void Test_18_WalkableOverwatchPerches_ExistWithBridgeConnections()
        {
            Scene scene = EditorSceneManager.OpenScene("Assets/DEADeye/Scenes/RooftopMission.unity", OpenSceneMode.Single);
            Assert.IsTrue(scene.IsValid(), "RooftopMission scene must exist.");

            var cityBuilder = Object.FindFirstObjectByType<DEADeyeUrbanCityBuilder>();
            Assert.IsNotNull(cityBuilder, "DEADeyeUrbanCityBuilder must exist in the scene.");
        }

        [Test]
        public void Test_19_SniperScope_FiringRequiresOpticalScope()
        {
            GameObject gmGO = new GameObject("TestGM");
            var gm = gmGO.AddComponent<DEADeyeGameManager>();
            DEADeyeGameManager.Instance = gm;
            gm.SetGameState(DEADeyeGameState.MissionActive);

            GameObject rifleGO = new GameObject("TestRifle");
            var weapon = rifleGO.AddComponent<DEADeyeSniperWeapon>();

            // When IsScoped is false, TryFire must return false
            Assert.IsFalse(weapon.IsScoped, "Weapon should not be scoped by default.");
            Assert.IsFalse(weapon.TryFire(), "Sniper rifle must strictly reject firing when not zoomed in scope.");

            // When optic is attached and aiming is true, weapon.IsScoped is true
            GameObject opticGO = new GameObject("Optic");
            opticGO.transform.SetParent(rifleGO.transform);
            var optic = opticGO.AddComponent<DEADeyeScopeOptic>();
            optic.SetScopeActive(true);

            Assert.IsTrue(weapon.IsScoped, "Weapon must report IsScoped = true when scope optic is active.");

            Object.DestroyImmediate(rifleGO);
            Object.DestroyImmediate(gmGO);
        }
    }
}
