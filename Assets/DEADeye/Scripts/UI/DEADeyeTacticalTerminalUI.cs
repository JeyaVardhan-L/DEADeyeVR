using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

namespace DEADeye
{
    /// <summary>
    /// Interactive tactical terminal UI rendered on both the 3D rooftop terminal
    /// and screen HUD, allowing mission selection, intel review, deployment, and debriefing.
    /// Provides fully clickable UI buttons with Input System compatibility and tactile audio feedback.
    /// </summary>
    public class DEADeyeTacticalTerminalUI : MonoBehaviour
    {
        public static DEADeyeTacticalTerminalUI Instance { get; set; }

        [Header("Mission Assets")]
        [SerializeField] private DEADeyeMissionDefinition[] missions;
        [SerializeField] private int selectedMissionIndex = 0;

        [Header("UI Text Displays")]
        [SerializeField] private Text headerOperationText;
        [SerializeField] private Text titleText;
        [SerializeField] private Text districtText;
        [SerializeField] private Text threatText;
        [SerializeField] private Text briefingText;
        [SerializeField] private Text primaryObjText;
        [SerializeField] private Text secondaryObjText;
        [SerializeField] private Text deployPromptText;

        [Header("Panels & Interactive Controls")]
        [SerializeField] private GameObject terminalCanvasRoot;
        [SerializeField] private GameObject debriefPanel;
        [SerializeField] private Text debriefStatusText;
        [SerializeField] private Text debriefStatsText;
        [SerializeField] private Text debriefRankText;
        [SerializeField] private List<Button> missionTabButtons = new List<Button>();
        [SerializeField] private Button deployButton;
        [SerializeField] private Button closeButton;

        private bool isTerminalActive = true;

        public bool IsTerminalActive => isTerminalActive;
        public DEADeyeMissionDefinition SelectedMission => (missions != null && missions.Length > 0 && selectedMissionIndex >= 0 && selectedMissionIndex < missions.Length) 
            ? missions[selectedMissionIndex] 
            : null;

        public static void EnsureEventSystem()
        {
            var es = FindFirstObjectByType<EventSystem>(FindObjectsInactive.Include);
            if (es == null)
            {
                GameObject esGO = new GameObject("EventSystem");
                es = esGO.AddComponent<EventSystem>();
            }

#if ENABLE_INPUT_SYSTEM
            var inputModule = es.GetComponent<InputSystemUIInputModule>();
            if (inputModule == null)
            {
                var legacyModule = es.GetComponent<StandaloneInputModule>();
                if (legacyModule != null)
                {
                    if (Application.isPlaying) Destroy(legacyModule);
                    else DestroyImmediate(legacyModule);
                }
                inputModule = es.gameObject.AddComponent<InputSystemUIInputModule>();
            }
#endif
        }

