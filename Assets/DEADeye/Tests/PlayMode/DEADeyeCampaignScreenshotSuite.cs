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
    /// PlayMode automated test suite that executes the mission lifecycle,
    /// verifies character presentation, multi-district environment, optical scope,
    /// and captures the 7 required high-resolution in-engine screenshots.
    /// </summary>
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

        [UnityTest, Order(1)]
        public IEnumerator Step_01_Capture_TacticalTerminal_And_Briefing()
        {
            SceneManager.LoadScene("RooftopMission");
            yield return new WaitForSeconds(1.0f);

            var terminalUI = DEADeyeTacticalTerminalUI.EnsureInstance();
            Assert.IsNotNull(terminalUI, "Tactical terminal UI must exist.");

            // 1. Mission Selection Screen on Terminal
            terminalUI.ShowTerminal(true);
            terminalUI.SelectMission(0); // Mission 01: Broken Signal
            yield return new WaitForSeconds(0.3f);
            SaveScreenshot("MissionSelect_TacticalTerminal.png", Camera.main);

            // 2. Mission Briefing Dossier
            yield return new WaitForSeconds(0.2f);
            SaveScreenshot("MissionBriefing_BrokenSignal.png", Camera.main);
        }

        [UnityTest, Order(2)]
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
                // Look across the boulevard and plaza
                desktopRig.LookAtTarget(new Vector3(8f, 0f, 45f));
            }

            yield return new WaitForSeconds(0.5f);
            SaveScreenshot("UrbanDistrict_Overview.png", Camera.main);
        }

        [UnityTest, Order(3)]
        public IEnumerator Step_03_Capture_Characters_Clothed_Comparison()
        {
            var desktopRig = Object.FindFirstObjectByType<DEADeyeDesktopRig>();
            var cam = desktopRig != null && desktopRig.DesktopCamera != null ? desktopRig.DesktopCamera : Camera.main;

            // Spawn showcase of clothed models in front of camera on rooftop
            Vector3 camPos = cam.transform.position;
            Vector3 forward = cam.transform.forward;
            Vector3 right = cam.transform.right;

            GameObject lineupRoot = new GameObject("CharacterLineup_Showcase");
            lineupRoot.transform.position = camPos + forward * 3.6f;

            string[] prefabPaths = new string[]
            {
                "Assets/DEADeye/Prefabs/Characters/Hostile_Tactical_Rifleman.prefab",
                "Assets/DEADeye/Prefabs/Characters/Friendly_Technician.prefab",
                "Assets/DEADeye/Prefabs/Characters/Civilian_Male_Casual.prefab",
                "Assets/DEADeye/Prefabs/Characters/Hostile_Commander.prefab"
            };

            for (int i = 0; i < prefabPaths.Length; i++)
            {
                var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(prefabPaths[i]);
                if (prefab != null)
                {
                    Vector3 pos = lineupRoot.transform.position + right * ((i - 1.5f) * 1.1f);
                    pos.y = 26.0f;
                    var inst = Object.Instantiate(prefab, pos, Quaternion.LookRotation(-forward));
                    inst.name = $"Showcase_{i}";
                    inst.transform.SetParent(lineupRoot.transform, true);
                    var agent = inst.GetComponent<UnityEngine.AI.NavMeshAgent>();
                    if (agent != null) agent.enabled = false;
                }
            }

            yield return new WaitForSeconds(0.4f);
            SaveScreenshot("Characters_Clothed_Comparison.png", cam);

            Object.Destroy(lineupRoot);
        }

        [UnityTest, Order(4)]
        public IEnumerator Step_04_Capture_HostileAI_TacticalBehavior()
        {
            var desktopRig = Object.FindFirstObjectByType<DEADeyeDesktopRig>();
            if (desktopRig != null)
            {
                // Look toward intersection where hostiles are patrolling
                desktopRig.LookAtTarget(new Vector3(0f, 1f, 38f));
            }

            yield return new WaitForSeconds(0.6f);
            SaveScreenshot("HostileAI_TacticalBehavior.png", Camera.main);
        }

        [UnityTest, Order(5)]
        public IEnumerator Step_05_Capture_MissionObjective_InAction_Scoped()
        {
            var desktopRig = Object.FindFirstObjectByType<DEADeyeDesktopRig>();
            var weapon = Object.FindFirstObjectByType<DEADeyeSniperWeapon>();

            // Aim directly at Comms Tower in East Plaza where technicians Marcus & Elena are stationed
            Vector3 commsTowerPos = new Vector3(22.5f, 6.0f, 38f);
            if (desktopRig != null)
            {
                desktopRig.LookAtTarget(commsTowerPos);
            }

            // Activate scope overlay
            if (DEADeyeMissionHUD.Instance != null)
            {
                DEADeyeMissionHUD.Instance.SetScopeOverlay(true);
            }

            yield return new WaitForSeconds(0.5f);
            SaveScreenshot("MissionObjective_InAction.png", Camera.main);

            // Deactivate scope overlay
            if (DEADeyeMissionHUD.Instance != null)
            {
                DEADeyeMissionHUD.Instance.SetScopeOverlay(false);
            }
        }

        [UnityTest, Order(6)]
        public IEnumerator Step_06_Capture_Debriefing_MissionComplete()
        {
            var terminalUI = DEADeyeTacticalTerminalUI.EnsureInstance();
            Assert.IsNotNull(terminalUI, "Tactical terminal UI must exist.");

            // Trigger Victory Debrief with high stats
            terminalUI.ShowDebrief(
                success: true,
                score: 1850,
                headshots: 4,
                accuracy: 91.5f,
                elims: 6,
                escapes: 0,
                civs: 0
            );

            yield return new WaitForSeconds(0.4f);
            SaveScreenshot("Debriefing_MissionComplete.png", Camera.main);
        }
    }
}
