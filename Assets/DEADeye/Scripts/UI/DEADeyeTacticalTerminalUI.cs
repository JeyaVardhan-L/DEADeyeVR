using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace DEADeye
{
    /// <summary>
    /// Interactive tactical terminal UI rendered on both the 3D rooftop terminal
    /// and screen HUD, allowing mission selection, intel review, and deployment.
    /// Fully autonomous: auto-constructs its complete UI hierarchy at runtime if needed.
    /// </summary>
    public class DEADeyeTacticalTerminalUI : MonoBehaviour
    {
        public static DEADeyeTacticalTerminalUI Instance { get; set; }

        [Header("Mission Assets")]
        [SerializeField] private DEADeyeMissionDefinition[] missions;
        [SerializeField] private int selectedMissionIndex = 0;

        [Header("UI Text Displays (Auto-bound or configured)")]
        [SerializeField] private Text headerOperationText;
        [SerializeField] private Text titleText;
        [SerializeField] private Text districtText;
        [SerializeField] private Text threatText;
        [SerializeField] private Text briefingText;
        [SerializeField] private Text primaryObjText;
        [SerializeField] private Text secondaryObjText;
        [SerializeField] private Text deployPromptText;

        [Header("Panels")]
        [SerializeField] private GameObject terminalCanvasRoot;
        [SerializeField] private GameObject debriefPanel;
        [SerializeField] private Text debriefStatusText;
        [SerializeField] private Text debriefStatsText;
        [SerializeField] private Text debriefRankText;

        private bool isTerminalActive = true;

        public bool IsTerminalActive => isTerminalActive;
        public DEADeyeMissionDefinition SelectedMission => (missions != null && missions.Length > 0) ? missions[selectedMissionIndex] : null;

        public static DEADeyeTacticalTerminalUI EnsureInstance(Transform canvasParent = null)
        {
            if (Instance != null) return Instance;
            var existing = UnityEngine.Object.FindFirstObjectByType<DEADeyeTacticalTerminalUI>(FindObjectsInactive.Include);
            if (existing != null)
            {
                Instance = existing;
                return Instance;
            }

            if (canvasParent == null)
            {
                var hud = UnityEngine.Object.FindFirstObjectByType<DEADeyeMissionHUD>();
                if (hud != null)
                {
                    Canvas c = hud.GetComponentInChildren<Canvas>();
                    if (c != null) canvasParent = c.transform;
                }
            }

            if (canvasParent == null)
            {
                Canvas c = UnityEngine.Object.FindFirstObjectByType<Canvas>();
                if (c != null) canvasParent = c.transform;
            }

            if (canvasParent == null)
            {
                GameObject cGO = new GameObject("TerminalCanvas");
                Canvas c = cGO.AddComponent<Canvas>();
                c.renderMode = RenderMode.ScreenSpaceOverlay;
                cGO.AddComponent<CanvasScaler>();
                cGO.AddComponent<GraphicRaycaster>();
                canvasParent = cGO.transform;
            }

            GameObject terminalGO = new GameObject("TacticalTerminalPanel");
            terminalGO.transform.SetParent(canvasParent, false);
            RectTransform rt = terminalGO.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.12f, 0.1f);
            rt.anchorMax = new Vector2(0.88f, 0.9f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            Image bg = terminalGO.AddComponent<Image>();
            bg.color = new Color(0.03f, 0.05f, 0.08f, 0.94f);

            Outline outline = terminalGO.AddComponent<Outline>();
            outline.effectColor = new Color(0.0f, 0.8f, 1.0f, 0.7f);
            outline.effectDistance = new Vector2(2f, -2f);

            var termUI = terminalGO.AddComponent<DEADeyeTacticalTerminalUI>();
            termUI.terminalCanvasRoot = terminalGO;
            termUI.LoadMissionAssets();
            termUI.BuildDynamicUIElements(terminalGO.transform);
            termUI.UpdateDisplay();
            Instance = termUI;
            return termUI;
        }

        private void Awake()
        {
            Instance = this;
            if (terminalCanvasRoot == null) terminalCanvasRoot = gameObject;
        }

        private void Start()
        {
            if (missions == null || missions.Length == 0)
            {
                LoadMissionAssets();
            }

            if (headerOperationText == null)
            {
                BuildDynamicUIElements(transform);
            }

            UpdateDisplay();
            ShowTerminal(true);
            if (debriefPanel != null) debriefPanel.SetActive(false);
        }

        private void Update()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb != null)
            {
                if (isTerminalActive)
                {
                    if (kb.digit1Key.wasPressedThisFrame) SelectMission(0);
                    else if (kb.digit2Key.wasPressedThisFrame) SelectMission(1);
                    else if (kb.digit3Key.wasPressedThisFrame) SelectMission(2);
                    else if (kb.digit4Key.wasPressedThisFrame) SelectMission(3);

                    if (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame || kb.eKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame)
                    {
                        DeploySelectedMission();
                    }
                }
                else
                {
                    if (kb.mKey.wasPressedThisFrame)
                    {
                        ToggleTerminal();
                    }
                }
            }
#endif
        }

        public void LoadMissionAssets()
        {
            missions = Resources.LoadAll<DEADeyeMissionDefinition>("Missions");
            if (missions == null || missions.Length == 0)
            {
#if UNITY_EDITOR
                string[] guids = UnityEditor.AssetDatabase.FindAssets("t:DEADeyeMissionDefinition");
                List<DEADeyeMissionDefinition> list = new List<DEADeyeMissionDefinition>();
                foreach (var g in guids)
                {
                    string path = UnityEditor.AssetDatabase.GUIDToAssetPath(g);
                    var m = UnityEditor.AssetDatabase.LoadAssetAtPath<DEADeyeMissionDefinition>(path);
                    if (m != null) list.Add(m);
                }
                missions = list.ToArray();
#endif
            }
        }

        public void BuildDynamicUIElements(Transform parent)
        {
            // Header
            headerOperationText = CreateText(parent, "Header", "DEADeye TACTICAL OVERWATCH NETWORK // MISSION SELECT", 24, new Vector2(24f, -16f), new Vector2(700f, 36f), new Color(0f, 0.9f, 1f));

            // Content Panel
            GameObject contentPanel = new GameObject("DossierContent");
            contentPanel.transform.SetParent(parent, false);
            RectTransform cpRT = contentPanel.AddComponent<RectTransform>();
            cpRT.anchorMin = new Vector2(0.03f, 0.15f);
            cpRT.anchorMax = new Vector2(0.97f, 0.82f);
            cpRT.offsetMin = Vector2.zero;
            cpRT.offsetMax = Vector2.zero;

            titleText = CreateText(contentPanel.transform, "OpTitle", "OPERATION: BROKEN SIGNAL", 26, new Vector2(10f, -10f), new Vector2(800f, 36f), Color.white);
            districtText = CreateText(contentPanel.transform, "Sector", "SECTOR: COMMERCIAL DISTRICT & COMMUNICATIONS PLAZA", 18, new Vector2(10f, -48f), new Vector2(800f, 28f), new Color(0.2f, 0.85f, 1f));
            threatText = CreateText(contentPanel.transform, "Threat", "THREAT LEVEL: HIGH | TIME: 120s", 16, new Vector2(10f, -78f), new Vector2(800f, 26f), new Color(1f, 0.8f, 0.2f));

            briefingText = CreateText(contentPanel.transform, "Briefing", 
                "Provide precision sniper overwatch from the rooftop perch. Protect friendly assets and eliminate all advancing hostile elements.", 
                16, new Vector2(10f, -115f), new Vector2(1000f, 110f), new Color(0.85f, 0.9f, 0.95f));

            primaryObjText = CreateText(contentPanel.transform, "Primary", "• PRIMARY: Protect Comms Technicians & Eliminate Hostiles", 18, new Vector2(10f, -235f), new Vector2(900f, 30f), new Color(0.2f, 1f, 0.4f));
            secondaryObjText = CreateText(contentPanel.transform, "Secondary", "• SECONDARY: Achieve at least 2 Headshots", 16, new Vector2(10f, -270f), new Vector2(900f, 28f), new Color(0.9f, 0.9f, 0.2f));

            deployPromptText = CreateText(parent, "DeployPrompt", "[ PRESS ENTER / SPACE / E / TRIGGER TO DEPLOY TO OVERWATCH ]", 20, new Vector2(24f, 20f), new Vector2(1100f, 36f), new Color(0f, 1f, 0.6f), isBottom: true);

            // Debrief Panel
            debriefPanel = new GameObject("DebriefPanel");
            debriefPanel.transform.SetParent(parent, false);
            RectTransform dbRT = debriefPanel.AddComponent<RectTransform>();
            dbRT.anchorMin = Vector2.zero;
            dbRT.anchorMax = Vector2.one;
            dbRT.offsetMin = Vector2.zero;
            dbRT.offsetMax = Vector2.zero;

            Image dbBg = debriefPanel.AddComponent<Image>();
            dbBg.color = new Color(0.02f, 0.04f, 0.06f, 0.97f);

            debriefStatusText = CreateText(debriefPanel.transform, "DebriefStatus", "OPERATION ACCOMPLISHED", 36, new Vector2(40f, -40f), new Vector2(800f, 50f), new Color(0.2f, 0.95f, 0.4f));
            debriefRankText = CreateText(debriefPanel.transform, "DebriefRank", "RANK: S", 48, new Vector2(850f, -35f), new Vector2(300f, 60f), Color.yellow);
            debriefStatsText = CreateText(debriefPanel.transform, "DebriefStats", "SCORE: 0\nELIMS: 0\nHEADSHOTS: 0", 22, new Vector2(40f, -120f), new Vector2(800f, 260f), Color.white);

            debriefPanel.SetActive(false);
        }

        private Text CreateText(Transform parent, string name, string text, int fontSize, Vector2 pos, Vector2 size, Color col, bool isBottom = false)
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

        public void SetMissions(DEADeyeMissionDefinition[] defs)
        {
            missions = defs;
            selectedMissionIndex = 0;
            UpdateDisplay();
        }

        public void SelectMission(int index)
        {
            if (missions == null || missions.Length == 0) return;
            selectedMissionIndex = Mathf.Clamp(index, 0, missions.Length - 1);
            UpdateDisplay();

            if (DEADeyeAudioManager.Instance != null)
            {
                DEADeyeAudioManager.Instance.PlayButtonClick();
            }
        }

        public void NextMission()
        {
            if (missions == null || missions.Length == 0) return;
            SelectMission((selectedMissionIndex + 1) % missions.Length);
        }

        public void PreviousMission()
        {
            if (missions == null || missions.Length == 0) return;
            SelectMission((selectedMissionIndex - 1 + missions.Length) % missions.Length);
        }

        public void DeploySelectedMission()
        {
            if (SelectedMission == null) return;

            ShowTerminal(false);
            if (debriefPanel != null) debriefPanel.SetActive(false);

            if (DEADeyeAudioManager.Instance != null)
            {
                DEADeyeAudioManager.Instance.PlayMissionStart();
            }

            if (DEADeyeMissionDirector.Instance != null)
            {
                DEADeyeMissionDirector.Instance.LaunchMission(SelectedMission);
            }
        }

        public void ShowTerminal(bool show)
        {
            isTerminalActive = show;
            if (terminalCanvasRoot != null) terminalCanvasRoot.SetActive(show);
        }

        public void ToggleTerminal()
        {
            ShowTerminal(!isTerminalActive);
        }

        public void ShowDebrief(bool success, int score, int headshots, float accuracy, int elims, int escapes, int civs)
        {
            isTerminalActive = false;
            if (terminalCanvasRoot != null) terminalCanvasRoot.SetActive(true);
            if (debriefPanel != null) debriefPanel.SetActive(true);

            if (debriefStatusText != null)
            {
                debriefStatusText.text = success ? "OPERATION ACCOMPLISHED" : "MISSION COMPROMISED";
                debriefStatusText.color = success ? new Color(0.2f, 0.95f, 0.4f) : new Color(0.95f, 0.25f, 0.25f);
            }

            string rank = "C";
            if (success)
            {
                if (accuracy >= 85f && headshots >= 2 && civs == 0) rank = "S";
                else if (accuracy >= 70f && civs == 0) rank = "A";
                else if (civs == 0) rank = "B";
                else rank = "C";
            }
            else
            {
                rank = "F";
            }

            if (debriefRankText != null)
            {
                debriefRankText.text = $"RANK: {rank}";
                debriefRankText.color = rank == "S" ? Color.yellow : (rank == "A" ? Color.green : Color.white);
            }

            if (debriefStatsText != null)
            {
                debriefStatsText.text = 
                    $"FINAL SCORE: {score:N0}\n" +
                    $"TARGETS NEUTRALIZED: {elims}\n" +
                    $"PRECISION HEADSHOTS: {headshots}\n" +
                    $"ACCURACY RATING: {accuracy:F1}%\n" +
                    $"CIVILIAN COLLATERAL: {civs}\n" +
                    $"TARGETS ESCAPED: {escapes}";
            }
        }

        public void ReturnToTerminal()
        {
            if (debriefPanel != null) debriefPanel.SetActive(false);
            ShowTerminal(true);
            UpdateDisplay();
        }

        private void UpdateDisplay()
        {
            var m = SelectedMission;
            if (m == null) return;

            if (headerOperationText != null) headerOperationText.text = $"TACTICAL TERMINAL // {m.operationCode}";
            if (titleText != null) titleText.text = m.operationTitle;
            if (districtText != null) districtText.text = $"SECTOR: {m.districtName}";
            if (threatText != null) threatText.text = $"THREAT LEVEL: {m.threatLevel} | TIME: {m.timeLimitSeconds:0}s";
            if (briefingText != null) briefingText.text = m.tacticalBriefing;
            if (primaryObjText != null) primaryObjText.text = $"• PRIMARY: {m.primaryObjective}";
            if (secondaryObjText != null) secondaryObjText.text = $"• SECONDARY: {m.secondaryObjective}";
            if (deployPromptText != null) deployPromptText.text = "[ PRESS ENTER / E / TRIGGER TO DEPLOY ]";
        }
    }
}
