using UnityEngine;

namespace DEADeye
{
    /// <summary>
    /// Urban Criminal hostile.
    /// Humanoid operative silhouette with high-contrast tactical vest, helmet, and carbine.
    /// Navigates sidewalks, crosswalks, and alleys attempting to reach extraction points.
    /// High-contrast colors ensure immediate visibility from high rooftop sniper perches.
    /// </summary>
    public class DEADeyeHostileCriminal : DEADeyeHostileAI
    {
        [Header("Procedural Visuals")]
        [SerializeField] private Material vestMaterial;
        [SerializeField] private Material skinMaterial;
        [SerializeField] private Material pantsMaterial;
        [SerializeField] private Material helmetMaterial;

        private Transform leftLegTransform;
        private Transform rightLegTransform;
        private Transform leftArmTransform;
        private Transform rightArmTransform;
        private Transform torsoTransform;

        protected override void Awake()
        {
            hostileType = HostileType.Criminal;
            classificationName = "CRIMINAL";
            basePoints = 100;
            maxHealth = 100;
            walkSpeed = 1.6f;
            alertSpeed = 2.6f;
            escapeSpeed = 3.2f;

            base.Awake();
            BuildProceduralHumanoid();
        }

        private void BuildProceduralHumanoid()
        {
            if (transform.Find("VisualRoot") != null) return;

            GameObject root = new GameObject("VisualRoot");
            root.transform.SetParent(transform, false);

            Shader litShader = Shader.Find("Universal Render Pipeline/Lit");
            if (litShader == null) litShader = Shader.Find("Standard");

            if (vestMaterial == null)
            {
                vestMaterial = new Material(litShader) { name = "CriminalVestMat" };
                vestMaterial.color = new Color(0.90f, 0.28f, 0.14f); // High-contrast tactical orange/crimson
            }
            if (skinMaterial == null)
            {
                skinMaterial = new Material(litShader) { name = "CriminalSkinMat" };
                skinMaterial.color = new Color(0.88f, 0.72f, 0.60f);
            }
            if (pantsMaterial == null)
            {
                pantsMaterial = new Material(litShader) { name = "CriminalPantsMat" };
                pantsMaterial.color = new Color(0.20f, 0.24f, 0.28f); // Tactical grey
            }
            if (helmetMaterial == null)
            {
                helmetMaterial = new Material(litShader) { name = "CriminalHelmetMat" };
                helmetMaterial.color = new Color(0.92f, 0.92f, 0.90f); // High-vis ballistic white helmet
            }

            // Torso (High-vis Tactical Vest)
            GameObject torso = GameObject.CreatePrimitive(PrimitiveType.Cube);
            torso.name = "Torso";
            torso.transform.SetParent(root.transform, false);
            torso.transform.localPosition = new Vector3(0f, 1.05f, 0f);
            torso.transform.localScale = new Vector3(0.58f, 0.76f, 0.35f);
            torso.GetComponent<Renderer>().material = vestMaterial;
            Destroy(torso.GetComponent<Collider>());
            torsoTransform = torso.transform;

            // Head with dedicated headshot trigger
            GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "Head";
            head.transform.SetParent(root.transform, false);
            head.transform.localPosition = new Vector3(0f, 1.62f, 0f);
            head.transform.localScale = new Vector3(0.28f, 0.32f, 0.28f);
            head.GetComponent<Renderer>().material = skinMaterial;
            SphereCollider headCollider = head.GetComponent<SphereCollider>();
            headCollider.isTrigger = true;
            head.AddComponent<DEADeyeHeadshotTrigger>().Initialize(this);

            // Ballistic Helmet with Visor
            GameObject helmet = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            helmet.name = "Helmet";
            helmet.transform.SetParent(root.transform, false);
            helmet.transform.localPosition = new Vector3(0f, 1.72f, 0f);
            helmet.transform.localScale = new Vector3(0.34f, 0.24f, 0.34f);
            helmet.GetComponent<Renderer>().material = helmetMaterial;
            Destroy(helmet.GetComponent<Collider>());

            // Legs (Combat trousers)
            GameObject leftLeg = GameObject.CreatePrimitive(PrimitiveType.Cube);
            leftLeg.name = "LeftLeg";
            leftLeg.transform.SetParent(root.transform, false);
            leftLeg.transform.localPosition = new Vector3(-0.16f, 0.36f, 0f);
            leftLeg.transform.localScale = new Vector3(0.18f, 0.72f, 0.22f);
            leftLeg.GetComponent<Renderer>().material = pantsMaterial;
            Destroy(leftLeg.GetComponent<Collider>());
            leftLegTransform = leftLeg.transform;

            GameObject rightLeg = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rightLeg.name = "RightLeg";
            rightLeg.transform.SetParent(root.transform, false);
            rightLeg.transform.localPosition = new Vector3(0.16f, 0.36f, 0f);
            rightLeg.transform.localScale = new Vector3(0.18f, 0.72f, 0.22f);
            rightLeg.GetComponent<Renderer>().material = pantsMaterial;
            Destroy(rightLeg.GetComponent<Collider>());
            rightLegTransform = rightLeg.transform;

            // Arms & Carbine
            GameObject leftArm = GameObject.CreatePrimitive(PrimitiveType.Cube);
            leftArm.name = "LeftArm";
            leftArm.transform.SetParent(root.transform, false);
            leftArm.transform.localPosition = new Vector3(-0.36f, 1.0f, 0.05f);
            leftArm.transform.localScale = new Vector3(0.15f, 0.65f, 0.16f);
            leftArm.GetComponent<Renderer>().material = vestMaterial;
            Destroy(leftArm.GetComponent<Collider>());
            leftArmTransform = leftArm.transform;

            GameObject rightArm = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rightArm.name = "RightArm";
            rightArm.transform.SetParent(root.transform, false);
            rightArm.transform.localPosition = new Vector3(0.36f, 1.0f, 0.05f);
            rightArm.transform.localScale = new Vector3(0.15f, 0.65f, 0.16f);
            rightArm.GetComponent<Renderer>().material = vestMaterial;
            Destroy(rightArm.GetComponent<Collider>());
            rightArmTransform = rightArm.transform;

            // Tactical Carbine Rifle carried across chest
            GameObject carbine = GameObject.CreatePrimitive(PrimitiveType.Cube);
            carbine.name = "Carbine";
            carbine.transform.SetParent(root.transform, false);
            carbine.transform.localPosition = new Vector3(0.12f, 0.95f, 0.22f);
            carbine.transform.localRotation = Quaternion.Euler(15f, -25f, 10f);
            carbine.transform.localScale = new Vector3(0.08f, 0.14f, 0.65f);
            carbine.GetComponent<Renderer>().material = pantsMaterial;
            Destroy(carbine.GetComponent<Collider>());
        }

