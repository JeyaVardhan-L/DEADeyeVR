using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using Unity.AI.Navigation;
using Unity.XR.CoreUtils;
using DEADeye;

namespace DEADeye.Editor
{
    /// <summary>
    /// Master Scene Builder for DEADeye VR.
    /// Programmatically generates and validates the three authoritative scenes:
    /// 1. BootScene.unity - Authoritative core initialization and routing.
    /// 2. LobbyScene.unity - Dojo briefing room, animated avatar, tactical UI menu.
    /// 3. RooftopMission.unity - High-rise urban sniper perch, NavMesh streets, hostile AI, HUD.
    /// </summary>
    public static class DEADeyeMasterSceneBuilder
    {
        public const string SCENE_DIR = "Assets/DEADeye/Scenes";
        public const string BOOT_SCENE_PATH = "Assets/DEADeye/Scenes/BootScene.unity";
        public const string LOBBY_SCENE_PATH = "Assets/DEADeye/Scenes/LobbyScene.unity";
        public const string ROOFTOP_SCENE_PATH = "Assets/DEADeye/Scenes/RooftopMission.unity";

        [MenuItem("DEADeye/Build All Game Scenes", priority = 10)]
        public static void BuildAllScenes()
        {
            if (!Directory.Exists(SCENE_DIR))
            {
                Directory.CreateDirectory(SCENE_DIR);
            }

            Debug.Log("[DEADeyeMasterSceneBuilder] Starting comprehensive build of all game scenes...");

            BuildBootScene();
            BuildLobbyScene();
            BuildRooftopMissionScene();
            ConfigureBuildSettings();

            Debug.Log("[DEADeyeMasterSceneBuilder] SUCCESS: All three scenes built, baked, and registered in Build Settings!");
        }

        public static void BuildBootScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "BootScene";

            // Boot Camera (Solid Black)
            GameObject camGO = new GameObject("BootCamera");
            Camera cam = camGO.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            cam.cullingMask = 0;
            camGO.AddComponent<AudioListener>();

            // Authoritative Core Singletons (Dedicated GameObjects)
            new GameObject("DEADeyeGameManager").AddComponent<DEADeyeGameManager>();
            new GameObject("DEADeyeSceneTransition").AddComponent<DEADeyeSceneTransition>();
            new GameObject("DEADeyeInputRouter").AddComponent<DEADeyeInputRouter>();
            new GameObject("DEADeyeAudioManager").AddComponent<DEADeyeAudioManager>();
            new GameObject("DEADeyeBootLoader").AddComponent<DEADeyeBootLoader>();

            EditorSceneManager.SaveScene(scene, BOOT_SCENE_PATH);
            Debug.Log($"[DEADeyeMasterSceneBuilder] Saved {BOOT_SCENE_PATH}");
        }

        public static void BuildLobbyScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "LobbyScene";

            // Lighting
            GameObject dirLightGO = new GameObject("Tactical_DirectionalLight");
            Light dirLight = dirLightGO.AddComponent<Light>();
            dirLight.type = LightType.Directional;
            dirLight.color = new Color(1.0f, 0.95f, 0.88f);
            dirLight.intensity = 1.25f;
            dirLightGO.transform.rotation = Quaternion.Euler(45f, -30f, 0f);

            // Ground Floor (30m x 30m)
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "LobbyFloor";
            floor.transform.position = new Vector3(0f, -0.15f, 0f);
            floor.transform.localScale = new Vector3(30f, 0.3f, 30f);
            Material floorMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Foundation/M_Floor_Slate.mat");
            if (floorMat != null) floor.GetComponent<MeshRenderer>().sharedMaterial = floorMat;

