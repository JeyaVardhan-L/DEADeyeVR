using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.AI;

namespace DEADeye.Editor
{
    public static class DEADeyeCharacterSetup
    {
        private const string BASE_PATH = "Assets/DEADeye/Art/Characters/Quaternius";
        private const string PREFAB_PATH = "Assets/DEADeye/Prefabs/Hostiles";

        [MenuItem("DEADeye/Setup Characters and Animations")]
        public static void SetupAllCharacters()
        {
            Debug.Log("[DEADeyeCharacterSetup] Starting Quaternius character & animation setup...");

            if (!Directory.Exists(PREFAB_PATH))
            {
                Directory.CreateDirectory(PREFAB_PATH);
            }

            string matDir = $"{BASE_PATH}/Materials";
            if (!Directory.Exists(matDir))
            {
                Directory.CreateDirectory(matDir);
            }

            // 1. Configure Model Importers
            ConfigureModelImporter($"{BASE_PATH}/Models/Superhero_Male_FullBody.fbx");
            ConfigureModelImporter($"{BASE_PATH}/Models/Superhero_Female_FullBody.fbx");
            ConfigureAnimationImporter($"{BASE_PATH}/Animations/UAL1_Standard.fbx");

            AssetDatabase.Refresh();

            // 2. Create Materials
            Material matEnforcer = CreateOrUpdateMaterial($"{matDir}/Mat_Hostile_Enforcer.mat", $"{BASE_PATH}/Textures/T_Superhero_Male_Dark.png", new Color(0.85f, 0.85f, 0.85f));
            Material matInfiltrator = CreateOrUpdateMaterial($"{matDir}/Mat_Hostile_Infiltrator.mat", $"{BASE_PATH}/Textures/T_Superhero_Female_Dark_BaseColor.png", new Color(0.9f, 0.35f, 0.25f));
            Material matCommander = CreateOrUpdateMaterial($"{matDir}/Mat_Hostile_Commander.mat", $"{BASE_PATH}/Textures/T_Superhero_Male_Ligh.png", new Color(0.92f, 0.85f, 0.75f));
            Material matHairDark = CreateOrUpdateMaterial($"{matDir}/Mat_Hair_Dark.mat", $"{BASE_PATH}/Textures/T_Hair_1_BaseColor.png", new Color(0.3f, 0.25f, 0.22f));
            Material matHairBeard = CreateOrUpdateMaterial($"{matDir}/Mat_Hair_Beard.mat", $"{BASE_PATH}/Textures/T_Hair_2_BaseColor.png", new Color(0.45f, 0.4f, 0.35f));
            Material matEyes = CreateOrUpdateMaterial($"{matDir}/Mat_Eyes.mat", $"{BASE_PATH}/Textures/T_Eye_Brown.png", Color.white);

            // 3. Create Animator Controller
            AnimatorController animController = BuildAnimatorController($"{BASE_PATH}/Animations/AC_HostileHumanoid.controller", $"{BASE_PATH}/Animations/UAL1_Standard.fbx");

            // 4. Build 3 Prefabs
            BuildHostilePrefab(HumanoidVariant.Enforcer, "Hostile_Enforcer", $"{BASE_PATH}/Models/Superhero_Male_FullBody.fbx", $"{BASE_PATH}/Models/Hair_Buzzed.fbx", matEnforcer, matHairDark, matEyes, animController, 1.0f);
            BuildHostilePrefab(HumanoidVariant.Infiltrator, "Hostile_Infiltrator", $"{BASE_PATH}/Models/Superhero_Female_FullBody.fbx", $"{BASE_PATH}/Models/Hair_SimpleParted.fbx", matInfiltrator, matHairDark, matEyes, animController, 0.98f);
            BuildHostilePrefab(HumanoidVariant.Commander, "Hostile_Commander", $"{BASE_PATH}/Models/Superhero_Male_FullBody.fbx", $"{BASE_PATH}/Models/Hair_Beard.fbx", matCommander, matHairBeard, matEyes, animController, 1.06f);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[DEADeyeCharacterSetup] Character setup completed successfully!");
        }

        private static void ConfigureModelImporter(string assetPath)
        {
            ModelImporter importer = AssetImporter.GetAtPath(assetPath) as ModelImporter;
            if (importer != null)
            {
                importer.animationType = ModelImporterAnimationType.Human;
                importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                importer.materialImportMode = ModelImporterMaterialImportMode.None;
                importer.SaveAndReimport();
            }
        }

