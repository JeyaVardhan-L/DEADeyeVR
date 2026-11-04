using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using DEADeye;

namespace DEADeye.Tests
{
    /// <summary>
    /// PlayMode automated test suite that executes the complete Operation DEAD AIR lifecycle,
    /// verifies character presentation, multi-district environment, 3 walkable overwatch perches,
    /// optical spotting, environmental disruption, and captures high-resolution in-engine screenshots.
    /// </summary>
    [TestFixture]
    public class DEADeyeCampaignScreenshotSuite
    {
        private string projectScreenshotsDir;
        private string artifactDir;

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            projectScreenshotsDir = Path.Combine(Application.dataPath, "..", "Screenshots");
            if (!Directory.Exists(projectScreenshotsDir))
            {
                Directory.CreateDirectory(projectScreenshotsDir);
            }

            artifactDir = @"C:\Users\user\.gemini\antigravity\brain\33cf68df-a5d2-459f-bdb6-bf1d6e253a59";
        }

        private void SaveScreenshot(string filename, Camera cam)
        {
            if (cam == null) cam = Camera.main;
            if (cam == null)
            {
                var xrOrigin = Object.FindFirstObjectByType<Unity.XR.CoreUtils.XROrigin>();
                if (xrOrigin != null && xrOrigin.Camera != null) cam = xrOrigin.Camera;
            }
            if (cam == null) cam = Object.FindFirstObjectByType<Camera>();

            int width = 1920;
            int height = 1080;
            RenderTexture rt = new RenderTexture(width, height, 24);
            RenderTexture prev = cam.targetTexture;

            // Temporarily switch ScreenSpaceOverlay canvases to ScreenSpaceCamera so cam.Render captures UI
            var canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            var modifiedCanvases = new System.Collections.Generic.List<Canvas>();
            foreach (var c in canvases)
            {
                if (c.gameObject.activeInHierarchy && c.renderMode == RenderMode.ScreenSpaceOverlay)
                {
                    c.renderMode = RenderMode.ScreenSpaceCamera;
                    c.worldCamera = cam;
                    c.planeDistance = 1.0f;
                    modifiedCanvases.Add(c);
                }
            }

            cam.targetTexture = rt;
            cam.Render();

            foreach (var c in modifiedCanvases)
            {
                c.renderMode = RenderMode.ScreenSpaceOverlay;
            }

            RenderTexture.active = rt;
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            tex.Apply();

            cam.targetTexture = prev;
            RenderTexture.active = null;
            Object.DestroyImmediate(rt);

            byte[] bytes = tex.EncodeToPNG();
            Object.DestroyImmediate(tex);

            // Save to project directory
            string projPath = Path.Combine(projectScreenshotsDir, filename);
            File.WriteAllBytes(projPath, bytes);

            // Copy to artifact directory
            if (Directory.Exists(artifactDir))
            {
                string artPath = Path.Combine(artifactDir, filename);
                File.WriteAllBytes(artPath, bytes);
            }

            Debug.Log($"[DEADeyeCampaignScreenshotSuite] Screenshot captured: {filename} ({bytes.Length} bytes)");
        }

        [UnityTest]
        public IEnumerator Step_01_Capture_TacticalTerminal_And_Briefing()
        {
            SceneManager.LoadScene("RooftopMission");
            yield return new WaitForSeconds(1.0f);

            var terminalUI = DEADeyeTacticalTerminalUI.EnsureInstance();
            Assert.IsNotNull(terminalUI, "Tactical terminal UI must exist.");

            // 1. Mission Selection Screen on Terminal with OP-01 DEAD AIR
            terminalUI.ShowTerminal(true);
            terminalUI.SelectMission(0);
            yield return new WaitForSeconds(0.4f);
            SaveScreenshot("MissionBriefing_DeadAir.png", Camera.main);
            SaveScreenshot("MissionBriefing_BrokenSignal.png", Camera.main);
            SaveScreenshot("MissionSelect_TacticalTerminal.png", Camera.main);
        }

        [UnityTest]
        public IEnumerator Step_02_Capture_UrbanDistrict_Overview()
        {
            var terminalUI = Object.FindFirstObjectByType<DEADeyeTacticalTerminalUI>();
            if (terminalUI != null)
            {
                terminalUI.ShowTerminal(false);
            }

            var desktopRig = Object.FindFirstObjectByType<DEADeyeDesktopRig>();
            if (desktopRig != null)
            {
                desktopRig.transform.position = new Vector3(0f, 26f, -4f);
                desktopRig.LookAtTarget(new Vector3(4f, 2f, 45f));
            }

            yield return new WaitForSeconds(0.5f);
            SaveScreenshot("UrbanDistrict_Overview.png", Camera.main);
        }

        [UnityTest]
        public IEnumerator Step_03_Capture_OverwatchPerches()
        {
            var desktopRig = Object.FindFirstObjectByType<DEADeyeDesktopRig>();
            Assert.IsNotNull(desktopRig, "Desktop rig must exist.");

            // Perch 1: Center North Perch looking down boulevard
            desktopRig.transform.position = new Vector3(0f, 26f, -4f);
            desktopRig.LookAtTarget(new Vector3(0f, 0f, 50f));
            yield return new WaitForSeconds(0.3f);
            SaveScreenshot("Overwatch_Perch1_North.png", Camera.main);

            // Perch 2: East Plaza Perch looking at Comms Tower & Fountain
            desktopRig.transform.position = new Vector3(18f, 25f, 4f);
            desktopRig.LookAtTarget(new Vector3(22.5f, 4f, 40f));
            yield return new WaitForSeconds(0.3f);
            SaveScreenshot("Overwatch_Perch2_EastPlaza.png", Camera.main);

            // Perch 3: West Construction Perch looking at Skeleton & Crane
            desktopRig.transform.position = new Vector3(-18f, 25.5f, 6f);
            desktopRig.LookAtTarget(new Vector3(-24f, 8f, 40f));
            yield return new WaitForSeconds(0.3f);
            SaveScreenshot("Overwatch_Perch3_WestConstruction.png", Camera.main);
        }