            // Dojo Environment Model
            string dojoFbxPath = "Assets/DEADeye/Art/Environments/Dojo/source/Dojo 4_2.fbx";
            GameObject dojoPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(dojoFbxPath);
            if (dojoPrefab != null)
            {
                GameObject dojoInstance = (GameObject)PrefabUtility.InstantiatePrefab(dojoPrefab);
                dojoInstance.name = "Dojo_Environment";
                dojoInstance.transform.position = new Vector3(0f, 0f, 0f);
                dojoInstance.transform.rotation = Quaternion.identity;
                dojoInstance.AddComponent<DEADeyeDojoLobbyManager>();
            }

            // Briefing Staging Table
            GameObject table = GameObject.CreatePrimitive(PrimitiveType.Cube);
            table.name = "BriefingTable";
            table.transform.position = new Vector3(0f, 0.45f, 2.4f);
            table.transform.localScale = new Vector3(2.4f, 0.9f, 1.2f);
            if (floorMat != null) table.GetComponent<MeshRenderer>().sharedMaterial = floorMat;

            // Sniper Rifle Model Display on Table
            string riflePath = "Assets/DEADeye/Art/Weapons/SniperRifle/source/model/model/model.dae";
            GameObject riflePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(riflePath);
            if (riflePrefab != null)
            {
                GameObject rifleDisplay = (GameObject)PrefabUtility.InstantiatePrefab(riflePrefab);
                rifleDisplay.name = "XM500_Sniper_Display";
                rifleDisplay.transform.position = new Vector3(0f, 0.95f, 2.4f);
                rifleDisplay.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
                rifleDisplay.transform.localScale = Vector3.one * 0.15f;
            }

            // Tactical Operative Avatar standing behind table
            GameObject avatarGO = new GameObject("LobbyOperativeAvatar");
            avatarGO.transform.position = new Vector3(0f, 0f, 3.8f);
            avatarGO.transform.rotation = Quaternion.Euler(0f, 180f, 0f); // Facing the player at (0,0,0)
            avatarGO.AddComponent<DEADeyeLobbyAvatarController>();

            // Player Setup (Desktop Rig & VR XR Origin)
            SetupPlayerController(Vector3.zero, Quaternion.identity);

            // Lobby UI Menu
            GameObject uiGO = new GameObject("DEADeye_LobbyUI");
            uiGO.AddComponent<DEADeyeLobbyUI>();

