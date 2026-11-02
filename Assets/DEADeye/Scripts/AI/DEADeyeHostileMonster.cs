using UnityEngine;

namespace DEADeye
{
    /// <summary>
    /// Urban Monster hostile.
    /// Fast-moving, hunched predatory silhouette with glowing emissive eyes.
    /// </summary>
    public class DEADeyeHostileMonster : DEADeyeHostileAI
    {
        [Header("Procedural Visuals")]
        [SerializeField] private Material bodyMaterial;
        [SerializeField] private Material eyeMaterial;
        [SerializeField] private Material hornMaterial;

        private Transform leftForearmTransform;
        private Transform rightForearmTransform;
        private Transform leftHindTransform;
        private Transform rightHindTransform;
        private Transform torsoTransform;

        protected override void Awake()
        {
            hostileType = HostileType.Monster;
            classificationName = "MONSTER";
            basePoints = 250;
            maxHealth = 150;
            walkSpeed = 2.4f;
            alertSpeed = 3.4f;
            escapeSpeed = 4.2f;

            base.Awake();
            BuildProceduralBeast();
        }

        private void BuildProceduralBeast()
        {
            if (transform.Find("VisualRoot") != null) return;

            GameObject root = new GameObject("VisualRoot");
            root.transform.SetParent(transform, false);

            Shader litShader = Shader.Find("Universal Render Pipeline/Lit");
            if (litShader == null) litShader = Shader.Find("Standard");

            if (bodyMaterial == null)
            {
                bodyMaterial = new Material(litShader) { name = "MonsterBodyMat" };
                bodyMaterial.color = new Color(0.52f, 0.14f, 0.18f); // High-contrast demonic crimson
            }
            if (eyeMaterial == null)
            {
                Shader unlitShader = Shader.Find("Universal Render Pipeline/Unlit");
                if (unlitShader == null) unlitShader = Shader.Find("Unlit/Color");
                eyeMaterial = new Material(unlitShader) { name = "MonsterEyeMat" };
                eyeMaterial.color = new Color(1.0f, 0.45f, 0.0f); // Bright glowing orange
            }
            if (hornMaterial == null)
            {
                hornMaterial = new Material(litShader) { name = "MonsterHornMat" };
                hornMaterial.color = new Color(0.35f, 0.28f, 0.25f);
            }

            // Torso
            GameObject torso = GameObject.CreatePrimitive(PrimitiveType.Cube);
            torso.name = "HunchedTorso";
            torso.transform.SetParent(root.transform, false);
            torso.transform.localPosition = new Vector3(0f, 1.15f, 0.15f);
            torso.transform.localRotation = Quaternion.Euler(20f, 0f, 0f);
            torso.transform.localScale = new Vector3(0.85f, 0.75f, 0.65f);
            torso.GetComponent<Renderer>().material = bodyMaterial;
            Destroy(torso.GetComponent<Collider>());
            torsoTransform = torso.transform;

            // Head
            GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "MonsterHead";
            head.transform.SetParent(root.transform, false);
            head.transform.localPosition = new Vector3(0f, 1.45f, 0.55f);
            head.transform.localScale = new Vector3(0.48f, 0.42f, 0.58f);
            head.GetComponent<Renderer>().material = bodyMaterial;
            SphereCollider headCollider = head.GetComponent<SphereCollider>();
            headCollider.isTrigger = true;
            head.AddComponent<DEADeyeHeadshotTrigger>().Initialize(this);

            // Glowing eyes
            GameObject leftEye = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            leftEye.name = "LeftEye";
            leftEye.transform.SetParent(head.transform, false);
            leftEye.transform.localPosition = new Vector3(-0.35f, 0.2f, 0.45f);
            leftEye.transform.localScale = Vector3.one * 0.22f;
            leftEye.GetComponent<Renderer>().material = eyeMaterial;
            Destroy(leftEye.GetComponent<Collider>());

            GameObject rightEye = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            rightEye.name = "RightEye";
            rightEye.transform.SetParent(head.transform, false);
            rightEye.transform.localPosition = new Vector3(0.35f, 0.2f, 0.45f);
            rightEye.transform.localScale = Vector3.one * 0.22f;
            rightEye.GetComponent<Renderer>().material = eyeMaterial;
            Destroy(rightEye.GetComponent<Collider>());

            // Horns
            GameObject leftHorn = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            leftHorn.name = "LeftHorn";
            leftHorn.transform.SetParent(head.transform, false);
            leftHorn.transform.localPosition = new Vector3(-0.45f, 0.55f, 0.05f);
            leftHorn.transform.localRotation = Quaternion.Euler(25f, -30f, 35f);
            leftHorn.transform.localScale = new Vector3(0.12f, 0.35f, 0.12f);
            leftHorn.GetComponent<Renderer>().material = hornMaterial;
            Destroy(leftHorn.GetComponent<Collider>());

            GameObject rightHorn = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            rightHorn.name = "RightHorn";
            rightHorn.transform.SetParent(head.transform, false);
            rightHorn.transform.localPosition = new Vector3(0.45f, 0.55f, 0.05f);
            rightHorn.transform.localRotation = Quaternion.Euler(25f, 30f, -35f);
            rightHorn.transform.localScale = new Vector3(0.12f, 0.35f, 0.12f);
            rightHorn.GetComponent<Renderer>().material = hornMaterial;
            Destroy(rightHorn.GetComponent<Collider>());

            // Forearms
            GameObject leftForearm = GameObject.CreatePrimitive(PrimitiveType.Cube);
            leftForearm.name = "LeftForearm";
            leftForearm.transform.SetParent(root.transform, false);
            leftForearm.transform.localPosition = new Vector3(-0.55f, 0.65f, 0.35f);
            leftForearm.transform.localRotation = Quaternion.Euler(-15f, 0f, 15f);
            leftForearm.transform.localScale = new Vector3(0.28f, 0.85f, 0.32f);
            leftForearm.GetComponent<Renderer>().material = bodyMaterial;
            Destroy(leftForearm.GetComponent<Collider>());
            leftForearmTransform = leftForearm.transform;

            GameObject rightForearm = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rightForearm.name = "RightForearm";
            rightForearm.transform.SetParent(root.transform, false);
            rightForearm.transform.localPosition = new Vector3(0.55f, 0.65f, 0.35f);
            rightForearm.transform.localRotation = Quaternion.Euler(-15f, 0f, -15f);
            rightForearm.transform.localScale = new Vector3(0.28f, 0.85f, 0.32f);
            rightForearm.GetComponent<Renderer>().material = bodyMaterial;
            Destroy(rightForearm.GetComponent<Collider>());
            rightForearmTransform = rightForearm.transform;

            // Hind legs
            GameObject leftHind = GameObject.CreatePrimitive(PrimitiveType.Cube);
            leftHind.name = "LeftHind";
            leftHind.transform.SetParent(root.transform, false);
            leftHind.transform.localPosition = new Vector3(-0.35f, 0.45f, -0.25f);
            leftHind.transform.localScale = new Vector3(0.24f, 0.75f, 0.28f);
            leftHind.GetComponent<Renderer>().material = bodyMaterial;
            Destroy(leftHind.GetComponent<Collider>());
            leftHindTransform = leftHind.transform;

            GameObject rightHind = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rightHind.name = "RightHind";
            rightHind.transform.SetParent(root.transform, false);
            rightHind.transform.localPosition = new Vector3(0.35f, 0.45f, -0.25f);
            rightHind.transform.localScale = new Vector3(0.24f, 0.75f, 0.28f);
            rightHind.GetComponent<Renderer>().material = bodyMaterial;
            Destroy(rightHind.GetComponent<Collider>());
            rightHindTransform = rightHind.transform;
        }

