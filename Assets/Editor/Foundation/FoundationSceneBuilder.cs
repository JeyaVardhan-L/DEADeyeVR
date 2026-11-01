using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using Unity.XR.CoreUtils;

namespace DEADeyeVR.Foundation.Editor
{
    public static class FoundationSceneBuilder
    {
        [MenuItem("DEADeye VR/Build Foundation Test Scene")]
        public static void BuildScene()
        {
            Debug.Log("[FoundationSceneBuilder] Building FoundationTest.unity...");

            // Create new empty scene
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 1. Shaders and Materials
            Shader litShader = Shader.Find("Universal Render Pipeline/Lit");
            if (litShader == null) litShader = Shader.Find("Standard");

            Material floorMat = CreateMaterial("M_Floor_Slate", new Color(0.18f, 0.20f, 0.22f), litShader);
            Material wallMat = CreateMaterial("M_Wall_Concrete", new Color(0.28f, 0.30f, 0.33f), litShader);
            Material tableMat = CreateMaterial("M_Table_Wood", new Color(0.35f, 0.25f, 0.18f), litShader);
            Material redMat = CreateMaterial("M_Grabbable_Red", new Color(0.9f, 0.2f, 0.2f), litShader);
            Material blueMat = CreateMaterial("M_Grabbable_Blue", new Color(0.2f, 0.5f, 0.95f), litShader);
            Material weaponMat = CreateMaterial("M_Weapon_Tactical", new Color(0.12f, 0.13f, 0.14f), litShader);
            Material barrelMat = CreateMaterial("M_Weapon_Barrel", new Color(0.7f, 0.72f, 0.75f), litShader);
            Material targetWhiteMat = CreateMaterial("M_Target_White", Color.white, litShader);
            Material targetYellowMat = CreateMaterial("M_Target_Yellow", new Color(1.0f, 0.85f, 0.1f), litShader);

            // 2. Lighting & Sky
            GameObject lightObj = new GameObject("Directional Light");
            Light light = lightObj.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1.0f, 0.96f, 0.9f);
            light.intensity = 1.25f;
            light.shadows = LightShadows.Soft;
            lightObj.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            // 3. Environment: Floor and Walls
            GameObject envRoot = new GameObject("Environment");

            // Solid Floor (30x30m, top surface at Y = 0)
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Floor_Solid30x30";
            floor.transform.SetParent(envRoot.transform);
            floor.transform.position = new Vector3(0f, -0.5f, 0f);
            floor.transform.localScale = new Vector3(30f, 1f, 30f);
            floor.GetComponent<MeshRenderer>().sharedMaterial = floorMat;

            // 4 Perimeter Walls (6m high, 1m thick)
            CreateWall("Wall_North", new Vector3(0f, 3f, 15f), new Vector3(30f, 6f, 1f), wallMat, envRoot.transform);
            CreateWall("Wall_South", new Vector3(0f, 3f, -15f), new Vector3(30f, 6f, 1f), wallMat, envRoot.transform);
            CreateWall("Wall_East", new Vector3(15f, 3f, 0f), new Vector3(1f, 6f, 30f), wallMat, envRoot.transform);
            CreateWall("Wall_West", new Vector3(-15f, 3f, 0f), new Vector3(1f, 6f, 30f), wallMat, envRoot.transform);

            // 4. Equipment Table
            GameObject table = GameObject.CreatePrimitive(PrimitiveType.Cube);
            table.name = "Equipment_Table";
            table.transform.position = new Vector3(0f, 0.4f, 1.5f);
            table.transform.localScale = new Vector3(2.4f, 0.8f, 1.0f);
            table.GetComponent<MeshRenderer>().sharedMaterial = tableMat;

            // 5. Physical Grabbable Objects on Table
            CreateGrabbableObject("Grabbable_Cube_Red", PrimitiveType.Cube, new Vector3(-0.6f, 0.92f, 1.5f), new Vector3(0.2f, 0.2f, 0.2f), redMat);
            CreateGrabbableObject("Grabbable_Sphere_Blue", PrimitiveType.Sphere, new Vector3(0.6f, 0.92f, 1.5f), new Vector3(0.22f, 0.22f, 0.22f), blueMat);

            // 6. Weapon-Shaped Interactable Object
            GameObject weaponObj = CreateWeaponObject(new Vector3(0f, 0.92f, 1.5f), weaponMat, barrelMat);

            // 7. Interactive Target with Readout
            GameObject targetObj = CreateTargetObject(new Vector3(0f, 1.6f, 7.5f), redMat, targetWhiteMat, targetYellowMat);

