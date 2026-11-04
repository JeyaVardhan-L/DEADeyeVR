using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace DEADeye
{
    /// <summary>
    /// Authoritative input router providing unified desktop and VR input queries.
    /// Ensures UI clicks never bleed into weapon fire.
    /// </summary>
    public class DEADeyeInputRouter : MonoBehaviour
    {
        public static DEADeyeInputRouter Instance { get; private set; }

        [Header("Desktop Sensitivity")]
        [Tooltip("Horizontal look sensitivity in degrees per pixel (tested around 0.10 - 0.15).")]
        [SerializeField] private float horizontalSensitivity = 0.12f;
        [Tooltip("Vertical look sensitivity in degrees per pixel (tested around 0.10 - 0.15).")]
        [SerializeField] private float verticalSensitivity = 0.12f;

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

        public bool IsPointerOverUI()
        {
            if (EventSystem.current == null) return false;
            return EventSystem.current.IsPointerOverGameObject();
        }

        public bool GetFreeLookHeld()
        {
            if (Keyboard.current != null && (Keyboard.current.leftAltKey.isPressed || Keyboard.current.rightAltKey.isPressed))
            {
                return true;
            }
            return false;
        }

        public bool GetFireDown()
        {
            // Do not fire if pointer is over UI or if player is holding Left Alt for mouse look
            if (IsPointerOverUI() || GetFreeLookHeld()) return false;

            // Desktop Left Click
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                return true;
            }

            return false;
        }

        public bool GetAimHeld()
        {
            if (Mouse.current != null && Mouse.current.rightButton.isPressed)
            {
                return true;
            }

            return false;
        }

        public bool GetAimDown()
        {
            if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame)
            {
                return true;
            }

            return false;
        }

        public bool GetReloadDown()
        {
            if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
            {
                return true;
            }

            return false;
        }

        public bool GetInteractDown()
        {
            if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            {
                return true;
            }

            return false;
        }

        public bool GetPauseDown()
        {
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                return true;
            }

            return false;
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
            return false;
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
            return move.normalized;
        }

        public Vector2 GetLookInput()
        {
            if (Mouse.current != null)
            {
                // Raw pixel delta - frame-rate independent degrees per pixel rotation
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
