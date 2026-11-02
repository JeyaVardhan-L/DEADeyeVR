using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DEADeye
{
    /// <summary>
    /// Authoritative central manager for DEADeye VR.
    /// Manages game lifecycle, global player mode, difficulty, and persistent settings.
    /// </summary>
    public class DEADeyeGameManager : MonoBehaviour
    {
        public static DEADeyeGameManager Instance
        {
            get => s_Instance;
            set => s_Instance = value;
        }
        private static DEADeyeGameManager s_Instance;

        [Header("State")]
        [SerializeField] private DEADeyeGameState currentState = DEADeyeGameState.MissionActive;
        [SerializeField] private DEADeyePlayerMode playerMode = DEADeyePlayerMode.Desktop;
        [SerializeField] private MissionDifficulty difficulty = MissionDifficulty.Operative;
        [SerializeField] private string selectedLoadout = "XM500_Sniper";

        public DEADeyeGameState CurrentState => currentState;
        public DEADeyePlayerMode PlayerMode => playerMode;
        public MissionDifficulty Difficulty => difficulty;
        public string SelectedLoadout => selectedLoadout;

        public event Action<DEADeyeGameState> OnGameStateChanged;
        public event Action<DEADeyePlayerMode> OnPlayerModeChanged;
        public event Action<MissionDifficulty> OnDifficultyChanged;
        public event Action<string> OnLoadoutChanged;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void AutoInitialize()
        {
            if (Instance == null)
            {
                var existing = FindFirstObjectByType<DEADeyeGameManager>();
                if (existing == null)
                {
                    GameObject go = new GameObject("DEADeyeGameManager");
                    go.AddComponent<DEADeyeGameManager>();
                }
            }
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;

            // Auto-detect VR vs Desktop mode
            DetectPlayerMode();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void DetectPlayerMode()
        {
            // If OpenXR or XR display is active and tracking, default to VR; otherwise Desktop
            bool xrActive = UnityEngine.XR.XRSettings.isDeviceActive;
            #if UNITY_ANDROID && !UNITY_EDITOR
            playerMode = DEADeyePlayerMode.VR;
            #else
            playerMode = xrActive ? DEADeyePlayerMode.VR : DEADeyePlayerMode.Desktop;
            #endif
        }

        public void SetGameState(DEADeyeGameState newState)
        {
            if (currentState == newState) return;
            currentState = newState;
            Debug.Log($"[DEADeyeGameManager] Game state changed to: {newState}");
            OnGameStateChanged?.Invoke(currentState);
        }

        public void SetPlayerMode(DEADeyePlayerMode newMode)
        {
            if (playerMode == newMode) return;
            playerMode = newMode;
            Debug.Log($"[DEADeyeGameManager] Player mode changed to: {newMode}");
            OnPlayerModeChanged?.Invoke(playerMode);
        }

        public void SetDifficulty(MissionDifficulty newDifficulty)
        {
            if (difficulty == newDifficulty) return;
            difficulty = newDifficulty;
            Debug.Log($"[DEADeyeGameManager] Difficulty set to: {newDifficulty}");
            OnDifficultyChanged?.Invoke(difficulty);
        }

        public void SetLoadout(string loadout)
        {
            if (selectedLoadout == loadout) return;
            selectedLoadout = loadout;
            Debug.Log($"[DEADeyeGameManager] Loadout set to: {loadout}");
            OnLoadoutChanged?.Invoke(selectedLoadout);
        }
    }
}
