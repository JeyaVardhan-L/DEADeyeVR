using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace DEADeye
{
    /// <summary>
    /// In-mission HUD displaying live telemetry: countdown clock, remaining hostiles,
    /// escape allowance meter, current score, ammo count, and end-of-mission results.
    /// Supports both Screen-Space and World-Space canvas mounting.
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

        [Header("Results Panel")]
        [SerializeField] private DEADeyeResultsUI resultsUI;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
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
            if (DEADeyeMissionDirector.Instance != null)
            {
                DEADeyeMissionDirector.Instance.OnTimerTick -= UpdateTimerDisplay;
                DEADeyeMissionDirector.Instance.OnMissionEnded -= HandleMissionEnded;
            }
            if (DEADeyeScoreManager.Instance != null)
            {
                DEADeyeScoreManager.Instance.OnScoreUpdated -= UpdateScoreDisplay;
                DEADeyeScoreManager.Instance.OnHostileCountUpdated -= UpdateHostileCountDisplay;
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

                // Telemetry Bar (Top Left)
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

                // Ammo Bar (Bottom Right)
                GameObject ammoBox = new GameObject("AmmoBox");
                ammoBox.transform.SetParent(canvasGO.transform, false);
                RectTransform abRT = ammoBox.AddComponent<RectTransform>();
                abRT.anchorMin = new Vector2(0.85f, 0.04f);
                abRT.anchorMax = new Vector2(0.97f, 0.12f);
                abRT.offsetMin = Vector2.zero;
                abRT.offsetMax = Vector2.zero;

                Image abBg = ammoBox.AddComponent<Image>();
                abBg.color = new Color(0.05f, 0.08f, 0.12f, 0.85f);

                ammoText = CreateText(ammoBox.transform, "AmmoText", "AMMO: 5 / 5", 22, new Vector2(15f, -15f), new Vector2(180f, 35f), new Color(0.18f, 0.85f, 0.98f));

                // Results UI
                GameObject resultsGO = new GameObject("ResultsUI");
                resultsGO.transform.SetParent(canvasGO.transform, false);
                resultsUI = resultsGO.AddComponent<DEADeyeResultsUI>();
            }
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
                ammoText.text = $"AMMO: {colTag}{current}</color> / {max}";
            }
        }

        private void HandleMissionEnded(bool isVictory)
        {
            if (resultsUI != null)
            {
                resultsUI.DisplayResults(isVictory);
            }
        }
    }
}

