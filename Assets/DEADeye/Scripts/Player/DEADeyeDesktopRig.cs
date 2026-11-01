using UnityEngine;

namespace DEADeye
{
    /// <summary>
    /// Dedicated First-Person Desktop Controller for keyboard & mouse gameplay.
    /// Provides smooth WASD locomotion, clamped mouse look, weapon holding, and interaction.
    /// Mutually exclusive with the XR Origin rig.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class DEADeyeDesktopRig : MonoBehaviour
    {
        [Header("Camera & Vision")]
        [SerializeField] private Camera desktopCamera;
        [SerializeField] private Transform cameraPivot;
        [SerializeField] private float moveSpeed = 4.0f;
        [SerializeField] private float gravity = -9.81f;
        [SerializeField] private float mouseSensitivity = 2.0f;
        [SerializeField] private float minPitch = -80f;
        [SerializeField] private float maxPitch = 80f;

        [Header("Weapon Mount")]
        [SerializeField] private Transform weaponSocket;
        [SerializeField] private Vector3 hipPosition = new Vector3(0.22f, -0.25f, 0.55f);
        [SerializeField] private Vector3 aimPosition = new Vector3(0f, -0.15f, 0.40f);
        [SerializeField] private float aimTransitionSpeed = 12f;

        private CharacterController characterController;
        private float pitch = 0f;
        private float yaw = 0f;
        private Vector3 velocity = Vector3.zero;
        private bool isAiming = false;

        public Camera DesktopCamera => desktopCamera;
        public Transform WeaponSocket => weaponSocket;
        public bool IsAiming => isAiming;

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();

            if (desktopCamera == null)
            {
                desktopCamera = GetComponentInChildren<Camera>();
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
        }

        private void Start()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void Update()
        {
            HandleMouseLook();
            HandleMovement();
            HandleAiming();
            HandleWeaponInput();

            // Unlock cursor if Escape is pressed
            if (DEADeyeInputRouter.Instance != null && DEADeyeInputRouter.Instance.GetPauseDown())
            {
                Cursor.lockState = Cursor.lockState == CursorLockMode.Locked ? CursorLockMode.None : CursorLockMode.Locked;
                Cursor.visible = Cursor.lockState != CursorLockMode.Locked;
            }
        }

        private void HandleMouseLook()
        {
            if (Cursor.lockState != CursorLockMode.Locked) return;

            Vector2 lookInput = Vector2.zero;
            if (DEADeyeInputRouter.Instance != null)
            {
                lookInput = DEADeyeInputRouter.Instance.GetLookInput();
            }
            else if (UnityEngine.InputSystem.Mouse.current != null)
            {
                lookInput = UnityEngine.InputSystem.Mouse.current.delta.ReadValue() * (mouseSensitivity * 0.1f);
            }

            yaw += lookInput.x;
            pitch -= lookInput.y;
            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

            transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            if (cameraPivot != null)
            {
                cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
            }
        }

        private void HandleMovement()
        {
            Vector2 moveInput = Vector2.zero;
            if (DEADeyeInputRouter.Instance != null)
            {
                moveInput = DEADeyeInputRouter.Instance.GetMoveInput();
            }
            else if (UnityEngine.InputSystem.Keyboard.current != null)
            {
                if (UnityEngine.InputSystem.Keyboard.current.wKey.isPressed) moveInput.y += 1f;
                if (UnityEngine.InputSystem.Keyboard.current.sKey.isPressed) moveInput.y -= 1f;
                if (UnityEngine.InputSystem.Keyboard.current.aKey.isPressed) moveInput.x -= 1f;
                if (UnityEngine.InputSystem.Keyboard.current.dKey.isPressed) moveInput.x += 1f;
                moveInput = moveInput.normalized;
            }

            Vector3 moveDirection = (transform.forward * moveInput.y + transform.right * moveInput.x).normalized;

            if (characterController.isGrounded && velocity.y < 0f)
            {
                velocity.y = -2f;
            }

            velocity.y += gravity * Time.deltaTime;

            Vector3 motion = (moveDirection * moveSpeed + Vector3.up * velocity.y) * Time.deltaTime;
            characterController.Move(motion);
        }

        private void HandleAiming()
        {
            isAiming = DEADeyeInputRouter.Instance != null && DEADeyeInputRouter.Instance.GetAimHeld();

            if (weaponSocket != null)
            {
                Vector3 targetPos = isAiming ? aimPosition : hipPosition;
                weaponSocket.localPosition = Vector3.Lerp(weaponSocket.localPosition, targetPos, Time.deltaTime * aimTransitionSpeed);
            }

            if (desktopCamera != null)
            {
                float targetFov = isAiming ? 35f : 60f;
                desktopCamera.fieldOfView = Mathf.Lerp(desktopCamera.fieldOfView, targetFov, Time.deltaTime * aimTransitionSpeed);
            }
        }

        private void HandleWeaponInput()
        {
            if (Cursor.lockState != CursorLockMode.Locked) return;

            // Reload key
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
