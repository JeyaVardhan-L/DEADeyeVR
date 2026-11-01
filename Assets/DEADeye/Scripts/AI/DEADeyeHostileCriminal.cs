using UnityEngine;

namespace DEADeye
{
    /// <summary>
    /// Urban Criminal hostile.
    /// Humanoid silhouette with coat, hat, and street attire.
    /// Navigates sidewalks, crosswalks, and alleys attempting to reach extraction points.
    /// </summary>
    public class DEADeyeHostileCriminal : DEADeyeHostileAI
    {
        [Header("Procedural Visuals")]
        [SerializeField] private Material coatMaterial;
        [SerializeField] private Material skinMaterial;
        [SerializeField] private Material pantsMaterial;

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

            if (coatMaterial == null)
            {
                coatMaterial = new Material(litShader) { name = "CriminalCoatMat" };
                coatMaterial.color = new Color(0.18f, 0.22f, 0.28f);
            }
            if (skinMaterial == null)
            {
                skinMaterial = new Material(litShader) { name = "CriminalSkinMat" };
                skinMaterial.color = new Color(0.85f, 0.68f, 0.55f);
            }
            if (pantsMaterial == null)
            {
                pantsMaterial = new Material(litShader) { name = "CriminalPantsMat" };
                pantsMaterial.color = new Color(0.12f, 0.12f, 0.14f);
            }

            // Torso (Coat)
            GameObject torso = GameObject.CreatePrimitive(PrimitiveType.Cube);
            torso.name = "Torso";
            torso.transform.SetParent(root.transform, false);
            torso.transform.localPosition = new Vector3(0f, 1.05f, 0f);
            torso.transform.localScale = new Vector3(0.55f, 0.75f, 0.32f);
            torso.GetComponent<Renderer>().material = coatMaterial;
            Destroy(torso.GetComponent<Collider>());

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

            // Hat
            GameObject hat = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            hat.name = "Hat";
            hat.transform.SetParent(root.transform, false);
            hat.transform.localPosition = new Vector3(0f, 1.78f, 0f);
            hat.transform.localScale = new Vector3(0.32f, 0.08f, 0.32f);
            hat.GetComponent<Renderer>().material = pantsMaterial;
            Destroy(hat.GetComponent<Collider>());

            // Legs
            GameObject leftLeg = GameObject.CreatePrimitive(PrimitiveType.Cube);
            leftLeg.name = "LeftLeg";
            leftLeg.transform.SetParent(root.transform, false);
            leftLeg.transform.localPosition = new Vector3(-0.16f, 0.36f, 0f);
            leftLeg.transform.localScale = new Vector3(0.18f, 0.72f, 0.22f);
            leftLeg.GetComponent<Renderer>().material = pantsMaterial;
            Destroy(leftLeg.GetComponent<Collider>());

            GameObject rightLeg = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rightLeg.name = "RightLeg";
            rightLeg.transform.SetParent(root.transform, false);
            rightLeg.transform.localPosition = new Vector3(0.16f, 0.36f, 0f);
            rightLeg.transform.localScale = new Vector3(0.18f, 0.72f, 0.22f);
            rightLeg.GetComponent<Renderer>().material = pantsMaterial;
            Destroy(rightLeg.GetComponent<Collider>());

            // Arms & briefcase
            GameObject leftArm = GameObject.CreatePrimitive(PrimitiveType.Cube);
            leftArm.name = "LeftArm";
            leftArm.transform.SetParent(root.transform, false);
            leftArm.transform.localPosition = new Vector3(-0.35f, 1.0f, 0.05f);
            leftArm.transform.localScale = new Vector3(0.14f, 0.65f, 0.16f);
            leftArm.GetComponent<Renderer>().material = coatMaterial;
            Destroy(leftArm.GetComponent<Collider>());

            GameObject rightArm = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rightArm.name = "RightArm";
            rightArm.transform.SetParent(root.transform, false);
            rightArm.transform.localPosition = new Vector3(0.35f, 1.0f, 0.05f);
            rightArm.transform.localScale = new Vector3(0.14f, 0.65f, 0.16f);
            rightArm.GetComponent<Renderer>().material = coatMaterial;
            Destroy(rightArm.GetComponent<Collider>());

            GameObject briefcase = GameObject.CreatePrimitive(PrimitiveType.Cube);
            briefcase.name = "Briefcase";
            briefcase.transform.SetParent(root.transform, false);
            briefcase.transform.localPosition = new Vector3(0.42f, 0.55f, 0.05f);
            briefcase.transform.localScale = new Vector3(0.12f, 0.28f, 0.38f);
            briefcase.GetComponent<Renderer>().material = pantsMaterial;
            Destroy(briefcase.GetComponent<Collider>());
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

