#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

namespace DEADeye.Editor
{
    public static class DEADeyeCharacterSetupUtility
    {
        private const string ModelsDir = "Assets/DEADeye/Art/Characters/Clothed";
        private const string TexturesDir = "Assets/DEADeye/Art/Characters/Clothed/Textures";
        private const string MaterialsDir = "Assets/DEADeye/Art/Characters/Clothed/Materials";
        private const string PrefabsDir = "Assets/DEADeye/Prefabs/Characters";

        [MenuItem("DEADeye/Characters/Generate Clothed Character Prefabs")]
        public static void GenerateClothedCharacterPrefabs()
        {
            if (!AssetDatabase.IsValidFolder(MaterialsDir))
            {
                AssetDatabase.CreateFolder(ModelsDir, "Materials");
            }
            if (!AssetDatabase.IsValidFolder("Assets/DEADeye/Prefabs"))
            {
                AssetDatabase.CreateFolder("Assets/DEADeye", "Prefabs");
            }
            if (!AssetDatabase.IsValidFolder(PrefabsDir))
            {
                AssetDatabase.CreateFolder("Assets/DEADeye/Prefabs", "Characters");
            }

            Shader litShader = Shader.Find("Universal Render Pipeline/Lit");
            if (litShader == null) litShader = Shader.Find("Standard");

            char[] letters = new char[] { 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r' };

            // 1. Create materials for each letter
            foreach (char c in letters)
            {
                string matPath = $"{MaterialsDir}/Mat_Character_{c}.mat";
                string texPath = $"{TexturesDir}/texture-{c}.png";

                Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
                if (tex == null)
                {
                    Debug.LogWarning($"Texture not found: {texPath}");
                    continue;
                }

                Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
                if (mat == null)
                {
                    mat = new Material(litShader) { name = $"Mat_Character_{c}" };
                    AssetDatabase.CreateAsset(mat, matPath);
                }

                mat.mainTexture = tex;
                mat.color = Color.white;
                mat.SetFloat("_Smoothness", 0.0f);
                mat.SetFloat("_Metallic", 0.0f);
                mat.SetFloat("_SpecularHighlights", 0.0f);
                mat.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");
                EditorUtility.SetDirty(mat);
            }

            AssetDatabase.SaveAssets();

            // 2. Build Specific Distinct Prefabs
            // Hostile Rifleman (char a)
            CreateOrUpdateCharacterPrefab("character-a", 'a', "Hostile_Tactical_Rifleman", CharacterRole.Hostile, "HOSTILE RIFLEMAN", 100);
            // Hostile Sniper / Scout (char b)
            CreateOrUpdateCharacterPrefab("character-b", 'b', "Hostile_Sniper_Scout", CharacterRole.Hostile, "HOSTILE SCOUT", 100);
            // Hostile Commander (char d)
            CreateOrUpdateCharacterPrefab("character-d", 'd', "Hostile_Commander", CharacterRole.Hostile, "SYNDICATE COMMANDER", 150);
            // Civilian Male (char e)
            CreateOrUpdateCharacterPrefab("character-e", 'e', "Civilian_Male_Casual", CharacterRole.Civilian, "CIVILIAN", 50);
            // Civilian Female (char g)
            CreateOrUpdateCharacterPrefab("character-g", 'g', "Civilian_Female_Casual", CharacterRole.Civilian, "CIVILIAN", 50);
            // Civilian Business (char i)
            CreateOrUpdateCharacterPrefab("character-i", 'i', "Civilian_Business", CharacterRole.Civilian, "CIVILIAN VIP", 75);
            // Friendly Technician (char k) - Mission 01 key character
            CreateOrUpdateCharacterPrefab("character-k", 'k', "Friendly_Technician", CharacterRole.Friendly, "COMMS TECHNICIAN", 100);
            // Friendly Security Escort (char m)
            CreateOrUpdateCharacterPrefab("character-m", 'm', "Friendly_Security_Escort", CharacterRole.Friendly, "SECURITY OPERATIVE", 120);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Successfully generated all clothed character prefabs!");
        }

        public enum CharacterRole
        {
            Hostile,
            Civilian,
            Friendly
        }

        private static void CreateOrUpdateCharacterPrefab(string modelName, char texChar, string prefabName, CharacterRole role, string classification, int maxHealth)
        {
            string modelPath = $"{ModelsDir}/{modelName}.glb";
            GameObject modelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            if (modelPrefab == null)
            {
                Debug.LogError($"Model not found: {modelPath}");
                return;
            }

            string matPath = $"{MaterialsDir}/Mat_Character_{texChar}.mat";
            Material charMat = AssetDatabase.LoadAssetAtPath<Material>(matPath);

            // Instantiate root
            GameObject root = new GameObject(prefabName);

            // Instantiate visual model as child
            GameObject visual = (GameObject)PrefabUtility.InstantiatePrefab(modelPrefab, root.transform);
            visual.name = "VisualModel";
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = Vector3.one;

            // Apply material to all child renderers
            if (charMat != null)
            {
                var renderers = visual.GetComponentsInChildren<Renderer>(true);
                foreach (var r in renderers)
                {
                    r.sharedMaterial = charMat;
                }
            }

            // Setup NavMeshAgent
            NavMeshAgent agent = root.AddComponent<NavMeshAgent>();
            agent.speed = role == CharacterRole.Hostile ? 1.8f : (role == CharacterRole.Friendly ? 1.5f : 1.2f);
            agent.angularSpeed = 240f;
            agent.acceleration = 8f;
            agent.radius = 0.35f;
            agent.height = 1.8f;
            agent.stoppingDistance = 0.5f;

            // Root Capsule Collider
            CapsuleCollider rootCol = root.AddComponent<CapsuleCollider>();
            rootCol.center = new Vector3(0f, 0.9f, 0f);
            rootCol.height = 1.8f;
            rootCol.radius = 0.35f;

            // Add Procedural Limb & Idle Animator
            DEADeyeProceduralCharacterAnimator procAnim = root.AddComponent<DEADeyeProceduralCharacterAnimator>();
            procAnim.AutoDetectTransforms();

            // Configure Role-specific components & Hitboxes
            if (role == CharacterRole.Hostile)
            {
                root.tag = "Untagged";
                DEADeyeHostileAI hostileAI = root.AddComponent<DEADeyeHostileAI>();
                DEADeyeHostileMarker marker = root.AddComponent<DEADeyeHostileMarker>();

                // Configure Hitboxes
                // Find head transform
                Transform headBone = FindDeepChild(visual.transform, "head");
                if (headBone != null)
                {
                    SphereCollider headCol = headBone.gameObject.AddComponent<SphereCollider>();
                    headCol.radius = 0.22f;
                    DEADeyeHeadshotTrigger headTrigger = headBone.gameObject.AddComponent<DEADeyeHeadshotTrigger>();
                    headTrigger.Initialize(hostileAI);
                }
                else
                {
                    // Fallback head trigger on root
                    GameObject headTriggerGO = new GameObject("HeadshotTrigger");
                    headTriggerGO.transform.SetParent(root.transform, false);
                    headTriggerGO.transform.localPosition = new Vector3(0f, 1.55f, 0f);
                    SphereCollider headCol = headTriggerGO.AddComponent<SphereCollider>();
                    headCol.radius = 0.22f;
                    DEADeyeHeadshotTrigger headTrigger = headTriggerGO.AddComponent<DEADeyeHeadshotTrigger>();
                    headTrigger.Initialize(hostileAI);
                }

                // Torso collider for body hits
                Transform torsoBone = FindDeepChild(visual.transform, "torso");
                if (torsoBone != null)
                {
                    BoxCollider torsoCol = torsoBone.gameObject.AddComponent<BoxCollider>();
                    torsoCol.size = new Vector3(0.5f, 0.6f, 0.35f);
                }
            }
            else if (role == CharacterRole.Civilian)
            {
                DEADeyeCivilianAI civilianAI = root.AddComponent<DEADeyeCivilianAI>();
                civilianAI.SetClassification(classification, maxHealth);

                // Civilian also has head and torso colliders for collision and penalty detection
                Transform torsoBone = FindDeepChild(visual.transform, "torso");
                if (torsoBone != null)
                {
                    BoxCollider torsoCol = torsoBone.gameObject.AddComponent<BoxCollider>();
                    torsoCol.size = new Vector3(0.5f, 0.6f, 0.35f);
                }
            }
            else if (role == CharacterRole.Friendly)
            {
                DEADeyeFriendlyAI friendlyAI = root.AddComponent<DEADeyeFriendlyAI>();
                friendlyAI.SetClassification(classification, maxHealth);

                Transform torsoBone = FindDeepChild(visual.transform, "torso");
                if (torsoBone != null)
                {
                    BoxCollider torsoCol = torsoBone.gameObject.AddComponent<BoxCollider>();
                    torsoCol.size = new Vector3(0.5f, 0.6f, 0.35f);
                }
            }

            // Save as Prefab
            string prefabPath = $"{PrefabsDir}/{prefabName}.prefab";
            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);
            Debug.Log($"Saved prefab: {prefabPath}");
        }

        private static Transform FindDeepChild(Transform parent, string name)
        {
            if (parent.name.ToLower().Contains(name.ToLower())) return parent;
            foreach (Transform child in parent)
            {
                Transform found = FindDeepChild(child, name);
                if (found != null) return found;
            }
            return null;
        }
    }
}
#endif