        private static void ConfigureAnimationImporter(string assetPath)
        {
            ModelImporter importer = AssetImporter.GetAtPath(assetPath) as ModelImporter;
            if (importer != null)
            {
                importer.animationType = ModelImporterAnimationType.Human;
                importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                importer.materialImportMode = ModelImporterMaterialImportMode.None;

                // Configure standard clip clips if needed
                ModelImporterClipAnimation[] clips = importer.defaultClipAnimations;
                if (clips != null && clips.Length > 0)
                {
                    foreach (var c in clips)
                    {
                        if (c.name.Contains("Loop") || c.name.Contains("Walk") || c.name.Contains("Jog") || c.name.Contains("Sprint") || c.name.Contains("Idle"))
                        {
                            c.loopTime = true;
                        }
                    }
                    importer.clipAnimations = clips;
                }
                importer.SaveAndReimport();
            }
        }

        private static Material CreateOrUpdateMaterial(string path, string texturePath, Color tint)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");

            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }

            mat.color = tint;
            mat.SetFloat("_Smoothness", 0.15f);
            mat.SetFloat("_Metallic", 0.0f);

            if (!string.IsNullOrEmpty(texturePath))
            {
                Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
                if (tex != null)
                {
                    mat.mainTexture = tex;
                }
            }

            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static AnimatorController BuildAnimatorController(string controllerPath, string animFbxPath)
        {
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
            if (controller == null)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            }

            // Ensure parameters
            AddFloatParam(controller, "Speed");
            AddTriggerParam(controller, "Hit");
            AddTriggerParam(controller, "Die");

            // Find Clips from UAL1_Standard
            Object[] allAssets = AssetDatabase.LoadAllAssetsAtPath(animFbxPath);
            AnimationClip idleClip = FindClip(allAssets, "Idle_Loop") ?? FindClip(allAssets, "Idle");
            AnimationClip walkClip = FindClip(allAssets, "Walk_Loop") ?? FindClip(allAssets, "Walk");
            AnimationClip runClip = FindClip(allAssets, "Jog_Fwd_Loop") ?? FindClip(allAssets, "Sprint_Loop");
            AnimationClip hitClip = FindClip(allAssets, "Hit_Chest") ?? FindClip(allAssets, "Hit_Head");
            AnimationClip deathClip = FindClip(allAssets, "Death01") ?? FindClip(allAssets, "Death");

            AnimatorStateMachine sm = controller.layers[0].stateMachine;
            sm.states = new ChildAnimatorState[0]; // Reset states cleanly
            sm.anyStateTransitions = new AnimatorStateTransition[0];

            AnimatorState idleState = sm.AddState("Idle");
            idleState.motion = idleClip;

            AnimatorState walkState = sm.AddState("Walk");
            walkState.motion = walkClip;

            AnimatorState runState = sm.AddState("Run");
            runState.motion = runClip;

            AnimatorState hitState = sm.AddState("HitReaction");
            hitState.motion = hitClip;

            AnimatorState deathState = sm.AddState("Death");
            deathState.motion = deathClip;

            sm.defaultState = idleState;

            // Idle <-> Walk
            var idleToWalk = idleState.AddTransition(walkState);
            idleToWalk.AddCondition(AnimatorConditionMode.Greater, 0.15f, "Speed");
            idleToWalk.hasExitTime = false;
            idleToWalk.duration = 0.2f;

            var walkToIdle = walkState.AddTransition(idleState);
            walkToIdle.AddCondition(AnimatorConditionMode.Less, 0.15f, "Speed");
            walkToIdle.hasExitTime = false;
            walkToIdle.duration = 0.2f;

            // Walk <-> Run
            var walkToRun = walkState.AddTransition(runState);
            walkToRun.AddCondition(AnimatorConditionMode.Greater, 2.2f, "Speed");
            walkToRun.hasExitTime = false;
            walkToRun.duration = 0.2f;

            var runToWalk = runState.AddTransition(walkState);
            runToWalk.AddCondition(AnimatorConditionMode.Less, 2.2f, "Speed");
            runToWalk.hasExitTime = false;
            runToWalk.duration = 0.2f;

            // AnyState -> Hit
            var anyToHit = sm.AddAnyStateTransition(hitState);
            anyToHit.AddCondition(AnimatorConditionMode.If, 0, "Hit");
            anyToHit.hasExitTime = false;
            anyToHit.duration = 0.1f;

            var hitToIdle = hitState.AddTransition(idleState);
            hitToIdle.hasExitTime = true;
            hitToIdle.duration = 0.2f;

