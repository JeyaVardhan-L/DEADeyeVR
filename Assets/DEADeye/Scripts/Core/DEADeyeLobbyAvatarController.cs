using UnityEngine;

namespace DEADeye
{
    /// <summary>
    /// Displays the player's tactical operative avatar standing in the Dojo Lobby during operations briefing.
    /// Provides smooth idle breathing and rifle presentation.
    /// </summary>
    public class DEADeyeLobbyAvatarController : MonoBehaviour
    {
        public static DEADeyeLobbyAvatarController Instance { get; private set; }

        [Header("Visual Root")]
        [SerializeField] private GameObject avatarRoot;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            BuildTacticalAvatar();
        }

        private void BuildTacticalAvatar()
        {
            if (avatarRoot != null) return;

            avatarRoot = new GameObject("TacticalSniperAvatar");
            avatarRoot.transform.SetParent(transform, false);
            avatarRoot.transform.localPosition = Vector3.zero;
            avatarRoot.transform.localRotation = Quaternion.identity;

            Shader litShader = Shader.Find("Universal Render Pipeline/Lit");
            if (litShader == null) litShader = Shader.Find("Standard");

            Material suitMat = new Material(litShader) { name = "SuitMat", color = new Color(0.12f, 0.15f, 0.18f) }; // Charcoal suit
            Material vestMat = new Material(litShader) { name = "VestMat", color = new Color(0.22f, 0.25f, 0.22f) }; // Olive vest
            Material skinMat = new Material(litShader) { name = "SkinMat", color = new Color(0.85f, 0.68f, 0.55f) };
            Material visorMat = new Material(litShader) { name = "VisorMat", color = new Color(0.1f, 0.85f, 0.95f) }; // Cyan visor

            // Torso / Tactical Vest
            GameObject torso = GameObject.CreatePrimitive(PrimitiveType.Cube);
            torso.name = "Torso";
            torso.transform.SetParent(avatarRoot.transform, false);
            torso.transform.localPosition = new Vector3(0f, 1.15f, 0f);
            torso.transform.localScale = new Vector3(0.52f, 0.60f, 0.28f);
            torso.GetComponent<Renderer>().material = vestMat;
            Destroy(torso.GetComponent<Collider>());

            // Head & Tactical Visor
            GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "Head";
            head.transform.SetParent(avatarRoot.transform, false);
            head.transform.localPosition = new Vector3(0f, 1.62f, 0f);
            head.transform.localScale = new Vector3(0.24f, 0.28f, 0.24f);
            head.GetComponent<Renderer>().material = skinMat;
            Destroy(head.GetComponent<Collider>());

            GameObject visor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visor.name = "Visor";
            visor.transform.SetParent(head.transform, false);
            visor.transform.localPosition = new Vector3(0f, 0.04f, 0.12f);
            visor.transform.localScale = new Vector3(0.22f, 0.08f, 0.06f);
            visor.GetComponent<Renderer>().material = visorMat;
            Destroy(visor.GetComponent<Collider>());

            // Legs
            GameObject leftLeg = GameObject.CreatePrimitive(PrimitiveType.Cube);
            leftLeg.name = "LeftLeg";
            leftLeg.transform.SetParent(avatarRoot.transform, false);
            leftLeg.transform.localPosition = new Vector3(-0.16f, 0.42f, 0f);
            leftLeg.transform.localScale = new Vector3(0.18f, 0.84f, 0.22f);
            leftLeg.GetComponent<Renderer>().material = suitMat;
            Destroy(leftLeg.GetComponent<Collider>());

            GameObject rightLeg = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rightLeg.name = "RightLeg";
            rightLeg.transform.SetParent(avatarRoot.transform, false);
            rightLeg.transform.localPosition = new Vector3(0.16f, 0.42f, 0f);
            rightLeg.transform.localScale = new Vector3(0.18f, 0.84f, 0.22f);
            rightLeg.GetComponent<Renderer>().material = suitMat;
            Destroy(rightLeg.GetComponent<Collider>());

            // Slung Sniper Rifle on back
            GameObject rifleModel = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rifleModel.name = "SlungRifle";
            rifleModel.transform.SetParent(torso.transform, false);
            rifleModel.transform.localPosition = new Vector3(0.05f, 0f, -0.22f);
            rifleModel.transform.localRotation = Quaternion.Euler(0f, 0f, -40f);
            rifleModel.transform.localScale = new Vector3(0.12f, 1.4f, 0.14f);
            rifleModel.GetComponent<Renderer>().material = suitMat;
            Destroy(rifleModel.GetComponent<Collider>());
        }

        private void Update()
        {
            if (avatarRoot != null)
            {
                // Subtle breathing animation
                float breath = Mathf.Sin(Time.time * 2.0f) * 0.015f;
                avatarRoot.transform.localPosition = new Vector3(0f, breath, 0f);
            }
        }
    }
}

