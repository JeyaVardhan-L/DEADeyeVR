using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Unity.XR.CoreUtils;

namespace DEADeyeVR.Foundation.Tests
{
    public class FoundationPlayModeTests
    {
        [UnityTest]
        public IEnumerator PlayMode_FoundationTestScene_VerifiesFullInteractionAndScreenshots()
        {
            // Load FoundationTest scene
            if (Application.isPlaying)
            {
                AsyncOperation op = SceneManager.LoadSceneAsync("FoundationTest", LoadSceneMode.Single);
                while (!op.isDone)
                {
                    yield return null;
                }
            }
            else
            {
                UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/FoundationTest.unity", UnityEditor.SceneManagement.OpenSceneMode.Single);
                yield return null;
            }

            // Allow scene physics and XR Origin to initialize
            for (int i = 0; i < 10; i++)
            {
                yield return new WaitForFixedUpdate();
            }

            Debug.Log("[PlayModeTest] Scene FoundationTest loaded successfully.");

            // 1. Verify XR Origin Rig
            XROrigin xrOrigin = Object.FindFirstObjectByType<XROrigin>();
            Assert.IsNotNull(xrOrigin, "XROrigin must exist in FoundationTest scene.");
            Assert.IsNotNull(xrOrigin.Camera, "Camera must be assigned to XROrigin.");
            Debug.Log($"[PlayModeTest] XROrigin verified at {xrOrigin.transform.position}, Camera at {xrOrigin.Camera.transform.position}");

            // 2. Verify Grounding on Floor Collider
            Ray groundRay = new Ray(xrOrigin.Camera.transform.position, Vector3.down);
            bool groundHit = Physics.Raycast(groundRay, out RaycastHit hitInfo, 5f);
            Assert.IsTrue(groundHit, "Player camera must be vertically positioned over a solid floor collider.");
            Assert.AreEqual(0.0f, hitInfo.point.y, 0.05f, "Floor top surface must be at Y = 0.0m.");
            Debug.Log($"[PlayModeTest] Player grounded on '{hitInfo.collider.name}' at Y={hitInfo.point.y:F2}m");

            // 3. Verify Weapon and Target
            FoundationWeapon weapon = Object.FindFirstObjectByType<FoundationWeapon>();
            FoundationTarget target = Object.FindFirstObjectByType<FoundationTarget>();
            Assert.IsNotNull(weapon, "FoundationWeapon must exist in scene.");
            Assert.IsNotNull(target, "FoundationTarget must exist in scene.");

            // 4. Test Weapon Aim and Fire in Live Scene
            weapon.transform.LookAt(target.transform.position);
            Physics.SyncTransforms();

            int hitsBefore = target.HitCount;
            bool shotFired = weapon.Fire();

            yield return new WaitForSeconds(0.2f);

            Assert.IsTrue(shotFired, "Weapon firing must return true on target impact.");
            Assert.Greater(target.HitCount, hitsBefore, "Target hit count must increment in live play mode.");
            Debug.Log($"[PlayModeTest] Weapon hit confirmed! Target hits: {target.HitCount}");

            // 5. Capture Live Screenshot
            string screenshotsDir = Path.Combine(Application.dataPath, "..", "Screenshots");
            if (!Directory.Exists(screenshotsDir))
                Directory.CreateDirectory(screenshotsDir);

            string screenshotPath = Path.Combine(screenshotsDir, "FoundationTest_PlayMode.png");
            Camera cam = (xrOrigin != null && xrOrigin.Camera != null) ? xrOrigin.Camera : Camera.main;
            if (cam != null)
            {
                RenderTexture rt = new RenderTexture(1920, 1080, 24);
                cam.targetTexture = rt;
                cam.Render();

                RenderTexture.active = rt;
                Texture2D tex = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0);
                tex.Apply();

                cam.targetTexture = null;
                RenderTexture.active = null;
                Object.DestroyImmediate(rt);

                byte[] bytes = tex.EncodeToPNG();
                Object.DestroyImmediate(tex);

                File.WriteAllBytes(screenshotPath, bytes);
                Debug.Log($"[PlayModeTest] Screenshot encoded to: {screenshotPath} ({bytes.Length} bytes)");
            }
            else
            {
                ScreenCapture.CaptureScreenshot(screenshotPath);
                Debug.Log($"[PlayModeTest] ScreenCapture triggered to: {screenshotPath}");
            }

            // 6. Write Verification Report JSON
            string reportPath = Path.Combine(screenshotsDir, "foundation_verification_report.json");
            string json = $@"{{
  ""test_suite"": ""FoundationPlayModeTests"",
  ""timestamp"": ""{System.DateTime.UtcNow:o}"",
  ""unity_version"": ""{Application.unityVersion}"",
  ""platform"": ""{Application.platform}"",
  ""scene_name"": ""FoundationTest"",
  ""xr_origin_detected"": true,
  ""grounding_verified"": true,
  ""ground_hit_y"": {hitInfo.point.y},
  ""weapon_detected"": true,
  ""target_detected"": true,
  ""shots_fired"": {weapon.TotalShotsFired},
  ""target_hits_registered"": {target.HitCount},
  ""screenshot_path"": ""Screenshots/FoundationTest_PlayMode.png"",
  ""playmode_verification_status"": ""SUCCESS""
}}";
            File.WriteAllText(reportPath, json);
            Debug.Log($"[PlayModeTest] Report written to: {reportPath}");

            yield return new WaitForSeconds(0.5f);
        }
    }
}

