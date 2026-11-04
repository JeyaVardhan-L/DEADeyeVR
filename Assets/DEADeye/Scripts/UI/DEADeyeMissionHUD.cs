using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace DEADeye
{
    /// <summary>
    /// In-mission HUD displaying live COD Mobile style sniper telemetry:
    /// - Top-left: Countdown timer, current score, remaining hostiles, and escape count.
    /// - Top-center: Objective Tracker bar.
    /// - Top-right: Permanent "[ MISSION INFO (I) ]" button.
    /// - Bottom-right: Ammunition count and reload prompt.
    /// - Center: Dynamic hit/headshot feedback (+100 BODY HIT / +300 HEADSHOT) with hitmarker.
    /// - Center: Optical circular sniper scope reticle overlay (strictly raycast-transparent).
    /// - Full-screen Tactical Mission Info overlay (toggled via 'I' key or button click).
    /// - Pause menu with Resume, Restart, and Quit.
    /// - Results debriefing screen.
    /// </summary>
    public class DEADeyeMissionHUD : MonoBehaviour
    {
        public static DEADeyeMissionHUD Instance { get; private set; }

        private static TMP_FontAsset cachedFontAsset;
        public static TMP_FontAsset GetDefaultFont()
        {
            if (cachedFontAsset == null)
            {
                cachedFontAsset = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
                if (cachedFontAsset == null)
                {
                    TMP_FontAsset[] fonts = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
                    if (fonts != null && fonts.Length > 0)
                    {
                        cachedFontAsset = fonts[0];
                    }
                }
            }
            return cachedFontAsset;
        }

        [Header("Telemetry Elements")]
        [SerializeField] private TextMeshProUGUI timerText;
        [SerializeField] private TextMeshProUGUI hostilesRemainingText;
        [SerializeField] private TextMeshProUGUI escapesText;
        [SerializeField] private TextMeshProUGUI scoreText;
        [SerializeField] private TextMeshProUGUI ammoText;

        [Header("Tactical Recon & Radio Elements")]
        [SerializeField] private TextMeshProUGUI objectiveText;
        [SerializeField] private TextMeshProUGUI objectiveTitleText;
        [SerializeField] private TextMeshProUGUI objectiveRationaleText;
        [SerializeField] private TextMeshProUGUI objectiveProgressText;
        [SerializeField] private Outline objectiveOutline;
        [SerializeField] private GameObject tutorialPanel;
        [SerializeField] private TextMeshProUGUI tutorialText;
        [SerializeField] private GameObject spottingBannerPanel;
        [SerializeField] private TextMeshProUGUI spottingTitleText;
        [SerializeField] private TextMeshProUGUI spottingDescText;
        [SerializeField] private GameObject radioDialoguePanel;
        [SerializeField] private TextMeshProUGUI radioSenderText;
        [SerializeField] private TextMeshProUGUI radioMessageText;

        [Header("Feedback Elements")]
        [SerializeField] private TextMeshProUGUI hitFeedbackText;
        [SerializeField] private GameObject hitmarkerObject;
        [SerializeField] private GameObject scopeOverlay;
        [SerializeField] private RawImage scopeLensRawImage;
        [SerializeField] private GameObject pausePanel;
        [SerializeField] private GameObject missionInfoPanel;

        [Header("Results Panel")]
        [SerializeField] private DEADeyeResultsUI resultsUI;

        private bool isPaused = false;
        private bool isMissionInfoActive = false;
        private Coroutine hitFeedbackCoroutine;
        private Coroutine spottingBannerCoroutine;
        private Coroutine radioDialogueCoroutine;
        private Coroutine objectiveFlashCoroutine;

        public bool IsPaused => isPaused;
        public bool IsMissionInfoActive => isMissionInfoActive;

        private void Awake()
        {
            Instance = this;
            Time.timeScale = 1f;
            isPaused = false;
            isMissionInfoActive = false;
            EnsureHUDCanvas();
        }

        private void Start()
        {
            if (DEADeyeMissionDirector.Instance != null)
            {
                DEADeyeMissionDirector.Instance.OnTimerTick += UpdateTimerDisplay;
                DEADeyeMissionDirector.Instance.OnMissionEnded += HandleMissionEnded;
            }

            if (DEADeyeScoreManager.Instance != null)
            {
                DEADeyeScoreManager.Instance.OnScoreUpdated += UpdateScoreDisplay;
                DEADeyeScoreManager.Instance.OnHostileCountUpdated += UpdateHostileCountDisplay;
                DEADeyeScoreManager.Instance.OnHitRegistered += ShowHitFeedback;
            }

            if (DEADeyeWeaponManager.Instance != null)
            {
                DEADeyeWeaponManager.Instance.OnAmmoChanged += UpdateAmmoDisplay;
                DEADeyeWeaponManager.Instance.OnReloadStarted += HandleReloadStarted;
                DEADeyeWeaponManager.Instance.OnReloadCompleted += HandleReloadCompleted;
            }

            var weapon = FindFirstObjectByType<DEADeyeSniperWeapon>();
            if (weapon != null && weapon.ScopeOptic != null && weapon.ScopeOptic.ScopeTargetTexture != null)
            {
                SetScopeTexture(weapon.ScopeOptic.ScopeTargetTexture);
            }

            UpdateTimerDisplay(DEADeyeMissionDirector.Instance != null ? DEADeyeMissionDirector.Instance.TimeRemaining : 120f);
            UpdateScoreDisplay(DEADeyeScoreManager.Instance != null ? DEADeyeScoreManager.Instance.CurrentScore : 0);
            UpdateAmmoDisplay(5, 5);
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }

            if (DEADeyeMissionDirector.Instance != null)
            {
                DEADeyeMissionDirector.Instance.OnTimerTick -= UpdateTimerDisplay;
                DEADeyeMissionDirector.Instance.OnMissionEnded -= HandleMissionEnded;
            }
            if (DEADeyeScoreManager.Instance != null)
            {
                DEADeyeScoreManager.Instance.OnScoreUpdated -= UpdateScoreDisplay;
                DEADeyeScoreManager.Instance.OnHostileCountUpdated -= UpdateHostileCountDisplay;
                DEADeyeScoreManager.Instance.OnHitRegistered -= ShowHitFeedback;
            }
            if (DEADeyeWeaponManager.Instance != null)
            {
                DEADeyeWeaponManager.Instance.OnAmmoChanged -= UpdateAmmoDisplay;
                DEADeyeWeaponManager.Instance.OnReloadStarted -= HandleReloadStarted;
                DEADeyeWeaponManager.Instance.OnReloadCompleted -= HandleReloadCompleted;
            }
        }

        private void EnsureHUDCanvas()
        {
            DEADeyeTacticalTerminalUI.EnsureEventSystem();

            Canvas canvas = GetComponentInChildren<Canvas>();
            if (canvas == null)
            {
                GameObject canvasGO = new GameObject("HUDCanvas");
                canvasGO.transform.SetParent(transform, false);

                canvas = canvasGO.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 10;

                CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.matchWidthOrHeight = 0.5f;

                canvasGO.AddComponent<GraphicRaycaster>();

                // 1. Telemetry Bar (Top Left)
                GameObject topBar = new GameObject("TelemetryBar");
                topBar.transform.SetParent(canvasGO.transform, false);
                RectTransform tbRT = topBar.AddComponent<RectTransform>();
                tbRT.anchorMin = new Vector2(0.02f, 0.86f);
                tbRT.anchorMax = new Vector2(0.32f, 0.98f);
                tbRT.offsetMin = Vector2.zero;
                tbRT.offsetMax = Vector2.zero;

                Image tbBg = topBar.AddComponent<Image>();
                tbBg.color = new Color(0.04f, 0.06f, 0.09f, 0.88f);

                Outline tbOutline = topBar.AddComponent<Outline>();
                tbOutline.effectColor = new Color(0.0f, 0.85f, 1.0f, 0.55f);
                tbOutline.effectDistance = new Vector2(1.5f, -1.5f);

                timerText = CreateText(topBar.transform, "TimerText", "TIME: 02:00", 22, new Vector2(16f, -10f), new Vector2(200f, 32f), Color.white);
                scoreText = CreateText(topBar.transform, "ScoreText", "SCORE: 0000", 22, new Vector2(210f, -10f), new Vector2(200f, 32f), new Color(0.0f, 0.9f, 1.0f));
                hostilesRemainingText = CreateText(topBar.transform, "HostilesText", "HOSTILES: 8", 18, new Vector2(16f, -50f), new Vector2(200f, 28f), Color.white);
                escapesText = CreateText(topBar.transform, "EscapesText", "ESCAPED: 0 / 3", 18, new Vector2(210f, -50f), new Vector2(200f, 28f), new Color(1.0f, 0.85f, 0.2f));

                // 2. Objective Tracker Bar (Top Center)
                GameObject objBar = new GameObject("ObjectiveBar");
                objBar.transform.SetParent(canvasGO.transform, false);
                RectTransform objRT = objBar.AddComponent<RectTransform>();
                objRT.anchorMin = new Vector2(0.34f, 0.85f);
                objRT.anchorMax = new Vector2(0.78f, 0.98f);
                objRT.offsetMin = Vector2.zero;
                objRT.offsetMax = Vector2.zero;

                Image objBg = objBar.AddComponent<Image>();
                objBg.color = new Color(0.03f, 0.06f, 0.10f, 0.92f);
                Outline objOutline = objBar.AddComponent<Outline>();
                objOutline.effectColor = new Color(0.0f, 0.85f, 1.0f, 0.6f);
                objOutline.effectDistance = new Vector2(1.5f, -1.5f);
                objectiveOutline = objOutline;

                objectiveTitleText = CreateText(objBar.transform, "ObjectiveTitleText", "OBJECTIVE 1 OF 6: RECON & SPOT", 17, new Vector2(16f, -6f), new Vector2(800f, 26f), new Color(0.0f, 0.95f, 1.0f));
                objectiveRationaleText = CreateText(objBar.transform, "ObjectiveRationaleText", "Dwell optical scope reticle on Elena, Marcus, and the broadcast tower to confirm tactical status.", 13, new Vector2(16f, -34f), new Vector2(800f, 38f), new Color(0.85f, 0.90f, 0.95f));
                objectiveProgressText = CreateText(objBar.transform, "ObjectiveProgressText", "TARGETS SPOTTED: 0 / 3", 13, new Vector2(16f, -74f), new Vector2(800f, 22f), new Color(0.0f, 1.0f, 0.5f));
                objectiveText = objectiveTitleText;

                // 2b. PERMANENT MISSION INFO HUD BUTTON (Top Right: X: 80% to 98%, Y: 88% to 98%)
                GameObject infoBtnGO = new GameObject("Btn_HUD_MissionInfo");
                infoBtnGO.transform.SetParent(canvasGO.transform, false);
                RectTransform ibRT = infoBtnGO.AddComponent<RectTransform>();
                ibRT.anchorMin = new Vector2(0.80f, 0.88f);
                ibRT.anchorMax = new Vector2(0.98f, 0.98f);
                ibRT.offsetMin = Vector2.zero;
                ibRT.offsetMax = Vector2.zero;

                Image ibBg = infoBtnGO.AddComponent<Image>();
                ibBg.color = new Color(0.06f, 0.12f, 0.18f, 0.95f);
                ibBg.raycastTarget = true;

                Outline ibOutline = infoBtnGO.AddComponent<Outline>();
                ibOutline.effectColor = new Color(0.0f, 0.85f, 1.0f, 0.8f);
                ibOutline.effectDistance = new Vector2(1.5f, -1.5f);

                Button infoBtn = infoBtnGO.AddComponent<Button>();
                infoBtn.targetGraphic = ibBg;
                infoBtn.onClick.AddListener(ToggleMissionInfo);

                GameObject ibTextGO = new GameObject("Text");
                ibTextGO.transform.SetParent(infoBtnGO.transform, false);
                RectTransform ibtRT = ibTextGO.AddComponent<RectTransform>();
                ibtRT.anchorMin = Vector2.zero;
                ibtRT.anchorMax = Vector2.one;
                ibtRT.offsetMin = Vector2.zero;
                ibtRT.offsetMax = Vector2.zero;

                TextMeshProUGUI ibTMP = ibTextGO.AddComponent<TextMeshProUGUI>();
                ibTMP.font = GetDefaultFont();
                ibTMP.text = "<b>[ MISSION INFO (I) ]</b>\n<size=12><color=#00E5FF>CLICK OR PRESS 'I'</color></size>";
                ibTMP.fontSize = 15;
                ibTMP.alignment = TextAlignmentOptions.Center;
                ibTMP.color = Color.white;
                ibTMP.raycastTarget = false;

                // 3. Tactical Spotting Banner (Center High)
                spottingBannerPanel = new GameObject("SpottingBannerPanel");
                spottingBannerPanel.transform.SetParent(canvasGO.transform, false);
                RectTransform sbRT = spottingBannerPanel.AddComponent<RectTransform>();
                sbRT.anchorMin = new Vector2(0.28f, 0.74f);
                sbRT.anchorMax = new Vector2(0.72f, 0.85f);
                sbRT.offsetMin = Vector2.zero;
                sbRT.offsetMax = Vector2.zero;

                Image sbBg = spottingBannerPanel.AddComponent<Image>();
                sbBg.color = new Color(0.02f, 0.05f, 0.08f, 0.94f);
                Outline sbOutline = spottingBannerPanel.AddComponent<Outline>();
                sbOutline.effectColor = new Color(0.0f, 0.9f, 1.0f, 0.8f);
                sbOutline.effectDistance = new Vector2(1.5f, -1.5f);

                spottingTitleText = CreateText(spottingBannerPanel.transform, "SpotTitle", "[ TARGET IDENTIFIED ]", 18, new Vector2(16f, -8f), new Vector2(800f, 26f), new Color(0.0f, 0.9f, 1.0f));
                spottingDescText = CreateText(spottingBannerPanel.transform, "SpotDesc", "Tactical recon intel details.", 14, new Vector2(16f, -38f), new Vector2(800f, 36f), Color.white);
                spottingBannerPanel.SetActive(false);

                // 4. Radio Dialogue Subtitles (Bottom Left)
                radioDialoguePanel = new GameObject("RadioDialoguePanel");
                radioDialoguePanel.transform.SetParent(canvasGO.transform, false);
                RectTransform rdRT = radioDialoguePanel.AddComponent<RectTransform>();
                rdRT.anchorMin = new Vector2(0.02f, 0.18f);
                rdRT.anchorMax = new Vector2(0.42f, 0.28f);
                rdRT.offsetMin = Vector2.zero;
                rdRT.offsetMax = Vector2.zero;

                Image rdBg = radioDialoguePanel.AddComponent<Image>();
                rdBg.color = new Color(0.02f, 0.04f, 0.07f, 0.90f);
                Outline rdOutline = radioDialoguePanel.AddComponent<Outline>();
                rdOutline.effectColor = new Color(0.2f, 0.85f, 1.0f, 0.6f);
                rdOutline.effectDistance = new Vector2(1.5f, -1.5f);

                radioSenderText = CreateText(radioDialoguePanel.transform, "RadioSender", "▶ RADIO // CONTROL", 15, new Vector2(12f, -8f), new Vector2(400f, 22f), new Color(0.0f, 0.9f, 1.0f));
                radioMessageText = CreateText(radioDialoguePanel.transform, "RadioMessage", "Overwatch, maintain surveillance on the district.", 14, new Vector2(12f, -32f), new Vector2(700f, 34f), Color.white);
                radioDialoguePanel.SetActive(false);

                // 5. Ammo Bar (Bottom Right)
                GameObject ammoBox = new GameObject("AmmoBox");
                ammoBox.transform.SetParent(canvasGO.transform, false);
                RectTransform abRT = ammoBox.AddComponent<RectTransform>();
                abRT.anchorMin = new Vector2(0.80f, 0.04f);
                abRT.anchorMax = new Vector2(0.98f, 0.15f);
                abRT.offsetMin = Vector2.zero;
                abRT.offsetMax = Vector2.zero;

                Image abBg = ammoBox.AddComponent<Image>();
                abBg.color = new Color(0.04f, 0.06f, 0.09f, 0.88f);

                Outline abOutline = ammoBox.AddComponent<Outline>();
                abOutline.effectColor = new Color(0.0f, 0.85f, 1.0f, 0.55f);
                abOutline.effectDistance = new Vector2(1.5f, -1.5f);

                ammoText = CreateText(ammoBox.transform, "AmmoText", "AMMO: 5 / \u221E\n<size=15><color=#888888>[R] RELOAD</color></size>", 22, new Vector2(16f, -8f), new Vector2(260f, 60f), new Color(0.0f, 0.9f, 1.0f));

                // 6. Center Optical Sniper Scope Overlay (RAYCAST TRANSPARENT)
                BuildScopeOverlay(canvasGO.transform);

                // 7. Hit Feedback & Hitmarker (Center)
                BuildHitFeedback(canvasGO.transform);

                // 8. Pause Panel
                BuildPauseMenu(canvasGO.transform);

                // 9. Full-screen Mission Info Overlay
                BuildMissionInfoOverlay(canvasGO.transform);

                // 10. Results UI
                GameObject resultsGO = new GameObject("ResultsUI");
                resultsGO.transform.SetParent(canvasGO.transform, false);
                resultsUI = resultsGO.AddComponent<DEADeyeResultsUI>();
            }
        }

        public void SetScopeTexture(RenderTexture rt)
        {
            if (scopeLensRawImage != null)
            {
                scopeLensRawImage.texture = rt;
            }
        }

        private void BuildScopeOverlay(Transform parent)
        {
            scopeOverlay = new GameObject("ScopeReticleOverlay");
            scopeOverlay.transform.SetParent(parent, false);
            RectTransform soRT = scopeOverlay.AddComponent<RectTransform>();
            soRT.anchorMin = Vector2.zero;
            soRT.anchorMax = Vector2.one;
            soRT.offsetMin = Vector2.zero;
            soRT.offsetMax = Vector2.zero;

            // CRITICAL: Scope overlay must never intercept mouse clicks!
            CanvasGroup cg = scopeOverlay.AddComponent<CanvasGroup>();
            cg.blocksRaycasts = false;
            cg.interactable = false;

            float scopeDiameter = 860f;

            // 1. Center Magnified Optic Render (RawImage)
            GameObject lensGO = new GameObject("ScopeMagnifiedLens");
            lensGO.transform.SetParent(scopeOverlay.transform, false);
            RectTransform lensRT = lensGO.AddComponent<RectTransform>();
            lensRT.anchorMin = new Vector2(0.5f, 0.5f);
            lensRT.anchorMax = new Vector2(0.5f, 0.5f);
            lensRT.sizeDelta = new Vector2(scopeDiameter, scopeDiameter);

            scopeLensRawImage = lensGO.AddComponent<RawImage>();
            scopeLensRawImage.color = Color.white;
            scopeLensRawImage.raycastTarget = false;

            // 2. Circular Vignette / Bezel Mask Texture over lens
            GameObject vignetteGO = new GameObject("ScopeCircularVignette");
            vignetteGO.transform.SetParent(scopeOverlay.transform, false);
            RectTransform vigRT = vignetteGO.AddComponent<RectTransform>();
            vigRT.anchorMin = new Vector2(0.5f, 0.5f);
            vigRT.anchorMax = new Vector2(0.5f, 0.5f);
            vigRT.sizeDelta = new Vector2(scopeDiameter, scopeDiameter);

            Image vigImg = vignetteGO.AddComponent<Image>();
            vigImg.sprite = CreateCircularVignetteSprite(512);
            vigImg.color = Color.white;
            vigImg.raycastTarget = false;

            // 3. Peripheral Black Housing Panels
            CreateVignetteBorder(scopeOverlay.transform, new Vector2(0f, 0f), new Vector2(0.5f, 1f), new Vector2(-scopeDiameter * 0.5f, 0f), isLeftRight: true);
            CreateVignetteBorder(scopeOverlay.transform, new Vector2(0.5f, 0f), new Vector2(1f, 1f), new Vector2(scopeDiameter * 0.5f, 0f), isLeftRight: true);
            CreateVignetteBorder(scopeOverlay.transform, new Vector2(0f, 0.5f), new Vector2(1f, 1f), new Vector2(0f, scopeDiameter * 0.5f), isLeftRight: false);
            CreateVignetteBorder(scopeOverlay.transform, new Vector2(0f, 0f), new Vector2(1f, 0.5f), new Vector2(0f, -scopeDiameter * 0.5f), isLeftRight: false);

            // 4. Reticle Crosshairs (Fine black/red sniper lines)
            GameObject reticleRoot = new GameObject("ReticleOptics");
            reticleRoot.transform.SetParent(scopeOverlay.transform, false);
            RectTransform retRT = reticleRoot.AddComponent<RectTransform>();
            retRT.anchorMin = new Vector2(0.5f, 0.5f);
            retRT.anchorMax = new Vector2(0.5f, 0.5f);
            retRT.sizeDelta = new Vector2(scopeDiameter, scopeDiameter);

            // Horizontal crosshair line
            GameObject chH = new GameObject("Crosshair_H");
            chH.transform.SetParent(reticleRoot.transform, false);
            RectTransform chHRT = chH.AddComponent<RectTransform>();
            chHRT.sizeDelta = new Vector2(scopeDiameter * 0.92f, 2.5f);
            Image chHImg = chH.AddComponent<Image>();
            chHImg.color = new Color(0.1f, 0.1f, 0.1f, 0.9f);
            chHImg.raycastTarget = false;

            // Vertical crosshair line
            GameObject chV = new GameObject("Crosshair_V");
            chV.transform.SetParent(reticleRoot.transform, false);
            RectTransform chVRT = chV.AddComponent<RectTransform>();
            chVRT.sizeDelta = new Vector2(2.5f, scopeDiameter * 0.92f);
            Image chVImg = chV.AddComponent<Image>();
            chVImg.color = new Color(0.1f, 0.1f, 0.1f, 0.9f);
            chVImg.raycastTarget = false;

            // Mil-dot hash tick marks along axes
            for (int i = 1; i <= 5; i++)
            {
                float offset = i * 65f;
                CreateTickMark(reticleRoot.transform, new Vector2(offset, 0f), isVertical: true);
                CreateTickMark(reticleRoot.transform, new Vector2(-offset, 0f), isVertical: true);
                CreateTickMark(reticleRoot.transform, new Vector2(0f, offset), isVertical: false);
                CreateTickMark(reticleRoot.transform, new Vector2(0f, -offset), isVertical: false);
            }

            // Central Glowing Red Aiming Pip
            GameObject centerPip = new GameObject("CenterRedPip");
            centerPip.transform.SetParent(reticleRoot.transform, false);
            RectTransform pipRT = centerPip.AddComponent<RectTransform>();
            pipRT.sizeDelta = new Vector2(6f, 6f);
            Image pipImg = centerPip.AddComponent<Image>();
            pipImg.color = new Color(1.0f, 0.1f, 0.1f, 0.95f);
            pipImg.raycastTarget = false;

            // Tactical Telemetry within Scope
            CreateScopeLabel(reticleRoot.transform, "Scope_Mag", "MAG 5.0X", new Vector2(-220f, 240f));
            CreateScopeLabel(reticleRoot.transform, "Scope_Elev", "ELEV +0.2", new Vector2(220f, 240f));
            CreateScopeLabel(reticleRoot.transform, "Scope_Model", "DEADEYE XM500 OPTIC", new Vector2(0f, -280f), fontSize: 13);

            scopeOverlay.SetActive(false);
        }

        private void CreateTickMark(Transform parent, Vector2 pos, bool isVertical)
        {
            GameObject tick = new GameObject("TickMark");
            tick.transform.SetParent(parent, false);
            RectTransform rt = tick.AddComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = isVertical ? new Vector2(2f, 10f) : new Vector2(10f, 2f);
            Image img = tick.AddComponent<Image>();
            img.color = new Color(0.1f, 0.1f, 0.1f, 0.85f);
            img.raycastTarget = false;
        }

        private void CreateScopeLabel(Transform parent, string name, string text, Vector2 pos, float fontSize = 15f)
        {
            GameObject label = new GameObject(name);
            label.transform.SetParent(parent, false);
            RectTransform rt = label.AddComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(200f, 30f);

            TextMeshProUGUI tmp = label.AddComponent<TextMeshProUGUI>();
            tmp.font = GetDefaultFont();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(0.0f, 0.9f, 1.0f, 0.85f);
            tmp.fontStyle = FontStyles.Bold;
            tmp.raycastTarget = false;
        }

        private void CreateVignetteBorder(Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 edgeOffset, bool isLeftRight)
        {
            GameObject border = new GameObject("HousingPanel");
            border.transform.SetParent(parent, false);
            RectTransform rt = border.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            if (isLeftRight)
            {
                if (edgeOffset.x < 0)
                {
                    rt.offsetMin = Vector2.zero;
                    rt.offsetMax = new Vector2(edgeOffset.x, 0f);
                }
                else
                {
                    rt.offsetMin = new Vector2(edgeOffset.x, 0f);
                    rt.offsetMax = Vector2.zero;
                }
            }
            else
            {
                if (edgeOffset.y > 0)
                {
                    rt.offsetMin = new Vector2(0f, edgeOffset.y);
                    rt.offsetMax = Vector2.zero;
                }
                else
                {
                    rt.offsetMin = Vector2.zero;
                    rt.offsetMax = new Vector2(0f, edgeOffset.y);
                }
            }
            Image img = border.AddComponent<Image>();
            img.color = new Color(0.015f, 0.015f, 0.02f, 1.0f);
            img.raycastTarget = false;
        }

        private Sprite CreateCircularVignetteSprite(int size)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color[] colors = new Color[size * size];
            Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
            float innerRadius = size * 0.44f;
            float outerRadius = size * 0.495f;

            Color transparent = new Color(0f, 0f, 0f, 0f);
            Color dark = new Color(0.015f, 0.015f, 0.02f, 1.0f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center);
                    if (dist < innerRadius)
                    {
                        colors[y * size + x] = transparent;
                    }
                    else if (dist < outerRadius)
                    {
                        float alpha = Mathf.SmoothStep(0f, 1f, (dist - innerRadius) / (outerRadius - innerRadius));
                        colors[y * size + x] = Color.Lerp(transparent, dark, alpha);
                    }
                    else
                    {
                        colors[y * size + x] = dark;
                    }
                }
            }

            tex.SetPixels(colors);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }

        private void BuildHitFeedback(Transform parent)
        {
            GameObject textGO = new GameObject("HitFeedbackText");
            textGO.transform.SetParent(parent, false);
            RectTransform rt = textGO.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0f, 75f);
            rt.sizeDelta = new Vector2(500f, 60f);

            hitFeedbackText = textGO.AddComponent<TextMeshProUGUI>();
            hitFeedbackText.font = GetDefaultFont();
            hitFeedbackText.fontSize = 28;
            hitFeedbackText.alignment = TextAlignmentOptions.Center;
            hitFeedbackText.fontStyle = FontStyles.Bold;
            hitFeedbackText.text = "";
            hitFeedbackText.raycastTarget = false;

            hitmarkerObject = new GameObject("Hitmarker");
            hitmarkerObject.transform.SetParent(parent, false);
            RectTransform hmRT = hitmarkerObject.AddComponent<RectTransform>();
            hmRT.anchorMin = new Vector2(0.5f, 0.5f);
            hmRT.anchorMax = new Vector2(0.5f, 0.5f);
            hmRT.sizeDelta = new Vector2(30f, 30f);

            CreateHitmarkerLine(hitmarkerObject.transform, 45f);
            CreateHitmarkerLine(hitmarkerObject.transform, -45f);

            hitmarkerObject.SetActive(false);
        }

        private void CreateHitmarkerLine(Transform parent, float angle)
        {
            GameObject line = new GameObject("Line");
            line.transform.SetParent(parent, false);
            RectTransform rt = line.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(24f, 3.0f);
            rt.localRotation = Quaternion.Euler(0f, 0f, angle);
            Image img = line.AddComponent<Image>();
            img.color = new Color(1.0f, 1.0f, 1.0f, 0.95f);
            img.raycastTarget = false;
        }

        private void BuildPauseMenu(Transform parent)
        {
            pausePanel = new GameObject("PausePanel");
            pausePanel.transform.SetParent(parent, false);
            RectTransform pRT = pausePanel.AddComponent<RectTransform>();
            pRT.anchorMin = new Vector2(0.35f, 0.28f);
            pRT.anchorMax = new Vector2(0.65f, 0.72f);
            pRT.offsetMin = Vector2.zero;
            pRT.offsetMax = Vector2.zero;

            Image bg = pausePanel.AddComponent<Image>();
            bg.color = new Color(0.05f, 0.07f, 0.10f, 0.96f);

            Outline outline = pausePanel.AddComponent<Outline>();
            outline.effectColor = new Color(0.0f, 0.85f, 1.0f, 0.8f);
            outline.effectDistance = new Vector2(2f, -2f);

            CreateText(pausePanel.transform, "PauseTitle", "MISSION PAUSED", 28, new Vector2(140f, -25f), new Vector2(300f, 40f), new Color(0.0f, 0.9f, 1.0f));

            CreateMenuButton(pausePanel.transform, "Btn_Resume", "▶  RESUME MISSION", new Vector2(0f, 45f), OnResumeClicked);
            CreateMenuButton(pausePanel.transform, "Btn_Restart", "↺  RESTART MISSION", new Vector2(0f, -25f), OnRestartClicked);
            CreateMenuButton(pausePanel.transform, "Btn_Quit", "✕  QUIT GAME", new Vector2(0f, -95f), OnQuitClicked);

            pausePanel.SetActive(false);
        }

        private void BuildMissionInfoOverlay(Transform parent)
        {
            missionInfoPanel = new GameObject("MissionInfoOverlayPanel");
            missionInfoPanel.transform.SetParent(parent, false);
            RectTransform miRT = missionInfoPanel.AddComponent<RectTransform>();
            miRT.anchorMin = new Vector2(0.05f, 0.05f);
            miRT.anchorMax = new Vector2(0.95f, 0.95f);
            miRT.offsetMin = Vector2.zero;
            miRT.offsetMax = Vector2.zero;

            Image bg = missionInfoPanel.AddComponent<Image>();
            bg.color = new Color(0.03f, 0.06f, 0.10f, 0.98f);

            Outline ol = missionInfoPanel.AddComponent<Outline>();
            ol.effectColor = new Color(0.0f, 0.85f, 1.0f, 0.9f);
            ol.effectDistance = new Vector2(2.5f, -2.5f);

            // Top Header
            CreateText(missionInfoPanel.transform, "MITitle", "OPERATION BRIEFING & TACTICAL INTEL", 26, new Vector2(30f, -20f), new Vector2(800f, 35f), new Color(0.0f, 0.9f, 1.0f));
            CreateText(missionInfoPanel.transform, "MISub", "OPERATION 01: DEAD AIR // CENTRAL COMMERCIAL PLAZA", 16, new Vector2(30f, -56f), new Vector2(800f, 25f), new Color(0.4f, 0.8f, 1.0f));

            // Close / Resume button top right
            GameObject closeBtnGO = new GameObject("Btn_CloseInfo");
            closeBtnGO.transform.SetParent(missionInfoPanel.transform, false);
            RectTransform cbRT = closeBtnGO.AddComponent<RectTransform>();
            cbRT.anchorMin = new Vector2(1f, 1f);
            cbRT.anchorMax = new Vector2(1f, 1f);
            cbRT.pivot = new Vector2(1f, 1f);
            cbRT.anchoredPosition = new Vector2(-25f, -20f);
            cbRT.sizeDelta = new Vector2(220f, 50f);

            Image cbBg = closeBtnGO.AddComponent<Image>();
            cbBg.color = new Color(0.0f, 0.55f, 0.85f, 1.0f);
            Button cbBtn = closeBtnGO.AddComponent<Button>();
            cbBtn.targetGraphic = cbBg;
            cbBtn.onClick.AddListener(ToggleMissionInfo);

            GameObject cbText = new GameObject("Text");
            cbText.transform.SetParent(closeBtnGO.transform, false);
            RectTransform cbtRT = cbText.AddComponent<RectTransform>();
            cbtRT.anchorMin = Vector2.zero;
            cbtRT.anchorMax = Vector2.one;
            cbtRT.offsetMin = Vector2.zero;
            cbtRT.offsetMax = Vector2.zero;

            TextMeshProUGUI cbTMP = cbText.AddComponent<TextMeshProUGUI>();
            cbTMP.font = GetDefaultFont();
            cbTMP.text = "<b>RESUME (I / ESC)</b>";
            cbTMP.fontSize = 16;
            cbTMP.alignment = TextAlignmentOptions.Center;
            cbTMP.color = Color.white;
            cbTMP.raycastTarget = false;

            // Narrative Story Box (Left column)
            GameObject storyBox = new GameObject("StoryBox");
            storyBox.transform.SetParent(missionInfoPanel.transform, false);
            RectTransform sbRT = storyBox.AddComponent<RectTransform>();
            sbRT.anchorMin = new Vector2(0.02f, 0.10f);
            sbRT.anchorMax = new Vector2(0.50f, 0.88f);
            sbRT.offsetMin = Vector2.zero;
            sbRT.offsetMax = Vector2.zero;

            Image sbg = storyBox.AddComponent<Image>();
            sbg.color = new Color(0.05f, 0.08f, 0.12f, 0.85f);

            CreateText(storyBox.transform, "SBHeader", "MISSION OVERVIEW & BACKGROUND", 19, new Vector2(18f, -14f), new Vector2(500f, 26f), new Color(0.0f, 0.9f, 1.0f));
            CreateText(storyBox.transform, "SBContent",
                "A hostile syndicate strike team has deployed a high-frequency communications jammer to hijack the city relay tower, plunging the metropolitan district into a radio blackout.\n\n" +
                "Two civilian communications engineers—Elena and Marcus—have slipped into the plaza below under cover to access the municipal broadcast relay tower console and restore emergency transmission.\n\n" +
                "Your role as Overwatch Sniper:\n" +
                "• Maintain rooftop surveillance and positively identify tactical assets.\n" +
                "• Cut power to the syndicate jammer by sniping the high-voltage power junction on the utility pole.\n" +
                "• Shield Elena and Marcus from advancing syndicate assault teams.\n" +
                "• Spot and eliminate Commander Vance before his armored exfil departs.\n" +
                "• Escort the team's extraction across the boulevard.",
                14, new Vector2(18f, -50f), new Vector2(820f, 420f), new Color(0.88f, 0.92f, 0.98f));

            // Personnel Dossiers (Right column)
            GameObject dossierBox = new GameObject("DossierBox");
            dossierBox.transform.SetParent(missionInfoPanel.transform, false);
            RectTransform dbRT = dossierBox.AddComponent<RectTransform>();
            dbRT.anchorMin = new Vector2(0.52f, 0.10f);
            dbRT.anchorMax = new Vector2(0.98f, 0.88f);
            dbRT.offsetMin = Vector2.zero;
            dbRT.offsetMax = Vector2.zero;

            Image dbg = dossierBox.AddComponent<Image>();
            dbg.color = new Color(0.05f, 0.08f, 0.12f, 0.85f);

            CreateText(dossierBox.transform, "DBHeader", "TACTICAL DOSSIERS & KEY TARGETS", 19, new Vector2(18f, -14f), new Vector2(500f, 26f), new Color(0.0f, 0.9f, 1.0f));

            // Card 1: Elena
            BuildDossierCard(dossierBox.transform, "ElenaCard", "ELENA // LEAD COMMS ENGINEER", "FRIENDLY [MUST PROTECT]", new Color(0f, 1f, 0.4f),
                "Civilian field jacket, radio harness. Interfacing directly with the municipal relay tower console.", new Vector2(18f, -48f), new Vector2(780f, 110f));

            // Card 2: Marcus
            BuildDossierCard(dossierBox.transform, "MarcusCard", "MARCUS // FIELD TECHNICIAN", "FRIENDLY [MUST PROTECT]", new Color(0f, 1f, 0.4f),
                "Civilian cargo attire, cable spool. Routing fiber conduit connections. If either technician falls, mission fails.", new Vector2(18f, -170f), new Vector2(780f, 110f));

            // Card 3: Commander Vance
            BuildDossierCard(dossierBox.transform, "VanceCard", "COMMANDER VANCE // SYNDICATE LEADER", "HOSTILE [MUST ELIMINATE]", new Color(1f, 0.25f, 0.25f),
                "Heavy tactical plate carrier, radio headset, sidearm. Coordinating the syndicate assault waves.", new Vector2(18f, -292f), new Vector2(780f, 110f));

            missionInfoPanel.SetActive(false);
        }

        private void BuildDossierCard(Transform parent, string name, string title, string badge, Color badgeColor, string desc, Vector2 pos, Vector2 size)
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
            bg.color = new Color(0.03f, 0.05f, 0.08f, 0.95f);

            Outline ol = cardGO.AddComponent<Outline>();
            ol.effectColor = badgeColor * 0.7f;
            ol.effectDistance = new Vector2(1.5f, -1.5f);

            CreateText(cardGO.transform, "Title", title, 16, new Vector2(12f, -10f), new Vector2(480f, 24f), Color.white);

            GameObject badgeGO = new GameObject("Badge");
            badgeGO.transform.SetParent(cardGO.transform, false);
            RectTransform bRT = badgeGO.AddComponent<RectTransform>();
            bRT.anchorMin = new Vector2(1f, 1f);
            bRT.anchorMax = new Vector2(1f, 1f);
            bRT.pivot = new Vector2(1f, 1f);
            bRT.anchoredPosition = new Vector2(-12f, -10f);
            bRT.sizeDelta = new Vector2(250f, 26f);

            Image bImg = badgeGO.AddComponent<Image>();
            bImg.color = badgeColor * 0.2f;

            GameObject badgeTextGO = new GameObject("BadgeText");
            badgeTextGO.transform.SetParent(badgeGO.transform, false);
            RectTransform btRT = badgeTextGO.AddComponent<RectTransform>();
            btRT.anchorMin = Vector2.zero;
            btRT.anchorMax = Vector2.one;
            btRT.offsetMin = Vector2.zero;
            btRT.offsetMax = Vector2.zero;

            TextMeshProUGUI bTMP = badgeTextGO.AddComponent<TextMeshProUGUI>();
            bTMP.font = GetDefaultFont();
            bTMP.text = badge;
            bTMP.fontSize = 13;
            bTMP.alignment = TextAlignmentOptions.Center;
            bTMP.color = badgeColor;
            bTMP.fontStyle = FontStyles.Bold;

            CreateText(cardGO.transform, "Desc", desc, 13, new Vector2(12f, -44f), new Vector2(750f, 55f), new Color(0.85f, 0.88f, 0.92f));
        }

        private void CreateMenuButton(Transform parent, string name, string label, Vector2 anchoredPos, UnityEngine.Events.UnityAction onClick)
        {
            GameObject btnGO = new GameObject(name);
            btnGO.transform.SetParent(parent, false);
            RectTransform rt = btnGO.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = new Vector2(300f, 48f);

            Image img = btnGO.AddComponent<Image>();
            img.color = new Color(0.10f, 0.15f, 0.22f, 0.95f);

            Button btn = btnGO.AddComponent<Button>();
            btn.targetGraphic = img;

            GameObject textGO = new GameObject("Text");
            textGO.transform.SetParent(btnGO.transform, false);
            RectTransform trt = textGO.AddComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = Vector2.zero;
            trt.offsetMax = Vector2.zero;

            TextMeshProUGUI tmp = textGO.AddComponent<TextMeshProUGUI>();
            tmp.font = GetDefaultFont();
            tmp.text = label;
            tmp.fontSize = 17;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            tmp.fontStyle = FontStyles.Bold;
            tmp.raycastTarget = false;

            btn.onClick.AddListener(() =>
            {
                if (DEADeyeAudioManager.Instance != null)
                {
                    DEADeyeAudioManager.Instance.PlayButtonClick();
                }
                onClick?.Invoke();
            });
        }

        private TextMeshProUGUI CreateText(Transform parent, string name, string content, float fontSize, Vector2 pos, Vector2 size, Color color)
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
            tmp.font = GetDefaultFont();
            tmp.text = content;
            tmp.fontSize = fontSize;
            tmp.color = color;
            tmp.fontStyle = FontStyles.Bold;
            return tmp;
        }

        public void UpdateTimerDisplay(float seconds)
        {
            if (timerText == null) return;
            int mins = Mathf.FloorToInt(seconds / 60f);
            int secs = Mathf.FloorToInt(seconds % 60f);
            timerText.text = $"TIME: {mins:00}:{secs:00}";
        }

        public void UpdateScoreDisplay(int score)
        {
            if (scoreText != null)
            {
                scoreText.text = $"SCORE: {score:D4}";
            }
        }

        public void UpdateHostileCountDisplay(int eliminated, int escaped)
        {
            if (escapesText != null && DEADeyeMissionDirector.Instance != null)
            {
                int maxAllowed = DEADeyeMissionDirector.Instance.MaxEscapesAllowed;
                string colTag = escaped >= maxAllowed ? "<color=red>" : escaped > 0 ? "<color=yellow>" : "<color=#00FF99>";
                escapesText.text = $"ESCAPED: {colTag}{escaped} / {maxAllowed}</color>";
            }

            if (hostilesRemainingText != null && DEADeyeMissionDirector.Instance != null)
            {
                hostilesRemainingText.text = $"HOSTILES: {DEADeyeMissionDirector.Instance.HostilesRemaining}";
            }
        }

        private void HandleReloadStarted()
        {
            if (ammoText != null)
            {
                ammoText.text = "<color=#FFCC00>RELOADING...</color>\n<size=15><color=#888888>PLEASE WAIT</color></size>";
            }
        }

        private void HandleReloadCompleted()
        {
            if (DEADeyeWeaponManager.Instance != null)
            {
                UpdateAmmoDisplay(DEADeyeWeaponManager.Instance.CurrentAmmo, DEADeyeWeaponManager.Instance.MagazineCapacity);
            }
        }

        public void UpdateAmmoDisplay(int current, int max)
        {
            if (ammoText != null)
            {
                if (DEADeyeWeaponManager.Instance != null && DEADeyeWeaponManager.Instance.IsReloading)
                {
                    ammoText.text = "<color=#FFCC00>RELOADING...</color>\n<size=15><color=#888888>PLEASE WAIT</color></size>";
                    return;
                }

                string colTag = current == 0 ? "<color=red>" : "<color=#00E5FF>";
                string prompt = current == 0 ? "<color=#FF5555>[R] RELOAD REQUIRED</color>" : "<color=#888888>[R] RELOAD</color>";
                ammoText.text = $"AMMO: {colTag}{current}</color> / \u221E\n<size=15>{prompt}</size>";
            }
        }

        public void SetObjectiveDisplay(string objective)
        {
            if (objectiveTitleText != null)
            {
                objectiveTitleText.text = objective;
            }
            else if (objectiveText != null)
            {
                objectiveText.text = objective;
            }
        }

        public void UpdateObjectiveBar(string objective)
        {
            SetObjectiveDisplay(objective);
        }

        public void UpdateObjectiveTracker(int stepNum, int totalSteps, string title, string rationale, string progress = null)
        {
            if (objectiveTitleText != null)
            {
                objectiveTitleText.text = $"OBJECTIVE {stepNum} OF {totalSteps}: {title.ToUpper()}";
            }
            else if (objectiveText != null)
            {
                objectiveText.text = $"OBJECTIVE {stepNum} OF {totalSteps}: {title.ToUpper()}";
            }

            if (objectiveRationaleText != null)
            {
                objectiveRationaleText.text = rationale;
            }

            if (objectiveProgressText != null)
            {
                objectiveProgressText.text = progress ?? "";
            }

            if (DEADeyeAudioManager.Instance != null)
            {
                DEADeyeAudioManager.Instance.PlayObjectiveUpdate();
            }

            if (objectiveFlashCoroutine != null) StopCoroutine(objectiveFlashCoroutine);
            objectiveFlashCoroutine = StartCoroutine(FlashObjectiveOutline());
        }

        public void UpdateObjectiveProgress(string progress)
        {
            if (objectiveProgressText != null)
            {
                objectiveProgressText.text = progress;
            }
        }

        private IEnumerator FlashObjectiveOutline()
        {
            if (objectiveOutline == null) yield break;
            Color original = new Color(0.0f, 0.85f, 1.0f, 0.6f);
            Color flash = new Color(1.0f, 1.0f, 1.0f, 1.0f);

            for (float t = 0f; t < 0.6f; t += Time.deltaTime)
            {
                objectiveOutline.effectColor = Color.Lerp(flash, original, t / 0.6f);
                yield return null;
            }
            objectiveOutline.effectColor = original;
        }

        public void NotifyTargetSpotted()
        {
            if (DEADeyeAudioManager.Instance != null)
            {
                DEADeyeAudioManager.Instance.PlaySpottingChirp();
            }
        }

        public void ShowSpottingBanner(DEADeyeSpotable target)
        {
            if (target == null) return;
            if (spottingBannerCoroutine != null) StopCoroutine(spottingBannerCoroutine);
            spottingBannerCoroutine = StartCoroutine(SpottingBannerRoutine(target));
        }

        private IEnumerator SpottingBannerRoutine(DEADeyeSpotable target)
        {
            if (spottingBannerPanel != null)
            {
                if (spottingTitleText != null)
                {
                    string colHex;
                    switch (target.Classification)
                    {
                        case SpotClassification.Hostile:
                            colHex = "#FF3333";
                            break;
                        case SpotClassification.HostileCommander:
                            colHex = "#FF0055";
                            break;
                        case SpotClassification.Friendly:
                        case SpotClassification.FriendlyVIP:
                            colHex = "#00FF66";
                            break;
                        case SpotClassification.Objective:
                            colHex = "#FFD700";
                            break;
                        case SpotClassification.Environmental:
                        default:
                            colHex = "#00E5FF";
                            break;
                    }
                    spottingTitleText.text = $"<color={colHex}>[ TARGET IDENTIFIED: {target.TargetName} ]</color>";
                }
                if (spottingDescText != null)
                {
                    spottingDescText.text = target.TacticalDescription;
                }
                spottingBannerPanel.SetActive(true);
            }

            yield return new WaitForSeconds(3.5f);

            if (spottingBannerPanel != null)
            {
                spottingBannerPanel.SetActive(false);
            }
        }

        public void ShowRadioDialogue(string sender, string message)
        {
            if (radioDialogueCoroutine != null) StopCoroutine(radioDialogueCoroutine);
            radioDialogueCoroutine = StartCoroutine(RadioDialogueRoutine(sender, message));
        }

        private IEnumerator RadioDialogueRoutine(string sender, string message)
        {
            if (radioDialoguePanel != null)
            {
                if (radioSenderText != null)
                {
                    radioSenderText.text = $"<color=#00E5FF>▶ RADIO // {sender.ToUpper()}</color>";
                }
                if (radioMessageText != null)
                {
                    radioMessageText.text = $"\"{message}\"";
                }
                radioDialoguePanel.SetActive(true);
            }

            if (DEADeyeAudioManager.Instance != null)
            {
                DEADeyeAudioManager.Instance.PlayRadioChatter();
            }

            yield return new WaitForSeconds(4.5f);

            if (radioDialoguePanel != null)
            {
                radioDialoguePanel.SetActive(false);
            }
        }

        public void SetScopeOverlay(bool active)
        {
            if (scopeOverlay != null)
            {
                scopeOverlay.SetActive(active);
            }
        }

        public void ShowHitFeedback(bool isHeadshot, int points)
        {
            if (hitFeedbackCoroutine != null) StopCoroutine(hitFeedbackCoroutine);
            hitFeedbackCoroutine = StartCoroutine(HitFeedbackRoutine(isHeadshot, points));
        }

        private IEnumerator HitFeedbackRoutine(bool isHeadshot, int points)
        {
            if (hitFeedbackText != null)
            {
                hitFeedbackText.text = isHeadshot ? $"<color=#FFD700>+300 PRECISION HEADSHOT!</color>" : $"<color=#00E5FF>+100 BODY HIT</color>";
            }

            if (hitmarkerObject != null)
            {
                hitmarkerObject.SetActive(true);
            }

            yield return new WaitForSeconds(0.6f);

            if (hitFeedbackText != null)
            {
                hitFeedbackText.text = "";
            }

            if (hitmarkerObject != null)
            {
                hitmarkerObject.SetActive(false);
            }
        }

        public void TogglePause()
        {
            if (isMissionInfoActive)
            {
                ToggleMissionInfo(); // Close mission info first
                return;
            }

            isPaused = !isPaused;
            if (pausePanel != null)
            {
                pausePanel.SetActive(isPaused);
            }

            Time.timeScale = isPaused ? 0f : 1f;
            Cursor.lockState = isPaused ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = isPaused;
        }

        public void ToggleMissionInfo()
        {
            isMissionInfoActive = !isMissionInfoActive;
            if (missionInfoPanel != null)
            {
                missionInfoPanel.SetActive(isMissionInfoActive);
            }

            if (DEADeyeAudioManager.Instance != null)
            {
                DEADeyeAudioManager.Instance.PlayButtonClick();
            }

            Time.timeScale = isMissionInfoActive ? 0f : (isPaused ? 0f : 1f);
            Cursor.lockState = isMissionInfoActive ? CursorLockMode.None : (isPaused ? CursorLockMode.None : CursorLockMode.Locked);
            Cursor.visible = isMissionInfoActive || isPaused;
        }

        private void OnResumeClicked()
        {
            TogglePause();
        }

        private void OnRestartClicked()
        {
            Time.timeScale = 1f;
            UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
        }

        private void OnQuitClicked()
        {
            Time.timeScale = 1f;
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }

        private void HandleMissionEnded(bool victory)
        {
            if (resultsUI != null)
            {
                resultsUI.ShowResults(victory);
            }
        }
    }
}