        protected override void Update()
        {
            base.Update();

            if (isEliminated)
            {
                // Fall down upon elimination
                transform.localRotation = Quaternion.Slerp(transform.localRotation, Quaternion.Euler(85f, transform.eulerAngles.y, 0f), Time.deltaTime * 6f);
                return;
            }

            // Limb walk animation while moving
            if (agent != null && agent.velocity.sqrMagnitude > 0.04f)
            {
                float speed = agent.velocity.magnitude * 5.0f;
                float angle = Mathf.Sin(Time.time * speed) * 26f;

                if (leftLegTransform != null) leftLegTransform.localRotation = Quaternion.Euler(angle, 0f, 0f);
                if (rightLegTransform != null) rightLegTransform.localRotation = Quaternion.Euler(-angle, 0f, 0f);
                if (leftArmTransform != null) leftArmTransform.localRotation = Quaternion.Euler(-angle * 0.75f, 0f, 0f);
                if (rightArmTransform != null) rightArmTransform.localRotation = Quaternion.Euler(angle * 0.75f, 0f, 0f);
                if (torsoTransform != null) torsoTransform.localPosition = new Vector3(0f, 1.05f + Mathf.Abs(Mathf.Sin(Time.time * speed * 2f)) * 0.03f, 0f);
            }
        }
    }

    /// <summary>
    /// Attached to the head collider to register precision sniper headshots.
    /// </summary>
    public class DEADeyeHeadshotTrigger : MonoBehaviour
    {
        private DEADeyeHostileAI parentHostile;
        public DEADeyeHostileAI ParentHostile => parentHostile;

        public void Initialize(DEADeyeHostileAI hostile)
        {
            parentHostile = hostile;
        }

        public void RegisterHeadshot(int damage, Vector3 point, Vector3 normal)
        {
            if (parentHostile != null)
            {
                parentHostile.TakeDamage(damage, point, normal, isHeadshot: true);
            }
        }
    }
}
