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
    /// - Center: Full optical sniper scope reticle overlay.
    /// - Pause menu with Resume, Restart, and Quit.
    /// - Results debriefing screen.
    /// </summary>
    public class DEADeyeMissionHUD : MonoBehaviour
    {
        public static DEADeyeMissionHUD Instance { get; private set; }

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
                tbRT.anchorMin = new Vector2(0.02f, 0.88f);
                tbRT.anchorMax = new Vector2(0.35f, 0.98f);
                tbRT.offsetMin = Vector2.zero;
                tbRT.offsetMax = Vector2.zero;

                Image tbBg = topBar.AddComponent<Image>();
                tbBg.color = new Color(0.05f, 0.08f, 0.12f, 0.85f);

                Outline tbOutline = topBar.AddComponent<Outline>();
                tbOutline.effectColor = new Color(0.18f, 0.75f, 0.92f, 0.5f);

                timerText = CreateText(topBar.transform, "TimerText", "TIME: 02:00", 22, new Vector2(15f, -12f), new Vector2(180f, 30f), Color.white);
                scoreText = CreateText(topBar.transform, "ScoreText", "SCORE: 0000", 22, new Vector2(210f, -12f), new Vector2(200f, 30f), new Color(0.18f, 0.85f, 0.98f));
                hostilesRemainingText = CreateText(topBar.transform, "HostilesText", "HOSTILES: 8", 18, new Vector2(15f, -48f), new Vector2(180f, 25f), Color.white);
                escapesText = CreateText(topBar.transform, "EscapesText", "ESCAPED: 0 / 3", 18, new Vector2(210f, -48f), new Vector2(200f, 25f), new Color(0.95f, 0.85f, 0.2f));

                // 2. Ammo Bar (Bottom Right)
                GameObject ammoBox = new GameObject("AmmoBox");
                ammoBox.transform.SetParent(canvasGO.transform, false);
                RectTransform abRT = ammoBox.AddComponent<RectTransform>();
                abRT.anchorMin = new Vector2(0.82f, 0.04f);
                abRT.anchorMax = new Vector2(0.97f, 0.14f);
                abRT.offsetMin = Vector2.zero;
                abRT.offsetMax = Vector2.zero;

                Image abBg = ammoBox.AddComponent<Image>();
                abBg.color = new Color(0.05f, 0.08f, 0.12f, 0.85f);

                Outline abOutline = ammoBox.AddComponent<Outline>();
                abOutline.effectColor = new Color(0.18f, 0.75f, 0.92f, 0.5f);

                ammoText = CreateText(ammoBox.transform, "AmmoText", "AMMO: 5 / 5\n<size=14><color=#888888>[R] RELOAD</color></size>", 20, new Vector2(15f, -10f), new Vector2(240f, 55f), new Color(0.18f, 0.85f, 0.98f));

                // 3. Center Scope Reticle Overlay
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

        private void BuildScopeOverlay(Transform parent)
        {
            scopeOverlay = new GameObject("ScopeReticleOverlay");
            scopeOverlay.transform.SetParent(parent, false);
            RectTransform soRT = scopeOverlay.AddComponent<RectTransform>();
            soRT.anchorMin = Vector2.zero;
            soRT.anchorMax = Vector2.one;
            soRT.offsetMin = Vector2.zero;
            soRT.offsetMax = Vector2.zero;

            // Center Crosshair Lines (Ultra crisp black/red sniper lines)
            GameObject crosshairH = new GameObject("Crosshair_H");
            crosshairH.transform.SetParent(scopeOverlay.transform, false);
            RectTransform chRT = crosshairH.AddComponent<RectTransform>();
            chRT.anchorMin = new Vector2(0.2f, 0.5f);
            chRT.anchorMax = new Vector2(0.8f, 0.5f);
            chRT.sizeDelta = new Vector2(0f, 2f);
            Image chImg = crosshairH.AddComponent<Image>();
            chImg.color = new Color(0.95f, 0.15f, 0.15f, 0.85f);

            GameObject crosshairV = new GameObject("Crosshair_V");
            crosshairV.transform.SetParent(scopeOverlay.transform, false);
            RectTransform cvRT = crosshairV.AddComponent<RectTransform>();
            cvRT.anchorMin = new Vector2(0.5f, 0.15f);
            cvRT.anchorMax = new Vector2(0.5f, 0.85f);
            cvRT.sizeDelta = new Vector2(2f, 0f);
            Image cvImg = crosshairV.AddComponent<Image>();
            cvImg.color = new Color(0.95f, 0.15f, 0.15f, 0.85f);

            // Center Aiming Pip
            GameObject centerDot = new GameObject("CenterDot");
            centerDot.transform.SetParent(scopeOverlay.transform, false);
            RectTransform cdRT = centerDot.AddComponent<RectTransform>();
            cdRT.anchorMin = new Vector2(0.5f, 0.5f);
            cdRT.anchorMax = new Vector2(0.5f, 0.5f);
            cdRT.sizeDelta = new Vector2(6f, 6f);
            Image cdImg = centerDot.AddComponent<Image>();
            cdImg.color = Color.red;

            // Scoped Vignette Masks (Dark perimeter border)
            CreateVignetteBorder(scopeOverlay.transform, new Vector2(0f, 0f), new Vector2(0.22f, 1f)); // Left
            CreateVignetteBorder(scopeOverlay.transform, new Vector2(0.78f, 0f), new Vector2(1f, 1f)); // Right
            CreateVignetteBorder(scopeOverlay.transform, new Vector2(0.22f, 0.85f), new Vector2(0.78f, 1f)); // Top
            CreateVignetteBorder(scopeOverlay.transform, new Vector2(0.22f, 0f), new Vector2(0.78f, 0.15f)); // Bottom

            scopeOverlay.SetActive(false);
        }

        private void CreateVignetteBorder(Transform parent, Vector2 min, Vector2 max)
        {
            GameObject border = new GameObject("VignetteBorder");
            border.transform.SetParent(parent, false);
            RectTransform rt = border.AddComponent<RectTransform>();
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            Image img = border.AddComponent<Image>();
            img.color = new Color(0.02f, 0.02f, 0.03f, 0.95f);
        }

        private void BuildHitFeedback(Transform parent)
        {
            // Hit feedback text ("+100 BODY HIT", "+300 HEADSHOT")
            GameObject textGO = new GameObject("HitFeedbackText");
            textGO.transform.SetParent(parent, false);
            RectTransform rt = textGO.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0f, 65f);
            rt.sizeDelta = new Vector2(400f, 50f);

            hitFeedbackText = textGO.AddComponent<TextMeshProUGUI>();
            hitFeedbackText.fontSize = 26;
            hitFeedbackText.alignment = TextAlignmentOptions.Center;
            hitFeedbackText.fontStyle = FontStyles.Bold;
            hitFeedbackText.text = "";

            // Hitmarker icon (Classic X)
            hitmarkerObject = new GameObject("Hitmarker");
            hitmarkerObject.transform.SetParent(parent, false);
            RectTransform hmRT = hitmarkerObject.AddComponent<RectTransform>();
            hmRT.anchorMin = new Vector2(0.5f, 0.5f);
            hmRT.anchorMax = new Vector2(0.5f, 0.5f);
            hmRT.sizeDelta = new Vector2(28f, 28f);

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
            rt.sizeDelta = new Vector2(22f, 2.5f);
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
            bg.color = new Color(0.06f, 0.08f, 0.12f, 0.95f);

            Outline outline = pausePanel.AddComponent<Outline>();
            outline.effectColor = new Color(0.18f, 0.75f, 0.92f, 0.8f);
            outline.effectDistance = new Vector2(2f, -2f);

            CreateText(pausePanel.transform, "PauseTitle", "MISSION PAUSED", 28, new Vector2(140f, -25f), new Vector2(300f, 40f), new Color(0.18f, 0.85f, 0.98f));

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
            img.color = new Color(0.12f, 0.16f, 0.22f, 0.95f);

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
                ammoText.text = $"AMMO: {colTag}{current}</color> / {max}\n<size=14><color=#888888>[R] RELOAD</color></size>";
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

            yield return new WaitForSeconds(0.45f);

            if (hitFeedbackText != null) hitFeedbackText.text = "";
            if (hitmarkerObject != null) hitmarkerObject.SetActive(false);
        }

        public void TogglePause()
        {
            isPaused = !isPaused;
            Time.timeScale = isPaused ? 0f : 1f;

            if (pausePanel != null)
            {
                pausePanel.SetActive(isPaused);
            }

            Cursor.lockState = isPaused ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = isPaused;
        }

        public void OnResumeClicked()
        {
            if (isPaused) TogglePause();
        }

        public void OnRestartClicked()
        {
            Time.timeScale = 1f;
            UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
        }

        public void OnQuitClicked()
        {
            Time.timeScale = 1f;
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            #else
            Application.Quit();
            #endif
        }

        private void HandleMissionEnded(bool isVictory)
        {
            SetScopeOverlay(false);
            if (resultsUI != null)
            {
                resultsUI.DisplayResults(isVictory);
            }
        }
    }
}
