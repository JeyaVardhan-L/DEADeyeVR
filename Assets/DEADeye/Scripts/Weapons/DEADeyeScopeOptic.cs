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
        [SerializeField] private float magnificationFov = 12f; // ~5x zoom compared to 60 fov
        [SerializeField] private float highMagnificationFov = 6f; // ~10x zoom
        [SerializeField] private bool useHighZoom = false;

        [Header("State")]
        [SerializeField] private bool isAiming = false;

        public Camera ScopeCamera => scopeCamera;
        public bool IsAiming => isAiming;
        public RenderTexture ScopeTargetTexture => scopeTargetTexture;

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

        public void SetupScopeRenderTexture()
        {
            if (scopeTargetTexture != null) return;

            // 1024x1024 texture for crisp optical sniper magnification
            scopeTargetTexture = new RenderTexture(1024, 1024, 16, RenderTextureFormat.ARGB32)
            {
                name = "RT_SniperScopeLens"
            };
            scopeTargetTexture.Create();

            if (scopeCamera != null)
            {
                scopeCamera.targetTexture = scopeTargetTexture;
                scopeCamera.fieldOfView = useHighZoom ? highMagnificationFov : magnificationFov;
            }

            if (lensRenderer != null && lensRenderer.material != null)
            {
                lensRenderer.material.mainTexture = scopeTargetTexture;
            }
        }

        public void SetScopeActive(bool active)
        {
            isAiming = active;
            if (active && scopeTargetTexture == null)
            {
                SetupScopeRenderTexture();
            }

            if (scopeCamera != null)
            {
                scopeCamera.enabled = active;
            }
            if (lensRenderer != null)
            {
                lensRenderer.enabled = active;
            }

            if (DEADeyeMissionHUD.Instance != null)
            {
                if (active && scopeTargetTexture != null)
                {
                    DEADeyeMissionHUD.Instance.SetScopeTexture(scopeTargetTexture);
                }
                DEADeyeMissionHUD.Instance.SetScopeOverlay(active);
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
