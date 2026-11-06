using System.Collections.Generic;
using UnityEngine;

namespace DEADeye
{
    /// <summary>
    /// Enforces safety boundary constraints across the entire walkable rooftop and catwalk network.
    /// Resets the player ONLY if they fall below the rooftop level into the city streets.
    /// Supports smooth WASD movement across Perch 1, East Catwalk, East Perch, West Skybridge, and West Perch.
    /// </summary>
    public class DEADeyeRooftopBoundary : MonoBehaviour
    {
        [Header("Global Boundary Constraints")]
        [SerializeField] private Vector3 networkCenter = new Vector3(0f, 26f, -1f);
        [SerializeField] private Vector3 networkSize = new Vector3(64f, 24f, 40f);
        [SerializeField] private float minimumSafeY = 18.0f; // Rooftops are at ~26m; streets are at 0.15m

        [Header("Safe Return Nodes")]
        [SerializeField] private Vector3 perch1SafePos = new Vector3(0f, 26.0f, -2.5f);
        [SerializeField] private Vector3 perch2SafePos = new Vector3(18f, 26.0f, 4.0f);
        [SerializeField] private Vector3 perch3SafePos = new Vector3(-18f, 26.0f, 6.0f);

        public System.Action OnBoundaryResetTriggered;

        private void LateUpdate()
        {
            Transform playerT = null;
            CharacterController cc = null;

            if (DEADeyePlayerController.Instance != null && 
                DEADeyePlayerController.Instance.DesktopRig != null && 
                DEADeyePlayerController.Instance.DesktopRig.gameObject.activeInHierarchy)
            {
                playerT = DEADeyePlayerController.Instance.DesktopRig.transform;
                cc = DEADeyePlayerController.Instance.DesktopRig.GetComponent<CharacterController>();
            }
            else if (DEADeyePlayerController.Instance != null && 
                     DEADeyePlayerController.Instance.VrRig != null && 
                     DEADeyePlayerController.Instance.VrRig.activeInHierarchy)
            {
                playerT = DEADeyePlayerController.Instance.VrRig.transform;
                cc = DEADeyePlayerController.Instance.VrRig.GetComponent<CharacterController>();
            }
            else
            {
                var desktopRig = FindFirstObjectByType<DEADeyeDesktopRig>();
                if (desktopRig != null && desktopRig.gameObject.activeInHierarchy)
                {
                    playerT = desktopRig.transform;
                    cc = desktopRig.GetComponent<CharacterController>();
                }
            }

            if (playerT == null) return;

            // If CharacterController is firmly grounded on any valid floor or bridge, player has not fallen
            if (cc != null && cc.enabled && cc.isGrounded)
            {
                return;
            }

            Vector3 currentPos = playerT.position;
            Vector3 min = networkCenter - networkSize * 0.5f;
            Vector3 max = networkCenter + networkSize * 0.5f;

            // Only trigger reset if player has physically fallen off the rooftop (Y < 18m)
            // or stepped far outside the entire urban overwatch network perimeter
            bool hasFallen = currentPos.y < minimumSafeY;
            bool isOutsidePerimeter = currentPos.x < min.x || currentPos.x > max.x || currentPos.z < min.z || currentPos.z > max.z;

            if (hasFallen || isOutsidePerimeter)
            {
                Vector3 targetSafePerch = GetNearestSafePerch(currentPos);
                Debug.LogWarning($"[DEADeyeRooftopBoundary] Player fell off rooftop at {currentPos} (minY={minimumSafeY}). Resetting to nearest safe perch {targetSafePerch}.");

                if (cc != null) cc.enabled = false;
                playerT.position = targetSafePerch;
                if (cc != null) cc.enabled = true;

                OnBoundaryResetTriggered?.Invoke();
            }
        }

        private Vector3 GetNearestSafePerch(Vector3 fromPos)
        {
            if (fromPos.x > 8f) return perch2SafePos;
            if (fromPos.x < -8f) return perch3SafePos;
            return perch1SafePos;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(networkCenter, networkSize);
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(perch1SafePos, 0.4f);
            Gizmos.DrawSphere(perch2SafePos, 0.4f);
            Gizmos.DrawSphere(perch3SafePos, 0.4f);
        }
    }
}
