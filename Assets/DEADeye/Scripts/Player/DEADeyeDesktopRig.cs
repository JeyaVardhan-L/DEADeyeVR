using UnityEngine;

namespace DEADeye
{
    /// <summary>
    /// Dedicated First-Person Desktop Controller for keyboard & mouse gameplay.
    /// Provides smooth WASD locomotion, sprint modifier, clamped mouse look, weapon holding, and interaction.
    /// Mutually exclusive with the XR Origin rig.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class DEADeyeDesktopRig : MonoBehaviour
    {
        [Header("Camera & Vision")]
        [SerializeField] private Camera desktopCamera;
        [SerializeField] private Transform cameraPivot;
        [SerializeField] private float moveSpeed = 3.6f;
        [SerializeField] private float sprintMultiplier = 1.5f;
        [SerializeField] private float gravity = -9.81f;
        [SerializeField] private float minPitch = -85f;
        [SerializeField] private float maxPitch = 85f;

        [Header("Weapon Mount")]
        [SerializeField] private DEADeyeSniperWeapon equippedWeapon;
        [SerializeField] private Transform weaponSocket;
        [SerializeField] private Vector3 hipPosition = new Vector3(0.24f, -0.22f, 0.62f);
        [SerializeField] private Vector3 aimPosition = new Vector3(0f, -0.16f, 0.35f);
        [SerializeField] private float aimTransitionSpeed = 14f;

        private CharacterController characterController;
        private float pitch = 0f;
        private float yaw = 0f;
        private Vector3 velocity = Vector3.zero;
        private bool isAiming = false;

        public Camera DesktopCamera => desktopCamera;
        public Transform WeaponSocket => weaponSocket;
        public DEADeyeSniperWeapon EquippedWeapon => equippedWeapon;
        public bool IsAiming => isAiming;

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();

            if (desktopCamera == null)
            {
                desktopCamera = GetComponentInChildren<Camera>();
            }

            if (desktopCamera != null)
            {
                desktopCamera.tag = "MainCamera";
            }

            if (cameraPivot == null && desktopCamera != null)
            {
                cameraPivot = desktopCamera.transform;
            }

            if (weaponSocket == null && cameraPivot != null)
            {
                GameObject socket = new GameObject("DesktopWeaponSocket");
                socket.transform.SetParent(cameraPivot, false);
                socket.transform.localPosition = hipPosition;
                weaponSocket = socket.transform;
            }

            yaw = transform.eulerAngles.y;
            pitch = 14f; // Naturally angled down toward street intersection
            if (cameraPivot != null)
            {
                cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
            }
        }

        private void Start()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            if (equippedWeapon == null)
            {
                equippedWeapon = FindFirstObjectByType<DEADeyeSniperWeapon>();
            }