        protected override void Update()
        {
            base.Update();

            if (isEliminated)
            {
                // Fall forward upon elimination
                transform.localRotation = Quaternion.Slerp(transform.localRotation, Quaternion.Euler(-80f, transform.eulerAngles.y, 0f), Time.deltaTime * 6f);
                return;
            }

            // Quadruped/beast predatory stride animation
            if (agent != null && agent.velocity.sqrMagnitude > 0.04f)
            {
                float speed = agent.velocity.magnitude * 6.0f;
                float angle = Mathf.Sin(Time.time * speed) * 30f;

                if (leftForearmTransform != null) leftForearmTransform.localRotation = Quaternion.Euler(-15f + angle, 0f, 15f);
                if (rightForearmTransform != null) rightForearmTransform.localRotation = Quaternion.Euler(-15f - angle, 0f, -15f);
                if (leftHindTransform != null) leftHindTransform.localRotation = Quaternion.Euler(-angle, 0f, 0f);
                if (rightHindTransform != null) rightHindTransform.localRotation = Quaternion.Euler(angle, 0f, 0f);
                if (torsoTransform != null) torsoTransform.localPosition = new Vector3(0f, 1.15f + Mathf.Abs(Mathf.Sin(Time.time * speed * 2f)) * 0.05f, 0.15f);
            }
        }
    }
}

