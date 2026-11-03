#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

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

                var defaultMission = AssetDatabase.LoadAssetAtPath<DEADeyeMissionDefinition>("Assets/DEADeye/Data/Missions/Mission_01_BrokenSignal.asset");

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
            rt.anchorMin = new Vector2(0.15f, 0.12f);
            rt.anchorMax = new Vector2(0.85f, 0.88f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            Image bg = terminalGO.AddComponent<Image>();
            bg.color = new Color(0.03f, 0.05f, 0.08f, 0.94f);

            Outline outline = terminalGO.AddComponent<Outline>();
            outline.effectColor = new Color(0.0f, 0.8f, 1.0f, 0.7f);
            outline.effectDistance = new Vector2(2f, -2f);

            // Title Header
            var header = CreateText(terminalGO.transform, "Header", "DEADeye TACTICAL OVERWATCH NETWORK // MISSION SELECT", 24, new Vector2(24f, -16f), new Vector2(700f, 36f), new Color(0f, 0.9f, 1f));

            // Mission Selection Tabs (OP-01 to OP-04)
            string[] tabs = new string[] { "[1] OP-01: BROKEN SIGNAL", "[2] OP-02: MOVING TARGET", "[3] OP-03: NO CLEAR SHOT", "[4] OP-04: DEAD GROUND" };
            for (int i = 0; i < tabs.Length; i++)
            {
                float x = 24f + (i * 260f);
                CreateTabButton(terminalGO.transform, $"Tab_{i + 1}", tabs[i], new Vector2(x, -60f), i);
            }

            // Content Panel
            GameObject contentPanel = new GameObject("DossierContent");
            contentPanel.transform.SetParent(terminalGO.transform, false);
            RectTransform cpRT = contentPanel.AddComponent<RectTransform>();
            cpRT.anchorMin = new Vector2(0.03f, 0.15f);
            cpRT.anchorMax = new Vector2(0.97f, 0.78f);
            cpRT.offsetMin = Vector2.zero;
            cpRT.offsetMax = Vector2.zero;

            var opTitle = CreateText(contentPanel.transform, "OpTitle", "OPERATION: BROKEN SIGNAL", 26, new Vector2(10f, -10f), new Vector2(800f, 36f), Color.white);
            var sector = CreateText(contentPanel.transform, "Sector", "SECTOR: COMMERCIAL DISTRICT & COMMUNICATIONS PLAZA", 18, new Vector2(10f, -48f), new Vector2(800f, 28f), new Color(0.2f, 0.85f, 1f));
            var threat = CreateText(contentPanel.transform, "Threat", "THREAT LEVEL: HIGH (HEAVILY ARMED SYNDICATE STRIKE SQUAD) | TIME: 120s", 16, new Vector2(10f, -78f), new Vector2(800f, 26f), new Color(1f, 0.8f, 0.2f));

            var briefing = CreateText(contentPanel.transform, "Briefing", 
                "Syndicate hostiles have sabotaged the municipal relay tower and pinned down our two-man comms engineering team in the commercial plaza.\n\n" +
                "Provide precision sniper overwatch from the rooftop perch. Protect both technicians while they restore the transmission array, and eliminate all advancing hostile elements before they compromise the site.", 
                16, new Vector2(10f, -115f), new Vector2(1000f, 120f), new Color(0.85f, 0.9f, 0.95f));

            var primary = CreateText(contentPanel.transform, "Primary", "• PRIMARY: Protect 2 Comms Technicians & Neutralize all 6 Hostiles", 18, new Vector2(10f, -245f), new Vector2(900f, 30f), new Color(0.2f, 1f, 0.4f));
            var secondary = CreateText(contentPanel.transform, "Secondary", "• SECONDARY: Achieve at least 2 Headshots without friendly casualties", 16, new Vector2(10f, -280f), new Vector2(900f, 28f), new Color(0.9f, 0.9f, 0.2f));

            // Deploy Button / Instruction Prompt
            var prompt = CreateText(terminalGO.transform, "DeployPrompt", "[ PRESS ENTER / SPACE / E / TRIGGER TO DEPLOY TO OVERWATCH ]", 20, new Vector2(24f, 20f), new Vector2(1100f, 36f), new Color(0f, 1f, 0.6f), isBottom: true);

            // Debrief Panel (Child, initially inactive)
            GameObject debriefGO = new GameObject("DebriefPanel");
            debriefGO.transform.SetParent(terminalGO.transform, false);
            RectTransform dbRT = debriefGO.AddComponent<RectTransform>();
            dbRT.anchorMin = Vector2.zero;
            dbRT.anchorMax = Vector2.one;
            dbRT.offsetMin = Vector2.zero;
            dbRT.offsetMax = Vector2.zero;

            Image dbBg = debriefGO.AddComponent<Image>();
            dbBg.color = new Color(0.02f, 0.04f, 0.06f, 0.97f);

            var dbStatus = CreateText(debriefGO.transform, "DebriefStatus", "OPERATION ACCOMPLISHED", 36, new Vector2(40f, -40f), new Vector2(800f, 50f), new Color(0.2f, 0.95f, 0.4f));
            var dbRank = CreateText(debriefGO.transform, "DebriefRank", "RANK: S", 48, new Vector2(850f, -35f), new Vector2(300f, 60f), Color.yellow);
            var dbStats = CreateText(debriefGO.transform, "DebriefStats", "SCORE: 0\nELIMS: 0\nHEADSHOTS: 0", 22, new Vector2(40f, -120f), new Vector2(800f, 260f), Color.white);

            var dbPrompt = CreateText(debriefGO.transform, "DebriefPrompt", "[ PRESS ENTER / E / TRIGGER TO RETURN TO TERMINAL ]", 20, new Vector2(40f, 25f), new Vector2(900f, 36f), new Color(0f, 0.9f, 1f), isBottom: true);

            debriefGO.SetActive(false);

            // Wire up DEADeyeTacticalTerminalUI component
            var termUI = terminalGO.AddComponent<DEADeyeTacticalTerminalUI>();
            SerializedObject termSO = new SerializedObject(termUI);

            termSO.FindProperty("headerOperationText").objectReferenceValue = header;
            termSO.FindProperty("titleText").objectReferenceValue = opTitle;
            termSO.FindProperty("districtText").objectReferenceValue = sector;
            termSO.FindProperty("threatText").objectReferenceValue = threat;
            termSO.FindProperty("briefingText").objectReferenceValue = briefing;
            termSO.FindProperty("primaryObjText").objectReferenceValue = primary;
            termSO.FindProperty("secondaryObjText").objectReferenceValue = secondary;
            termSO.FindProperty("deployPromptText").objectReferenceValue = prompt;

            termSO.FindProperty("terminalCanvasRoot").objectReferenceValue = terminalGO;
            termSO.FindProperty("debriefPanel").objectReferenceValue = debriefGO;
            termSO.FindProperty("debriefStatusText").objectReferenceValue = dbStatus;
            termSO.FindProperty("debriefStatsText").objectReferenceValue = dbStats;
            termSO.FindProperty("debriefRankText").objectReferenceValue = dbRank;

            // Load all 4 missions into terminal
            var m1 = AssetDatabase.LoadAssetAtPath<DEADeyeMissionDefinition>("Assets/DEADeye/Data/Missions/Mission_01_BrokenSignal.asset");
            var m2 = AssetDatabase.LoadAssetAtPath<DEADeyeMissionDefinition>("Assets/DEADeye/Data/Missions/Mission_02_MovingTarget.asset");
            var m3 = AssetDatabase.LoadAssetAtPath<DEADeyeMissionDefinition>("Assets/DEADeye/Data/Missions/Mission_03_NoClearShot.asset");
            var m4 = AssetDatabase.LoadAssetAtPath<DEADeyeMissionDefinition>("Assets/DEADeye/Data/Missions/Mission_04_DeadGround.asset");

            var mProp = termSO.FindProperty("missions");
            mProp.arraySize = 4;
            mProp.GetArrayElementAtIndex(0).objectReferenceValue = m1;
            mProp.GetArrayElementAtIndex(1).objectReferenceValue = m2;
            mProp.GetArrayElementAtIndex(2).objectReferenceValue = m3;
            mProp.GetArrayElementAtIndex(3).objectReferenceValue = m4;

            termSO.ApplyModifiedProperties();
            EditorUtility.SetDirty(termUI);
        }

        private static Text CreateText(Transform parent, string name, string text, int fontSize, Vector2 pos, Vector2 size, Color col, bool isBottom = false)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            RectTransform rt = go.AddComponent<RectTransform>();
            if (isBottom)
            {
                rt.anchorMin = new Vector2(0f, 0f);
                rt.anchorMax = new Vector2(0f, 0f);
                rt.pivot = new Vector2(0f, 0f);
            }
            else
            {
                rt.anchorMin = new Vector2(0f, 1f);
                rt.anchorMax = new Vector2(0f, 1f);
                rt.pivot = new Vector2(0f, 1f);
            }
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            Text txt = go.AddComponent<Text>();
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (txt.font == null) txt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            txt.text = text;
            txt.fontSize = fontSize;
            txt.color = col;
            txt.fontStyle = FontStyle.Bold;
            return txt;
        }

        private static void CreateTabButton(Transform parent, string name, string label, Vector2 pos, int index)
        {
            GameObject btnGO = new GameObject(name);
            btnGO.transform.SetParent(parent, false);
            RectTransform rt = btnGO.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(250f, 32f);

            Image img = btnGO.AddComponent<Image>();
            img.color = new Color(0.08f, 0.16f, 0.22f, 0.9f);

            Button btn = btnGO.AddComponent<Button>();
            ColorBlock cb = btn.colors;
            cb.highlightedColor = new Color(0.12f, 0.45f, 0.65f);
            cb.pressedColor = new Color(0f, 0.8f, 1f);
            btn.colors = cb;

            CreateText(btnGO.transform, "Label", label, 15, new Vector2(10f, -6f), new Vector2(230f, 22f), Color.white);
        }
    }
}
#endif