            if (equippedWeapon != null)
            {
                AttachWeapon(equippedWeapon.transform);
            }
        }

        private void Update()
        {
            HandleMouseLook();
            HandleMovement();
            HandleAiming();
            HandleWeaponInput();
        }

        private void HandleMouseLook()
        {
            // Re-capture cursor if user clicks on the game view during active gameplay
            if (UnityEngine.InputSystem.Mouse.current != null && UnityEngine.InputSystem.Mouse.current.leftButton.wasPressedThisFrame)
            {
                bool isPaused = DEADeyeMissionHUD.Instance != null && DEADeyeMissionHUD.Instance.IsPaused;
                bool isMissionActive = DEADeyeGameManager.Instance != null && DEADeyeGameManager.Instance.CurrentState == DEADeyeGameState.MissionActive;
                bool isOverUI = DEADeyeInputRouter.Instance != null && DEADeyeInputRouter.Instance.IsPointerOverUI();

                if (isMissionActive && !isPaused && !isOverUI && Cursor.lockState != CursorLockMode.Locked)
                {
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                }
            }

            if (Cursor.lockState != CursorLockMode.Locked) return;

            Vector2 lookInput = Vector2.zero;
            if (DEADeyeInputRouter.Instance != null)
            {
                lookInput = DEADeyeInputRouter.Instance.GetLookInput();
            }
            else if (UnityEngine.InputSystem.Mouse.current != null)
            {
                Vector2 delta = UnityEngine.InputSystem.Mouse.current.delta.ReadValue();
                lookInput = new Vector2(delta.x * 0.12f, delta.y * 0.12f);
            }

            // Lower sensitivity when zoomed in scope (proportional to FOV magnification)
            float sensMultiplier = isAiming ? 0.35f : 1.0f;
            yaw = Mathf.Repeat(yaw + lookInput.x * sensMultiplier, 360f);
            pitch -= lookInput.y * sensMultiplier;
            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

            transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            if (cameraPivot != null)
            {
                cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
            }
        }

        public void SetLookRotation(float targetPitch, float targetYaw)
        {
            pitch = Mathf.Clamp(targetPitch, minPitch, maxPitch);
            yaw = Mathf.Repeat(targetYaw, 360f);
            transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            if (cameraPivot != null)
            {
                cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
            }
        }

        public void LookAtTarget(Vector3 worldTarget)
        {
            Transform camT = DesktopCamera != null ? DesktopCamera.transform : transform;
            Vector3 dir = (worldTarget - camT.position).normalized;
            if (dir.sqrMagnitude > 0.0001f)
            {
                Quaternion lookRot = Quaternion.LookRotation(dir);
                Vector3 euler = lookRot.eulerAngles;
                float targetPitch = euler.x > 180f ? euler.x - 360f : euler.x;
                float targetYaw = euler.y;
                SetLookRotation(targetPitch, targetYaw);
            }
        }

        private void HandleMovement()
        {
            if (characterController != null && characterController.enabled)
            {
                Vector2 moveInput = Vector2.zero;
                if (DEADeyeInputRouter.Instance != null)
                {
                    moveInput = DEADeyeInputRouter.Instance.GetMoveInput();
                }

                bool isSprinting = DEADeyeInputRouter.Instance != null && DEADeyeInputRouter.Instance.GetSprintHeld();
                float currentSpeed = moveSpeed * (isSprinting ? sprintMultiplier : 1.0f);
                if (isAiming) currentSpeed *= 0.45f;

                Vector3 moveDirection = (transform.right * moveInput.x + transform.forward * moveInput.y).normalized;
                Vector3 horizontalMove = moveDirection * currentSpeed;

                if (characterController.isGrounded && velocity.y < 0f)
                {
                    velocity.y = -2f;
                }
                velocity.y += gravity * Time.deltaTime;

                Vector3 fullMove = (horizontalMove + Vector3.up * velocity.y) * Time.deltaTime;
                characterController.Move(fullMove);
            }
        }

        private void HandleAiming()
        {
            isAiming = DEADeyeInputRouter.Instance != null && DEADeyeInputRouter.Instance.GetAimHeld();

            if (equippedWeapon != null && equippedWeapon.ScopeOptic != null)
            {
                equippedWeapon.ScopeOptic.SetScopeActive(isAiming);
            }

            if (DEADeyeMissionHUD.Instance != null)
            {
                DEADeyeMissionHUD.Instance.SetScopeOverlay(isAiming);
            }

            if (weaponSocket != null)
            {
                Vector3 targetPos = isAiming ? aimPosition : hipPosition;
                weaponSocket.localPosition = Vector3.Lerp(weaponSocket.localPosition, targetPos, Time.deltaTime * aimTransitionSpeed);
            }

            if (desktopCamera != null)
            {
                float targetFov = isAiming ? 32f : 60f;
                desktopCamera.fieldOfView = Mathf.Lerp(desktopCamera.fieldOfView, targetFov, Time.deltaTime * aimTransitionSpeed);
            }
        }

        private void HandleWeaponInput()
        {
            // Pause toggle on Escape
            if (DEADeyeInputRouter.Instance != null && DEADeyeInputRouter.Instance.GetPauseDown())
            {
                if (DEADeyeMissionHUD.Instance != null)
                {
                    DEADeyeMissionHUD.Instance.TogglePause();
                }
                return;
            }

            if (Cursor.lockState != CursorLockMode.Locked) return;

            // Fire on LMB
            if (DEADeyeInputRouter.Instance != null && DEADeyeInputRouter.Instance.GetFireDown())
            {
                if (equippedWeapon != null)
                {
                    equippedWeapon.TryFire();
                }
            }

            // Reload key (R)
            if (DEADeyeInputRouter.Instance != null && DEADeyeInputRouter.Instance.GetReloadDown())
            {
                if (DEADeyeWeaponManager.Instance != null)
                {
                    DEADeyeWeaponManager.Instance.Reload();
                }
            }
        }

        public void AttachWeapon(Transform weaponTransform)
        {
            if (weaponSocket == null || weaponTransform == null) return;
            weaponTransform.SetParent(weaponSocket, false);
            weaponTransform.localPosition = Vector3.zero;
            weaponTransform.localRotation = Quaternion.identity;
        }
    }
}
