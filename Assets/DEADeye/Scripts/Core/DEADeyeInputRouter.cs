using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.XR;

namespace DEADeye
{
    /// <summary>
    /// Authoritative input router providing unified desktop and Meta Quest VR input queries.
    /// Supports:
    /// - Desktop FPS controls (WASD, Mouse look, LMB Fire, RMB Scope, R Reload, Esc Pause, etc.)
    /// - Standalone Meta Quest 2 / 3 Touch controllers (Trigger Fire, Grip Scope, A/X Reload, B/Y Info, Menu Pause, Thumbstick Move)
    /// </summary>
    public class DEADeyeInputRouter : MonoBehaviour
    {
        public static DEADeyeInputRouter Instance { get; private set; }

        [Header("Desktop Sensitivity")]
        [Tooltip("Horizontal look sensitivity in degrees per pixel (tested around 0.10 - 0.15).")]
        [SerializeField] private float horizontalSensitivity = 0.12f;
        [Tooltip("Vertical look sensitivity in degrees per pixel (tested around 0.10 - 0.15).")]
        [SerializeField] private float verticalSensitivity = 0.12f;

        // VR Touch Controller state tracking for reliable edge detection
        private bool prevRightTrigger = false;
        private bool prevRightGrip = false;
        private bool prevLeftGrip = false;
        private bool prevButtonA = false;
        private bool prevButtonB = false;
        private bool prevButtonX = false;
        private bool prevButtonY = false;
        private bool prevMenuButton = false;

        private bool vrTriggerDown = false;
        private bool vrTriggerHeld = false;
        private bool vrGripHeld = false;
        private bool vrGripDown = false;
        private bool vrReloadDown = false;
        private bool vrInfoDown = false;
        private bool vrTerminalDown = false;
        private bool vrPauseDown = false;
        private Vector2 vrMoveInput = Vector2.zero;

        public float HorizontalSensitivity
        {
            get => horizontalSensitivity;
            set => horizontalSensitivity = Mathf.Clamp(value, 0.01f, 2.0f);
        }

        public float VerticalSensitivity
        {
            get => verticalSensitivity;
            set => verticalSensitivity = Mathf.Clamp(value, 0.01f, 2.0f);
        }

        public float MouseSensitivity
        {
            get => horizontalSensitivity * 10f;
            set
            {
                horizontalSensitivity = Mathf.Clamp(value * 0.1f, 0.01f, 2.0f);
                verticalSensitivity = horizontalSensitivity;
            }
        }

        private void Update()
        {
            PollVRControllers();
        }

        /// <summary>
        /// Reads hardware state from Meta Quest Touch Controllers via UnityEngine.XR.InputDevices.
        /// Works out of the box in standalone VR without requiring desktop window focus or special assets.
        /// </summary>
        private void PollVRControllers()
        {
            UnityEngine.XR.InputDevice rightHand = UnityEngine.XR.InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
            UnityEngine.XR.InputDevice leftHand = UnityEngine.XR.InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);

            // 1. Right Trigger (Fire / UI Select)
            bool curRightTrigger = false;
            if (rightHand.isValid)
            {
                bool tb = false;
                float tv = 0f;
                if (rightHand.TryGetFeatureValue(UnityEngine.XR.CommonUsages.triggerButton, out tb))
                {
                    curRightTrigger = tb;
                }
                else if (rightHand.TryGetFeatureValue(UnityEngine.XR.CommonUsages.trigger, out tv))
                {
                    curRightTrigger = tv > 0.5f;
                }
            }
            vrTriggerDown = curRightTrigger && !prevRightTrigger;
            vrTriggerHeld = curRightTrigger;
            prevRightTrigger = curRightTrigger;

            // 2. Grip (Scope Aiming / Optical Zoom on either hand)
            bool curRightGrip = false;
            if (rightHand.isValid)
            {
                bool gb = false;
                float gv = 0f;
                if (rightHand.TryGetFeatureValue(UnityEngine.XR.CommonUsages.gripButton, out gb))
                {
                    curRightGrip = gb;
                }
                else if (rightHand.TryGetFeatureValue(UnityEngine.XR.CommonUsages.grip, out gv))
                {
                    curRightGrip = gv > 0.35f;
                }
            }
            bool curLeftGrip = false;
            if (leftHand.isValid)
            {
                bool lgb = false;
                float lgv = 0f;
                if (leftHand.TryGetFeatureValue(UnityEngine.XR.CommonUsages.gripButton, out lgb))
                {
                    curLeftGrip = lgb;
                }
                else if (leftHand.TryGetFeatureValue(UnityEngine.XR.CommonUsages.grip, out lgv))
                {
                    curLeftGrip = lgv > 0.35f;
                }
            }
            bool anyGrip = curRightGrip || curLeftGrip;
            bool prevAnyGrip = prevRightGrip || prevLeftGrip;
            vrGripDown = anyGrip && !prevAnyGrip;
            vrGripHeld = anyGrip;
            prevRightGrip = curRightGrip;
            prevLeftGrip = curLeftGrip;

