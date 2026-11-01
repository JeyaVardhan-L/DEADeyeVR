using UnityEngine;

namespace DEADeye
{
    /// <summary>
    /// Optical sniper scope rendering high-magnification city view onto a circular RenderTexture lens.
    /// Manages reticle alignment, FOV magnification, and performance-saving culling when unequipped.
    /// </summary>
    public class DEADeyeScopeOptic : MonoBehaviour
    {
        [Header("Optic Camera")]
        [SerializeField] private Camera scopeCamera;
        [SerializeField] private Renderer lensRenderer;
        [SerializeField] private float magnificationFov = 15f; // ~4x zoom compared to 60 fov
        [SerializeField] private float highMagnificationFov = 8f; // ~8x zoom
        [SerializeField] private bool useHighZoom = false;

        [Header("State")]
        [SerializeField] private bool isAiming = false;

        public Camera ScopeCamera => scopeCamera;
        public bool IsAiming => isAiming;

        private RenderTexture scopeTargetTexture;

        private void Awake()
        {
            if (scopeCamera == null)
            {
                scopeCamera = GetComponentInChildren<Camera>();
            }

            SetupScopeRenderTexture();
            SetScopeActive(false);
        }

        private void SetupScopeRenderTexture()
        {
            if (scopeTargetTexture != null) return;

            // 512x512 texture optimal for mobile Quest performance & crisp reticle
            scopeTargetTexture = new RenderTexture(512, 512, 16, RenderTextureFormat.ARGB32)
            {
                name = "RT_SniperScopeLens"
            };
            scopeTargetTexture.Create();

            if (scopeCamera != null)
            {
                scopeCamera.targetTexture = scopeTargetTexture;
                scopeCamera.fieldOfView = useHighZoom ? highMagnificationFov : magnificationFov;
            }

            if (lensRenderer != null)
            {
                lensRenderer.material.mainTexture = scopeTargetTexture;
            }
        }

        public void SetScopeActive(bool active)
        {
            isAiming = active;
            if (scopeCamera != null)
            {
                scopeCamera.enabled = active;
            }
            if (lensRenderer != null)
            {
                lensRenderer.enabled = active;
            }
        }

        public void ToggleMagnification()
        {
            useHighZoom = !useHighZoom;
            if (scopeCamera != null)
            {
                scopeCamera.fieldOfView = useHighZoom ? highMagnificationFov : magnificationFov;
            }
        }

        private void OnDestroy()
        {
            if (scopeTargetTexture != null)
            {
                scopeTargetTexture.Release();
                Destroy(scopeTargetTexture);
            }
        }
    }
}