            // 8. XR Origin (XR Rig)
            string xrOriginPath = "Assets/Samples/XR Interaction Toolkit/3.4.0/Starter Assets/Prefabs/XR Origin (XR Rig).prefab";
            GameObject xrOriginPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(xrOriginPath);
            GameObject xrOriginInstance = null;
            if (xrOriginPrefab != null)
            {
                xrOriginInstance = (GameObject)PrefabUtility.InstantiatePrefab(xrOriginPrefab);
                xrOriginInstance.name = "XR Origin (XR Rig)";
                xrOriginInstance.transform.position = Vector3.zero;
                xrOriginInstance.transform.rotation = Quaternion.identity;
                Debug.Log("[FoundationSceneBuilder] Instantiated official XR Origin (XR Rig).");
            }
            else
            {
                Debug.LogError($"[FoundationSceneBuilder] Failed to find XR Origin prefab at {xrOriginPath}");
            }

            // 9. XR Interaction Simulator
            string simulatorPath = "Assets/Samples/XR Interaction Toolkit/3.4.0/XR Interaction Simulator/XR Interaction Simulator.prefab";
            GameObject simPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(simulatorPath);
            if (simPrefab != null)
            {
                GameObject simInstance = (GameObject)PrefabUtility.InstantiatePrefab(simPrefab);
                simInstance.name = "XR Interaction Simulator";
                Debug.Log("[FoundationSceneBuilder] Instantiated official XR Interaction Simulator.");
            }
            else
            {
                Debug.LogWarning($"[FoundationSceneBuilder] XR Interaction Simulator prefab not found at {simulatorPath}");
            }

            // 10. Automated Verification Runner
            GameObject runnerObj = new GameObject("FoundationVerificationRunner");
            var runner = runnerObj.AddComponent<FoundationVerificationRunner>();

            // Link references
            SerializedObject so = new SerializedObject(runner);
            if (xrOriginInstance != null)
                so.FindProperty("xrOrigin").objectReferenceValue = xrOriginInstance.GetComponent<XROrigin>();
            so.FindProperty("weapon").objectReferenceValue = weaponObj.GetComponent<FoundationWeapon>();
            so.FindProperty("target").objectReferenceValue = targetObj.GetComponent<FoundationTarget>();
            so.FindProperty("floorCollider").objectReferenceValue = floor.GetComponent<Collider>();
            so.ApplyModifiedProperties();

            // 11. Save Scene
            string scenePath = "Assets/Scenes/FoundationTest.unity";
            EditorSceneManager.SaveScene(scene, scenePath);
            Debug.Log($"[FoundationSceneBuilder] Scene saved to {scenePath}");

            // 12. Set in EditorBuildSettings
            EditorBuildSettingsScene[] buildScenes = new EditorBuildSettingsScene[]
            {
                new EditorBuildSettingsScene(scenePath, true)
            };
            EditorBuildSettings.scenes = buildScenes;
            Debug.Log("[FoundationSceneBuilder] FoundationTest.unity registered as default build scene.");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static GameObject CreateWall(string name, Vector3 pos, Vector3 scale, Material mat, Transform parent)
        {
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = name;
            wall.transform.SetParent(parent);
            wall.transform.position = pos;
            wall.transform.localScale = scale;
            wall.GetComponent<MeshRenderer>().sharedMaterial = mat;
            return wall;
        }

        private static GameObject CreateGrabbableObject(string name, PrimitiveType type, Vector3 pos, Vector3 scale, Material mat)
        {
            GameObject obj = GameObject.CreatePrimitive(type);
            obj.name = name;
            obj.transform.position = pos;
            obj.transform.localScale = scale;
            obj.GetComponent<MeshRenderer>().sharedMaterial = mat;

            Rigidbody rb = obj.AddComponent<Rigidbody>();
            rb.mass = 0.5f;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

            XRGrabInteractable grab = obj.AddComponent<XRGrabInteractable>();
            grab.throwOnDetach = true;
            grab.smoothPosition = true;
            grab.smoothRotation = true;

            return obj;
        }

        private static GameObject CreateWeaponObject(Vector3 pos, Material bodyMat, Material barrelMat)
        {
            GameObject weaponRoot = new GameObject("FoundationWeapon_Interactable");
            weaponRoot.transform.position = pos;
            weaponRoot.transform.rotation = Quaternion.Euler(0f, 90f, 0f);

            // Receiver / Frame
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = "Weapon_Body";
            body.transform.SetParent(weaponRoot.transform, false);
            body.transform.localPosition = Vector3.zero;
            body.transform.localScale = new Vector3(0.08f, 0.12f, 0.45f);
            body.GetComponent<MeshRenderer>().sharedMaterial = bodyMat;
            Object.DestroyImmediate(body.GetComponent<Collider>());

            // Pistol Grip
            GameObject grip = GameObject.CreatePrimitive(PrimitiveType.Cube);
            grip.name = "Weapon_Grip";
            grip.transform.SetParent(weaponRoot.transform, false);
            grip.transform.localPosition = new Vector3(0f, -0.12f, -0.1f);
            grip.transform.localRotation = Quaternion.Euler(15f, 0f, 0f);
            grip.transform.localScale = new Vector3(0.06f, 0.16f, 0.08f);
            grip.GetComponent<MeshRenderer>().sharedMaterial = bodyMat;
            Object.DestroyImmediate(grip.GetComponent<Collider>());

            // Barrel
            GameObject barrel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            barrel.name = "Weapon_Barrel";
            barrel.transform.SetParent(weaponRoot.transform, false);
            barrel.transform.localPosition = new Vector3(0f, 0.02f, 0.32f);
            barrel.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            barrel.transform.localScale = new Vector3(0.04f, 0.18f, 0.04f);
            barrel.GetComponent<MeshRenderer>().sharedMaterial = barrelMat;
            Object.DestroyImmediate(barrel.GetComponent<Collider>());

            // Barrel Muzzle Point
            GameObject muzzle = new GameObject("BarrelPoint");
            muzzle.transform.SetParent(weaponRoot.transform, false);
            muzzle.transform.localPosition = new Vector3(0f, 0.02f, 0.52f);
            muzzle.transform.localRotation = Quaternion.identity;

            // Physics Collider on Root
            BoxCollider col = weaponRoot.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, -0.04f, 0.1f);
            col.size = new Vector3(0.12f, 0.32f, 0.65f);