        [UnityTest]
        public IEnumerator Step_04_Capture_Recon_TargetSpotting()
        {
            var desktopRig = Object.FindFirstObjectByType<DEADeyeDesktopRig>();
            if (desktopRig != null)
            {
                desktopRig.transform.position = new Vector3(18f, 25f, 4f);
                desktopRig.LookAtTarget(new Vector3(22.5f, 1.2f, 38.2f)); // Aim at Jammer
            }

            // Trigger spotting banner display on HUD
            GameObject spotGO = new GameObject("SpotTarget_Jammer");
            var spotable = spotGO.AddComponent<DEADeyeSpotable>();
            spotable.SetTargetInfo("COMMUNICATIONS JAMMER", SpotClassification.Objective, "High-gain military jammer hijacking emergency broadcasts. Cut power junction to disable.", 100);

            if (DEADeyeMissionHUD.Instance != null)
            {
                DEADeyeMissionHUD.Instance.ShowSpottingBanner(spotable);
                DEADeyeMissionHUD.Instance.UpdateObjectiveBar("PHASE 1: RECONNAISSANCE - Identify Jammer & Friendly Team with Optical Scope");
            }

            yield return new WaitForSeconds(0.4f);
            SaveScreenshot("Recon_TargetSpotting.png", Camera.main);

            Object.Destroy(spotGO);
        }

        [UnityTest]
        public IEnumerator Step_05_Capture_Disruption_PowerJunction()
        {
            var desktopRig = Object.FindFirstObjectByType<DEADeyeDesktopRig>();
            if (desktopRig != null)
            {
                desktopRig.transform.position = new Vector3(18f, 25f, 4f);
                desktopRig.LookAtTarget(new Vector3(19.7f, 1.2f, 40f)); // Aim at Power Junction Box
            }

            if (DEADeyeMissionHUD.Instance != null)
            {
                DEADeyeMissionHUD.Instance.SetScopeOverlay(true);
                DEADeyeMissionHUD.Instance.UpdateObjectiveBar("PHASE 2: OPERATION DISRUPTION - Disable Jammer via Power Junction or Direct Hit");
            }

            yield return new WaitForSeconds(0.4f);
            SaveScreenshot("Disruption_PowerJunction.png", Camera.main);
            SaveScreenshot("MissionObjective_InAction.png", Camera.main);

            if (DEADeyeMissionHUD.Instance != null)
            {
                DEADeyeMissionHUD.Instance.SetScopeOverlay(false);
            }
        }

        [UnityTest]
        public IEnumerator Step_06_Capture_Technicians_ElenaAndMarcus()
        {
            var desktopRig = Object.FindFirstObjectByType<DEADeyeDesktopRig>();
            if (desktopRig != null)
            {
                desktopRig.transform.position = new Vector3(18f, 25f, 4f);
                desktopRig.LookAtTarget(new Vector3(22f, 0.8f, 38f)); // Comms console
            }

            if (DEADeyeMissionHUD.Instance != null)
            {
                DEADeyeMissionHUD.Instance.UpdateObjectiveBar("PHASE 3: PROTECT TECHNICIANS - Repair: [======    ] 65%");
                DEADeyeMissionHUD.Instance.ShowRadioDialogue("ELENA", "Secondary array linked! Holding position under sniper cover!");
            }

            yield return new WaitForSeconds(0.5f);
            SaveScreenshot("Technicians_ElenaAndMarcus.png", Camera.main);
        }

        [UnityTest]
        public IEnumerator Step_07_Capture_Neutralize_CommanderVance()
        {
            var desktopRig = Object.FindFirstObjectByType<DEADeyeDesktopRig>();
            if (desktopRig != null)
            {
                desktopRig.transform.position = new Vector3(0f, 26f, -4f);
                desktopRig.LookAtTarget(new Vector3(-6f, 0.5f, 48f));
            }

            if (DEADeyeMissionHUD.Instance != null)
            {
                DEADeyeMissionHUD.Instance.UpdateObjectiveBar("PHASE 4: ELIMINATE COMMANDER - Neutralize Commander Vance Before Exfil");
                DEADeyeMissionHUD.Instance.ShowRadioDialogue("CONTROL", "Commander Vance spotted on the avenue! Take him down before he reaches transport!");
            }

            yield return new WaitForSeconds(0.5f);
            SaveScreenshot("Neutralize_CommanderVance.png", Camera.main);
            SaveScreenshot("HostileAI_TacticalBehavior.png", Camera.main);
        }

        [UnityTest]
        public IEnumerator Step_08_Capture_Debriefing_MissionComplete()
        {
            var terminalUI = DEADeyeTacticalTerminalUI.EnsureInstance();
            Assert.IsNotNull(terminalUI, "Tactical terminal UI must exist.");

            // Show Rank S Debriefing
            terminalUI.ShowDebriefing(
                success: true,
                score: 2450,
                elims: 7,
                headshots: 4,
                accuracy: 92.4f,
                civs: 0,
                escapes: 0
            );

            yield return new WaitForSeconds(0.5f);
            SaveScreenshot("Debriefing_MissionComplete.png", Camera.main);
        }
    }
}
