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
                Destroy(this);
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

            // Ensure weapon socketing and rig enforcement after initial scene startup
            EnforceActiveRig();
        }

        private void Update()
        {
            // VR-specific global menu and interface toggles
            if (DEADeyeGameManager.Instance != null && DEADeyeGameManager.Instance.PlayerMode == DEADeyePlayerMode.VR)
            {
                HandleVRGlobalInput();
            }
        }

        private void HandleVRGlobalInput()
        {
            if (DEADeyeInputRouter.Instance == null) return;

            // Pause toggle on Left Controller Menu Button
            if (DEADeyeInputRouter.Instance.GetPauseDown())
            {
                if (DEADeyeMissionHUD.Instance != null)
                {
                    DEADeyeMissionHUD.Instance.TogglePause();
                }
            }

            // Mission Info toggle on Right Controller Button B
            if (DEADeyeInputRouter.Instance.GetMissionInfoToggleDown())
            {
                if (DEADeyeMissionHUD.Instance != null)
                {
                    DEADeyeMissionHUD.Instance.ToggleMissionInfo();
                }
            }

            // Tactical Terminal toggle on Left Controller Button Y
            if (DEADeyeInputRouter.Instance.GetTerminalToggleDown())
            {
                if (DEADeyeTacticalTerminalUI.Instance != null)
                {
                    DEADeyeTacticalTerminalUI.Instance.ToggleTerminal();
                }
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }

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
                if (vrRig != null)
                {
                    vrRig.SetActive(true);
                    AttachWeaponToVRHand();
                }
                Debug.Log("[DEADeyePlayerController] Active Player Rig: VR (XR Origin)");
            }
            else
            {
                if (vrRig != null) vrRig.SetActive(false);
                if (desktopRig != null)
                {
                    desktopRig.gameObject.SetActive(true);
                    AttachWeaponToDesktopRig();
                }
                Debug.Log("[DEADeyePlayerController] Active Player Rig: Desktop FPS Rig");
            }
        }

        private void AttachWeaponToVRHand()
        {
            if (vrRig == null) return;

            var weapon = FindFirstObjectByType<DEADeyeSniperWeapon>();
            if (weapon == null) return;

            // Find Right Controller inside vrRig
            Transform rightController = null;
            Transform[] allChildren = vrRig.GetComponentsInChildren<Transform>(true);
            foreach (var t in allChildren)
            {
                if (t.name == "Right Controller")
                {
                    rightController = t;
                    break;
                }
            }

            if (rightController != null)
            {
                Transform socket = rightController.Find("VRWeaponSocket");
                if (socket == null)
                {
                    GameObject socketGO = new GameObject("VRWeaponSocket");
                    socketGO.transform.SetParent(rightController, false);
                    // Natural 1-to-1 rifle grip pose in right controller hand
                    socketGO.transform.localPosition = new Vector3(0f, -0.04f, 0.12f);
                    socketGO.transform.localRotation = Quaternion.identity;
                    socket = socketGO.transform;
                }

                weapon.transform.SetParent(socket, false);
                weapon.transform.localPosition = Vector3.zero;
                weapon.transform.localRotation = Quaternion.identity;

                // Lock physics so the weapon doesn't drop to floor
                Rigidbody rb = weapon.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.isKinematic = true;
                    rb.useGravity = false;
                }

                Debug.Log($"[DEADeyePlayerController] Mounted {weapon.name} to VR Right Controller at: {socket.name}");
            }
        }

        private void AttachWeaponToDesktopRig()
        {
            if (desktopRig == null) return;
            var weapon = FindFirstObjectByType<DEADeyeSniperWeapon>();
            if (weapon != null)
            {
                desktopRig.AttachWeapon(weapon.transform);
            }
        }
    }
}
