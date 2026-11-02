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
        [SerializeField] private float mouseSensitivity = 2.0f;

        public float MouseSensitivity
        {
            get => mouseSensitivity;
            set => mouseSensitivity = Mathf.Clamp(value, 0.1f, 10f);
        }

        public bool IsPointerOverUI()
        {
            if (EventSystem.current == null) return false;
            return EventSystem.current.IsPointerOverGameObject();
        }

        public bool GetFireDown()
        {
            if (IsPointerOverUI()) return false;

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
                Vector2 delta = Mouse.current.delta.ReadValue();
                return delta * (mouseSensitivity * 0.1f);
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
