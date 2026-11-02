using System.Collections;
using UnityEngine;
using UnityEngine.AI;

namespace DEADeye
{
    public enum HumanoidVariant
    {
        Enforcer,       // Heavy tactical enforcer with combat carbine
        Infiltrator,    // Fast agile female scout in stealth suit
        Commander       // Heavy armored syndicate commander
    }

    /// <summary>
    /// Production humanoid hostile AI powered by real rigged 3D models and Animator controllers.
    /// Features separate torso and head hitboxes, procedural/skeletal animations,
    /// hit reactions, and death animations while preserving full DEADeyeHostileAI mechanics.
    /// </summary>
    public class DEADeyeHostileHumanoid : DEADeyeHostileAI
    {
        [Header("Humanoid Visuals")]
        [SerializeField] private HumanoidVariant variant = HumanoidVariant.Enforcer;
        [SerializeField] private Animator animator;
        [SerializeField] private Transform visualRoot;

        public HumanoidVariant Variant => variant;
        public Animator HostileAnimator => animator;

        protected override void Awake()
        {
            base.Awake();

            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }

            // Ensure body and head colliders are properly separated and aligned
            SetupHitboxes();
        }

        public void InitializeVariant(HumanoidVariant varType)
        {
            variant = varType;
            switch (variant)
            {
                case HumanoidVariant.Enforcer:
                    classificationName = "ENFORCER";
                    hostileType = HostileType.Criminal;
                    maxHealth = 100;
                    basePoints = 100;
                    walkSpeed = 1.6f;
                    alertSpeed = 2.8f;
                    escapeSpeed = 3.6f;
                    break;

                case HumanoidVariant.Infiltrator:
                    classificationName = "INFILTRATOR";
                    hostileType = HostileType.Criminal;
                    maxHealth = 80;
                    basePoints = 120;
                    walkSpeed = 1.9f;
                    alertSpeed = 3.2f;
                    escapeSpeed = 4.0f;
                    break;

                case HumanoidVariant.Commander:
                    classificationName = "COMMANDER";
                    hostileType = HostileType.Monster;
                    maxHealth = 150;
                    basePoints = 200;
                    walkSpeed = 1.4f;
                    alertSpeed = 2.4f;
                    escapeSpeed = 3.2f;
                    break;
            }

            currentHealth = maxHealth;
            if (agent != null)
            {
                agent.speed = WalkSpeed;
            }
        }

        private void SetupHitboxes()
        {
            // Remove any root capsule collider that might enclose both head and body
            Collider rootCol = GetComponent<Collider>();
            if (rootCol != null && !(rootCol is CharacterController))
            {
                Destroy(rootCol);
            }

            // 1. Torso Body Collider: Covers strictly the torso / chest, NOT the whole body indiscriminately
            GameObject torsoGO = transform.Find("Hitbox_Torso")?.gameObject;
            if (torsoGO == null)
            {
                torsoGO = new GameObject("Hitbox_Torso");
                torsoGO.transform.SetParent(transform, false);
            }

            CapsuleCollider torsoCollider = torsoGO.GetComponent<CapsuleCollider>();
            if (torsoCollider == null)
            {
                torsoCollider = torsoGO.AddComponent<CapsuleCollider>();
            }
            torsoCollider.center = new Vector3(0f, 1.05f, 0f);
            torsoCollider.height = 0.65f;
            torsoCollider.radius = 0.25f;
            torsoCollider.isTrigger = false;

            // 2. Head Collider: Precision sphere centered directly on the head
            GameObject headGO = transform.Find("Hitbox_Head")?.gameObject;
            if (headGO == null)
            {
                headGO = new GameObject("Hitbox_Head");
                headGO.transform.SetParent(transform, false);
            }

            SphereCollider headCollider = headGO.GetComponent<SphereCollider>();
            if (headCollider == null)
            {
                headCollider = headGO.AddComponent<SphereCollider>();
            }
            headCollider.center = new Vector3(0f, 1.62f, 0f);
            headCollider.radius = 0.16f;
            headCollider.isTrigger = true;

            DEADeyeHeadshotTrigger trigger = headGO.GetComponent<DEADeyeHeadshotTrigger>();
            if (trigger == null)
            {
                trigger = headGO.AddComponent<DEADeyeHeadshotTrigger>();
            }
            trigger.Initialize(this);
        }

        protected override void Update()
        {
            base.Update();

            if (isEliminated) return;

            // Sync NavMeshAgent velocity to Animator Speed parameter
            if (animator != null && agent != null && agent.enabled)
            {
                float currentSpeed = agent.velocity.magnitude;
                animator.SetFloat("Speed", currentSpeed);
            }
        }

        public override void TakeDamage(int damage, Vector3 hitPoint, Vector3 hitNormal, bool isHeadshot = false)
        {
            base.TakeDamage(damage, hitPoint, hitNormal, isHeadshot);

            if (!isEliminated)
            {
                if (animator != null)
                {
                    animator.SetTrigger("Hit");
                }
            }
        }

        public override void ChangeState(HostileStateType newState)
        {
            base.ChangeState(newState);

            if (newState == HostileStateType.Eliminated)
            {
                if (animator != null)
                {
                    animator.SetTrigger("Die");
                }

                // Disable colliders so eliminated bodies do not obstruct future sniper shots
                foreach (var col in GetComponentsInChildren<Collider>())
                {
                    col.enabled = false;
                }

                // Stop NavMesh navigation immediately
                if (agent != null && agent.enabled)
                {
                    if (agent.isOnNavMesh)
                    {
                        agent.isStopped = true;
                    }
                    agent.enabled = false;
                }
            }
        }
    }
}

