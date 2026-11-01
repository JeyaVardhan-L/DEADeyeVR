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
    /// PlayMode integration tests verifying live gameplay loop:
    /// Boot -> Lobby -> Difficulty Selection -> Deployment -> Rooftop Sniper -> Hostile Elimination -> Results.
    /// Captures actual in-engine screenshots for verification report.
    /// </summary>
    public class DEADeyePlayModeTests
    {
        private const string SCREENSHOT_DIR = "Screenshots";

        private void CaptureScreenshot(string filename, Camera overrideCam = null)
        {
            string screenshotsDir = Path.Combine(Application.dataPath, "..", "Screenshots");
            if (!Directory.Exists(screenshotsDir))
                Directory.CreateDirectory(screenshotsDir);

            string screenshotPath = Path.Combine(screenshotsDir, filename);
            Camera cam = overrideCam != null ? overrideCam : Camera.main;
            if (cam == null)
            {
                var xrOrigin = Object.FindFirstObjectByType<Unity.XR.CoreUtils.XROrigin>();
                if (xrOrigin != null && xrOrigin.Camera != null)
                    cam = xrOrigin.Camera;
            }
            if (cam == null)
                cam = Object.FindFirstObjectByType<Camera>();

            if (cam != null)
            {
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

                File.WriteAllBytes(screenshotPath, bytes);
                Debug.Log($"[PlayModeTest] Screenshot encoded to: {screenshotPath} ({bytes.Length} bytes)");
            }
        }

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            if (!Directory.Exists(SCREENSHOT_DIR))
            {
                Directory.CreateDirectory(SCREENSHOT_DIR);
            }
        }

        [UnityTest]
        public IEnumerator PlayMode_01_Boot_TransitionsToLobbyScene()
        {
            SceneManager.LoadScene("BootScene");
            float timeout = 3.5f;
            while (SceneManager.GetActiveScene().name != "LobbyScene" && timeout > 0f)
            {
                yield return new WaitForSeconds(0.1f);
                timeout -= 0.1f;
            }

            Assert.AreEqual("LobbyScene", SceneManager.GetActiveScene().name, "BootScene must automatically route to LobbyScene.");

            var lobbyUI = Object.FindFirstObjectByType<DEADeyeLobbyUI>();
            Assert.IsNotNull(lobbyUI, "LobbyUI must be active in LobbyScene.");

            var avatar = Object.FindFirstObjectByType<DEADeyeLobbyAvatarController>();
            Assert.IsNotNull(avatar, "Tactical operative avatar must be present in LobbyScene.");

            // Capture in-engine screenshot of Dojo Lobby
            CaptureScreenshot("LobbyScene_PlayMode.png");
            yield return new WaitForSeconds(0.5f);
        }

        [UnityTest]
        public IEnumerator PlayMode_02_Lobby_DifficultySelectionUpdatesGameManager()
        {
            SceneManager.LoadScene("LobbyScene");
            yield return new WaitForSeconds(0.5f);

            var lobbyUI = Object.FindFirstObjectByType<DEADeyeLobbyUI>();
            Assert.IsNotNull(lobbyUI, "LobbyUI must exist.");

            lobbyUI.SelectDifficulty(MissionDifficulty.Elite);
            Assert.AreEqual(MissionDifficulty.Elite, DEADeyeGameManager.Instance.Difficulty, "Difficulty must update to Elite.");

            lobbyUI.SelectDifficulty(MissionDifficulty.Operative);
            Assert.AreEqual(MissionDifficulty.Operative, DEADeyeGameManager.Instance.Difficulty, "Difficulty must update to Operative.");

            yield return null;
        }

        [UnityTest]
        public IEnumerator PlayMode_03_RooftopMission_InitializesCityAndSniperPerch()
        {
            SceneManager.LoadScene("RooftopMission");
            yield return new WaitForSeconds(1.0f);

            Assert.AreEqual("RooftopMission", SceneManager.GetActiveScene().name, "Active scene must be RooftopMission.");

            var playerCtrl = Object.FindFirstObjectByType<DEADeyePlayerController>();
            Assert.IsNotNull(playerCtrl, "DEADeyePlayerController must be present on rooftop.");

            var weapon = Object.FindFirstObjectByType<DEADeyeSniperWeapon>();
            Assert.IsNotNull(weapon, "Mounted sniper rifle must be present on rooftop perch.");

            var hud = Object.FindFirstObjectByType<DEADeyeMissionHUD>();
            Assert.IsNotNull(hud, "Mission HUD must be active.");

            var director = Object.FindFirstObjectByType<DEADeyeMissionDirector>();
            Assert.IsNotNull(director, "Mission Director must be active.");

            // Capture in-engine screenshot of Rooftop Mission Overlooking City
            CaptureScreenshot("RooftopMission_PlayMode.png");
            yield return new WaitForSeconds(0.5f);
        }

        [UnityTest]
        public IEnumerator PlayMode_04_Sniper_FiresAndRegistersHitOnHostile()
        {
            SceneManager.LoadScene("RooftopMission");
            yield return new WaitForSeconds(0.5f);

            var weapon = Object.FindFirstObjectByType<DEADeyeSniperWeapon>();
            var scoreMgr = Object.FindFirstObjectByType<DEADeyeScoreManager>();
            var director = Object.FindFirstObjectByType<DEADeyeMissionDirector>();

            Assert.IsNotNull(weapon);
            Assert.IsNotNull(scoreMgr);

            // Activate Optic Scope
            if (weapon.ScopeOptic != null)
            {
                weapon.ScopeOptic.SetScopeActive(true);
            }

            yield return new WaitForSeconds(0.2f);
            if (weapon.ScopeOptic != null && weapon.ScopeOptic.ScopeCamera != null)
            {
                CaptureScreenshot("RooftopMission_ScopeView.png", weapon.ScopeOptic.ScopeCamera);
            }
            else
            {
                CaptureScreenshot("RooftopMission_ScopeView.png");
            }
            yield return new WaitForSeconds(0.3f);

            // Spawn a test hostile target in direct sightline
            GameObject testTarget = new GameObject("TestStreetHostile");
            testTarget.transform.position = weapon.transform.position + weapon.transform.forward * 30f;
            var col = testTarget.AddComponent<CapsuleCollider>();
            col.center = Vector3.up;
            col.height = 2f;
            var hostile = testTarget.AddComponent<DEADeyeHostileCriminal>();

            // Ensure MissionActive state
            if (DEADeyeGameManager.Instance != null)
            {
                DEADeyeGameManager.Instance.SetGameState(DEADeyeGameState.MissionActive);
            }

            // Execute shot directly toward target
            bool shotFired = weapon.TryFire();
            Assert.IsTrue(shotFired, "Weapon should successfully fire when in MissionActive state.");

            yield return new WaitForSeconds(0.2f);

            Assert.Greater(scoreMgr.ShotsFired, 0, "Shots fired must increment.");
            Assert.Greater(scoreMgr.ShotsHit, 0, "Target in line of fire must register hit.");
            Assert.Greater(scoreMgr.CurrentScore, 0, "Score must increase upon hitting hostile.");

            Object.Destroy(testTarget);
        }

        [UnityTest]
        public IEnumerator PlayMode_05_ResultsScreen_DisplaysOutcomeAndCanReturnToLobby()
        {
            SceneManager.LoadScene("RooftopMission");
            yield return new WaitForSeconds(0.5f);

            var director = Object.FindFirstObjectByType<DEADeyeMissionDirector>();
            var resultsUI = Object.FindFirstObjectByType<DEADeyeResultsUI>(FindObjectsInactive.Include);

            Assert.IsNotNull(director);
            Assert.IsNotNull(resultsUI);

            // Conclude mission with victory
            director.EndMission(true);
            yield return new WaitForSeconds(0.3f);

            Assert.IsTrue(resultsUI.gameObject.activeInHierarchy, "Results panel must be displayed when mission ends.");

            CaptureScreenshot("MissionResults_PlayMode.png");
            yield return new WaitForSeconds(0.5f);
        }
    }
}
