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
            if (characterController != null)
            {
                characterController.stepOffset = 0.4f;
                characterController.slopeLimit = 45f;
                characterController.minMoveDistance = 0f;
            }

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
            // Desktop Mode: Start with cursor unlocked and visible so player can interact with UI
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

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
            HandleAiming();
            HandleMouseLook();
            HandleMovement();
            HandleWeaponInput();
        }

        private void HandleMouseLook()
        {
            // CRITICAL REQUIREMENT: Camera look / aiming is ONLY enabled while Left Alt is held!
            // When Left Alt is pressed / held:
            // - Hide and lock cursor.
            // - Enable camera rotation (yaw & pitch).
            // - Does NOT change zoom state.
            // - Does NOT fire the weapon.
            // - Does NOT trigger UI buttons.
            // When Left Alt is released:
            // - Immediately stop camera rotation.
            // - Unlock and show the cursor.
            // - Allow normal UI interaction.
            // RMB controls scope zoom only, completely independent of camera look.

            bool isFreeLooking = DEADeyeInputRouter.Instance != null && DEADeyeInputRouter.Instance.GetFreeLookHeld();

            if (!isFreeLooking)
            {
                if (Cursor.lockState != CursorLockMode.None)
                {
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                }
                return;
            }

            if (Cursor.lockState != CursorLockMode.Locked)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }

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

                if (characterController.isGrounded)
                {
                    if (velocity.y < 0f) velocity.y = -2f;
                    if (DEADeyeInputRouter.Instance != null && DEADeyeInputRouter.Instance.GetJumpDown())
                    {
                        velocity.y = 4.8f; // Smooth jump impulse
                    }
                }
                velocity.y += gravity * Time.deltaTime;

                Vector3 fullMove = (horizontalMove + Vector3.up * velocity.y) * Time.deltaTime;
                characterController.Move(fullMove);
            }
        }

        private bool overrideAiming = false;
        private bool forcedAimState = false;

        public void SetAiming(bool aiming)
        {
            overrideAiming = true;
            forcedAimState = aiming;
            isAiming = aiming;
            if (equippedWeapon != null && equippedWeapon.ScopeOptic != null)
            {
                equippedWeapon.ScopeOptic.SetScopeActive(isAiming);
            }
            if (DEADeyeMissionHUD.Instance != null)
            {
                DEADeyeMissionHUD.Instance.SetScopeOverlay(isAiming);
            }
        }

        public void ClearAimOverride()
        {
            overrideAiming = false;
        }

        private void HandleAiming()
        {
            if (overrideAiming)
            {
                isAiming = forcedAimState;
            }
            else
            {
                isAiming = DEADeyeInputRouter.Instance != null && DEADeyeInputRouter.Instance.GetAimHeld();
            }

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

            // Tactical Terminal toggle on Y key
            if (DEADeyeInputRouter.Instance != null && DEADeyeInputRouter.Instance.GetTerminalToggleDown())
            {
                if (DEADeyeTacticalTerminalUI.Instance != null)
                {
                    DEADeyeTacticalTerminalUI.Instance.ToggleTerminal();
                }
            }

            // Reload key (R) - Always responsive in desktop mode, regardless of aiming or cursor lock state!
            if (DEADeyeInputRouter.Instance != null && DEADeyeInputRouter.Instance.GetReloadDown())
            {
                if (DEADeyeWeaponManager.Instance != null)
                {
                    DEADeyeWeaponManager.Instance.Reload();
                }
            }

            // Fire on LMB - Only fire if NOT holding Left Alt (free look) and NOT clicking on UI
            bool isFreeLooking = DEADeyeInputRouter.Instance != null && DEADeyeInputRouter.Instance.GetFreeLookHeld();
            if (!isFreeLooking)
            {
                if (DEADeyeInputRouter.Instance != null && DEADeyeInputRouter.Instance.GetFireDown())
                {
                    if (equippedWeapon != null)
                    {
                        equippedWeapon.TryFire();
                    }
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
