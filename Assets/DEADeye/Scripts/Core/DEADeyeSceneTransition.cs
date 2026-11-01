using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DEADeye
{
    /// <summary>
    /// Handles asynchronous scene transitions and state orchestration.
    /// </summary>
    public class DEADeyeSceneTransition : MonoBehaviour
    {
        public static DEADeyeSceneTransition Instance { get; private set; }

        public const string SCENE_BOOT = "BootScene";
        public const string SCENE_LOBBY = "LobbyScene";
        public const string SCENE_ROOFTOP = "RooftopMission";

        [SerializeField] private float transitionDelay = 0.05f;
        private bool isTransitioning = false;

        public bool IsTransitioning => isTransitioning;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void AutoInitialize()
        {
            if (Instance == null)
            {
                var existing = FindFirstObjectByType<DEADeyeSceneTransition>();
                if (existing == null)
                {
                    GameObject go = new GameObject("DEADeyeSceneTransition");
                    go.AddComponent<DEADeyeSceneTransition>();
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
            DontDestroyOnLoad(gameObject);
        }

        public void LoadLobby()
        {
            StartCoroutine(TransitionRoutine(SCENE_LOBBY, DEADeyeGameState.Lobby));
        }

        public void LoadRooftopMission()
        {
            StartCoroutine(TransitionRoutine(SCENE_ROOFTOP, DEADeyeGameState.MissionActive));
        }

        public void LoadBoot()
        {
            StartCoroutine(TransitionRoutine(SCENE_BOOT, DEADeyeGameState.Boot));
        }

        private IEnumerator TransitionRoutine(string sceneName, DEADeyeGameState targetState)
        {
            if (isTransitioning) yield break;
            isTransitioning = true;

            Debug.Log($"[DEADeyeSceneTransition] Starting transition to {sceneName} ({targetState})...");

            if (transitionDelay > 0f)
            {
                yield return new WaitForSeconds(transitionDelay);
            }

            AsyncOperation asyncOp = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
            if (asyncOp != null)
            {
                while (!asyncOp.isDone)
                {
                    yield return null;
                }
            }

            Scene loadedScene = SceneManager.GetSceneByName(sceneName);
            if (loadedScene.IsValid())
            {
                SceneManager.SetActiveScene(loadedScene);
            }

            if (DEADeyeGameManager.Instance != null)
            {
                DEADeyeGameManager.Instance.SetGameState(targetState);
            }

            isTransitioning = false;
            Debug.Log($"[DEADeyeSceneTransition] Transition to {sceneName} completed.");
        }
    }
}