            // 3. Primary Buttons (A on Right Hand, X on Left Hand) -> Reload
            bool curA = false;
            if (rightHand.isValid)
            {
                bool pa = false;
                if (rightHand.TryGetFeatureValue(UnityEngine.XR.CommonUsages.primaryButton, out pa))
                {
                    curA = pa;
                }
            }
            bool curX = false;
            if (leftHand.isValid)
            {
                bool px = false;
                if (leftHand.TryGetFeatureValue(UnityEngine.XR.CommonUsages.primaryButton, out px))
                {
                    curX = px;
                }
            }
            vrReloadDown = (curA && !prevButtonA) || (curX && !prevButtonX);
            prevButtonA = curA;
            prevButtonX = curX;

            // 4. Secondary Buttons (B on Right Hand, Y on Left Hand) -> Mission Info / Terminal
            bool curB = false;
            if (rightHand.isValid)
            {
                bool pb = false;
                if (rightHand.TryGetFeatureValue(UnityEngine.XR.CommonUsages.secondaryButton, out pb))
                {
                    curB = pb;
                }
            }
            bool curY = false;
            if (leftHand.isValid)
            {
                bool py = false;
                if (leftHand.TryGetFeatureValue(UnityEngine.XR.CommonUsages.secondaryButton, out py))
                {
                    curY = py;
                }
            }
            vrInfoDown = curB && !prevButtonB;
            vrTerminalDown = curY && !prevButtonY;
            prevButtonB = curB;
            prevButtonY = curY;

            // 5. Menu Button (Left Hand) -> Pause
            bool curMenu = false;
            if (leftHand.isValid)
            {
                bool mb = false;
                if (leftHand.TryGetFeatureValue(UnityEngine.XR.CommonUsages.menuButton, out mb))
                {
                    curMenu = mb;
                }
            }
            vrPauseDown = curMenu && !prevMenuButton;
            prevMenuButton = curMenu;

            // 6. Left Thumbstick -> Locomotion Move
            vrMoveInput = Vector2.zero;
            if (leftHand.isValid)
            {
                Vector2 lAxis = Vector2.zero;
                if (leftHand.TryGetFeatureValue(UnityEngine.XR.CommonUsages.primary2DAxis, out lAxis))
                {
                    if (lAxis.sqrMagnitude > 0.04f)
                    {
                        vrMoveInput = lAxis;
                    }
                }
            }
        }

        public bool IsPointerOverUI()
        {
            // When cursor is locked into the game viewport, mouse clicks are in-game actions, not UI clicks
            if (Cursor.lockState == CursorLockMode.Locked) return false;
            if (EventSystem.current == null) return false;
            return EventSystem.current.IsPointerOverGameObject();
        }

        public bool GetFireDown()
        {
            // Never fire if clicking on an interactive UI element
            if (IsPointerOverUI()) return false;

            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                return true;
            }

            return vrTriggerDown;
        }

        public bool GetFireHeld()
        {
            if (IsPointerOverUI()) return false;

            if (Mouse.current != null && Mouse.current.leftButton.isPressed)
            {
                return true;
            }

            return vrTriggerHeld;
        }

        public bool GetAimHeld()
        {
            if (Mouse.current != null && Mouse.current.rightButton.isPressed)
            {
                return true;
            }

            return vrGripHeld;
        }

        public bool GetAimDown()
        {
            if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame)
            {
                return true;
            }

            return vrGripDown;
        }

        public bool GetReloadDown()
        {
            if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
            {
                return true;
            }

            return vrReloadDown;
        }

        public bool GetInteractDown()
        {
            if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            {
                return true;
            }

            return vrReloadDown;
        }

        public bool GetPauseDown()
        {
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                return true;
            }

            return vrPauseDown;
        }

        public bool GetMissionInfoToggleDown()
        {
            if (Keyboard.current != null && Keyboard.current.iKey.wasPressedThisFrame)
            {
                return true;
            }

            return vrInfoDown;
        }

        public bool GetSprintHeld()
        {
            if (Keyboard.current != null && (Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed))
            {
                return true;
            }
            return false;
        }

        public bool GetJumpDown()
        {
            if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                return true;
            }
            return false;
        }

        public bool GetTerminalToggleDown()
        {
            if (Keyboard.current != null && (Keyboard.current.yKey.wasPressedThisFrame || Keyboard.current.tKey.wasPressedThisFrame))
            {
                return true;
            }

            return vrTerminalDown;
        }

        public Vector2 GetMoveInput()
        {
            Vector2 move = Vector2.zero;
            if (Keyboard.current != null)
            {
                if (Keyboard.current.wKey.isPressed) move.y += 1f;
                if (Keyboard.current.sKey.isPressed) move.y -= 1f;
                if (Keyboard.current.aKey.isPressed) move.x -= 1f;
                if (Keyboard.current.dKey.isPressed) move.x += 1f;
            }
            if (move != Vector2.zero) return move.normalized;
            return vrMoveInput;
        }

        public Vector2 GetLookInput()
        {
            if (Mouse.current != null)
            {
                // Raw pixel delta - zero when mouse does not move (zero drift guaranteed)
                Vector2 delta = Mouse.current.delta.ReadValue();
                return new Vector2(delta.x * horizontalSensitivity, delta.y * verticalSensitivity);
            }
            return Vector2.zero;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
}
