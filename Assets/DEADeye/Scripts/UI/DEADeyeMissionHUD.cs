using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace DEADeye
{
    /// <summary>
    /// In-mission HUD displaying live COD Mobile style sniper telemetry:
    /// - Top-left: Countdown timer, current score, remaining hostiles, and escape count.
    /// - Bottom-right: Ammunition count and reload prompt.
    /// - Center: Dynamic hit/headshot feedback (+100 BODY HIT / +300 HEADSHOT) with hitmarker.
    /// - Center: Optical circular sniper scope reticle overlay with live magnified camera render.
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

        [Header("Feedback Elements")]
        [SerializeField] private TextMeshProUGUI hitFeedbackText;
        [SerializeField] private GameObject hitmarkerObject;
        [SerializeField] private GameObject scopeOverlay;
        [SerializeField] private RawImage scopeLensRawImage;
        [SerializeField] private GameObject pausePanel;

        [Header("Results Panel")]
        [SerializeField] private DEADeyeResultsUI resultsUI;

        private bool isPaused = false;
        private Coroutine hitFeedbackCoroutine;

        public bool IsPaused => isPaused;

        private void Awake()
        {
            Instance = this;
            EnsureHUDCanvas();
        }

        private void Start()
        {
            // Subscribe to Mission Director
            if (DEADeyeMissionDirector.Instance != null)
            {
                DEADeyeMissionDirector.Instance.OnTimerTick += UpdateTimerDisplay;
                DEADeyeMissionDirector.Instance.OnMissionEnded += HandleMissionEnded;
            }

            // Subscribe to Score Manager
            if (DEADeyeScoreManager.Instance != null)
            {
                DEADeyeScoreManager.Instance.OnScoreUpdated += UpdateScoreDisplay;
                DEADeyeScoreManager.Instance.OnHostileCountUpdated += UpdateHostileCountDisplay;
                DEADeyeScoreManager.Instance.OnHitRegistered += ShowHitFeedback;
            }

            // Subscribe to Weapon Manager
            if (DEADeyeWeaponManager.Instance != null)
            {
                DEADeyeWeaponManager.Instance.OnAmmoChanged += UpdateAmmoDisplay;
            }

            // Find scope optic if available
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
            }
        }

        private void EnsureHUDCanvas()
        {
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
                tbRT.anchorMax = new Vector2(0.36f, 0.98f);
                tbRT.offsetMin = Vector2.zero;
                tbRT.offsetMax = Vector2.zero;

                Image tbBg = topBar.AddComponent<Image>();
                tbBg.color = new Color(0.04f, 0.06f, 0.09f, 0.88f);

                Outline tbOutline = topBar.AddComponent<Outline>();
                tbOutline.effectColor = new Color(0.0f, 0.85f, 1.0f, 0.55f);
                tbOutline.effectDistance = new Vector2(1.5f, -1.5f);

                timerText = CreateText(topBar.transform, "TimerText", "TIME: 02:00", 22, new Vector2(16f, -10f), new Vector2(200f, 32f), Color.white);
                scoreText = CreateText(topBar.transform, "ScoreText", "SCORE: 0000", 22, new Vector2(230f, -10f), new Vector2(200f, 32f), new Color(0.0f, 0.9f, 1.0f));
                hostilesRemainingText = CreateText(topBar.transform, "HostilesText", "HOSTILES: 8", 18, new Vector2(16f, -50f), new Vector2(200f, 28f), Color.white);
                escapesText = CreateText(topBar.transform, "EscapesText", "ESCAPED: 0 / 3", 18, new Vector2(230f, -50f), new Vector2(200f, 28f), new Color(1.0f, 0.85f, 0.2f));

                // 2. Ammo Bar (Bottom Right)
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

                ammoText = CreateText(ammoBox.transform, "AmmoText", "AMMO: 5 / 5\n<size=15><color=#888888>[R] RELOAD</color></size>", 22, new Vector2(16f, -8f), new Vector2(260f, 60f), new Color(0.0f, 0.9f, 1.0f));

                // 3. Center Optical Sniper Scope Overlay (Circular Lens + Magnification)
                BuildScopeOverlay(canvasGO.transform);

                // 4. Hit Feedback & Hitmarker (Center)
                BuildHitFeedback(canvasGO.transform);

                // 5. Pause Panel
                BuildPauseMenu(canvasGO.transform);

                // 6. Results UI
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

            float scopeDiameter = 860f;

            // 1. Center Magnified Optic Render (RawImage displaying ScopeCamera's RenderTexture)
            GameObject lensGO = new GameObject("ScopeMagnifiedLens");
            lensGO.transform.SetParent(scopeOverlay.transform, false);
            RectTransform lensRT = lensGO.AddComponent<RectTransform>();
            lensRT.anchorMin = new Vector2(0.5f, 0.5f);
            lensRT.anchorMax = new Vector2(0.5f, 0.5f);
            lensRT.sizeDelta = new Vector2(scopeDiameter, scopeDiameter);

            scopeLensRawImage = lensGO.AddComponent<RawImage>();
            scopeLensRawImage.color = Color.white;

            // 2. Circular Vignette / Bezel Mask Texture over the lens
            GameObject vignetteGO = new GameObject("ScopeCircularVignette");
            vignetteGO.transform.SetParent(scopeOverlay.transform, false);
            RectTransform vigRT = vignetteGO.AddComponent<RectTransform>();
            vigRT.anchorMin = new Vector2(0.5f, 0.5f);
            vigRT.anchorMax = new Vector2(0.5f, 0.5f);
            vigRT.sizeDelta = new Vector2(scopeDiameter, scopeDiameter);

            Image vigImg = vignetteGO.AddComponent<Image>();
            vigImg.sprite = CreateCircularVignetteSprite(512);
            vigImg.color = Color.white;

            // 3. Peripheral Black Housing Panels (Covers screen edges outside circular scope)
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

            // Vertical crosshair line
            GameObject chV = new GameObject("Crosshair_V");
            chV.transform.SetParent(reticleRoot.transform, false);
            RectTransform chVRT = chV.AddComponent<RectTransform>();
            chVRT.sizeDelta = new Vector2(2.5f, scopeDiameter * 0.92f);
            Image chVImg = chV.AddComponent<Image>();
            chVImg.color = new Color(0.1f, 0.1f, 0.1f, 0.9f);

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
            img.color = new Color(0.02f, 0.02f, 0.03f, 1.0f);
        }

        private Sprite CreateCircularVignetteSprite(int size)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color[] colors = new Color[size * size];
            float center = size * 0.5f;
            float innerRadius = size * 0.465f;
            float outerRadius = size * 0.495f;
            Color dark = new Color(0.02f, 0.02f, 0.03f, 1.0f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                    if (dist < innerRadius)
                    {
                        colors[y * size + x] = new Color(0f, 0f, 0f, 0f); // Clear lens
                    }
                    else if (dist < outerRadius)
                    {
                        float t = (dist - innerRadius) / (outerRadius - innerRadius);
                        colors[y * size + x] = new Color(dark.r, dark.g, dark.b, t);
                    }
                    else
                    {
                        colors[y * size + x] = dark; // Solid housing
                    }
                }
            }

            tex.SetPixels(colors);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }

        private void BuildHitFeedback(Transform parent)
        {
            // Hit feedback text ("+100 BODY HIT", "+300 HEADSHOT")
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

            // Hitmarker icon (Classic X)
            hitmarkerObject = new GameObject("Hitmarker");
            hitmarkerObject.transform.SetParent(parent, false);
            RectTransform hmRT = hitmarkerObject.AddComponent<RectTransform>();
            hmRT.anchorMin = new Vector2(0.5f, 0.5f);
            hmRT.anchorMax = new Vector2(0.5f, 0.5f);
            hmRT.sizeDelta = new Vector2(30f, 30f);

            // 4 diagonals forming X
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

            btn.onClick.AddListener(onClick);
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

        public void UpdateAmmoDisplay(int current, int max)
        {
            if (ammoText != null)
            {
                string colTag = current == 0 ? "<color=red>" : "<color=#00E5FF>";
                ammoText.text = $"AMMO: {colTag}{current}</color> / ∞\n<size=15><color=#888888>[R] RELOAD</color></size>";
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
            isPaused = !isPaused;
            if (pausePanel != null)
            {
                pausePanel.SetActive(isPaused);
            }

            Time.timeScale = isPaused ? 0f : 1f;
            Cursor.lockState = isPaused ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = isPaused;
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
