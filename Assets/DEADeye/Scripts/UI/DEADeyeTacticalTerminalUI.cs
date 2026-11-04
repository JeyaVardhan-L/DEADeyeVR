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
    /// Full-screen high-contrast Tactical Mission Selection Hub & Briefing Terminal.
    /// Provides 4 distinct Mission Cards:
    /// - Op 01: DEAD AIR (Available / Playable)
    /// - Op 02: THE LAST STOP (Locked)
    /// - Op 03: SILENT ENTRY (Locked)
    /// - Op 04: DEAD GROUND (Locked)
    /// Displays story background, key personnel dossier cards, sequential objective flow,
    /// and a prominent "DEPLOY INTO MISSION" button that launches the active operation.
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
        public DEADeyeMissionDefinition SelectedMission
        {
            get
            {
                if (missions != null && missions.Length > 0 && selectedMissionIndex >= 0 && selectedMissionIndex < missions.Length)
                {
                    return missions[selectedMissionIndex];
                }
                if (DEADeyeMissionDirector.Instance != null && DEADeyeMissionDirector.Instance.ActiveMission != null)
                {
                    return DEADeyeMissionDirector.Instance.ActiveMission;
                }
                return null;
            }
        }

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
                c.sortingOrder = 50;
                CanvasScaler cs = cGO.AddComponent<CanvasScaler>();
                cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                cs.referenceResolution = new Vector2(1920, 1080);
                cs.matchWidthOrHeight = 0.5f;
                cGO.AddComponent<GraphicRaycaster>();
                canvasParent = cGO.transform;
            }

            GameObject terminalGO = new GameObject("TacticalTerminalPanel");
            terminalGO.transform.SetParent(canvasParent, false);
            RectTransform rt = terminalGO.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            Image bg = terminalGO.AddComponent<Image>();
            bg.color = new Color(0.02f, 0.04f, 0.07f, 0.98f);
            bg.raycastTarget = true;

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

            if (DEADeyeGameManager.Instance != null)
            {
                DEADeyeGameManager.Instance.SetGameState(DEADeyeGameState.MissionBriefing);
            }
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

                    if (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame || kb.eKey.wasPressedThisFrame)
                    {
                        DeploySelectedMission();
                    }

                    if (kb.escapeKey.wasPressedThisFrame || kb.yKey.wasPressedThisFrame)
                    {
                        ToggleTerminal();
                    }
                }
                else
                {
                    if (kb.yKey.wasPressedThisFrame || kb.tKey.wasPressedThisFrame)
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
                list.Sort((a, b) => string.Compare(a.operationCode, b.operationCode, StringComparison.Ordinal));
                missions = list.ToArray();
#endif
            }
        }

        public void BuildDynamicUIElements(Transform parent)
        {
            EnsureEventSystem();

            List<GameObject> toDestroy = new List<GameObject>();
            foreach (Transform child in parent)
            {
                toDestroy.Add(child.gameObject);
            }
            foreach (var go in toDestroy)
            {
                if (Application.isPlaying) Destroy(go);
                else DestroyImmediate(go);
            }

            // =========================================================================
            // TOP HEADER BAR (Y: 92% to 100%)
            // =========================================================================
            GameObject topBar = new GameObject("TopHeaderBar");
            topBar.transform.SetParent(parent, false);
            RectTransform tbRT = topBar.AddComponent<RectTransform>();
            tbRT.anchorMin = new Vector2(0f, 0.92f);
            tbRT.anchorMax = new Vector2(1f, 1f);
            tbRT.offsetMin = Vector2.zero;
            tbRT.offsetMax = Vector2.zero;

            Image tbBg = topBar.AddComponent<Image>();
            tbBg.color = new Color(0.04f, 0.08f, 0.14f, 0.95f);

            headerOperationText = CreateText(topBar.transform, "Header", "DEADEYE OPERATIONS // CLASSIFIED MISSION SELECTION HUB", 22, new Vector2(30f, -12f), new Vector2(800f, 32f), new Color(0f, 0.9f, 1f));
            CreateText(topBar.transform, "HeaderSub", "SELECT OPERATION [1-4] TO INSPECT TACTICAL INTEL & DEPLOY", 15, new Vector2(30f, -44f), new Vector2(800f, 24f), new Color(0.3f, 0.75f, 0.9f));

            // Mission Selection Tabs (OP-01 to OP-04)
            missionTabButtons.Clear();
            string[] tabLabels = new string[] { "[1] OP-01: DEAD AIR", "[2] OP-02: THE LAST STOP", "[3] OP-03: SILENT ENTRY", "[4] OP-04: DEAD GROUND" };
            float tabStartX = 780f;
            float tabWidth = 220f;
            for (int i = 0; i < tabLabels.Length; i++)
            {
                int index = i;
                Button tabBtn = CreateButton(topBar.transform, $"Tab_{i}", tabLabels[i], new Vector2(tabStartX + (i * (tabWidth + 8f)), -16f), new Vector2(tabWidth, 42f), () => SelectMission(index));
                missionTabButtons.Add(tabBtn);
            }

            // Close button top-right
            closeButton = CreateButton(topBar.transform, "Btn_Close", "[ X ]", new Vector2(-30f, -16f), new Vector2(70f, 42f), () => ShowTerminal(false), isTopRight: true);

            // =========================================================================
            // LEFT COLUMN: THE STORY / BACKGROUND (X: 2% to 49%, Y: 32% to 90%)
            // =========================================================================
            GameObject leftCol = new GameObject("LeftColumn_Story");
            leftCol.transform.SetParent(parent, false);
            RectTransform lcRT = leftCol.AddComponent<RectTransform>();
            lcRT.anchorMin = new Vector2(0.02f, 0.32f);
            lcRT.anchorMax = new Vector2(0.49f, 0.90f);
            lcRT.offsetMin = Vector2.zero;
            lcRT.offsetMax = Vector2.zero;

            Image lcBg = leftCol.AddComponent<Image>();
            lcBg.color = new Color(0.03f, 0.06f, 0.10f, 0.88f);
            Outline lcOutline = leftCol.AddComponent<Outline>();
            lcOutline.effectColor = new Color(0.0f, 0.75f, 0.95f, 0.5f);
            lcOutline.effectDistance = new Vector2(1.5f, -1.5f);

            CreateText(leftCol.transform, "StoryHeader", "THE SITUATION & MISSION BACKGROUND", 20, new Vector2(20f, -16f), new Vector2(600f, 28f), new Color(0.0f, 0.9f, 1.0f));

            titleText = CreateText(leftCol.transform, "OpTitle", "OPERATION 01: DEAD AIR", 24, new Vector2(20f, -50f), new Vector2(700f, 32f), Color.white);
            districtText = CreateText(leftCol.transform, "Sector", "SECTOR: CENTRAL COMMERCIAL PLAZA & COMMS INTERCHANGE", 15, new Vector2(20f, -84f), new Vector2(750f, 24f), new Color(0.2f, 0.85f, 1f));
            threatText = CreateText(leftCol.transform, "Threat", "STATUS: AVAILABLE | DIFFICULTY: MODERATE | TIME: 120s", 15, new Vector2(20f, -110f), new Vector2(750f, 24f), new Color(1f, 0.8f, 0.2f));

            briefingText = CreateText(leftCol.transform, "StoryBody",
                "A hostile syndicate strike team has deployed an emergency frequency jammer to hijack the municipal communication grid, plunging the entire metropolitan district into a radio blackout.\n\n" +
                "You are positioned in high-angle rooftop overwatch above the Central Commercial Plaza. Two unarmed civilian field technicians—Elena and Marcus—have slipped into the plaza under cover to access the municipal broadcast relay tower console and restore emergency transmission.\n\n" +
                "Your role is absolute precision overwatch:\n" +
                "• Surveil the plaza and identify all tactical assets.\n" +
                "• Cut power to the syndicate jammer by sniping its high-voltage power junction.\n" +
                "• Shield Elena and Marcus from advancing hostile assault squads while they work.\n" +
                "• Intercept and neutralize Commander Vance before his armored exfil departs.\n" +
                "• Cover the team's extraction across the boulevard to the transit terminal.",
                15, new Vector2(20f, -145f), new Vector2(850f, 340f), new Color(0.88f, 0.92f, 0.98f));

            // =========================================================================
            // RIGHT COLUMN: PEOPLE INVOLVED / TARGET PROFILES (X: 51% to 98%, Y: 32% to 90%)
            // =========================================================================
            GameObject rightCol = new GameObject("RightColumn_Personnel");
            rightCol.transform.SetParent(parent, false);
            RectTransform rcRT = rightCol.AddComponent<RectTransform>();
            rcRT.anchorMin = new Vector2(0.51f, 0.32f);
            rcRT.anchorMax = new Vector2(0.98f, 0.90f);
            rcRT.offsetMin = Vector2.zero;
            rcRT.offsetMax = Vector2.zero;

            Image rcBg = rightCol.AddComponent<Image>();
            rcBg.color = new Color(0.03f, 0.06f, 0.10f, 0.88f);
            Outline rcOutline = rightCol.AddComponent<Outline>();
            rcOutline.effectColor = new Color(0.0f, 0.75f, 0.95f, 0.5f);
            rcOutline.effectDistance = new Vector2(1.5f, -1.5f);

            CreateText(rightCol.transform, "PersonnelHeader", "KEY PERSONNEL & TARGET PROFILES", 20, new Vector2(20f, -16f), new Vector2(600f, 28f), new Color(0.0f, 0.9f, 1.0f));

            // Card 1: Elena
            BuildPersonnelCard(rightCol.transform, "Card_Elena", 
                "ELENA // LEAD COMMS ENGINEER", 
                "FRIENDLY // MUST PROTECT", 
                new Color(0.0f, 0.95f, 0.4f), 
                new Color(0.02f, 0.20f, 0.08f, 0.9f),
                "Civilian field outfit with tool harness. She is advancing across the plaza to access the municipal relay tower console and restore the emergency broadcast array.",
                new Vector2(20f, -54f), new Vector2(840f, 120f));

            // Card 2: Marcus
            BuildPersonnelCard(rightCol.transform, "Card_Marcus", 
                "MARCUS // ASSISTANT FIELD TECHNICIAN", 
                "FRIENDLY // MUST PROTECT", 
                new Color(0.0f, 0.95f, 0.4f), 
                new Color(0.02f, 0.20f, 0.08f, 0.9f),
                "Civilian field attire with cable spool. Assisting Elena with ground cable conduits and power connections. If either technician is eliminated, the broadcast fails.",
                new Vector2(20f, -186f), new Vector2(840f, 120f));

            // Card 3: Commander Vance
            BuildPersonnelCard(rightCol.transform, "Card_Vance", 
                "COMMANDER VANCE // SYNDICATE STRIKE LEADER", 
                "HOSTILE // MUST ELIMINATE", 
                new Color(1.0f, 0.25f, 0.25f), 
                new Color(0.25f, 0.03f, 0.03f, 0.9f),
                "Heavily armored tactical plate carrier, radio headset, sidearm. Coordinating the jammer deployment from the north perimeter and staging to escape via convoy.",
                new Vector2(20f, -318f), new Vector2(840f, 120f));

            CreateText(rightCol.transform, "ROEText", 
                "RULES OF ENGAGEMENT: Zero civilian casualties permitted. Protect Elena & Marcus at all costs.", 
                14, new Vector2(20f, -450f), new Vector2(840f, 26f), new Color(1.0f, 0.85f, 0.2f));

            // =========================================================================
            // BOTTOM AREA: 4 OPERATION CARDS SUMMARY (X: 2% to 98%, Y: 11% to 30%)
            // =========================================================================
            GameObject botArea = new GameObject("BottomArea_Operations");
            botArea.transform.SetParent(parent, false);
            RectTransform baRT = botArea.AddComponent<RectTransform>();
            baRT.anchorMin = new Vector2(0.02f, 0.11f);
            baRT.anchorMax = new Vector2(0.98f, 0.30f);
            baRT.offsetMin = Vector2.zero;
            baRT.offsetMax = Vector2.zero;

            Image baBg = botArea.AddComponent<Image>();
            baBg.color = new Color(0.03f, 0.05f, 0.08f, 0.88f);
            Outline baOutline = botArea.AddComponent<Outline>();
            baOutline.effectColor = new Color(0.0f, 0.75f, 0.95f, 0.5f);
            baOutline.effectDistance = new Vector2(1.5f, -1.5f);

            CreateText(botArea.transform, "ObjHeader", "CAMPAIGN OPERATIONS HUB // SELECT DESTINATION DISTRICT", 18, new Vector2(20f, -10f), new Vector2(600f, 24f), new Color(0.0f, 0.9f, 1.0f));

            float cardW = 420f;
            float cardSpacing = 16f;
            float startX = 20f;

            string[,] opCards = new string[,]
            {
                { "OP-01: DEAD AIR", "CENTRAL COMMERCIAL PLAZA\nStatus: AVAILABLE\nDifficulty: Moderate\nObjective: Protect 2 Techs & Destroy Jammer" },
                { "OP-02: THE LAST STOP", "TRANSPORT INTERCHANGE DEPOT\nStatus: LOCKED [REQ OP-01]\nDifficulty: Hard\nObjective: Intercept Armored Convoy" },
                { "OP-03: SILENT ENTRY", "RESIDENTIAL BROWNSTONES\nStatus: LOCKED [REQ OP-02]\nDifficulty: Expert\nObjective: Clear Fortified Alleyways (Zero Civ)" },
                { "OP-04: DEAD GROUND", "CONSTRUCTION SITE & CRANE\nStatus: LOCKED [REQ OP-03]\nDifficulty: Maximum\nObjective: Counter-Snipe 28m Tower Crane" }
            };

            for (int i = 0; i < 4; i++)
            {
                int index = i;
                Vector2 pos = new Vector2(startX + (i * (cardW + cardSpacing)), -40f);
                BuildOperationCard(botArea.transform, $"OpCard_{i + 1}", opCards[i, 0], opCards[i, 1], pos, new Vector2(cardW, 140f), i == 0, () => SelectMission(index));
            }

            // =========================================================================
            // FOOTER BAR: DEPLOY BUTTON & PROMPT (Y: 0 to 10%)
            // =========================================================================
            GameObject footerBar = new GameObject("FooterBar");
            footerBar.transform.SetParent(parent, false);
            RectTransform fbRT = footerBar.AddComponent<RectTransform>();
            fbRT.anchorMin = new Vector2(0f, 0f);
            fbRT.anchorMax = new Vector2(1f, 0.10f);
            fbRT.offsetMin = Vector2.zero;
            fbRT.offsetMax = Vector2.zero;

            Image fbBg = footerBar.AddComponent<Image>();
            fbBg.color = new Color(0.02f, 0.04f, 0.07f, 0.95f);

            deployButton = CreateButton(footerBar.transform, "Btn_Deploy", "▶   DEPLOY INTO MISSION   ◀", new Vector2(710f, 14f), new Vector2(500f, 58f), DeploySelectedMission, isBottom: true);
            deployButton.GetComponentInChildren<Text>().fontSize = 20;

            ColorBlock dcb = deployButton.colors;
            dcb.normalColor = new Color(0.0f, 0.55f, 0.85f, 1.0f);
            dcb.highlightedColor = new Color(0.0f, 0.75f, 1.0f, 1.0f);
            dcb.pressedColor = new Color(0.0f, 0.95f, 0.5f, 1.0f);
            deployButton.colors = dcb;

            deployPromptText = CreateText(footerBar.transform, "DeployPrompt", "[ CLICK DEPLOY OR PRESS SPACE / ENTER / E ]", 15, new Vector2(740f, 76f), new Vector2(440f, 22f), new Color(0.4f, 0.8f, 1.0f), isBottom: true);
            deployPromptText.alignment = TextAnchor.MiddleCenter;

            // Debrief Panel
            BuildDebriefPanel(parent);
        }

        private void BuildPersonnelCard(Transform parent, string name, string personName, string badgeText, Color badgeCol, Color badgeBgCol, string desc, Vector2 pos, Vector2 size)
        {
            GameObject cardGO = new GameObject(name);
            cardGO.transform.SetParent(parent, false);
            RectTransform rt = cardGO.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            Image bg = cardGO.AddComponent<Image>();
            bg.color = new Color(0.04f, 0.08f, 0.12f, 0.85f);
            Outline ol = cardGO.AddComponent<Outline>();
            ol.effectColor = badgeCol * 0.6f;
            ol.effectDistance = new Vector2(1f, -1f);

            CreateText(cardGO.transform, "Name", personName, 17, new Vector2(14f, -10f), new Vector2(480f, 24f), Color.white);

            GameObject badgeGO = new GameObject("Badge");
            badgeGO.transform.SetParent(cardGO.transform, false);
            RectTransform bRT = badgeGO.AddComponent<RectTransform>();
            bRT.anchorMin = new Vector2(1f, 1f);
            bRT.anchorMax = new Vector2(1f, 1f);
            bRT.pivot = new Vector2(1f, 1f);
            bRT.anchoredPosition = new Vector2(-14f, -10f);
            bRT.sizeDelta = new Vector2(250f, 26f);

            Image bImg = badgeGO.AddComponent<Image>();
            bImg.color = badgeBgCol;

            GameObject badgeTextGO = new GameObject("BadgeText");
            badgeTextGO.transform.SetParent(badgeGO.transform, false);
            RectTransform btRT = badgeTextGO.AddComponent<RectTransform>();
            btRT.anchorMin = Vector2.zero;
            btRT.anchorMax = Vector2.one;
            btRT.offsetMin = Vector2.zero;
            btRT.offsetMax = Vector2.zero;

            Text bTxt = badgeTextGO.AddComponent<Text>();
            bTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (bTxt.font == null) bTxt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            bTxt.text = badgeText;
            bTxt.fontSize = 13;
            bTxt.alignment = TextAnchor.MiddleCenter;
            bTxt.color = badgeCol;
            bTxt.fontStyle = FontStyle.Bold;

            CreateText(cardGO.transform, "Desc", desc, 13, new Vector2(14f, -44f), new Vector2(810f, 65f), new Color(0.85f, 0.88f, 0.92f));
        }

        private void BuildOperationCard(Transform parent, string name, string title, string content, Vector2 pos, Vector2 size, bool isAvailable, Action onClick)
        {
            GameObject cardGO = new GameObject(name);
            cardGO.transform.SetParent(parent, false);
            RectTransform rt = cardGO.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            Image bg = cardGO.AddComponent<Image>();
            bg.color = isAvailable ? new Color(0.06f, 0.12f, 0.18f, 0.95f) : new Color(0.03f, 0.05f, 0.07f, 0.80f);

            Outline ol = cardGO.AddComponent<Outline>();
            ol.effectColor = isAvailable ? new Color(0.0f, 0.85f, 1.0f, 0.7f) : new Color(0.3f, 0.35f, 0.4f, 0.4f);
            ol.effectDistance = new Vector2(1f, -1f);

            Button btn = cardGO.AddComponent<Button>();
            btn.targetGraphic = bg;
            btn.onClick.AddListener(() =>
            {
                if (DEADeyeAudioManager.Instance != null)
                {
                    DEADeyeAudioManager.Instance.PlayButtonClick();
                }
                onClick?.Invoke();
            });

            CreateText(cardGO.transform, "Title", title, 16, new Vector2(12f, -8f), new Vector2(size.x - 24f, 22f), isAvailable ? new Color(0f, 0.95f, 1f) : new Color(0.55f, 0.6f, 0.65f));
            CreateText(cardGO.transform, "Content", content, 13, new Vector2(12f, -34f), new Vector2(size.x - 24f, 95f), isAvailable ? Color.white : new Color(0.5f, 0.55f, 0.6f));
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

            debriefStatusText = CreateText(debriefPanel.transform, "DebriefStatus", "OPERATION ACCOMPLISHED", 36, new Vector2(60f, -60f), new Vector2(900f, 50f), new Color(0.2f, 0.95f, 0.4f));
            debriefRankText = CreateText(debriefPanel.transform, "DebriefRank", "RANK: S", 56, new Vector2(1400f, -50f), new Vector2(400f, 70f), Color.yellow);
            debriefStatsText = CreateText(debriefPanel.transform, "DebriefStats", "SCORE: 0\nELIMS: 0\nHEADSHOTS: 0", 24, new Vector2(60f, -160f), new Vector2(1000f, 320f), Color.white);

            CreateButton(debriefPanel.transform, "Btn_DebriefRestart", "↺   RESTART OPERATION", new Vector2(60f, 40f), new Vector2(340f, 56f), RestartCurrentMission, isBottom: true);
            CreateButton(debriefPanel.transform, "Btn_DebriefTerminal", "☰   RETURN TO TERMINAL", new Vector2(430f, 40f), new Vector2(340f, 56f), ReturnToTerminal, isBottom: true);

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
            txt.raycastTarget = false;

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

        public void UpdateDisplay()
        {
            if (SelectedMission == null) return;

            if (titleText != null)
            {
                titleText.text = $"OPERATION {SelectedMission.operationCode}: {SelectedMission.operationTitle}";
            }
            if (districtText != null)
            {
                districtText.text = $"SECTOR: {SelectedMission.districtName}";
            }
            if (threatText != null)
            {
                string statusCol = SelectedMission.isLocked ? "<color=red>[ LOCKED ]</color>" : "<color=#00FF99>[ AVAILABLE ]</color>";
                threatText.text = $"STATUS: {statusCol} | THREAT: {SelectedMission.threatLevel} | TIME: {SelectedMission.timeLimitSeconds:F0}s";
            }
            if (briefingText != null)
            {
                briefingText.text = SelectedMission.tacticalBriefing;
            }

            if (deployButton != null)
            {
                Text btnText = deployButton.GetComponentInChildren<Text>();
                if (SelectedMission.isLocked)
                {
                    deployButton.interactable = false;
                    if (btnText != null) btnText.text = "OPERATION LOCKED [COMPLETE PRIOR OP]";
                    if (deployPromptText != null) deployPromptText.text = "<color=#FF5555>[ COMPLETE PRIOR OPERATION TO UNLOCK ]</color>";
                }
                else
                {
                    deployButton.interactable = true;
                    if (btnText != null) btnText.text = "▶   DEPLOY INTO MISSION   ◀";
                    if (deployPromptText != null) deployPromptText.text = "[ CLICK DEPLOY OR PRESS SPACE / ENTER / E ]";
                }
            }
        }

        public void DeploySelectedMission()
        {
            if (SelectedMission == null || SelectedMission.isLocked) return;

            ShowTerminal(false);
            if (debriefPanel != null) debriefPanel.SetActive(false);

            if (DEADeyeAudioManager.Instance != null)
            {
                DEADeyeAudioManager.Instance.PlayMissionStart();
            }

            if (DEADeyeGameManager.Instance != null)
            {
                DEADeyeGameManager.Instance.SetGameState(DEADeyeGameState.MissionActive);
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

        public void ReturnToTerminal()
        {
            if (debriefPanel != null) debriefPanel.SetActive(false);
            ShowTerminal(true);
        }

        public void ShowTerminal(bool show)
        {
            isTerminalActive = show;
            if (terminalCanvasRoot != null)
            {
                terminalCanvasRoot.SetActive(show);
            }

            if (show)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
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
    }
}
