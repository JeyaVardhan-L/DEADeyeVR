using UnityEngine;
using UnityEngine.XR;
using Unity.XR.CoreUtils;
using UnityEngine.InputSystem.XR;

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

        [Header("VR Rig Tracking References")]
        private Camera vrCamera;
        private Transform vrCameraOffset;
        private TrackedPoseDriver vrTrackedPoseDriver;
        private XROrigin vrXROrigin;

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
            // VR-specific global menu, interface toggles and head tracking
            if (DEADeyeGameManager.Instance != null && DEADeyeGameManager.Instance.PlayerMode == DEADeyePlayerMode.VR)
            {
                HandleVRGlobalInput();
                UpdateVRHeadTracking();
            }
        }

        private void LateUpdate()
        {
            // Enforce VR camera head tracking and height alignment before render
            if (DEADeyeGameManager.Instance != null && DEADeyeGameManager.Instance.PlayerMode == DEADeyePlayerMode.VR)
            {
                UpdateVRHeadTracking();
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
                    ConfigureVRRigAndTracking();
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

        private void ConfigureVRRigAndTracking()
        {
            if (vrRig == null) return;

            // 1. Locate XROrigin & enforce Floor origin mode with 1.7m standard eye height
            vrXROrigin = vrRig.GetComponent<XROrigin>();
            if (vrXROrigin != null)
            {
                vrXROrigin.RequestedTrackingOriginMode = XROrigin.TrackingOriginMode.Floor;
                vrXROrigin.CameraYOffset = 1.7f;
            }

            // 2. Locate Camera Offset
            vrCameraOffset = vrRig.transform.Find("Camera Offset");

            // 3. Locate Main Camera & TrackedPoseDriver
            vrCamera = vrRig.GetComponentInChildren<Camera>(true);
            if (vrCamera != null)
            {
                vrTrackedPoseDriver = vrCamera.GetComponent<TrackedPoseDriver>();
                if (vrTrackedPoseDriver != null)
                {
                    // Ensure TrackedPoseDriver does not mute updates due to trackingState evaluation
                    vrTrackedPoseDriver.ignoreTrackingState = true;

                    // Explicitly enable input actions if referenced
                    if (vrTrackedPoseDriver.positionInput.action != null && !vrTrackedPoseDriver.positionInput.action.enabled)
                    {
                        vrTrackedPoseDriver.positionInput.action.Enable();
                    }
                    if (vrTrackedPoseDriver.rotationInput.action != null && !vrTrackedPoseDriver.rotationInput.action.enabled)
                    {
                        vrTrackedPoseDriver.rotationInput.action.Enable();
                    }
                    if (vrTrackedPoseDriver.trackingStateInput.action != null && !vrTrackedPoseDriver.trackingStateInput.action.enabled)
                    {
                        vrTrackedPoseDriver.trackingStateInput.action.Enable();
                    }
                }
            }

            Debug.Log("[DEADeyePlayerController] VR Camera & Tracking configured: XROrigin=Floor (yOffset=1.7), TrackedPoseDriver.ignoreTrackingState=true");
        }

        private void UpdateVRHeadTracking()
        {
            if (vrCamera == null)
            {
                if (vrRig != null) vrCamera = vrRig.GetComponentInChildren<Camera>();
                if (vrCamera == null) return;
            }

            // Direct hardware query from OpenXR CenterEye node
            InputDevice centerEye = InputDevices.GetDeviceAtXRNode(XRNode.CenterEye);
            if (!centerEye.isValid)
            {
                centerEye = InputDevices.GetDeviceAtXRNode(XRNode.Head);
            }

            if (centerEye.isValid)
            {
                // 1. Head rotation: 360-degree look rotation
                if (centerEye.TryGetFeatureValue(CommonUsages.centerEyeRotation, out Quaternion headRot) ||
                    centerEye.TryGetFeatureValue(CommonUsages.deviceRotation, out headRot))
                {
                    if (headRot != Quaternion.identity)
                    {
                        vrCamera.transform.localRotation = headRot;
                    }
                }

                // 2. Head position: 1-to-1 physical tracking
                if (centerEye.TryGetFeatureValue(CommonUsages.centerEyePosition, out Vector3 headPos) ||
                    centerEye.TryGetFeatureValue(CommonUsages.devicePosition, out headPos))
                {
                    if (headPos.sqrMagnitude > 0.0001f)
                    {
                        vrCamera.transform.localPosition = headPos;
                    }
                }
            }

            // 3. Viewpoint height safeguard:
            // If the combined camera height (offset + localPos) is near floor level (< 1.3m),
            // lift the Camera Offset so the player stands naturally at eye height (1.7m) overlooking the city
            if (vrCameraOffset != null)
            {
                float currentTotalHeight = vrCameraOffset.localPosition.y + vrCamera.transform.localPosition.y;
                if (currentTotalHeight < 1.3f)
                {
                    Vector3 offsetPos = vrCameraOffset.localPosition;
                    offsetPos.y = Mathf.Max(0f, 1.7f - vrCamera.transform.localPosition.y);
                    vrCameraOffset.localPosition = offsetPos;
                }
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
