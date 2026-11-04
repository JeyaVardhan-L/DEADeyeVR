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

        private void LateUpdate()
        {
            Transform playerT = null;
            if (DEADeyePlayerController.Instance != null && DEADeyePlayerController.Instance.ActiveCamera != null)
            {
                playerT = DEADeyePlayerController.Instance.ActiveCamera.transform;
            }
            else
            {
                var desktopRig = FindFirstObjectByType<DEADeyeDesktopRig>();
                if (desktopRig != null) playerT = desktopRig.transform;
            }

            if (playerT == null) return;

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

                if (DEADeyePlayerController.Instance != null && DEADeyePlayerController.Instance.DesktopRig != null)
                {
                    var cc = DEADeyePlayerController.Instance.DesktopRig.GetComponent<CharacterController>();
                    if (cc != null) cc.enabled = false;
                    DEADeyePlayerController.Instance.DesktopRig.transform.position = targetSafePerch;
                    if (cc != null) cc.enabled = true;
                }
                else if (DEADeyePlayerController.Instance != null && DEADeyePlayerController.Instance.VrRig != null)
                {
                    DEADeyePlayerController.Instance.VrRig.transform.position = targetSafePerch;
                }
                else
                {
                    var rig = FindFirstObjectByType<DEADeyeDesktopRig>();
                    if (rig != null)
                    {
                        var cc = rig.GetComponent<CharacterController>();
                        if (cc != null) cc.enabled = false;
                        rig.transform.position = targetSafePerch;
                        if (cc != null) cc.enabled = true;
                    }
                }
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
