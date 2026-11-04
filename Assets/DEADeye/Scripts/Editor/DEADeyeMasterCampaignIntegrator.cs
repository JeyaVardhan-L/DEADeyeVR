#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace DEADeye.Editor
{
    public static class DEADeyeMasterCampaignIntegrator
    {
        [MenuItem("DEADeye/Integration/Build Complete Campaign & Scene")]
        public static void BuildCompleteCampaignAndScene()
        {
            Debug.Log("[DEADeyeMasterCampaignIntegrator] Starting master integration...");

            // 1. Generate Missions
            DEADeyeMissionSetupUtility.CreateCampaignMissionAssets();

            // 2. Generate Clothed Characters
            DEADeyeCharacterSetupUtility.GenerateClothedCharacterPrefabs();

            // 3. Open authoritative scene
            string scenePath = "Assets/DEADeye/Scenes/RooftopMission.unity";
            var scene = EditorSceneManager.OpenScene(scenePath);
            Debug.Log($"[DEADeyeMasterCampaignIntegrator] Opened scene: {scene.name}");

            // 4. Update City Builder & Regenerate City
            var cityBuilder = Object.FindFirstObjectByType<DEADeyeUrbanCityBuilder>();
            if (cityBuilder != null)
            {
                cityBuilder.BuildCityEnvironment();
                Debug.Log("[DEADeyeMasterCampaignIntegrator] Successfully rebuilt city environment with all modular districts!");
            }

            // 5. Wire Mission Director
            var director = Object.FindFirstObjectByType<DEADeyeMissionDirector>();
            if (director != null)
            {
                var h1 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/DEADeye/Prefabs/Characters/Hostile_Tactical_Rifleman.prefab");
                var h2 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/DEADeye/Prefabs/Characters/Hostile_Sniper_Scout.prefab");
                var h3 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/DEADeye/Prefabs/Characters/Hostile_Commander.prefab");
                var tech = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/DEADeye/Prefabs/Characters/Friendly_Technician.prefab");
                var civ = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/DEADeye/Prefabs/Characters/Civilian_Male_Casual.prefab");
                var sec = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/DEADeye/Prefabs/Characters/Friendly_Security_Escort.prefab");

                var defaultMission = AssetDatabase.LoadAssetAtPath<DEADeyeMissionDefinition>("Assets/DEADeye/Data/Missions/Mission_01_DeadAir.asset");
                if (defaultMission == null)
                {
                    defaultMission = AssetDatabase.LoadAssetAtPath<DEADeyeMissionDefinition>("Assets/DEADeye/Data/Missions/Mission_01_BrokenSignal.asset");
                }

                SerializedObject so = new SerializedObject(director);
                so.FindProperty("activeMission").objectReferenceValue = defaultMission;

                var hostileArray = so.FindProperty("hostilePrefabs");
                hostileArray.arraySize = 3;
                hostileArray.GetArrayElementAtIndex(0).objectReferenceValue = h1;
                hostileArray.GetArrayElementAtIndex(1).objectReferenceValue = h2;
                hostileArray.GetArrayElementAtIndex(2).objectReferenceValue = h3;

                so.FindProperty("technicianPrefab").objectReferenceValue = tech;
                so.FindProperty("civilianPrefab").objectReferenceValue = civ;
                so.FindProperty("securityEscortPrefab").objectReferenceValue = sec;

                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(director);
                Debug.Log("[DEADeyeMasterCampaignIntegrator] Wired Mission Director with clothed character prefabs and Mission 01!");
            }

            // 6. Build Tactical Terminal UI on HUD Canvas
            var hud = Object.FindFirstObjectByType<DEADeyeMissionHUD>();
            if (hud != null)
            {
                Transform hudCanvas = hud.transform.Find("HUDCanvas");
                if (hudCanvas != null)
                {
                    BuildTacticalTerminalCanvas(hudCanvas);
                }
            }

            // 7. Wire Audio Manager with Studio Quality Clips
            var audioMgr = Object.FindFirstObjectByType<DEADeyeAudioManager>();
            if (audioMgr != null)
            {
                SerializedObject soAudio = new SerializedObject(audioMgr);
                soAudio.FindProperty("gunshotClip").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/DEADeye/Audio/Sniper_Gunshot_Close.wav");
                soAudio.FindProperty("boltCycleClip").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/DEADeye/Audio/Sniper_Bolt_Cycle.wav");
                soAudio.FindProperty("reloadClip").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/DEADeye/Audio/Sniper_Reload_MagIn.wav");
                soAudio.FindProperty("scopeToggleClip").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/DEADeye/Audio/Sniper_Scope_Toggle.wav");
                soAudio.FindProperty("concreteImpactClip").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/DEADeye/Audio/Impact_Concrete.wav");
                soAudio.FindProperty("metalImpactClip").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/DEADeye/Audio/Impact_Metal.wav");
                soAudio.FindProperty("fleshImpactClip").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/DEADeye/Audio/Impact_Body.wav");
                soAudio.ApplyModifiedProperties();
                EditorUtility.SetDirty(audioMgr);
                Debug.Log("[DEADeyeMasterCampaignIntegrator] Wired high-fidelity audio clips into DEADeyeAudioManager!");
            }

            // 8. Calibrate Desktop Rig Weapon Positioning
            var desktopRig = Object.FindFirstObjectByType<DEADeyeDesktopRig>();
            if (desktopRig != null)
            {
                SerializedObject soRig = new SerializedObject(desktopRig);
                soRig.FindProperty("hipPosition").vector3Value = new Vector3(0.22f, -0.20f, 0.58f);
                soRig.FindProperty("aimPosition").vector3Value = new Vector3(0f, -0.155f, 0.36f);
                soRig.ApplyModifiedProperties();
                EditorUtility.SetDirty(desktopRig);
                Debug.Log("[DEADeyeMasterCampaignIntegrator] Calibrated Desktop Rig weapon socket!");
            }

            // Mark Scene Dirty and Save
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[DEADeyeMasterCampaignIntegrator] Complete Campaign & Scene integrated and saved successfully!");
        }

        private static void BuildTacticalTerminalCanvas(Transform hudCanvas)
        {
            Transform existing = hudCanvas.Find("TacticalTerminalPanel");
            if (existing != null)
            {
                Object.DestroyImmediate(existing.gameObject);
            }

            GameObject terminalGO = new GameObject("TacticalTerminalPanel");
            terminalGO.transform.SetParent(hudCanvas, false);
            RectTransform rt = terminalGO.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            Image bg = terminalGO.AddComponent<Image>();
            bg.color = new Color(0.02f, 0.04f, 0.07f, 0.98f);
            bg.raycastTarget = true;

            var termUI = terminalGO.AddComponent<DEADeyeTacticalTerminalUI>();
            termUI.LoadMissionAssets();
            termUI.BuildDynamicUIElements(terminalGO.transform);
            EditorUtility.SetDirty(termUI);
        }
    }
}
#endif
