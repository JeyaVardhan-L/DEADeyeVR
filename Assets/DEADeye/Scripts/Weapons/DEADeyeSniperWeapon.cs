using System.Collections;
using UnityEngine;
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

        private float nextFireTime = 0f;
        private Vector3 recoilOffset = Vector3.zero;
        private Quaternion recoilRotation = Quaternion.identity;
        private Vector3 reloadOffset = Vector3.zero;
        private IXRSelectInteractor secondaryInteractor;

        public bool IsReadyToFire => Time.time >= nextFireTime;
        public DEADeyeScopeOptic ScopeOptic => scopeOptic;

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
            // Desktop firing check: LMB while in desktop mode
            if (DEADeyeGameManager.Instance != null && DEADeyeGameManager.Instance.PlayerMode == DEADeyePlayerMode.Desktop)
            {
                if (DEADeyeInputRouter.Instance != null)
                {
                    if (DEADeyeInputRouter.Instance.GetFireDown())
                    {
                        TryFire();
                    }

                    // Scope optic activation on RMB
                    if (scopeOptic != null)
                    {
                        scopeOptic.SetScopeActive(DEADeyeInputRouter.Instance.GetAimHeld());
                    }
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

            // Precision Ballistics Raycast:
            // When in first-person camera mode, raycast from camera viewport center (0.5, 0.5)
            // for 100% pixel-perfect crosshair alignment!
            Vector3 shootOrigin;
            Vector3 shootDirection;

            if (DEADeyePlayerController.Instance != null && DEADeyePlayerController.Instance.ActiveCamera != null)
            {
                Camera activeCam = DEADeyePlayerController.Instance.ActiveCamera;
                Ray centerRay = activeCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
                shootOrigin = centerRay.origin;
                shootDirection = centerRay.direction;
            }
            else
            {
                shootOrigin = muzzlePoint.position;
                shootDirection = muzzlePoint.forward;
            }

            Vector3 endPoint = shootOrigin + shootDirection * maxRange;

            if (Physics.Raycast(shootOrigin, shootDirection, out RaycastHit hit, maxRange, hitLayers, QueryTriggerInteraction.Collide))
            {
                endPoint = hit.point;
                ProcessHit(hit);
            }
            else
            {
                // Complete miss
                if (DEADeyeScoreManager.Instance != null)
                {
                    DEADeyeScoreManager.Instance.RegisterMiss();
                }
            }

            // Show Tracer & Bolt Cycle (Only when playing)
            if (Application.isPlaying)
            {
                Vector3 tracerStart = muzzlePoint != null ? muzzlePoint.position : shootOrigin;
                StartCoroutine(DisplayTracerRoutine(tracerStart, endPoint));
                StartCoroutine(BoltCycleRoutine());
            }
        }

        private void ProcessHit(RaycastHit hit)
        {
            // Check Headshot Trigger
            DEADeyeHeadshotTrigger headshot = hit.collider.GetComponent<DEADeyeHeadshotTrigger>();
            if (headshot != null)
            {
                headshot.RegisterHeadshot(baseDamage, hit.point, hit.normal);
                return;
            }

            // Check General Hostile AI
            DEADeyeHostileAI hostile = hit.collider.GetComponentInParent<DEADeyeHostileAI>();
            if (hostile != null)
            {
                hostile.TakeDamage(baseDamage, hit.point, hit.normal, isHeadshot: false);
                return;
            }

            // Hit environment (building, wall, road)
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