            Rigidbody rb = weaponRoot.AddComponent<Rigidbody>();
            rb.mass = 1.2f;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

            // XRI Grab
            XRGrabInteractable grab = weaponRoot.AddComponent<XRGrabInteractable>();
            grab.throwOnDetach = true;
            grab.smoothPosition = true;
            grab.smoothRotation = true;

            // Weapon Logic
            FoundationWeapon weapon = weaponRoot.AddComponent<FoundationWeapon>();

            return weaponRoot;
        }

        private static GameObject CreateTargetObject(Vector3 pos, Material redMat, Material whiteMat, Material yellowMat)
        {
            GameObject targetRoot = new GameObject("FoundationTarget_Bullseye");
            targetRoot.transform.position = pos;
            targetRoot.transform.rotation = Quaternion.Euler(0f, 180f, 0f);

            // Stand Post
            GameObject post = GameObject.CreatePrimitive(PrimitiveType.Cube);
            post.name = "Stand_Post";
            post.transform.SetParent(targetRoot.transform, false);
            post.transform.localPosition = new Vector3(0f, -0.8f, 0f);
            post.transform.localScale = new Vector3(0.12f, 1.6f, 0.12f);
            post.GetComponent<MeshRenderer>().sharedMaterial = redMat;

            // Outer Disk (Red)
            GameObject outer = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            outer.name = "Target_Outer_Red";
            outer.transform.SetParent(targetRoot.transform, false);
            outer.transform.localPosition = Vector3.zero;
            outer.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            outer.transform.localScale = new Vector3(1.4f, 0.04f, 1.4f);
            outer.GetComponent<MeshRenderer>().sharedMaterial = redMat;

            // Middle Ring (White)
            GameObject middle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            middle.name = "Target_Middle_White";
            middle.transform.SetParent(targetRoot.transform, false);
            middle.transform.localPosition = new Vector3(0f, 0f, 0.005f);
            middle.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            middle.transform.localScale = new Vector3(0.9f, 0.042f, 0.9f);
            middle.GetComponent<MeshRenderer>().sharedMaterial = whiteMat;
            Object.DestroyImmediate(middle.GetComponent<Collider>());

            // Bullseye Center (Yellow)
            GameObject center = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            center.name = "Target_Center_Bullseye";
            center.transform.SetParent(targetRoot.transform, false);
            center.transform.localPosition = new Vector3(0f, 0f, 0.01f);
            center.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            center.transform.localScale = new Vector3(0.4f, 0.045f, 0.4f);
            center.GetComponent<MeshRenderer>().sharedMaterial = yellowMat;
            Object.DestroyImmediate(center.GetComponent<Collider>());

            // Readout 3D Text
            GameObject textObj = new GameObject("TargetReadout_Text");
            textObj.transform.SetParent(targetRoot.transform, false);
            textObj.transform.localPosition = new Vector3(0f, 1.1f, 0f);
            textObj.transform.localRotation = Quaternion.Euler(0f, 180f, 0f); // Facing player

            TextMesh textMesh = textObj.AddComponent<TextMesh>();
            textMesh.text = "TARGET READY\nCOUNT: 0\nSTATUS: ACTIVE";
            textMesh.fontSize = 24;
            textMesh.characterSize = 0.045f;
            textMesh.alignment = TextAlignment.Center;
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.color = Color.white;

            // FoundationTarget Component
            FoundationTarget target = outer.AddComponent<FoundationTarget>();
            SerializedObject so = new SerializedObject(target);
            so.FindProperty("readoutText").objectReferenceValue = textMesh;
            so.ApplyModifiedProperties();

            return targetRoot;
        }

        private static Material CreateMaterial(string name, Color color, Shader shader)
        {
            string path = $"Assets/Materials/Foundation/{name}.mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(shader);
                mat.color = color;
                AssetDatabase.CreateAsset(mat, path);
            }
            else
            {
                mat.shader = shader;
                mat.color = color;
            }
            return mat;
        }
    }
}

