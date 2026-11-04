using System;
using System.Collections;
using UnityEngine;
using Random = UnityEngine.Random;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace DEADeye
{
    /// <summary>
    /// Authoritative sniper rifle supporting two-handed VR stabilization,
    /// Desktop FPS aiming, hitscan precision ballistics, muzzle flash, procedural recoil, and audio.
    /// Strictly blocks firing in lobby or when interacting with UI.
    /// </summary>
    public class DEADeyeSniperWeapon : XRGrabInteractable
    {
        [Header("Mount Points")]
        [SerializeField] private Transform muzzlePoint;
        [SerializeField] private Transform secondaryGripPoint;
        [SerializeField] private DEADeyeScopeOptic scopeOptic;

        public Transform MuzzlePoint => muzzlePoint;

        [Header("Ballistics")]
        [SerializeField] private float maxRange = 350f;
        [SerializeField] private int baseDamage = 100;
        [SerializeField] private float fireRateCooldown = 0.85f; // Bolt-action cycle
        [SerializeField] private LayerMask hitLayers = ~0;

        [Header("Recoil Recovery")]
        [SerializeField] private float recoilKickPitch = 4.5f;
        [SerializeField] private float recoilKickBack = 0.08f;
        [SerializeField] private float recoilRecoverySpeed = 12f;

        [Header("Effects")]
        [SerializeField] private ParticleSystem muzzleFlash;
        [SerializeField] private LineRenderer bulletTracer;

        [Header("Diagnostics")]
        [SerializeField] private bool enableDebugDiagnostics = true;

        private float nextFireTime = 0f;
        private Vector3 recoilOffset = Vector3.zero;
        private Quaternion recoilRotation = Quaternion.identity;
        private Vector3 reloadOffset = Vector3.zero;
        private IXRSelectInteractor secondaryInteractor;

        public bool IsReadyToFire => Time.time >= nextFireTime;
        public DEADeyeScopeOptic ScopeOptic => scopeOptic;
        public bool EnableDebugDiagnostics { get => enableDebugDiagnostics; set => enableDebugDiagnostics = value; }

        protected override void Awake()
        {
            base.Awake();

            // Ensure Rifle Mesh is true 1:1 scale (1.42m real-world length)
            Transform rifleMesh = transform.Find("RifleMesh");
            if (rifleMesh != null)
            {
                rifleMesh.localScale = Vector3.one;
            }

            if (muzzlePoint == null)
            {
                Transform existingMuzzle = transform.Find("MuzzlePoint");
                if (existingMuzzle != null)
                {
                    muzzlePoint = existingMuzzle;
                }
                else
                {
                    GameObject muzzleGO = new GameObject("MuzzlePoint");
                    muzzleGO.transform.SetParent(transform, false);
                    muzzleGO.transform.localPosition = new Vector3(0f, 0.08f, 0.85f);
                    muzzlePoint = muzzleGO.transform;
                }
            }

            if (scopeOptic == null)
            {
                scopeOptic = GetComponentInChildren<DEADeyeScopeOptic>();
            }

            SetupTracer();
        }

        private void SetupTracer()
        {
            if (bulletTracer == null)
            {
                bulletTracer = GetComponent<LineRenderer>();
                if (bulletTracer == null)
                {
                    bulletTracer = gameObject.AddComponent<LineRenderer>();
                }
            }

            bulletTracer.startWidth = 0.025f;
            bulletTracer.endWidth = 0.01f;
            bulletTracer.enabled = false;

            Shader tracerShader = Shader.Find("Universal Render Pipeline/Unlit");
            if (tracerShader == null) tracerShader = Shader.Find("Unlit/Color");
            Material tracerMat = new Material(tracerShader) { name = "TracerMat" };
            tracerMat.color = new Color(1.0f, 0.88f, 0.35f, 0.95f);
            bulletTracer.sharedMaterial = tracerMat;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            activated.AddListener(OnActivatedTrigger);

            if (DEADeyeWeaponManager.Instance != null)
            {
                DEADeyeWeaponManager.Instance.OnReloadStarted += HandleReloadStarted;
                DEADeyeWeaponManager.Instance.OnReloadCompleted += HandleReloadCompleted;
            }
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            activated.RemoveListener(OnActivatedTrigger);

            if (DEADeyeWeaponManager.Instance != null)
            {
                DEADeyeWeaponManager.Instance.OnReloadStarted -= HandleReloadStarted;
                DEADeyeWeaponManager.Instance.OnReloadCompleted -= HandleReloadCompleted;
            }
        }

        private void HandleReloadStarted()
        {
            reloadOffset = new Vector3(0.04f, -0.14f, -0.05f);
        }

        private void HandleReloadCompleted()
        {
            reloadOffset = Vector3.zero;
        }

        private void Update()
        {
            // Note: Desktop firing is handled solely by DEADeyeDesktopRig.HandleWeaponInput()
            // to strictly prevent double-firing on a single click.

            // Scope optic activation on RMB when in desktop mode
            if (DEADeyeGameManager.Instance != null && DEADeyeGameManager.Instance.PlayerMode == DEADeyePlayerMode.Desktop)
            {
                if (DEADeyeInputRouter.Instance != null && scopeOptic != null)
                {
                    scopeOptic.SetScopeActive(DEADeyeInputRouter.Instance.GetAimHeld());
                }
            }

            // Smooth procedural recoil recovery toward zero
            recoilOffset = Vector3.Lerp(recoilOffset, Vector3.zero, Time.deltaTime * recoilRecoverySpeed);
            recoilRotation = Quaternion.Slerp(recoilRotation, Quaternion.identity, Time.deltaTime * recoilRecoverySpeed);

            // Apply procedural offsets cleanly without accumulating drift
            transform.localPosition = recoilOffset + reloadOffset;
            transform.localRotation = recoilRotation;
        }

        private void OnActivatedTrigger(ActivateEventArgs args)
        {
            TryFire();
        }

        public bool TryFire()
        {
            // 1. Enforce Mission State (Never fire in Dojo Lobby, Results, or without MissionActive)
            if (DEADeyeGameManager.Instance == null || DEADeyeGameManager.Instance.CurrentState != DEADeyeGameState.MissionActive)
            {
                return false;
            }

            // 2. Prevent UI click bleeding
            if (DEADeyeInputRouter.Instance != null && DEADeyeInputRouter.Instance.IsPointerOverUI())
            {
                return false;
            }

            // 3. Fire rate cooldown
            if (Time.time < nextFireTime)
            {
                return false;
            }

            // 4. Weapon Ammo Check
            if (DEADeyeWeaponManager.Instance != null && !DEADeyeWeaponManager.Instance.CanFire())
            {
                return false;
            }

            ExecuteShot();
            return true;
        }

        private bool IsIgnoredCollider(Collider col)
        {
            if (col == null) return true;

            // Ignore invisible safety boundaries and rooftop boundary barriers
            if (col.name.Contains("SafetyBoundary") || col.name.Contains("RooftopBoundary") || col.name.Contains("SniperWeaponRest"))
                return true;

            // Ignore the weapon's own colliders (barrel, grab boxes, etc.)
            if (col.transform.IsChildOf(transform) || col.transform == transform)
                return true;

            // Ignore the player's own rig and character controller
            if (DEADeyePlayerController.Instance != null)
            {
                if (col.transform.IsChildOf(DEADeyePlayerController.Instance.transform) || col.transform == DEADeyePlayerController.Instance.transform)
                    return true;

                if (DEADeyePlayerController.Instance.DesktopRig != null)
                {
                    if (col.transform.IsChildOf(DEADeyePlayerController.Instance.DesktopRig.transform) || col.transform == DEADeyePlayerController.Instance.DesktopRig.transform)
                        return true;
                }

                if (DEADeyePlayerController.Instance.VrRig != null)
                {
                    if (col.transform.IsChildOf(DEADeyePlayerController.Instance.VrRig.transform) || col.transform == DEADeyePlayerController.Instance.VrRig.transform)
                        return true;
                }
            }

            // Ignore triggers that are NOT headshot, hitbox, or environmental interaction triggers
            if (col.isTrigger && col.GetComponent<DEADeyeHeadshotTrigger>() == null && col.GetComponentInParent<DEADeyeEnvironmentalProp>() == null)
            {
                return true;
            }

            return false;
        }

        private void ExecuteShot()
        {
            nextFireTime = Time.time + fireRateCooldown;

            // Consume ammo
            if (DEADeyeWeaponManager.Instance != null)
            {
                DEADeyeWeaponManager.Instance.ConsumeAmmo();
            }

            // Register shot in score manager
            if (DEADeyeScoreManager.Instance != null)
            {
                DEADeyeScoreManager.Instance.RegisterShotFired();
            }

            // Audio
            if (DEADeyeAudioManager.Instance != null)
            {
                DEADeyeAudioManager.Instance.PlayGunshot(muzzlePoint.position);
            }

            // Muzzle flash
            if (muzzleFlash != null)
            {
                muzzleFlash.Play();
            }

            // Apply procedural recoil kick
            recoilOffset = new Vector3(0f, 0.02f, -recoilKickBack);
            recoilRotation = Quaternion.Euler(-recoilKickPitch, Random.Range(-0.4f, 0.4f), 0f);

            // =========================================================================
            // REQUIRED AIMING ARCHITECTURE (Camera-Centre Targeting & Muzzle Obstruction)
            // =========================================================================

            // 1. Obtain current active aiming camera
            Camera activeAimingCam = null;
            if (scopeOptic != null && scopeOptic.IsAiming && scopeOptic.ScopeCamera != null && scopeOptic.ScopeCamera.enabled)
            {
                activeAimingCam = scopeOptic.ScopeCamera;
            }
            else if (DEADeyePlayerController.Instance != null && DEADeyePlayerController.Instance.ActiveCamera != null)
            {
                activeAimingCam = DEADeyePlayerController.Instance.ActiveCamera;
            }
            else
            {
                activeAimingCam = Camera.main;
            }

            // 2. Cast a ray through the exact screen centre
            Ray cameraAimRay = (activeAimingCam != null)
                ? activeAimingCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f))
                : new Ray(muzzlePoint.position, muzzlePoint.forward);

            // 3 & 4. Determine intended aim point (or max range if empty)
            RaycastHit[] cameraHits = Physics.RaycastAll(cameraAimRay, maxRange, hitLayers, QueryTriggerInteraction.Collide);
            System.Array.Sort(cameraHits, (a, b) => a.distance.CompareTo(b.distance));

            Vector3 intendedAimPoint = cameraAimRay.origin + cameraAimRay.direction * maxRange;
            bool hasValidCameraHit = false;
            RaycastHit firstValidCameraHit = default;

            foreach (var hit in cameraHits)
            {
                if (!IsIgnoredCollider(hit.collider))
                {
                    intendedAimPoint = hit.point;
                    firstValidCameraHit = hit;
                    hasValidCameraHit = true;
                    break;
                }
            }

            // 5. Calculate direction from actual rifle muzzle to intended aim point
            Vector3 muzzlePos = (muzzlePoint != null) ? muzzlePoint.position : cameraAimRay.origin;
            Vector3 muzzleDir = (intendedAimPoint - muzzlePos).normalized;
            float muzzleToAimDist = Vector3.Distance(muzzlePos, intendedAimPoint);

            // 6. Perform muzzle-origin obstruction check
            RaycastHit[] muzzleHits = Physics.RaycastAll(new Ray(muzzlePos, muzzleDir), muzzleToAimDist + 0.15f, hitLayers, QueryTriggerInteraction.Collide);
            System.Array.Sort(muzzleHits, (a, b) => a.distance.CompareTo(b.distance));

            bool shotBlockedByObstacle = false;
            RaycastHit effectiveHit = default;
            bool hasEffectiveHit = false;

            DEADeyeHostileAI intendedHostile = hasValidCameraHit ? firstValidCameraHit.collider.GetComponentInParent<DEADeyeHostileAI>() : null;

            foreach (var hit in muzzleHits)
            {
                if (IsIgnoredCollider(hit.collider)) continue;

                DEADeyeHostileAI hitHostile = hit.collider.GetComponentInParent<DEADeyeHostileAI>();

                // Did we hit the intended hostile?
                if (intendedHostile != null && hitHostile == intendedHostile)
                {
                    effectiveHit = hit;
                    hasEffectiveHit = true;
                    break;
                }

                // Did we reach the exact intended target collider?
                if (hasValidCameraHit && hit.collider == firstValidCameraHit.collider)
                {
                    effectiveHit = hit;
                    hasEffectiveHit = true;
                    break;
                }

                // Did an obstacle (wall, parapet, building) obstruct the bullet path earlier?
                if (hit.distance < muzzleToAimDist - 0.2f)
                {
                    shotBlockedByObstacle = true;
                    effectiveHit = hit;
                    hasEffectiveHit = true;
                    break;
                }
                else
                {
                    effectiveHit = hit;
                    hasEffectiveHit = true;
                    break;
                }
            }

            Vector3 actualImpactPoint = intendedAimPoint;

            // 7, 8 & 9. Register hit or register miss with full diagnostic feedback
            if (hasEffectiveHit)
            {
                actualImpactPoint = effectiveHit.point;

                if (shotBlockedByObstacle)
                {
                    // Shot blocked by a wall or building
                    if (enableDebugDiagnostics)
                    {
                        Debug.Log($"[DEADeyeSniperWeapon] BLOCKED: Shot blocked by obstacle '{effectiveHit.collider.name}' at {effectiveHit.distance:F1}m (Intended aim target distance: {muzzleToAimDist:F1}m)");
                        Debug.DrawRay(effectiveHit.point, Vector3.up * 0.4f, Color.red, 2.0f);
                    }

                    if (DEADeyeScoreManager.Instance != null)
                    {
                        DEADeyeScoreManager.Instance.RegisterMiss();
                    }
                }
                else
                {
                    // Unobstructed hit on target or surface
                    ProcessHit(effectiveHit);
                }
            }
            else if (hasValidCameraHit)
            {
                // Muzzle had an unhindered direct line of sight to the camera target
                ProcessHit(firstValidCameraHit);
                actualImpactPoint = firstValidCameraHit.point;
            }
            else
            {
                // Complete miss into open sky
                if (enableDebugDiagnostics)
                {
                    Debug.Log("[DEADeyeSniperWeapon] MISS: Aimed into open air, no target hit.");
                }

                if (DEADeyeScoreManager.Instance != null)
                {
                    DEADeyeScoreManager.Instance.RegisterMiss();
                }
            }

            // Diagnostics visualization
            if (enableDebugDiagnostics)
            {
                // Blue line: Camera aim ray
                Debug.DrawLine(cameraAimRay.origin, intendedAimPoint, Color.blue, 2.0f);
                // Yellow line: Muzzle-to-aim trajectory
                Debug.DrawLine(muzzlePos, actualImpactPoint, Color.yellow, 2.0f);
            }

            // Show Tracer & Bolt Cycle (Only when playing)
            if (Application.isPlaying)
            {
                StartCoroutine(DisplayTracerRoutine(muzzlePos, actualImpactPoint));
                StartCoroutine(BoltCycleRoutine());
                AlertNearbyHostilesToGunfire(muzzlePos, 65f);
            }
        }

        private void AlertNearbyHostilesToGunfire(Vector3 shotOrigin, float hearingRadius)
        {
            DEADeyeHostileAI[] hostiles = FindObjectsByType<DEADeyeHostileAI>(FindObjectsSortMode.None);
            foreach (var h in hostiles)
            {
                if (h != null && !h.IsEliminated)
                {
                    float dist = Vector3.Distance(h.transform.position, shotOrigin);
                    if (dist <= hearingRadius)
                    {
                        h.ReactToGunfire(shotOrigin);
                    }
                }
            }
        }

        private void ProcessHit(RaycastHit hit)
        {
            // Check Headshot Trigger (+300 pts)
            DEADeyeHeadshotTrigger headshot = hit.collider.GetComponent<DEADeyeHeadshotTrigger>();
            if (headshot != null)
            {
                if (enableDebugDiagnostics)
                {
                    string targetName = headshot.ParentHostile != null ? headshot.ParentHostile.ClassificationName : hit.collider.name;
                    Debug.Log($"[DEADeyeSniperWeapon] HEADSHOT: Target={targetName}, Collider={hit.collider.name}, Distance={hit.distance:F1}m, Damage={baseDamage * 3}, Blocked=False");
                    Debug.DrawRay(hit.point, Vector3.up * 0.6f, Color.green, 2.0f);
                }
                headshot.RegisterHeadshot(baseDamage, hit.point, hit.normal);
                return;
            }

            // Check General Hostile AI (+100 pts)
            DEADeyeHostileAI hostile = hit.collider.GetComponentInParent<DEADeyeHostileAI>();
            if (hostile != null)
            {
                if (enableDebugDiagnostics)
                {
                    Debug.Log($"[DEADeyeSniperWeapon] BODY HIT: Target={hostile.ClassificationName}, Collider={hit.collider.name}, Distance={hit.distance:F1}m, Damage={baseDamage}, Blocked=False");
                    Debug.DrawRay(hit.point, Vector3.up * 0.6f, Color.green, 2.0f);
                }
                hostile.TakeDamage(baseDamage, hit.point, hit.normal, isHeadshot: false);
                return;
            }

            // Check Civilian AI
            DEADeyeCivilianAI civilian = hit.collider.GetComponentInParent<DEADeyeCivilianAI>();
            if (civilian != null)
            {
                if (enableDebugDiagnostics)
                {
                    Debug.LogWarning($"[DEADeyeSniperWeapon] CIVILIAN HIT: Target={civilian.ClassificationName}, Distance={hit.distance:F1}m");
                }
                civilian.TakeDamage(baseDamage, hit.point, hit.normal, isHeadshot: false);
                return;
            }

            // Check Friendly VIP AI
            DEADeyeFriendlyAI friendly = hit.collider.GetComponentInParent<DEADeyeFriendlyAI>();
            if (friendly != null)
            {
                if (enableDebugDiagnostics)
                {
                    Debug.LogWarning($"[DEADeyeSniperWeapon] FRIENDLY HIT: Target={friendly.ClassificationName}, Distance={hit.distance:F1}m");
                }
                friendly.TakeDamage(baseDamage, hit.point, hit.normal, isHeadshot: false);
                return;
            }

            // Check Environmental Interaction Prop (Power junction, alarm bell, transformer, gate)
            DEADeyeEnvironmentalProp prop = hit.collider.GetComponentInParent<DEADeyeEnvironmentalProp>();
            if (prop != null)
            {
                if (enableDebugDiagnostics)
                {
                    Debug.Log($"[DEADeyeSniperWeapon] ENVIRONMENTAL PROP HIT: Prop={prop.PropTitle}, Distance={hit.distance:F1}m");
                }
                prop.TakeDamage(baseDamage, hit.point, hit.normal);
                return;
            }

            // Check Communications Jammer Device Direct Hit
            if (hit.collider.name.IndexOf("Jammer", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                if (enableDebugDiagnostics)
                {
                    Debug.Log($"[DEADeyeSniperWeapon] JAMMER DISH HIT: Object={hit.collider.name}, Distance={hit.distance:F1}m");
                }
                if (DEADeyeMissionDirector.Instance != null)
                {
                    DEADeyeMissionDirector.Instance.HandleJammerDishHit();
                }
                return;
            }

            // Hit environment (building, wall, road)
            if (enableDebugDiagnostics)
            {
                Debug.Log($"[DEADeyeSniperWeapon] ENVIRONMENT HIT: Object={hit.collider.name}, Distance={hit.distance:F1}m, Blocked=False");
                Debug.DrawRay(hit.point, Vector3.up * 0.3f, Color.red, 2.0f);
            }

            if (DEADeyeScoreManager.Instance != null)
            {
                DEADeyeScoreManager.Instance.RegisterMiss();
            }
        }

        private IEnumerator DisplayTracerRoutine(Vector3 start, Vector3 end)
        {
            if (bulletTracer == null) yield break;

            bulletTracer.SetPosition(0, start);
            bulletTracer.SetPosition(1, end);
            bulletTracer.enabled = true;

            yield return new WaitForSeconds(0.06f);
            bulletTracer.enabled = false;
        }

        private IEnumerator BoltCycleRoutine()
        {
            yield return new WaitForSeconds(fireRateCooldown * 0.45f);
            if (DEADeyeAudioManager.Instance != null)
            {
                Vector3 pos = muzzlePoint != null ? muzzlePoint.position : transform.position;
                DEADeyeAudioManager.Instance.PlayBoltCycle(pos);
            }
        }
    }
}
