using UnityEngine;

namespace DEADeye
{
    /// <summary>
    /// Authoritative player controller managing the lifecycle of VR vs Desktop rigs.
    /// Strictly guarantees only one player rig is active at any time.
    /// </summary>
    public class DEADeyePlayerController : MonoBehaviour
    {
        public static DEADeyePlayerController Instance { get; private set; }

        [Header("Rig Instances")]
        [SerializeField] private GameObject vrRig;
        [SerializeField] private DEADeyeDesktopRig desktopRig;

        public GameObject VrRig => vrRig;
        public DEADeyeDesktopRig DesktopRig => desktopRig;

        public Camera ActiveCamera
        {
            get
            {
                if (desktopRig != null && desktopRig.gameObject.activeInHierarchy)
                {
                    return desktopRig.DesktopCamera;
                }
                if (vrRig != null && vrRig.activeInHierarchy)
                {
                    return vrRig.GetComponentInChildren<Camera>();
                }
                return Camera.main;
            }
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            EnforceActiveRig();
        }

        private void Start()
        {
            if (DEADeyeGameManager.Instance != null)
            {
                DEADeyeGameManager.Instance.OnPlayerModeChanged += HandlePlayerModeChanged;
            }
        }

        private void OnDestroy()
        {
            if (DEADeyeGameManager.Instance != null)
            {
                DEADeyeGameManager.Instance.OnPlayerModeChanged -= HandlePlayerModeChanged;
            }
        }

        private void HandlePlayerModeChanged(DEADeyePlayerMode mode)
        {
            EnforceActiveRig();
        }

        public void EnforceActiveRig(DEADeyePlayerMode? explicitMode = null)
        {
            DEADeyePlayerMode mode = explicitMode ?? (DEADeyeGameManager.Instance != null ? DEADeyeGameManager.Instance.PlayerMode : DEADeyePlayerMode.Desktop);

            if (mode == DEADeyePlayerMode.VR)
            {
                if (desktopRig != null) desktopRig.gameObject.SetActive(false);
                if (vrRig != null) vrRig.SetActive(true);
                Debug.Log("[DEADeyePlayerController] Active Player Rig: VR (XR Origin)");
            }
            else
            {
                if (vrRig != null) vrRig.SetActive(false);
                if (desktopRig != null) desktopRig.gameObject.SetActive(true);
                Debug.Log("[DEADeyePlayerController] Active Player Rig: Desktop FPS Rig");
            }
        }
    }
}