        public static DEADeyeTacticalTerminalUI EnsureInstance(Transform canvasParent = null)
        {
            if (Instance != null) return Instance;
            var existing = FindFirstObjectByType<DEADeyeTacticalTerminalUI>(FindObjectsInactive.Include);
            if (existing != null)
            {
                Instance = existing;
                return Instance;
            }

            EnsureEventSystem();

            if (canvasParent == null)
            {
                var hud = FindFirstObjectByType<DEADeyeMissionHUD>();
                if (hud != null)
                {
                    Canvas c = hud.GetComponentInChildren<Canvas>();
                    if (c != null) canvasParent = c.transform;
                }
            }

            if (canvasParent == null)
            {
                Canvas c = FindFirstObjectByType<Canvas>();
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
            rt.anchorMin = new Vector2(0.10f, 0.08f);
            rt.anchorMax = new Vector2(0.90f, 0.92f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            Image bg = terminalGO.AddComponent<Image>();
            bg.color = new Color(0.03f, 0.05f, 0.08f, 0.96f);
            bg.raycastTarget = true;

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
            EnsureEventSystem();
            if (terminalCanvasRoot == null) terminalCanvasRoot = gameObject;
        }

        private void Start()
        {
            EnsureEventSystem();

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

                    if (kb.yKey.wasPressedThisFrame || kb.escapeKey.wasPressedThisFrame)
                    {
                        ToggleTerminal();
                    }
                }
                else
                {
                    if (kb.yKey.wasPressedThisFrame || kb.mKey.wasPressedThisFrame)
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
            EnsureEventSystem();

            // Header Title Bar
            headerOperationText = CreateText(parent, "Header", "DEADeye TACTICAL OVERWATCH NETWORK // OPERATION BRIEFING", 22, new Vector2(24f, -14f), new Vector2(750f, 32f), new Color(0f, 0.9f, 1f));

            // Close Button [X] in top-right
            closeButton = CreateButton(parent, "Btn_Close", "[ CLOSE (Y) ]", new Vector2(-24f, -14f), new Vector2(150f, 32f), () => ShowTerminal(false), isTopRight: true);

            // Tab Buttons for Operations [1] to [4]
            missionTabButtons.Clear();
            string[] tabLabels = new string[]
            {
                "1. OP DEAD AIR",
                "2. OP MOVING TARGET",
                "3. OP NO CLEAR SHOT",
                "4. OP DEAD GROUND"
            };

            float tabStartX = 24f;
            float tabWidth = 220f;
            float tabSpacing = 10f;
            for (int i = 0; i < tabLabels.Length; i++)
            {
                int index = i;
                Vector2 tabPos = new Vector2(tabStartX + (i * (tabWidth + tabSpacing)), -54f);
                Button btn = CreateButton(parent, $"Btn_Tab_{i}", tabLabels[i], tabPos, new Vector2(tabWidth, 38f), () => SelectMission(index));
                missionTabButtons.Add(btn);
            }

            // Central Dossier Content Panel
            GameObject contentPanel = new GameObject("DossierContent");
            contentPanel.transform.SetParent(parent, false);
            RectTransform cpRT = contentPanel.AddComponent<RectTransform>();
            cpRT.anchorMin = new Vector2(0.02f, 0.14f);
            cpRT.anchorMax = new Vector2(0.98f, 0.85f);
            cpRT.offsetMin = Vector2.zero;
            cpRT.offsetMax = Vector2.zero;

            Image cpBg = contentPanel.AddComponent<Image>();
            cpBg.color = new Color(0.02f, 0.03f, 0.05f, 0.85f);

            titleText = CreateText(contentPanel.transform, "OpTitle", "OPERATION: DEAD AIR", 26, new Vector2(16f, -12f), new Vector2(850f, 36f), Color.white);
            districtText = CreateText(contentPanel.transform, "Sector", "SECTOR: CENTRAL COMMERCIAL PLAZA & COMMS INTERCHANGE", 17, new Vector2(16f, -48f), new Vector2(850f, 26f), new Color(0.2f, 0.85f, 1f));
            threatText = CreateText(contentPanel.transform, "Threat", "THREAT LEVEL: CRITICAL | OPERATION: THE BLACKOUT PROTOCOL", 16, new Vector2(16f, -76f), new Vector2(850f, 24f), new Color(1f, 0.8f, 0.2f));

            briefingText = CreateText(contentPanel.transform, "Briefing", 
                "A hostile syndicate has deployed an emergency frequency jammer to hijack the city grid. Two friendly technicians (Elena & Marcus) are moving under cover to restore the broadcast. Observe developing events, identify threats, disable the jammer, protect the technicians, eliminate the hostile commander, and secure the civilian evacuation convoy.", 
                16, new Vector2(16f, -108f), new Vector2(1050f, 100f), new Color(0.85f, 0.9f, 0.95f));

            primaryObjText = CreateText(contentPanel.transform, "Primary", "• PRIMARY: Disrupt Jammer, Protect Technicians, Neutralize Commander & Escort Convoy", 17, new Vector2(16f, -220f), new Vector2(950f, 28f), new Color(0.2f, 1f, 0.4f));
            secondaryObjText = CreateText(contentPanel.transform, "Secondary", "• SECONDARY: Complete Recon Spotting & Maintain Zero Civilian Collateral", 16, new Vector2(16f, -252f), new Vector2(950f, 26f), new Color(0.9f, 0.9f, 0.2f));

            // Prominent Clickable Deploy Button at Bottom
            deployButton = CreateButton(parent, "Btn_Deploy", "▶   DEPLOY TO OVERWATCH   ◀", new Vector2(24f, 18f), new Vector2(400f, 52f), DeploySelectedMission, isBottom: true);
            deployPromptText = CreateText(parent, "DeployPrompt", "[ CLICK BUTTON OR PRESS SPACE / ENTER / E TO DEPLOY ]", 15, new Vector2(440f, 32f), new Vector2(600f, 28f), new Color(0.5f, 0.7f, 0.8f), isBottom: true);

            // Debrief Panel
            BuildDebriefPanel(parent);
        }

        private void BuildDebriefPanel(Transform parent)
        {
            debriefPanel = new GameObject("DebriefPanel");
            debriefPanel.transform.SetParent(parent, false);
            RectTransform dbRT = debriefPanel.AddComponent<RectTransform>();
            dbRT.anchorMin = Vector2.zero;
            dbRT.anchorMax = Vector2.one;
            dbRT.offsetMin = Vector2.zero;
            dbRT.offsetMax = Vector2.zero;

            Image dbBg = debriefPanel.AddComponent<Image>();
            dbBg.color = new Color(0.02f, 0.04f, 0.07f, 0.98f);
            dbBg.raycastTarget = true;

            debriefStatusText = CreateText(debriefPanel.transform, "DebriefStatus", "OPERATION ACCOMPLISHED", 36, new Vector2(40f, -40f), new Vector2(800f, 50f), new Color(0.2f, 0.95f, 0.4f));
            debriefRankText = CreateText(debriefPanel.transform, "DebriefRank", "RANK: S", 52, new Vector2(850f, -35f), new Vector2(300f, 65f), Color.yellow);
            debriefStatsText = CreateText(debriefPanel.transform, "DebriefStats", "SCORE: 0\nELIMS: 0\nHEADSHOTS: 0", 22, new Vector2(40f, -120f), new Vector2(800f, 260f), Color.white);

            // Clickable Restart and Return Buttons in Debrief
            CreateButton(debriefPanel.transform, "Btn_DebriefRestart", "↺   RESTART OPERATION", new Vector2(40f, 30f), new Vector2(300f, 48f), RestartCurrentMission, isBottom: true);
            CreateButton(debriefPanel.transform, "Btn_DebriefTerminal", "☰   RETURN TO TERMINAL", new Vector2(360f, 30f), new Vector2(300f, 48f), ReturnToTerminal, isBottom: true);

            debriefPanel.SetActive(false);
        }

        private Button CreateButton(Transform parent, string name, string label, Vector2 pos, Vector2 size, Action onClick, bool isBottom = false, bool isTopRight = false)
        {
            GameObject btnGO = new GameObject(name);
            btnGO.transform.SetParent(parent, false);
            RectTransform rt = btnGO.AddComponent<RectTransform>();

            if (isBottom)
            {
                rt.anchorMin = new Vector2(0f, 0f);
                rt.anchorMax = new Vector2(0f, 0f);
                rt.pivot = new Vector2(0f, 0f);
            }
            else if (isTopRight)
            {
                rt.anchorMin = new Vector2(1f, 1f);
                rt.anchorMax = new Vector2(1f, 1f);
                rt.pivot = new Vector2(1f, 1f);
            }
            else
            {
                rt.anchorMin = new Vector2(0f, 1f);
                rt.anchorMax = new Vector2(0f, 1f);
                rt.pivot = new Vector2(0f, 1f);
            }

            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            Image img = btnGO.AddComponent<Image>();
            img.color = new Color(0.08f, 0.18f, 0.28f, 0.95f);
            img.raycastTarget = true;

            Button btn = btnGO.AddComponent<Button>();
            btn.targetGraphic = img;

            ColorBlock cb = btn.colors;
            cb.normalColor = new Color(0.08f, 0.18f, 0.28f, 0.95f);
            cb.highlightedColor = new Color(0.15f, 0.40f, 0.60f, 1.0f);
            cb.pressedColor = new Color(0.0f, 0.75f, 0.95f, 1.0f);
            cb.selectedColor = new Color(0.12f, 0.32f, 0.50f, 1.0f);
            btn.colors = cb;

            Navigation nav = btn.navigation;
            nav.mode = Navigation.Mode.None;
            btn.navigation = nav;

            GameObject textGO = new GameObject("Label");
            textGO.transform.SetParent(btnGO.transform, false);
            RectTransform trt = textGO.AddComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = Vector2.zero;
            trt.offsetMax = Vector2.zero;

            Text txt = textGO.AddComponent<Text>();
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (txt.font == null) txt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            txt.text = label;
            txt.fontSize = 15;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = Color.white;
            txt.fontStyle = FontStyle.Bold;
            txt.raycastTarget = false; // Prevent child text from intercepting clicks

            btn.onClick.AddListener(() =>
            {
                if (DEADeyeAudioManager.Instance != null)
                {
                    DEADeyeAudioManager.Instance.PlayButtonClick();
                }
                onClick?.Invoke();
            });

            return btn;
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
            txt.raycastTarget = false;
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

        public void RestartCurrentMission()
        {
            if (SelectedMission != null && DEADeyeMissionDirector.Instance != null)
            {
                if (debriefPanel != null) debriefPanel.SetActive(false);
                DEADeyeMissionDirector.Instance.LaunchMission(SelectedMission);
            }
        }

        public void ShowTerminal(bool show)
        {
            isTerminalActive = show;
            if (terminalCanvasRoot != null)
            {
                terminalCanvasRoot.SetActive(show);
            }

            // Ensure cursor is released and visible when terminal is active
            if (show)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }

        public void ToggleTerminal()
        {
            ShowTerminal(!isTerminalActive);
        }

        public void ShowDebrief(bool success, int score, int headshots, float accuracy, int elims, int escapes, int civs)
        {
            ShowDebriefing(success, score, elims, headshots, accuracy, civs, escapes);
        }

        public void ShowDebriefing(bool success, int score, int elims, int headshots, float accuracy, int civs, int escapes)
        {
            ShowTerminal(true);
            if (debriefPanel != null) debriefPanel.SetActive(true);

            if (debriefStatusText != null)
            {
                debriefStatusText.text = success ? "OPERATION ACCOMPLISHED" : "MISSION COMPROMISED";
                debriefStatusText.color = success ? new Color(0.2f, 0.95f, 0.4f) : new Color(0.95f, 0.25f, 0.25f);
            }

            string rank = "C";
            if (success)
            {
                if (accuracy >= 80f && headshots >= 2 && civs == 0) rank = "S";
                else if (accuracy >= 65f && civs == 0) rank = "A";
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
                    $"CIVILIAN CASUALTIES: {civs}\n" +
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
            if (titleText != null) titleText.text = $"OPERATION: {m.operationTitle.ToUpper()}";
            if (districtText != null) districtText.text = $"SECTOR: {m.districtName.ToUpper()}";
            if (threatText != null) threatText.text = $"THREAT LEVEL: {m.threatLevel} | TIME LIMIT: {m.timeLimitSeconds:0}s";
            if (briefingText != null) briefingText.text = m.tacticalBriefing;
            if (primaryObjText != null) primaryObjText.text = $"• PRIMARY: {m.primaryObjective}";
            if (secondaryObjText != null) secondaryObjText.text = $"• SECONDARY: {m.secondaryObjective}";
            if (deployPromptText != null) deployPromptText.text = "[ CLICK DEPLOY OR PRESS SPACE / ENTER / E ]";

            // Highlight active tab button
            for (int i = 0; i < missionTabButtons.Count; i++)
            {
                if (missionTabButtons[i] != null)
                {
                    ColorBlock cb = missionTabButtons[i].colors;
                    if (i == selectedMissionIndex)
                    {
                        cb.normalColor = new Color(0.0f, 0.65f, 0.85f, 1.0f);
                        cb.highlightedColor = new Color(0.2f, 0.85f, 1.0f, 1.0f);
                    }
                    else
                    {
                        cb.normalColor = new Color(0.08f, 0.16f, 0.24f, 0.95f);
                        cb.highlightedColor = new Color(0.14f, 0.28f, 0.40f, 1.0f);
                    }
                    missionTabButtons[i].colors = cb;
                }
            }
        }
    }
}
