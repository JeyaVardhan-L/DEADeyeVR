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
        public void Test_01_BootScene_ContainsAuthoritativeCoreSystems()
        {
            Scene scene = EditorSceneManager.OpenScene("Assets/DEADeye/Scenes/BootScene.unity", OpenSceneMode.Single);
            Assert.IsTrue(scene.IsValid(), "BootScene should exist and be valid.");

            var gm = Object.FindFirstObjectByType<DEADeyeGameManager>();
            var transition = Object.FindFirstObjectByType<DEADeyeSceneTransition>();
            var input = Object.FindFirstObjectByType<DEADeyeInputRouter>();
            var audio = Object.FindFirstObjectByType<DEADeyeAudioManager>();
            var bootLoader = Object.FindFirstObjectByType<DEADeyeBootLoader>();

            Assert.IsNotNull(gm, "BootScene must contain DEADeyeGameManager.");
            Assert.IsNotNull(transition, "BootScene must contain DEADeyeSceneTransition.");
            Assert.IsNotNull(input, "BootScene must contain DEADeyeInputRouter.");
            Assert.IsNotNull(audio, "BootScene must contain DEADeyeAudioManager.");
            Assert.IsNotNull(bootLoader, "BootScene must contain DEADeyeBootLoader.");
        }

        [Test]
        public void Test_02_LobbyScene_ContainsAvatarAndTacticalUI()
        {
            Scene scene = EditorSceneManager.OpenScene("Assets/DEADeye/Scenes/LobbyScene.unity", OpenSceneMode.Single);
            Assert.IsTrue(scene.IsValid(), "LobbyScene should exist and be valid.");

            var lobbyUI = Object.FindFirstObjectByType<DEADeyeLobbyUI>();
            var avatar = Object.FindFirstObjectByType<DEADeyeLobbyAvatarController>();
            var playerCtrl = Object.FindFirstObjectByType<DEADeyePlayerController>();
            var dojoMgr = Object.FindFirstObjectByType<DEADeyeDojoLobbyManager>();

            Assert.IsNotNull(lobbyUI, "LobbyScene must contain DEADeyeLobbyUI briefing board.");
            Assert.IsNotNull(avatar, "LobbyScene must contain DEADeyeLobbyAvatarController for tactical operative avatar.");
            Assert.IsNotNull(playerCtrl, "LobbyScene must contain DEADeyePlayerController.");
            Assert.IsNotNull(dojoMgr, "LobbyScene must contain DEADeyeDojoLobbyManager.");
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
    }
}
