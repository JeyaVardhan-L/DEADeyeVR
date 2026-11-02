using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace DEADeye
{
    /// <summary>
    /// Tactical operations briefing menu for the Dojo Lobby.
    /// Manages mission briefing, difficulty configuration ([RECRUIT], [OPERATIVE], [ELITE]),
    /// weapon loadout preview, and mission deployment.
    /// </summary>
    public class DEADeyeLobbyUI : MonoBehaviour
    {
        public static DEADeyeLobbyUI Instance { get; private set; }

        [Header("Canvas & Hierarchy")]
        [SerializeField] private Canvas lobbyCanvas;
        [SerializeField] private GraphicRaycaster graphicRaycaster;

        [Header("Buttons & Interactive Elements")]
        [SerializeField] private Button deployButton;
        [SerializeField] private Button recruitButton;
        [SerializeField] private Button operativeButton;
        [SerializeField] private Button eliteButton;
        [SerializeField] private Button exitButton;

        [Header("Text Displays")]
        [SerializeField] private TextMeshProUGUI briefingTitleText;
        [SerializeField] private TextMeshProUGUI briefingBodyText;
        [SerializeField] private TextMeshProUGUI difficultyDescriptionText;
        [SerializeField] private TextMeshProUGUI loadoutDescriptionText;

        [Header("Panels")]
        [SerializeField] private GameObject mainPanel;
        [SerializeField] private GameObject deployingOverlay;

        private void Awake()
        {
            Instance = this;
            EnsureCanvasAndEventSystem();
        }

        private void Start()
        {
            if (DEADeyeGameManager.Instance != null)
            {
                DEADeyeGameManager.Instance.SetGameState(DEADeyeGameState.Lobby);
            }

            UpdateDifficultyUI();
            UpdateLoadoutUI();
        }

        private void EnsureCanvasAndEventSystem()
        {
            // Ensure EventSystem
            if (FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                GameObject esGO = new GameObject("EventSystem");
                esGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
                esGO.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            }

            if (lobbyCanvas == null)
            {
                lobbyCanvas = GetComponent<Canvas>();
            }

            if (lobbyCanvas == null)
            {
                BuildTacticalUI();
            }
        }

        public void BuildTacticalUI()
        {
            GameObject canvasGO = new GameObject("LobbyCanvas");
            canvasGO.transform.SetParent(transform, false);

            lobbyCanvas = canvasGO.AddComponent<Canvas>();
            lobbyCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            lobbyCanvas.sortingOrder = 50;

            CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            graphicRaycaster = canvasGO.AddComponent<GraphicRaycaster>();

            // Main Briefing Panel (Left Side of Screen)
            mainPanel = new GameObject("BriefingPanel");
            mainPanel.transform.SetParent(canvasGO.transform, false);
            RectTransform panelRT = mainPanel.AddComponent<RectTransform>();
            panelRT.anchorMin = new Vector2(0.05f, 0.08f);
            panelRT.anchorMax = new Vector2(0.48f, 0.92f);
            panelRT.offsetMin = Vector2.zero;
            panelRT.offsetMax = Vector2.zero;

            Image panelBg = mainPanel.AddComponent<Image>();
            panelBg.color = new Color(0.08f, 0.10f, 0.13f, 0.94f); // Charcoal dark slate

            Outline outline = mainPanel.AddComponent<Outline>();
            outline.effectColor = new Color(0.18f, 0.75f, 0.92f, 0.75f);
            outline.effectDistance = new Vector2(2f, -2f);

            // Title
            briefingTitleText = CreateText(mainPanel.transform, "Title", "DEADeye VR — TACTICAL OPERATIONS", 26, TextAlignmentOptions.TopLeft,
                new Vector2(25f, -25f), new Vector2(500f, 40f), new Color(0.18f, 0.85f, 0.98f));

            // Mission Objective
            briefingBodyText = CreateText(mainPanel.transform, "BriefingBody",
                "<b>MISSION:</b> High-Rise Rooftop Overwatch\n" +
                "<b>LOCATION:</b> Metropolitan Central District\n" +
                "<b>INTEL:</b> Hostile syndicates and rogue entities are infiltrating city transit corridors.\n" +
                "<b>RULES OF ENGAGEMENT:</b>\n" +
                "• Occupy the high-rise sniper perch.\n" +
                "• Identify hostiles by overhead red indicators.\n" +
                "• Eliminate targets before extraction vehicles arrive.\n" +
                "• Headshots provide 3x score multiplier and instant takedown.\n" +
                "• Do NOT allow more than 3 hostiles to escape.",
                18, TextAlignmentOptions.TopLeft, new Vector2(25f, -80f), new Vector2(560f, 220f), Color.white);

            // Difficulty Buttons Header
            CreateText(mainPanel.transform, "DiffHeader", "<b>SELECT DIFFICULTY:</b>", 18, TextAlignmentOptions.TopLeft,
                new Vector2(25f, -315f), new Vector2(400f, 30f), new Color(0.18f, 0.85f, 0.98f));

            recruitButton = CreateButton(mainPanel.transform, "Btn_Recruit", "RECRUIT", new Vector2(25f, -355f), new Vector2(170f, 45f), () => SelectDifficulty(MissionDifficulty.Recruit));
            operativeButton = CreateButton(mainPanel.transform, "Btn_Operative", "OPERATIVE", new Vector2(205f, -355f), new Vector2(170f, 45f), () => SelectDifficulty(MissionDifficulty.Operative));
            eliteButton = CreateButton(mainPanel.transform, "Btn_Elite", "ELITE", new Vector2(385f, -355f), new Vector2(170f, 45f), () => SelectDifficulty(MissionDifficulty.Elite));

            difficultyDescriptionText = CreateText(mainPanel.transform, "DiffDesc", "", 16, TextAlignmentOptions.TopLeft,
                new Vector2(25f, -415f), new Vector2(540f, 50f), new Color(0.9f, 0.9f, 0.9f));

            // Loadout Header
            CreateText(mainPanel.transform, "LoadoutHeader", "<b>ASSIGNED LOADOUT:</b>", 18, TextAlignmentOptions.TopLeft,
                new Vector2(25f, -480f), new Vector2(400f, 30f), new Color(0.18f, 0.85f, 0.98f));

            loadoutDescriptionText = CreateText(mainPanel.transform, "LoadoutDesc",
                "• <b>XM-500 Tactical Sniper Rifle</b> (.50 BMG Caliber)\n" +
                "• Optical 4x/8x Mil-Dot Scope Reticle\n" +
                "• Integrated Muzzle Suppressor & Recoil Compensator\n" +
                "• 5-Round Detachable Box Magazine",
                16, TextAlignmentOptions.TopLeft, new Vector2(25f, -515f), new Vector2(540f, 100f), Color.white);

            // Deploy Button (Prominent Glowing Cyan)
            deployButton = CreateButton(mainPanel.transform, "Btn_Deploy", "▶  DEPLOY TO ROOFTOP MISSION",
                new Vector2(25f, -640f), new Vector2(540f, 65f), OnDeployClicked);
            deployButton.GetComponent<Image>().color = new Color(0.0f, 0.65f, 0.85f);
            var btnText = deployButton.GetComponentInChildren<TextMeshProUGUI>();
            if (btnText != null)
            {
                btnText.fontSize = 22;
                btnText.fontStyle = FontStyles.Bold;
            }

            // Exit Button
            exitButton = CreateButton(mainPanel.transform, "Btn_Exit", "EXIT APPLICATION",
                new Vector2(25f, -725f), new Vector2(200f, 40f), OnExitClicked);
            exitButton.GetComponent<Image>().color = new Color(0.25f, 0.28f, 0.32f);

            // Deploying Overlay
            deployingOverlay = new GameObject("DeployingOverlay");
            deployingOverlay.transform.SetParent(canvasGO.transform, false);
            RectTransform dolRT = deployingOverlay.AddComponent<RectTransform>();
            dolRT.anchorMin = Vector2.zero;
            dolRT.anchorMax = Vector2.one;
            dolRT.offsetMin = Vector2.zero;
            dolRT.offsetMax = Vector2.zero;
            Image dolImg = deployingOverlay.AddComponent<Image>();
            dolImg.color = new Color(0f, 0f, 0f, 0.85f);

            CreateText(deployingOverlay.transform, "DeployingText", "INFILTRATING HIGH-RISE ROOFTOP...\nSTAND BY", 28,
                TextAlignmentOptions.Center, Vector2.zero, new Vector2(600f, 100f), new Color(0.18f, 0.85f, 0.98f));
            deployingOverlay.SetActive(false);
        }

        private TextMeshProUGUI CreateText(Transform parent, string name, string content, float fontSize,
            TextAlignmentOptions alignment, Vector2 pos, Vector2 size, Color color)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);

            RectTransform rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.font = DEADeyeMissionHUD.GetDefaultFont();
            tmp.text = content;
            tmp.fontSize = fontSize;
            tmp.alignment = alignment;
            tmp.color = color;
            tmp.textWrappingMode = TextWrappingModes.Normal;
            return tmp;
        }

        private Button CreateButton(Transform parent, string name, string label, Vector2 pos, Vector2 size, UnityEngine.Events.UnityAction onClick)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);

            RectTransform rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            Image img = go.AddComponent<Image>();
            img.color = new Color(0.18f, 0.22f, 0.28f);

            Button btn = go.AddComponent<Button>();
            btn.targetGraphic = img;

            GameObject textGO = new GameObject("Text");
            textGO.transform.SetParent(go.transform, false);
            RectTransform trt = textGO.AddComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = Vector2.zero;
            trt.offsetMax = Vector2.zero;

            TextMeshProUGUI tmp = textGO.AddComponent<TextMeshProUGUI>();
            tmp.font = DEADeyeMissionHUD.GetDefaultFont();
            tmp.text = label;
            tmp.fontSize = 18;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;

            btn.onClick.AddListener(onClick);
            return btn;
        }

        public void SelectDifficulty(MissionDifficulty diff)
        {
            if (DEADeyeGameManager.Instance != null)
            {
                DEADeyeGameManager.Instance.SetDifficulty(diff);
            }
            UpdateDifficultyUI();
        }

        private void UpdateDifficultyUI()
        {
            MissionDifficulty diff = MissionDifficulty.Operative;
            if (DEADeyeGameManager.Instance != null)
            {
                diff = DEADeyeGameManager.Instance.Difficulty;
            }

            Color activeColor = new Color(0.0f, 0.75f, 0.95f);
            Color inactiveColor = new Color(0.18f, 0.22f, 0.28f);

            if (recruitButton != null) recruitButton.GetComponent<Image>().color = diff == MissionDifficulty.Recruit ? activeColor : inactiveColor;
            if (operativeButton != null) operativeButton.GetComponent<Image>().color = diff == MissionDifficulty.Operative ? activeColor : inactiveColor;
            if (eliteButton != null) eliteButton.GetComponent<Image>().color = diff == MissionDifficulty.Elite ? activeColor : inactiveColor;

            if (difficultyDescriptionText != null)
            {
                switch (diff)
                {
                    case MissionDifficulty.Recruit:
                        difficultyDescriptionText.text = "<color=#00FF99>RECRUIT:</color> Slower hostile patrol speeds. High observation window before alert.";
                        break;
                    case MissionDifficulty.Operative:
                        difficultyDescriptionText.text = "<color=#00E5FF>OPERATIVE (Standard):</color> Standard urban patrol speeds and hostile alert response.";
                        break;
                    case MissionDifficulty.Elite:
                        difficultyDescriptionText.text = "<color=#FF3366>ELITE:</color> High-speed hostiles with fast escape sprint urgency.";
                        break;
                }
            }
        }

        private void UpdateLoadoutUI()
        {
            // Loadout info updated
        }

        public void OnDeployClicked()
        {
            Debug.Log("[DEADeyeLobbyUI] Deploy clicked. Loading RooftopMission...");
            if (deployingOverlay != null)
            {
                deployingOverlay.SetActive(true);
            }

            if (DEADeyeSceneTransition.Instance != null)
            {
                DEADeyeSceneTransition.Instance.LoadRooftopMission();
            }
            else
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene(DEADeyeSceneTransition.SCENE_ROOFTOP);
            }
        }

        public void OnExitClicked()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
