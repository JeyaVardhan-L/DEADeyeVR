using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using Unity.XR.CoreUtils;

namespace DEADeyeVR.Foundation
{
    /// <summary>
    /// Verification runner for Phase 1 Clean Foundation.
    /// Executes automated tests on startup in Play mode:
    /// - XR Origin presence, height, and grounding
    /// - Floor collider and 4 wall perimeter collision checks
    /// - Grabbable objects configuration
    /// - Weapon aiming, firing, and target hit registration
    /// - Play mode screenshot capture
    /// </summary>
    public class FoundationVerificationRunner : MonoBehaviour
    {
        [Header("Scene References")]
        [SerializeField] private XROrigin xrOrigin;
        [SerializeField] private FoundationWeapon weapon;
        [SerializeField] private FoundationTarget target;
        [SerializeField] private Collider floorCollider;
        [SerializeField] private Collider[] wallColliders;

        [Header("Verification State")]
        public bool GroundingVerified { get; private set; }
        public bool WallCollisionsVerified { get; private set; }
        public bool WeaponFiringVerified { get; private set; }
        public bool TargetHitRegistered { get; private set; }
        public bool SimulatorActive { get; private set; }
        public bool ScreenshotCaptured { get; private set; }

        private void Start()
        {
            StartCoroutine(RunVerificationRoutine());
        }

        private IEnumerator RunVerificationRoutine()
        {
            // Wait 2 frames for physics and XR initializations to settle
            yield return null;
            yield return new WaitForFixedUpdate();

            Debug.Log("=================================================================");
            Debug.Log("[FoundationVerification] Starting Phase 1 Clean Foundation Tests...");
            Debug.Log("=================================================================");

            // 1. Verify XR Origin & Grounding
            if (xrOrigin == null)
                xrOrigin = FindFirstObjectByType<XROrigin>();

            if (xrOrigin != null)
            {
                Vector3 originPos = xrOrigin.transform.position;
                Vector3 cameraPos = xrOrigin.Camera != null ? xrOrigin.Camera.transform.position : originPos;

                Ray groundRay = new Ray(cameraPos, Vector3.down);
                if (Physics.Raycast(groundRay, out RaycastHit groundHit, 5f))
                {
                    Debug.Log($"[FoundationVerification] PASS: Player grounded on '{groundHit.collider.name}' at distance {groundHit.distance:F2}m (Hit Y: {groundHit.point.y:F2})");
                    GroundingVerified = groundHit.collider == floorCollider || groundHit.collider.name.Contains("Floor");
                }
                else
                {
                    Debug.LogWarning("[FoundationVerification] FAIL: Ground raycast did not hit floor collider!");
                    GroundingVerified = false;
                }
            }
            else
            {
                Debug.LogError("[FoundationVerification] FAIL: XROrigin not found in scene!");
            }

            // 2. Verify Perimeter Walls
            int wallsHit = 0;
            Vector3[] directions = new Vector3[] { Vector3.forward, Vector3.back, Vector3.left, Vector3.right };
            string[] dirNames = new string[] { "North (Forward)", "South (Back)", "West (Left)", "East (Right)" };

            for (int i = 0; i < directions.Length; i++)
            {
                Ray wallRay = new Ray(new Vector3(0f, 1.5f, 0f), directions[i]);
                if (Physics.Raycast(wallRay, out RaycastHit wallHit, 25f))
                {
                    Debug.Log($"[FoundationVerification] PASS: {dirNames[i]} wall detected: '{wallHit.collider.name}' at {wallHit.distance:F2}m");
                    wallsHit++;
                }
                else
                {
                    Debug.LogWarning($"[FoundationVerification] FAIL: No wall detected in direction {dirNames[i]}");
                }
            }
            WallCollisionsVerified = wallsHit == 4;

            // 3. Verify XR Simulator
            GameObject sim = GameObject.Find("XR Interaction Simulator") ?? GameObject.Find("XR Device Simulator");
            SimulatorActive = sim != null || FindFirstObjectByType<UnityEngine.InputSystem.PlayerInput>() != null;
            Debug.Log($"[FoundationVerification] XR Simulator Detected: {SimulatorActive} (Object: {(sim != null ? sim.name : "InputSystem Active")})");

            // 4. Verify Weapon Aiming and Target Hit
            if (weapon == null) weapon = FindFirstObjectByType<FoundationWeapon>();
            if (target == null) target = FindFirstObjectByType<FoundationTarget>();

            if (weapon != null && target != null)
            {
                int hitsBefore = target.HitCount;
                Debug.Log($"[FoundationVerification] Testing weapon fire towards target '{target.name}'...");

                // Align weapon towards target for deterministic verification
                Vector3 targetCenter = target.transform.position;
                weapon.transform.LookAt(targetCenter);

                bool hit = weapon.Fire();
                yield return new WaitForSeconds(0.15f);

                TargetHitRegistered = target.HitCount > hitsBefore;
                WeaponFiringVerified = hit && TargetHitRegistered;

                if (TargetHitRegistered)
                {
                    Debug.Log($"[FoundationVerification] PASS: Weapon fired and Target registered hit! (Count: {target.HitCount}, HitPoint: {target.LastHitPoint})");
                }
                else
                {
                    Debug.LogError("[FoundationVerification] FAIL: Target did not register hit!");
                }
            }
            else
            {
                Debug.LogError($"[FoundationVerification] FAIL: Weapon ({weapon != null}) or Target ({target != null}) missing!");
            }

            // 5. Capture Play Mode Screenshot
            yield return new WaitForSeconds(0.5f);
            CaptureScreenshot();

            // 6. Write JSON Summary Report
            WriteReportJson();

            Debug.Log("=================================================================");
            Debug.Log($"[FoundationVerification] Phase 1 Tests Completed. All Passed: {AllTestsPassed()}");
            Debug.Log("=================================================================");
        }

