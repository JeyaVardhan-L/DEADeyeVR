using System;
using UnityEngine;

namespace DEADeye
{
    /// <summary>
    /// Optical reconnaissance & target identification system.
    /// When the player observes a target through their optic for a brief dwell duration (~0.8s),
    /// the target is positively identified, triggering HUD telemetry, audio feedback, and tactical intel.
    /// </summary>
    public class DEADeyeSpottingSystem : MonoBehaviour
    {
        public static DEADeyeSpottingSystem Instance { get; private set; }

        [Header("Spotting Parameters")]
        [SerializeField] private float spotDwellRequired = 0.85f;
        [SerializeField] private float maxSpotDistance = 250f;
        [SerializeField] private LayerMask spotLayerMask = ~0;

        [Header("Runtime State")]
        [SerializeField] private DEADeyeSpotable currentTarget;
        [SerializeField] private float currentDwellTime = 0f;

        public DEADeyeSpotable CurrentTarget => currentTarget;
        public float SpotProgress => (currentTarget != null && !currentTarget.IsIdentified) 
            ? Mathf.Clamp01(currentDwellTime / spotDwellRequired) 
            : 0f;

        public event Action<DEADeyeSpotable> OnTargetSpotted;

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
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            Camera cam = Camera.main;
            var scopeOptic = FindFirstObjectByType<DEADeyeScopeOptic>();
            if (scopeOptic != null && scopeOptic.IsAiming && scopeOptic.ScopeCamera != null)
            {
                cam = scopeOptic.ScopeCamera;
            }
            else if (cam == null)
            {
                var desktopRig = FindFirstObjectByType<DEADeyeDesktopRig>();
                if (desktopRig != null) cam = desktopRig.DesktopCamera;
            }

            if (cam == null) return;

            Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            if (Physics.Raycast(ray, out RaycastHit hit, maxSpotDistance, spotLayerMask, QueryTriggerInteraction.Collide))
            {
                DEADeyeSpotable spotable = hit.collider.GetComponentInParent<DEADeyeSpotable>();
                if (spotable != null)
                {
                    if (spotable == currentTarget)
                    {
                        if (!spotable.IsIdentified)
                        {
                            currentDwellTime += Time.deltaTime;
                            if (currentDwellTime >= spotDwellRequired)
                            {
                                IdentifyTarget(spotable);
                            }
                        }
                    }
                    else
                    {
                        currentTarget = spotable;
                        currentDwellTime = 0f;
                    }
                    return;
                }
            }

            // Not aiming at a spotable target
            currentTarget = null;
            currentDwellTime = 0f;
        }

        private void IdentifyTarget(DEADeyeSpotable target)
        {
            if (target == null || target.IsIdentified) return;

            target.MarkIdentified();
            OnTargetSpotted?.Invoke(target);

            if (DEADeyeAudioManager.Instance != null)
            {
                DEADeyeAudioManager.Instance.PlaySpottingChirp();
            }

            if (DEADeyeMissionHUD.Instance != null)
            {
                DEADeyeMissionHUD.Instance.ShowSpottingBanner(target);
            }

            if (DEADeyeMissionDirector.Instance != null)
            {
                DEADeyeMissionDirector.Instance.HandleTargetSpotted(target);
            }

            Debug.Log($"[DEADeyeSpottingSystem] IDENTIFIED: {target.TargetName} ({target.Classification}) - {target.TacticalDescription}");
        }
    }
}