            // AnyState -> Death
            var anyToDeath = sm.AddAnyStateTransition(deathState);
            anyToDeath.AddCondition(AnimatorConditionMode.If, 0, "Die");
            anyToDeath.hasExitTime = false;
            anyToDeath.duration = 0.1f;

            EditorUtility.SetDirty(controller);
            return controller;
        }

        private static AnimationClip FindClip(Object[] assets, string nameQuery)
        {
            foreach (var a in assets)
            {
                if (a is AnimationClip clip && !clip.name.StartsWith("__preview__"))
                {
                    if (clip.name.Equals(nameQuery, System.StringComparison.OrdinalIgnoreCase) || clip.name.Contains(nameQuery))
                    {
                        return clip;
                    }
                }
            }
            return null;
        }

        private static void AddFloatParam(AnimatorController ac, string name)
        {
            foreach (var p in ac.parameters) if (p.name == name) return;
            ac.AddParameter(name, AnimatorControllerParameterType.Float);
        }

        private static void AddTriggerParam(AnimatorController ac, string name)
        {
            foreach (var p in ac.parameters) if (p.name == name) return;
            ac.AddParameter(name, AnimatorControllerParameterType.Trigger);
        }

        private static void BuildHostilePrefab(
            HumanoidVariant varType,
            string prefabName,
            string modelFbxPath,
            string hairFbxPath,
            Material bodyMat,
            Material hairMat,
            Material eyeMat,
            AnimatorController animController,
            float modelScale)
        {
            string prefabPath = $"{PREFAB_PATH}/{prefabName}.prefab";

            GameObject rootGO = new GameObject(prefabName);

            // NavMeshAgent setup
            NavMeshAgent agent = rootGO.AddComponent<NavMeshAgent>();
            agent.height = 1.8f;
            agent.radius = 0.35f;
            agent.baseOffset = 0f;
            agent.acceleration = 12f;
            agent.angularSpeed = 240f;
            agent.stoppingDistance = 0.5f;

            // Visual Model Root
            GameObject visualRoot = new GameObject("VisualModel");
            visualRoot.transform.SetParent(rootGO.transform, false);
            visualRoot.transform.localScale = Vector3.one * modelScale;

            GameObject bodyModelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(modelFbxPath);
            if (bodyModelPrefab != null)
            {
                GameObject bodyInstance = Object.Instantiate(bodyModelPrefab, visualRoot.transform);
                bodyInstance.name = "BodyMesh";
                bodyInstance.transform.localPosition = Vector3.zero;
                bodyInstance.transform.localRotation = Quaternion.identity;

                // Apply body material
                foreach (var r in bodyInstance.GetComponentsInChildren<Renderer>())
                {
                    if (r.name.Contains("Eye"))
                    {
                        r.sharedMaterial = eyeMat;
                    }
                    else
                    {
                        r.sharedMaterial = bodyMat;
                    }
                }

                // Animator setup on bodyInstance or visualRoot
                Animator anim = bodyInstance.GetComponent<Animator>();
                if (anim == null) anim = bodyInstance.AddComponent<Animator>();
                anim.runtimeAnimatorController = animController;
                anim.applyRootMotion = false;
            }

            // Hair/Headgear
            if (!string.IsNullOrEmpty(hairFbxPath))
            {
                GameObject hairPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(hairFbxPath);
                if (hairPrefab != null)
                {
                    GameObject hairInstance = Object.Instantiate(hairPrefab, visualRoot.transform);
                    hairInstance.name = "HairMesh";
                    hairInstance.transform.localPosition = Vector3.zero;
                    hairInstance.transform.localRotation = Quaternion.identity;
                    foreach (var r in hairInstance.GetComponentsInChildren<Renderer>())
                    {
                        r.sharedMaterial = hairMat;
                    }
                }
            }

            // Add Hostile AI Component
            DEADeyeHostileHumanoid humanoidAI = rootGO.AddComponent<DEADeyeHostileHumanoid>();
            humanoidAI.InitializeVariant(varType);

            // Add Overhead Tactical Marker
            DEADeyeHostileMarker marker = rootGO.AddComponent<DEADeyeHostileMarker>();

            // Save as Prefab
            PrefabUtility.SaveAsPrefabAsset(rootGO, prefabPath);
            Object.DestroyImmediate(rootGO);
            Debug.Log($"[DEADeyeCharacterSetup] Created prefab: {prefabPath}");
        }
    }
}

