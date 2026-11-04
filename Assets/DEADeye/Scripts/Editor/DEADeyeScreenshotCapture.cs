#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DEADeye.Editor
{
    public static class DEADeyeScreenshotCapture
    {
        private const string ARTIFACT_DIR = @"C:\Users\user\.gemini\antigravity\brain\33cf68df-a5d2-459f-bdb6-bf1d6e253a59";

        [MenuItem("DEADeye/QA/Capture All 10 High Quality Screenshots")]
        public static void CaptureAllScreenshots()
        {
            Debug.Log("[DEADeyeScreenshotCapture] Starting 10-shot capture sequence...");

            // 1. Ensure authoritative scene is loaded and up to date
            string scenePath = "Assets/DEADeye/Scenes/RooftopMission.unity";
            var scene = EditorSceneManager.OpenScene(scenePath);

            // Rebuild campaign environment to guarantee all updated geometry and materials are present
            DEADeyeMasterCampaignIntegrator.BuildCompleteCampaignAndScene();

            string projectScreenshotsDir = Path.Combine(Application.dataPath, "..", "Screenshots");
            if (!Directory.Exists(projectScreenshotsDir)) Directory.CreateDirectory(projectScreenshotsDir);
            if (!Directory.Exists(ARTIFACT_DIR)) Directory.CreateDirectory(ARTIFACT_DIR);

            // Setup Dedicated High-Resolution Render Camera
            GameObject camGO = new GameObject("QARenderCamera");
            Camera cam = camGO.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.Skybox;
            cam.nearClipPlane = 0.04f;
            cam.farClipPlane = 1000f;

            var desktopRig = Object.FindFirstObjectByType<DEADeyeDesktopRig>();
            var weapon = Object.FindFirstObjectByType<DEADeyeSniperWeapon>();

            try
            {
                // Shot 1: Urban District skyline and streetscape
                RenderShot(cam, new Vector3(0f, 27.5f, -2.5f), Quaternion.Euler(8f, 0f, 0f), 60f, "01_UrbanDistrict_Skyline.png", projectScreenshotsDir);

                // Shot 2: Boulevard street level with trees, planters, and traffic lights
                RenderShot(cam, new Vector3(-6.0f, 2.0f, 15f), Quaternion.Euler(4f, 18f, 0f), 60f, "02_Boulevard_TreesAndLights.png", projectScreenshotsDir);

                // Shot 3: Upgraded vehicles (wheels, bumpers, lights)
                Vector3 carPos = new Vector3(-6.2f, 0.75f, 22f);
                Vector3 carCamPos = new Vector3(-3.2f, 1.4f, 19.2f);
                RenderShot(cam, carCamPos, Quaternion.LookRotation(carPos - carCamPos), 45f, "03_Vehicles_Upgraded_Details.png", projectScreenshotsDir);

                // Shot 4: Composite building facades with window sills and illuminated panes
                Vector3 facadeTarget = new Vector3(-24f, 12f, 18f);
                Vector3 facadeCamPos = new Vector3(-8f, 5f, 12f);
                RenderShot(cam, facadeCamPos, Quaternion.LookRotation(facadeTarget - facadeCamPos), 55f, "04_BuildingFacades_WindowsAndSills.png", projectScreenshotsDir);

                // Shot 5: Rooftop equipment (HVAC chiller, stairwell penthouse, water tower, antenna mast)
                Vector3 roofTarget = new Vector3(-22f, 23.5f, 18f);
                Vector3 roofCamPos = new Vector3(-12f, 26.5f, 10f);
                RenderShot(cam, roofCamPos, Quaternion.LookRotation(roofTarget - roofCamPos), 50f, "05_RooftopEquipment_HVAC_WaterTower.png", projectScreenshotsDir);

                // Shot 6: First-person sniper rifle (clean matte gunmetal finish, zero sparkles)
                if (desktopRig != null && weapon != null)
                {
                    desktopRig.transform.position = new Vector3(0f, 26f, -3f);
                    desktopRig.AttachWeapon(weapon.transform);
                    desktopRig.SetLookRotation(14f, 0f);
                    if (weapon.ScopeOptic != null)
                    {
                        weapon.ScopeOptic.SetScopeActive(false);
                    }

                    cam.transform.position = new Vector3(0f, 27.6f, -3f);
                    cam.transform.rotation = Quaternion.Euler(14f, 0f, 0f);
                    RenderShot(cam, cam.transform.position, cam.transform.rotation, 60f, "06_SniperRifle_MatteFinish_NoSparkles.png", projectScreenshotsDir);
                }
                else
                {
                    RenderShot(cam, new Vector3(0f, 27.6f, -3f), Quaternion.Euler(14f, 0f, 0f), 60f, "06_SniperRifle_MatteFinish_NoSparkles.png", projectScreenshotsDir);
                }

                // Shot 7: Optical scope zoomed view
                if (weapon != null && weapon.ScopeOptic != null && weapon.ScopeOptic.ScopeCamera != null)
                {
                    weapon.ScopeOptic.SetScopeActive(true);
                    Camera scopeCam = weapon.ScopeOptic.ScopeCamera;
                    scopeCam.transform.position = new Vector3(0f, 27.6f, -3f);
                    scopeCam.transform.rotation = Quaternion.Euler(14f, 0f, 0f);
                    RenderShot(scopeCam, scopeCam.transform.position, scopeCam.transform.rotation, 14f, "07_OpticalScope_ZoomedView.png", projectScreenshotsDir);
                    weapon.ScopeOptic.SetScopeActive(false);
                }
                else
                {
                    RenderShot(cam, new Vector3(0f, 27.6f, -3f), Quaternion.Euler(14f, 0f, 0f), 15f, "07_OpticalScope_ZoomedView.png", projectScreenshotsDir);
                }

                // Shot 8: Military Comms Jammer array and physical power conduits leading to Power Junction
                Vector3 jammerTarget = new Vector3(21.5f, 2.0f, 38.5f);
                Vector3 jammerCamPos = new Vector3(17.2f, 3.2f, 33.0f);
                RenderShot(cam, jammerCamPos, Quaternion.LookRotation(jammerTarget - jammerCamPos), 52f, "08_MilitaryJammer_ArrayAndConduits.png", projectScreenshotsDir);

                // Shot 9: Walkable East bridge and catwalks
                Vector3 eastBridgeTarget = new Vector3(12.0f, 25.85f, 0.5f);
                Vector3 eastBridgeCamPos = new Vector3(4.8f, 26.5f, -6.8f);
                RenderShot(cam, eastBridgeCamPos, Quaternion.LookRotation(eastBridgeTarget - eastBridgeCamPos), 55f, "09_WalkableBridge_EastCatwalk.png", projectScreenshotsDir);

                // Shot 10: Walkable West bridge and edge parapets
                Vector3 westBridgeTarget = new Vector3(-12.0f, 25.85f, 2.0f);
                Vector3 westBridgeCamPos = new Vector3(-4.8f, 26.5f, -6.8f);
                RenderShot(cam, westBridgeCamPos, Quaternion.LookRotation(westBridgeTarget - westBridgeCamPos), 55f, "10_WalkableBridge_WestSkybridge.png", projectScreenshotsDir);

                Debug.Log("[DEADeyeScreenshotCapture] Successfully rendered and saved all 10 high-quality screenshots!");
            }
            finally
            {
                Object.DestroyImmediate(camGO);
            }
        }

        private static void RenderShot(Camera cam, Vector3 pos, Quaternion rot, float fov, string filename, string projectDir)
        {
            cam.transform.position = pos;
            cam.transform.rotation = rot;
            cam.fieldOfView = fov;

            int width = 1920;
            int height = 1080;
            RenderTexture rt = new RenderTexture(width, height, 24);
            RenderTexture prev = cam.targetTexture;

            cam.targetTexture = rt;
            cam.Render();

            RenderTexture.active = rt;
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            tex.Apply();

            cam.targetTexture = prev;
            RenderTexture.active = null;
            Object.DestroyImmediate(rt);

            byte[] bytes = tex.EncodeToPNG();
            Object.DestroyImmediate(tex);

            // Write to project screenshots directory
            string projPath = Path.Combine(projectDir, filename);
            File.WriteAllBytes(projPath, bytes);

            // Write to conversation artifact directory
            string artifactPath = Path.Combine(ARTIFACT_DIR, filename);
            File.WriteAllBytes(artifactPath, bytes);

            Debug.Log($"[DEADeyeScreenshotCapture] Saved '{filename}' ({bytes.Length} bytes) to {projPath} and {artifactPath}");
        }
    }
}
#endif

