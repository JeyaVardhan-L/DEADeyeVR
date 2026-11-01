using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace DEADeye
{
    /// <summary>
    /// End-of-round mission debriefing screen.
    /// Displays mission status, performance statistics, and interactive controls to retry or return to the lobby.
    /// </summary>
    public class DEADeyeResultsUI : MonoBehaviour
    {
        [Header("UI Hierarchy")]
        [SerializeField] private GameObject resultsPanel;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI subtitleText;
        [SerializeField] private TextMeshProUGUI statsText;
        [SerializeField] private Button retryButton;
        [SerializeField] private Button lobbyButton;

        private void Awake()
        {
            BuildResultsHierarchy();
            gameObject.SetActive(false);
        }

        private void BuildResultsHierarchy()
        {
            RectTransform rootRT = gameObject.AddComponent<RectTransform>();
            rootRT.anchorMin = Vector2.zero;
            rootRT.anchorMax = Vector2.one;
            rootRT.offsetMin = Vector2.zero;
            rootRT.offsetMax = Vector2.zero;

            // Background Dim
            Image dim = gameObject.AddComponent<Image>();
            dim.color = new Color(0f, 0f, 0f, 0.78f);

            // Center Debriefing Card
            resultsPanel = new GameObject("DebriefingCard");
            resultsPanel.transform.SetParent(transform, false);
            RectTransform cardRT = resultsPanel.AddComponent<RectTransform>();
            cardRT.anchorMin = new Vector2(0.28f, 0.18f);
            cardRT.anchorMax = new Vector2(0.72f, 0.82f);
            cardRT.offsetMin = Vector2.zero;
            cardRT.offsetMax = Vector2.zero;

            Image cardBg = resultsPanel.AddComponent<Image>();
            cardBg.color = new Color(0.07f, 0.09f, 0.12f, 0.96f);

            Outline cardOutline = resultsPanel.AddComponent<Outline>();
            cardOutline.effectColor = new Color(0.18f, 0.75f, 0.92f, 0.75f);
            cardOutline.effectDistance = new Vector2(2f, -2f);

            // Title
            titleText = CreateText(resultsPanel.transform, "Title", "MISSION ACCOMPLISHED", 32, TextAlignmentOptions.Center,
                new Vector2(0f, -30f), new Vector2(600f, 50f), new Color(0.18f, 0.95f, 0.45f));

            subtitleText = CreateText(resultsPanel.transform, "Subtitle", "CITY SECTOR DEFENDED", 18, TextAlignmentOptions.Center,
                new Vector2(0f, -80f), new Vector2(600f, 30f), Color.white);

            // Stats breakdown
            statsText = CreateText(resultsPanel.transform, "StatsText",
                "FINAL SCORE: <b>1,250 pts</b>\n\n" +
                "HOSTILES ELIMINATED: <b>8</b>\n" +
                "HOSTILES ESCAPED: <b>0</b>\n" +
                "PRECISION HEADSHOTS: <b>5</b>\n" +
                "SHOOTING ACCURACY: <b>85.7%</b>",
                20, TextAlignmentOptions.Center, new Vector2(0f, -140f), new Vector2(600f, 220f), Color.white);

            // Retry Button
            retryButton = CreateButton(resultsPanel.transform, "Btn_Retry", "▶  RETRY MISSION",
                new Vector2(-160f, -420f), new Vector2(240f, 55f), OnRetryClicked);
            retryButton.GetComponent<Image>().color = new Color(0.0f, 0.65f, 0.85f);

            // Return to Lobby Button
            lobbyButton = CreateButton(resultsPanel.transform, "Btn_Lobby", "RETURN TO DOJO LOBBY",
                new Vector2(160f, -420f), new Vector2(240f, 55f), OnLobbyClicked);
            lobbyButton.GetComponent<Image>().color = new Color(0.22f, 0.25f, 0.30f);
        }

        private TextMeshProUGUI CreateText(Transform parent, string name, string content, float fontSize,
            TextAlignmentOptions alignment, Vector2 anchoredPos, Vector2 size, Color color)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);

            RectTransform rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;

            TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = content;
            tmp.fontSize = fontSize;
            tmp.alignment = alignment;
            tmp.color = color;
            tmp.fontStyle = FontStyles.Bold;
            return tmp;
        }

        private Button CreateButton(Transform parent, string name, string label, Vector2 anchoredPos, Vector2 size, UnityEngine.Events.UnityAction onClick)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);

            RectTransform rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;

            Image img = go.AddComponent<Image>();
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
            tmp.text = label;
            tmp.fontSize = 18;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            tmp.fontStyle = FontStyles.Bold;

            btn.onClick.AddListener(onClick);
            return btn;
        }

        public void DisplayResults(bool isVictory)
        {
            gameObject.SetActive(true);

            // Unlock mouse cursor for UI selection on desktop
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            if (titleText != null)
            {
                titleText.text = isVictory ? "MISSION ACCOMPLISHED" : "MISSION FAILED";
                titleText.color = isVictory ? new Color(0.18f, 0.95f, 0.45f) : new Color(0.95f, 0.22f, 0.22f);
            }

            if (subtitleText != null)
            {
                subtitleText.text = isVictory ? "SECTOR QUARANTINED — HOSTILES INTERCEPTED" : "CRITICAL BREACH — ESCAPES EXCEEDED THRESHOLD";
            }

            if (statsText != null)
            {
                int score = DEADeyeScoreManager.Instance != null ? DEADeyeScoreManager.Instance.CurrentScore : 0;
                int eliminated = DEADeyeScoreManager.Instance != null ? DEADeyeScoreManager.Instance.EnemiesEliminated : 0;
                int escaped = DEADeyeScoreManager.Instance != null ? DEADeyeScoreManager.Instance.EnemiesEscaped : 0;
                int headshots = DEADeyeScoreManager.Instance != null ? DEADeyeScoreManager.Instance.Headshots : 0;
                float accuracy = DEADeyeScoreManager.Instance != null ? DEADeyeScoreManager.Instance.AccuracyPercentage : 0f;

                statsText.text =
                    $"FINAL SCORE: <b>{score:N0} pts</b>\n\n" +
                    $"HOSTILES ELIMINATED: <b>{eliminated}</b>\n" +
                    $"HOSTILES ESCAPED: <b>{escaped}</b>\n" +
                    $"HEADSHOT TAKEDOWNS: <b>{headshots}</b>\n" +
                    $"SHOOTING ACCURACY: <b>{accuracy:F1}%</b>";
            }
        }

        public void OnRetryClicked()
        {
            if (DEADeyeSceneTransition.Instance != null)
            {
                DEADeyeSceneTransition.Instance.LoadRooftopMission();
            }
            else
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene(DEADeyeSceneTransition.SCENE_ROOFTOP);
            }
        }

        public void OnLobbyClicked()
        {
            if (DEADeyeSceneTransition.Instance != null)
            {
                DEADeyeSceneTransition.Instance.LoadLobby();
            }
            else
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene(DEADeyeSceneTransition.SCENE_LOBBY);
            }
        }
    }
}

