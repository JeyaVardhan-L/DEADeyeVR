using System.Collections;
using UnityEngine;

namespace DEADeye
{
    /// <summary>
    /// Boot scene initializer.
    /// Ensures all persistent game systems are ready before routing into the Dojo Lobby.
    /// </summary>
    public class DEADeyeBootLoader : MonoBehaviour
    {
        [SerializeField] private float bootDelaySeconds = 0.1f;

        private IEnumerator Start()
        {
            Debug.Log("[DEADeyeBootLoader] Initializing DEADeye VR Core Systems...");

            if (DEADeyeGameManager.Instance != null)
            {
                DEADeyeGameManager.Instance.SetGameState(DEADeyeGameState.Boot);
            }

            yield return new WaitForSeconds(bootDelaySeconds);

            Debug.Log("[DEADeyeBootLoader] Boot sequence completed. Routing to Dojo Lobby...");
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