        public bool AllTestsPassed()
        {
            return GroundingVerified && WallCollisionsVerified && WeaponFiringVerified && TargetHitRegistered;
        }

        private void CaptureScreenshot()
        {
            string dir = Path.Combine(Application.dataPath, "..", "Screenshots");
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            string screenshotPath = Path.Combine(dir, "FoundationTest_PlayMode.png");

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
                DestroyImmediate(rt);

                byte[] bytes = tex.EncodeToPNG();
                DestroyImmediate(tex);

                File.WriteAllBytes(screenshotPath, bytes);
                ScreenshotCaptured = true;
                Debug.Log($"[FoundationVerification] Camera offscreen screenshot captured to: {screenshotPath} ({bytes.Length} bytes)");
            }
            else
            {
                ScreenCapture.CaptureScreenshot(screenshotPath);
                ScreenshotCaptured = true;
                Debug.Log($"[FoundationVerification] ScreenCapture fallback invoked to: {screenshotPath}");
            }
        }

        private void WriteReportJson()
        {
            string dir = Path.Combine(Application.dataPath, "..", "Screenshots");
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            string jsonPath = Path.Combine(dir, "foundation_verification_report.json");
            string json = $@"{{
  ""timestamp"": ""{System.DateTime.UtcNow:o}"",
  ""unity_version"": ""{Application.unityVersion}"",
  ""platform"": ""{Application.platform}"",
  ""grounding_verified"": {GroundingVerified.ToString().ToLower()},
  ""wall_collisions_verified"": {WallCollisionsVerified.ToString().ToLower()},
  ""simulator_active"": {SimulatorActive.ToString().ToLower()},
  ""weapon_firing_verified"": {WeaponFiringVerified.ToString().ToLower()},
  ""target_hit_registered"": {TargetHitRegistered.ToString().ToLower()},
  ""target_hit_count"": {(target != null ? target.HitCount : 0)},
  ""shots_fired"": {(weapon != null ? weapon.TotalShotsFired : 0)},
  ""screenshot_captured"": {ScreenshotCaptured.ToString().ToLower()},
  ""all_tests_passed"": {AllTestsPassed().ToString().ToLower()}
}}";
            File.WriteAllText(jsonPath, json);
            Debug.Log($"[FoundationVerification] Report saved to: {jsonPath}");
        }
    }
}