            EditorSceneManager.SaveScene(scene, LOBBY_SCENE_PATH);
            Debug.Log($"[DEADeyeMasterSceneBuilder] Saved {LOBBY_SCENE_PATH}");
        }

        public static void BuildRooftopMissionScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "RooftopMission";

            // Golden Hour City Sun
            GameObject sunGO = new GameObject("City_Sunlight");
            Light sun = sunGO.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(1.0f, 0.88f, 0.72f);
            sun.intensity = 1.3f;
            sunGO.transform.rotation = Quaternion.Euler(32f, -40f, 0f);

            // Mission Managers Root
            GameObject managersRoot = new GameObject("Mission_AuthoritativeManagers");
            DEADeyeMissionDirector director = managersRoot.AddComponent<DEADeyeMissionDirector>();
            DEADeyeScoreManager scoreManager = managersRoot.AddComponent<DEADeyeScoreManager>();
            DEADeyeEnemyRegistry enemyReg = managersRoot.AddComponent<DEADeyeEnemyRegistry>();
            DEADeyeWeaponManager weaponMgr = managersRoot.AddComponent<DEADeyeWeaponManager>();

            // Urban City Environment Builder
            GameObject cityGO = new GameObject("CityEnvironment_Controller");
            DEADeyeUrbanCityBuilder cityBuilder = cityGO.AddComponent<DEADeyeUrbanCityBuilder>();
            cityBuilder.BuildCityEnvironment();

            // Bake NavMesh Surface on the generated streets & sidewalks
            NavMeshSurface navSurface = cityGO.AddComponent<NavMeshSurface>();
            navSurface.collectObjects = CollectObjects.All;
            navSurface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            navSurface.BuildNavMesh();
            Debug.Log("[DEADeyeMasterSceneBuilder] Baked NavMesh for City Streets & Sidewalks.");

            // Player Perch at (0, 26.0, -4.5) overlooking city streets (Z > 0)
            Vector3 playerPerchPos = new Vector3(0f, 26.0f, -4.5f);
            SetupPlayerController(playerPerchPos, Quaternion.identity);

            // Rooftop Boundary Safety Barrier
            GameObject boundaryGO = new GameObject("RooftopSafetyBoundary");
            boundaryGO.transform.position = new Vector3(0f, 26.0f, -6.0f);
            boundaryGO.AddComponent<DEADeyeRooftopBoundary>();

            // Rooftop Sniper Weapon resting on front sandbag parapet at (0, 27.22, -1.1)
            BuildSniperWeaponObject(new Vector3(0f, 27.22f, -1.1f));

            // Mission Telemetry HUD & Results UI
            GameObject hudGO = new GameObject("DEADeye_MissionHUD");
            hudGO.AddComponent<DEADeyeMissionHUD>();

            EditorSceneManager.SaveScene(scene, ROOFTOP_SCENE_PATH);
            Debug.Log($"[DEADeyeMasterSceneBuilder] Saved {ROOFTOP_SCENE_PATH}");
        }

        private static GameObject SetupPlayerController(Vector3 rootPosition, Quaternion rootRotation)
        {
            GameObject playerControllerGO = new GameObject("DEADeye_PlayerController");
            playerControllerGO.transform.position = rootPosition;
            playerControllerGO.transform.rotation = rootRotation;

            DEADeyePlayerController controller = playerControllerGO.AddComponent<DEADeyePlayerController>();

            // 1. Desktop FPS Rig
            GameObject desktopGO = new GameObject("Desktop_FPSRig");
            desktopGO.layer = LayerMask.NameToLayer("Ignore Raycast");
            desktopGO.transform.SetParent(playerControllerGO.transform, false);
            desktopGO.transform.localPosition = Vector3.zero;
            desktopGO.transform.localRotation = Quaternion.identity;

            CharacterController cc = desktopGO.AddComponent<CharacterController>();
            cc.center = new Vector3(0f, 0.9f, 0f);
            cc.radius = 0.35f;
            cc.height = 1.8f;

            GameObject camGO = new GameObject("DesktopCamera");
            camGO.transform.SetParent(desktopGO.transform, false);
            camGO.transform.localPosition = new Vector3(0f, 1.6f, 0f);
            camGO.transform.localRotation = Quaternion.identity;
            Camera cam = camGO.AddComponent<Camera>();
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 600f;
            cam.fieldOfView = 60f;
            camGO.AddComponent<AudioListener>();

            DEADeyeDesktopRig desktopRig = desktopGO.AddComponent<DEADeyeDesktopRig>();

            // 2. VR XR Origin
            string xrOriginPath = "Assets/Samples/XR Interaction Toolkit/3.4.0/Starter Assets/Prefabs/XR Origin (XR Rig).prefab";
            GameObject xrOriginPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(xrOriginPath);
            GameObject xrOriginInstance = null;
            if (xrOriginPrefab != null)
            {
                xrOriginInstance = (GameObject)PrefabUtility.InstantiatePrefab(xrOriginPrefab);
                xrOriginInstance.name = "VR_XROriginRig";
                xrOriginInstance.transform.SetParent(playerControllerGO.transform, false);
                xrOriginInstance.transform.localPosition = Vector3.zero;
                xrOriginInstance.transform.localRotation = Quaternion.identity;
            }

            // XR Interaction Simulator for Editor testing
            string simPath = "Assets/Samples/XR Interaction Toolkit/3.4.0/XR Interaction Simulator/XR Interaction Simulator.prefab";
            GameObject simPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(simPath);
            if (simPrefab != null)
            {
                GameObject simInstance = (GameObject)PrefabUtility.InstantiatePrefab(simPrefab);
                simInstance.name = "XR Interaction Simulator";
                simInstance.transform.SetParent(playerControllerGO.transform, false);
            }

            // Link to controller
            SerializedObject so = new SerializedObject(controller);
            so.FindProperty("desktopRig").objectReferenceValue = desktopRig;
            if (xrOriginInstance != null)
            {
                so.FindProperty("vrRig").objectReferenceValue = xrOriginInstance;
            }
            so.ApplyModifiedProperties();

            controller.EnforceActiveRig();
            return playerControllerGO;
        }

        private static GameObject BuildSniperWeaponObject(Vector3 restingPosition)
        {
            GameObject rifleRoot = new GameObject("XM500_PrecisionSniper");
            rifleRoot.transform.position = restingPosition;
            rifleRoot.transform.rotation = Quaternion.identity;

            // Visual mesh
            string rifleDaePath = "Assets/DEADeye/Art/Weapons/SniperRifle/source/model/model/model.dae";
            GameObject modelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(rifleDaePath);
            if (modelPrefab != null)
            {
                GameObject meshInstance = (GameObject)PrefabUtility.InstantiatePrefab(modelPrefab);
                meshInstance.name = "RifleMesh";
                meshInstance.transform.SetParent(rifleRoot.transform, false);
                meshInstance.transform.localPosition = Vector3.zero;
                meshInstance.transform.localRotation = Quaternion.identity;
                meshInstance.transform.localScale = Vector3.one * 0.14f;
            }
            else
            {
                // Fallback procedural rifle body
                GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
                body.name = "RifleBody";
                body.transform.SetParent(rifleRoot.transform, false);
                body.transform.localScale = new Vector3(0.08f, 0.12f, 0.85f);
            }

            // Physical Grab Collider
            BoxCollider col = rifleRoot.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, 0.05f, 0.15f);
            col.size = new Vector3(0.15f, 0.25f, 0.95f);

            Rigidbody rb = rifleRoot.AddComponent<Rigidbody>();
            rb.isKinematic = true; // Grounded on sandbag rest until grabbed

            // Muzzle Point
            GameObject muzzle = new GameObject("MuzzlePoint");
            muzzle.transform.SetParent(rifleRoot.transform, false);
            muzzle.transform.localPosition = new Vector3(0f, 0.06f, 0.65f);

            // Scope Optic Camera & Lens
            GameObject scopeGO = new GameObject("SniperScopeOptic");
            scopeGO.transform.SetParent(rifleRoot.transform, false);
            scopeGO.transform.localPosition = new Vector3(0f, 0.12f, 0.05f);

            Camera scopeCam = scopeGO.AddComponent<Camera>();
            scopeCam.fieldOfView = 15f;
            scopeCam.nearClipPlane = 0.2f;
            scopeCam.farClipPlane = 600f;
            scopeCam.enabled = false;

            DEADeyeScopeOptic optic = scopeGO.AddComponent<DEADeyeScopeOptic>();

            // Sniper Weapon Script
            DEADeyeSniperWeapon weapon = rifleRoot.AddComponent<DEADeyeSniperWeapon>();

            return rifleRoot;
        }

        public static void ConfigureBuildSettings()
        {
            EditorBuildSettingsScene[] scenes = new EditorBuildSettingsScene[]
            {
                new EditorBuildSettingsScene(BOOT_SCENE_PATH, true),
                new EditorBuildSettingsScene(LOBBY_SCENE_PATH, true),
                new EditorBuildSettingsScene(ROOFTOP_SCENE_PATH, true),
                new EditorBuildSettingsScene("Assets/Scenes/FoundationTest.unity", true)
            };
            EditorBuildSettings.scenes = scenes;
            Debug.Log($"[DEADeyeMasterSceneBuilder] Registered {scenes.Length} scenes in EditorBuildSettings.");
        }
    }
}
