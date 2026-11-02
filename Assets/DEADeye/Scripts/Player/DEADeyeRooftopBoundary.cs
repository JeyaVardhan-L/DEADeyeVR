using UnityEngine;

namespace DEADeye
{
    /// <summary>
    /// Enforces physical perimeter boundaries and floor constraints on the sniper rooftop.
    /// Prevents the player from falling off or walking beyond parapet barriers.
    /// </summary>
    public class DEADeyeRooftopBoundary : MonoBehaviour
    {
        [Header("Boundary Box Constraints")]
        [SerializeField] private Vector3 center = new Vector3(0f, 26f, -6f);
        [SerializeField] private Vector3 size = new Vector3(16f, 10f, 14f);
        [SerializeField] private Vector3 safeSpawnPosition = new Vector3(0f, 26.0f, -2.5f);

        private void LateUpdate()
        {
            Transform playerT = null;
            if (DEADeyePlayerController.Instance != null && DEADeyePlayerController.Instance.ActiveCamera != null)
            {
                playerT = DEADeyePlayerController.Instance.ActiveCamera.transform;
            }

            if (playerT == null) return;

            Vector3 currentPos = playerT.position;
            Vector3 min = center - size * 0.5f;
            Vector3 max = center + size * 0.5f;

            // If player fell below floor or stepped outside rooftop box
            if (currentPos.y < min.y || currentPos.x < min.x || currentPos.x > max.x || currentPos.z < min.z || currentPos.z > max.z)
            {
                Debug.LogWarning($"[DEADeyeRooftopBoundary] Player breached rooftop boundary at {currentPos}. Resetting to safe perch {safeSpawnPosition}.");
                if (DEADeyePlayerController.Instance.DesktopRig != null && DEADeyePlayerController.Instance.DesktopRig.gameObject.activeInHierarchy)
                {
                    DEADeyePlayerController.Instance.DesktopRig.transform.position = safeSpawnPosition;
                }
                else if (DEADeyePlayerController.Instance.VrRig != null && DEADeyePlayerController.Instance.VrRig.activeInHierarchy)
                {
                    DEADeyePlayerController.Instance.VrRig.transform.position = safeSpawnPosition;
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(center, size);
        }
    }
}

