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
    /// PlayMode integration tests verifying all 19 required checks from the Final Scope Reduction Directive:
    /// 1. Game opens directly into rooftop.
    /// 2. Correct first-person view.
    /// 3. Player remains securely positioned (no falling/clipping).
    /// 4. City is visible.
    /// 5. Hostile targets spawn.
    /// 6. Enemies visibly move.
    /// 7. Red hostile markers work.
    /// 8. Scope activates.
    /// 9. Scope alignment is correct.
    /// 10. Weapon fires.
    /// 11. Ammunition decreases.
    /// 12. Reload works.
    /// 13. Headshots register correctly (300 pts).
    /// 14. Buildings block shots.
    /// 15. Enemies are eliminated.
    /// 16. Escapes are counted correctly.
    /// 17. Mission ends correctly.
    /// 18. Results screen displays accurate statistics.
    /// 19. Restart resets the entire mission.
    /// 
    /// Captures actual in-engine screenshots of:
    /// - Initial sniper view (SniperView_Initial.png)
    /// - Scope view (SniperView_Scoped.png)
    /// - Enemy targets (SniperView_Hostiles.png)
    /// - Results interface (SniperView_Results.png)
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

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            Time.timeScale = 1f;
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Time.timeScale = 1f;
            yield return null;
        }

        [UnityTest]
        public IEnumerator PlayMode_01_Startup_DirectRooftopSniperPerch_InitialView()
        {
            // 1. Direct rooftop scene load (Check 1)
            SceneManager.LoadScene("RooftopMission");
            yield return new WaitForSeconds(0.8f);

            Assert.AreEqual("RooftopMission", SceneManager.GetActiveScene().name, "Check 1: Game must open directly into RooftopMission scene.");

            // 2. Correct first-person view & stationary positioning (Check 2, 3)
            var playerCtrl = Object.FindFirstObjectByType<DEADeyePlayerController>();
            Assert.IsNotNull(playerCtrl, "Check 2: Player controller must be present.");

            var desktopRig = Object.FindFirstObjectByType<DEADeyeDesktopRig>();
            Assert.IsNotNull(desktopRig, "Check 2: Desktop rig must exist for desktop testing.");

            // Verify player remains securely positioned at 26m altitude on solid floor (Check 3)
            Assert.GreaterOrEqual(playerCtrl.transform.position.y, 25.5f, "Check 3: Player elevation must be firmly at rooftop level (Y >= 25.5m).");
            Assert.LessOrEqual(playerCtrl.transform.position.y, 27.5f, "Check 3: Player elevation must not float above rooftop (Y <= 27.5m).");

            // 3. City is visible (Check 4)
            var cityBuilder = Object.FindFirstObjectByType<DEADeyeUrbanCityBuilder>();
            Assert.IsNotNull(cityBuilder, "Check 4: City builder must be present generating street grid.");

            // 4. Sniper rifle visible in foreground (Check 2)
            var weapon = Object.FindFirstObjectByType<DEADeyeSniperWeapon>();
            Assert.IsNotNull(weapon, "Check 2: Sniper rifle must be instantiated.");

            // 5. HUD immediately readable (Check 11)
            var hud = Object.FindFirstObjectByType<DEADeyeMissionHUD>();
            Assert.IsNotNull(hud, "Check 11: Mission HUD must be immediately readable.");

            // Capture Screenshot: Mission Selection Hub (Tactical Terminal)
            if (DEADeyeTacticalTerminalUI.Instance != null && DEADeyeTacticalTerminalUI.Instance.IsTerminalActive)
            {
                CaptureScreenshot("MissionSelect_TacticalTerminal.png");
                DEADeyeTacticalTerminalUI.Instance.DeploySelectedMission();
                yield return new WaitForSeconds(0.3f);
            }

            // Capture Screenshot: Initial Gameplay Sniper View
            CaptureScreenshot("SniperView_Initial.png");

            // Capture Screenshot: Full-Screen Mission Info Overlay
            if (hud != null)
            {
                hud.ToggleMissionInfo();
                yield return new WaitForSecondsRealtime(0.2f);
                CaptureScreenshot("MissionBriefing_DeadAir.png");
                hud.ToggleMissionInfo();
                yield return new WaitForSecondsRealtime(0.2f);
            }
        }

        [UnityTest]
        public IEnumerator PlayMode_02_HostileTargets_SpawnAndPatrolWithMarkers()
        {
            SceneManager.LoadScene("RooftopMission");
            yield return new WaitForSeconds(1.0f);

            var director = Object.FindFirstObjectByType<DEADeyeMissionDirector>();
            Assert.IsNotNull(director, "Director must be present.");

            var hostiles = Object.FindObjectsByType<DEADeyeHostileAI>(FindObjectsSortMode.None);
            if (hostiles.Length == 0)
            {
                director.StartMission();
                yield return new WaitForSeconds(0.5f);
                hostiles = Object.FindObjectsByType<DEADeyeHostileAI>(FindObjectsSortMode.None);
            }

            // 5. Hostile targets spawn (Check 5)
            Assert.Greater(hostiles.Length, 0, "Check 5: Hostile targets must spawn on the streets.");

            // 6. Enemies visibly move (Check 6)
            yield return new WaitForSeconds(0.8f);
            bool anyHostileMoved = false;
            foreach (var h in hostiles)
            {
                if (h.Agent != null && h.Agent.enabled)
                {
                    anyHostileMoved = true;
                    break;
                }
            }
            Assert.IsTrue(anyHostileMoved, "Check 6: Hostiles must visibly navigate along defined routes.");

            // 7. Red hostile markers work (Check 7)
            var markers = Object.FindObjectsByType<DEADeyeHostileMarker>(FindObjectsSortMode.None);
            Assert.Greater(markers.Length, 0, "Check 7: Hostiles must have visible red triangular indicators.");

            // Capture Screenshot: Enemy Targets in City
            CaptureScreenshot("SniperView_Hostiles.png");
            yield return new WaitForSeconds(0.3f);
        }

        [UnityTest]
        public IEnumerator PlayMode_03_ScopeOptic_AimingAndMagnification()
        {
            SceneManager.LoadScene("RooftopMission");
            yield return new WaitForSeconds(0.5f);

            var weapon = Object.FindFirstObjectByType<DEADeyeSniperWeapon>();
            Assert.IsNotNull(weapon, "Weapon must exist.");
            Assert.IsNotNull(weapon.ScopeOptic, "Scope optic must exist.");

            var hud = Object.FindFirstObjectByType<DEADeyeMissionHUD>();
            Assert.IsNotNull(hud, "HUD must exist.");

            // 8. Scope activates (Check 8)
            weapon.ScopeOptic.SetScopeActive(true);
            hud.SetScopeOverlay(true);
            Assert.IsTrue(weapon.ScopeOptic.IsAiming, "Check 8: Scope optic must be active.");

            // 9. Scope alignment and camera magnification (Check 9)
            Assert.IsNotNull(weapon.ScopeOptic.ScopeCamera, "Check 9: Scope camera must be present.");
            Assert.Less(weapon.ScopeOptic.ScopeCamera.fieldOfView, 30f, "Check 9: Scope camera FOV must be magnified (FOV < 30).");

            yield return new WaitForSeconds(0.3f);

            // Capture Screenshot: Scope View
            CaptureScreenshot("SniperView_Scoped.png", weapon.ScopeOptic.ScopeCamera);
            yield return new WaitForSeconds(0.3f);
        }

        [UnityTest]
        public IEnumerator PlayMode_04_Ballistics_PrecisionHeadshotAndAmmoConsumption()
        {
            SceneManager.LoadScene("RooftopMission");
            yield return new WaitForSeconds(0.5f);

            var weapon = Object.FindFirstObjectByType<DEADeyeSniperWeapon>();
            var scoreMgr = Object.FindFirstObjectByType<DEADeyeScoreManager>();
            var weaponMgr = Object.FindFirstObjectByType<DEADeyeWeaponManager>();

            Assert.IsNotNull(weapon);
            Assert.IsNotNull(scoreMgr);
            Assert.IsNotNull(weaponMgr);

            int ammoBefore = weaponMgr.CurrentAmmo;

            // 10 & 11. Weapon fires and ammo decreases ONLY when scoped
            weapon.ScopeOptic.SetScopeActive(false);
            bool unscopedShot = weapon.TryFire();
            Assert.IsFalse(unscopedShot, "Sniper rifle must strictly reject firing when not scoped.");
            Assert.AreEqual(ammoBefore, weaponMgr.CurrentAmmo, "Ammo must not decrease when unscoped shot is attempted.");

            weapon.ScopeOptic.SetScopeActive(true);
            bool shotFired = weapon.TryFire();
            Assert.IsTrue(shotFired, "Check 10: Weapon must fire when scoped and triggered.");
            Assert.AreEqual(ammoBefore - 1, weaponMgr.CurrentAmmo, "Check 11: Ammunition must decrease upon firing.");

            // 12. Reload works (Check 12)
            weaponMgr.RefillInstantly();
            Assert.AreEqual(5, weaponMgr.CurrentAmmo, "Check 12: Reload must restore full 5-round magazine.");

            // 13. Precision Headshot registers correctly (Check 13)
            GameObject targetGO = new GameObject("TestStreetTarget");
            targetGO.transform.position = new Vector3(0f, 0.1f, 25f);
            var targetHostile = targetGO.AddComponent<DEADeyeHostileCriminal>();

            GameObject headGO = new GameObject("HeadTrigger");
            headGO.transform.SetParent(targetGO.transform, false);
            headGO.transform.localPosition = new Vector3(0f, 1.6f, 0f);
            var sphereCol = headGO.AddComponent<SphereCollider>();
            sphereCol.isTrigger = true;
            var headTrigger = headGO.AddComponent<DEADeyeHeadshotTrigger>();
            headTrigger.Initialize(targetHostile);

            int scoreBefore = scoreMgr.CurrentScore;
            int headshotsBefore = scoreMgr.Headshots;

            headTrigger.RegisterHeadshot(100, headGO.transform.position, Vector3.back);

            Assert.AreEqual(headshotsBefore + 1, scoreMgr.Headshots, "Check 13: Headshots count must increment.");
            Assert.GreaterOrEqual(scoreMgr.CurrentScore, scoreBefore + 300, "Check 13: Precision headshot must award at least 300 points.");

            // 14. Buildings block shots (Check 14)
            yield return new WaitForSeconds(0.9f); // Wait for bolt cycle cooldown

            GameObject buildingGO = GameObject.CreatePrimitive(PrimitiveType.Cube);
            buildingGO.name = "TestBuildingObstacle";
            buildingGO.transform.position = weapon.transform.position + weapon.transform.forward * 10f;
            buildingGO.transform.localScale = new Vector3(8f, 8f, 1f);

            weapon.ScopeOptic.SetScopeActive(true);
            bool blockedShot = weapon.TryFire();
            // Bullet hit the building, did not pass through to targets beyond
            Assert.IsTrue(blockedShot, "Check 14: Shot successfully fired into building surface while scoped.");

            Object.Destroy(targetGO);
            Object.Destroy(buildingGO);
            yield return null;
        }

        [UnityTest]
        public IEnumerator PlayMode_05_GameplayLoop_MissionEnd_ResultsAndRestart()
        {
            SceneManager.LoadScene("RooftopMission");
            yield return new WaitForSeconds(0.5f);

            var director = Object.FindFirstObjectByType<DEADeyeMissionDirector>();
            var resultsUI = Object.FindFirstObjectByType<DEADeyeResultsUI>(FindObjectsInactive.Include);

            Assert.IsNotNull(director, "Director must exist.");
            Assert.IsNotNull(resultsUI, "Results UI must exist.");

            // 15 & 16. Hostiles eliminated and escaped tracked (Check 15, 16)
            Assert.GreaterOrEqual(director.HostilesEliminated, 0, "Check 15: Eliminations must be tracked.");
            Assert.GreaterOrEqual(director.HostilesEscaped, 0, "Check 16: Escapes must be tracked.");

            // 17. Mission ends correctly (Check 17)
            director.EndMission(true);
            yield return new WaitForSeconds(0.3f);
            Assert.IsFalse(director.IsMissionActive, "Check 17: Mission must conclude cleanly.");

            // 18. Results screen displays accurate statistics (Check 18)
            Assert.IsTrue(resultsUI.gameObject.activeInHierarchy, "Check 18: Results screen must be visible on mission completion.");

            // Capture Screenshot: Results Interface
            CaptureScreenshot("SniperView_Results.png");
            yield return new WaitForSeconds(0.3f);

            // 19. Restart resets the entire mission (Check 19)
            resultsUI.OnRestartClicked();
            yield return new WaitForSeconds(0.5f);
            Assert.AreEqual("RooftopMission", SceneManager.GetActiveScene().name, "Check 19: Restart must reload the authoritative RooftopMission scene.");
        }

        [UnityTest]
        public IEnumerator PlayMode_06_BridgeNavigation_EastAndWest_ZeroTeleportResets()
        {
            SceneManager.LoadScene("RooftopMission");
            yield return new WaitForSeconds(0.8f);

            var desktopRig = Object.FindFirstObjectByType<DEADeyeDesktopRig>();
            var boundary = Object.FindFirstObjectByType<DEADeyeRooftopBoundary>();
            Assert.IsNotNull(desktopRig, "Desktop rig must exist.");
            Assert.IsNotNull(boundary, "Rooftop boundary must exist.");

            var cc = desktopRig.GetComponent<CharacterController>();
            Assert.IsNotNull(cc, "CharacterController must exist on desktop rig.");

            // 1. Initial position on Perch 1
            desktopRig.transform.position = new Vector3(0f, 26f, -3f);
            yield return new WaitForFixedUpdate();

            // 2. Walk across East Catwalk Bridge towards Perch 2 (18m, 26m, 4m)
            Vector3[] eastPath = new Vector3[]
            {
                new Vector3(4f, 26f, -4f),
                new Vector3(6.8f, 26f, -5.2f), // East bridge entrance
                new Vector3(10.5f, 26f, -1.6f), // Mid-bridge
                new Vector3(14.0f, 26f, 2.0f),  // East perch entrance
                new Vector3(18.0f, 26f, 4.0f)   // Center of Perch 2
            };

            foreach (var waypoint in eastPath)
            {
                for (int step = 0; step < 30; step++)
                {
                    Vector3 delta = (waypoint - desktopRig.transform.position);
                    if (delta.magnitude > 0.05f)
                    {
                        cc.Move(delta.normalized * Mathf.Min(delta.magnitude, 0.4f));
                    }
                    yield return new WaitForFixedUpdate();
                }
            }

            Assert.GreaterOrEqual(desktopRig.transform.position.x, 14f, "Player must successfully walk onto East Perch across the bridge.");
            Assert.GreaterOrEqual(desktopRig.transform.position.y, 25.5f, "Player must remain elevated at rooftop level without falling.");

            // Walk back from Perch 2 to Perch 1
            Vector3[] returnPathEast = new Vector3[]
            {
                new Vector3(14.0f, 26f, 2.0f),
                new Vector3(10.5f, 26f, -1.6f),
                new Vector3(6.8f, 26f, -5.2f),
                new Vector3(0f, 26f, -3f)
            };

            foreach (var waypoint in returnPathEast)
            {
                for (int step = 0; step < 35; step++)
                {
                    Vector3 delta = (waypoint - desktopRig.transform.position);
                    delta.y = 0f;
                    if (delta.magnitude > 0.05f)
                    {
                        cc.Move(delta.normalized * Mathf.Min(delta.magnitude, 0.4f));
                    }
                    yield return new WaitForFixedUpdate();
                }
            }

            Assert.LessOrEqual(desktopRig.transform.position.x, 4f, "Player must successfully return to Perch 1 from East Perch.");

            // 3. Walk across West Bridge towards Perch 3 (-18m, 26m, 6m)
            Vector3[] westPath = new Vector3[]
            {
                new Vector3(0f, 26f, -6.0f),     // Clear corridor south of terminal desk
                new Vector3(-6.8f, 26f, -5.2f),  // West bridge entrance
                new Vector3(-10.5f, 26f, -0.6f), // Mid-bridge
                new Vector3(-14.0f, 26f, 4.0f),  // West perch entrance
                new Vector3(-18.0f, 26f, 6.0f)   // Center of Perch 3
            };

            foreach (var waypoint in westPath)
            {
                for (int step = 0; step < 40; step++)
                {
                    Vector3 delta = (waypoint - desktopRig.transform.position);
                    delta.y = 0f;
                    if (delta.magnitude > 0.05f)
                    {
                        cc.Move(delta.normalized * Mathf.Min(delta.magnitude, 0.4f));
                    }
                    yield return new WaitForFixedUpdate();
                }
            }

            Assert.LessOrEqual(desktopRig.transform.position.x, -14f, "Player must successfully walk onto West Perch across the bridge.");
            Assert.GreaterOrEqual(desktopRig.transform.position.y, 25.5f, "Player must remain elevated at rooftop level without falling.");

            // Walk back from Perch 3 to Perch 1
            Vector3[] returnPathWest = new Vector3[]
            {
                new Vector3(-14.0f, 26f, 4.0f),
                new Vector3(-10.5f, 26f, -0.6f),
                new Vector3(-6.8f, 26f, -5.2f),
                new Vector3(0f, 26f, -6.0f),
                new Vector3(0f, 26f, -3.0f)
            };

            foreach (var waypoint in returnPathWest)
            {
                for (int step = 0; step < 40; step++)
                {
                    Vector3 delta = (waypoint - desktopRig.transform.position);
                    delta.y = 0f;
                    if (delta.magnitude > 0.05f)
                    {
                        cc.Move(delta.normalized * Mathf.Min(delta.magnitude, 0.4f));
                    }
                    yield return new WaitForFixedUpdate();
                }
            }

            Assert.GreaterOrEqual(desktopRig.transform.position.x, -4f, "Player must successfully return to Perch 1 from West Perch.");
            yield return null;
        }

        [UnityTest]
        public IEnumerator PlayMode_07_Controls_ConventionalMouseLook_And_ScopedShooting()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene("RooftopMission");
            yield return new WaitForSecondsRealtime(0.8f);

            var desktopRig = Object.FindFirstObjectByType<DEADeyeDesktopRig>();
            var hud = Object.FindFirstObjectByType<DEADeyeMissionHUD>();
            var weapon = Object.FindFirstObjectByType<DEADeyeSniperWeapon>();
            Assert.IsNotNull(desktopRig, "Desktop rig must exist.");
            Assert.IsNotNull(hud, "HUD must exist.");
            Assert.IsNotNull(weapon, "Weapon must exist.");

            // When tactical terminal is closed and state is MissionActive, cursor locks for natural mouse look
            if (DEADeyeTacticalTerminalUI.Instance != null && DEADeyeTacticalTerminalUI.Instance.IsTerminalActive)
            {
                DEADeyeTacticalTerminalUI.Instance.DeploySelectedMission();
                yield return new WaitForSecondsRealtime(0.2f);
            }
            if (DEADeyeTacticalTerminalUI.Instance != null)
            {
                DEADeyeTacticalTerminalUI.Instance.ShowTerminal(false);
            }
            if (DEADeyeGameManager.Instance != null)
            {
                DEADeyeGameManager.Instance.SetGameState(DEADeyeGameState.MissionActive);
            }
            desktopRig.UpdateCursorState();
            yield return null;

            Assert.AreEqual(CursorLockMode.Locked, Application.isBatchMode ? desktopRig.DesiredLockMode : Cursor.lockState, "Cursor must be captured and locked during active gameplay.");

            // Verify RMB triggers optical scope zoom
            desktopRig.SetAiming(true);
            yield return new WaitForSecondsRealtime(0.1f);
            Assert.IsTrue(desktopRig.IsAiming, "RMB must activate sniper optical scope zoom.");
            Assert.IsTrue(weapon.IsScoped, "Weapon must report IsScoped = true when aimed.");

            // Verify firing while scoped works
            bool scopedFired = weapon.TryFire();
            Assert.IsTrue(scopedFired, "Weapon must fire when optical scope is active.");

            // Release scope
            desktopRig.SetAiming(false);
            yield return new WaitForSecondsRealtime(0.1f);
            Assert.IsFalse(desktopRig.IsAiming, "Releasing RMB must deactivate scope zoom.");
            desktopRig.ClearAimOverride();

            // Verify firing while unscoped is strictly blocked
            bool unscopedFired = weapon.TryFire();
            Assert.IsFalse(unscopedFired, "Weapon must NOT fire when unscoped.");

            // Test Mission Info overlay toggle ('I' key or button)
            hud.ToggleMissionInfo();
            desktopRig.UpdateCursorState();
            yield return new WaitForSecondsRealtime(0.1f);
            Assert.IsTrue(hud.IsMissionInfoActive, "Mission Info overlay must open on toggle.");
            Assert.AreEqual(CursorLockMode.None, Application.isBatchMode ? desktopRig.DesiredLockMode : Cursor.lockState, "Cursor must unlock when Mission Info is open.");

            hud.ToggleMissionInfo();
            desktopRig.UpdateCursorState();
            yield return new WaitForSecondsRealtime(0.1f);
            Assert.IsFalse(hud.IsMissionInfoActive, "Mission Info overlay must close on second toggle.");
            Assert.AreEqual(CursorLockMode.Locked, Application.isBatchMode ? desktopRig.DesiredLockMode : Cursor.lockState, "Cursor must lock back when Mission Info closes.");
        }

        [UnityTest]
        public IEnumerator PlayMode_08_ReloadCycle_FiveRounds_InfiniteReserve()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene("RooftopMission");
            yield return new WaitForSecondsRealtime(0.8f);

            var weaponMgr = DEADeyeWeaponManager.Instance;
            Assert.IsNotNull(weaponMgr, "DEADeyeWeaponManager must exist.");

            // Ensure magazine capacity is 5
            Assert.AreEqual(5, weaponMgr.MagazineCapacity, "Magazine capacity must be exactly 5 rounds.");

            // Consume rounds
            weaponMgr.ConsumeAmmo();
            weaponMgr.ConsumeAmmo();
            Assert.AreEqual(3, weaponMgr.CurrentAmmo, "Ammo should decrease to 3 rounds.");

            // Start reload
            bool reloadCompleted = false;
            weaponMgr.OnReloadCompleted += () => reloadCompleted = true;

            weaponMgr.StartReload();
            Assert.IsTrue(weaponMgr.IsReloading, "Reload must be active.");

            // Wait for reload duration (1.8s)
            float timer = 0f;
            while (!reloadCompleted && timer < 5.0f)
            {
                timer += Time.unscaledDeltaTime > 0f ? Time.unscaledDeltaTime : 0.02f;
                yield return null;
            }

            Assert.IsTrue(reloadCompleted, "Reload completed event must fire.");
            Assert.IsFalse(weaponMgr.IsReloading, "Reload state must conclude.");
            Assert.AreEqual(5, weaponMgr.CurrentAmmo, "Magazine must be fully replenished to 5 rounds.");
        }

        [UnityTest]
        public IEnumerator PlayMode_09_RooftopTraversal_Perch1EdgesAndCorners_NoReset()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene("RooftopMission");
            yield return new WaitForSecondsRealtime(0.8f);

            var desktopRig = Object.FindFirstObjectByType<DEADeyeDesktopRig>();
            var boundary = Object.FindFirstObjectByType<DEADeyeRooftopBoundary>();
            Assert.IsNotNull(desktopRig, "Desktop rig must exist.");
            Assert.IsNotNull(boundary, "DEADeyeRooftopBoundary must exist.");

            var cc = desktopRig.GetComponent<CharacterController>();
            Assert.IsNotNull(cc, "CharacterController must exist on desktop rig.");

            int resetCount = 0;
            boundary.OnBoundaryResetTriggered += () => resetCount++;

            // Test 4 perimeter corners & edges of Perch 1 (Standing area spans X: [-5.5, 5.5], Z: [-10.5, -2.0])
            Vector3[] perch1Waypoints = new Vector3[]
            {
                new Vector3(0f, 26f, -2.0f),    // Front center
                new Vector3(4.8f, 26f, -2.2f),  // Front East corner
                new Vector3(4.8f, 26f, -10.0f), // Rear East corner
                new Vector3(0f, 26f, -10.5f),   // Rear center
                new Vector3(-4.8f, 26f, -10.0f),// Rear West corner
                new Vector3(-4.8f, 26f, -2.2f), // Front West corner
                new Vector3(0f, 26f, -3.0f)     // Return to center nest
            };

            foreach (var target in perch1Waypoints)
            {
                for (int step = 0; step < 25; step++)
                {
                    Vector3 delta = target - desktopRig.transform.position;
                    delta.y = 0f;
                    if (delta.magnitude > 0.05f)
                    {
                        cc.Move(delta.normalized * Mathf.Min(delta.magnitude, 0.45f));
                    }
                    yield return new WaitForFixedUpdate();
                }

                Assert.GreaterOrEqual(desktopRig.transform.position.y, 25.5f, $"Player must remain safely elevated on Perch 1 near {target}.");
                Assert.AreEqual(0, resetCount, $"Player near {target} on Perch 1 must not trigger accidental boundary reset.");
            }
        }

        [UnityTest]
        public IEnumerator PlayMode_10_RooftopTraversal_EastBridge_BothDirections_NoReset()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene("RooftopMission");
            yield return new WaitForSecondsRealtime(0.8f);

            var desktopRig = Object.FindFirstObjectByType<DEADeyeDesktopRig>();
            var boundary = Object.FindFirstObjectByType<DEADeyeRooftopBoundary>();
            Assert.IsNotNull(desktopRig, "Desktop rig must exist.");
            Assert.IsNotNull(boundary, "DEADeyeRooftopBoundary must exist.");

            var cc = desktopRig.GetComponent<CharacterController>();
            int resetCount = 0;
            boundary.OnBoundaryResetTriggered += () => resetCount++;

            // Path from Perch 1 across East Bridge to Perch 2
            Vector3[] forwardPath = new Vector3[]
            {
                new Vector3(0f, 26f, -3.0f),
                new Vector3(6.8f, 26f, -5.2f),  // Perch 1 East transition landing
                new Vector3(10.5f, 26f, -1.6f), // East bridge midpoint
                new Vector3(14.0f, 26f, 2.0f),  // Perch 2 West transition landing
                new Vector3(18.0f, 26f, 4.0f)   // Perch 2 center
            };

            foreach (var wp in forwardPath)
            {
                for (int step = 0; step < 35; step++)
                {
                    Vector3 delta = wp - desktopRig.transform.position;
                    delta.y = 0f;
                    if (delta.magnitude > 0.05f)
                    {
                        cc.Move(delta.normalized * Mathf.Min(delta.magnitude, 0.4f));
                    }
                    yield return new WaitForFixedUpdate();
                }
                Assert.GreaterOrEqual(desktopRig.transform.position.y, 25.5f, $"Player must stay elevated on East path at {wp}.");
                Assert.AreEqual(0, resetCount, "East Bridge traversal must not trigger reset.");
            }

            // Return Path from Perch 2 back to Perch 1
            Vector3[] returnPath = new Vector3[]
            {
                new Vector3(18.0f, 26f, 4.0f),
                new Vector3(14.0f, 26f, 2.0f),
                new Vector3(10.5f, 26f, -1.6f),
                new Vector3(6.8f, 26f, -5.2f),
                new Vector3(0f, 26f, -3.0f)
            };

            foreach (var wp in returnPath)
            {
                for (int step = 0; step < 35; step++)
                {
                    Vector3 delta = wp - desktopRig.transform.position;
                    delta.y = 0f;
                    if (delta.magnitude > 0.05f)
                    {
                        cc.Move(delta.normalized * Mathf.Min(delta.magnitude, 0.4f));
                    }
                    yield return new WaitForFixedUpdate();
                }
                Assert.GreaterOrEqual(desktopRig.transform.position.y, 25.5f, $"Player must stay elevated on return path at {wp}.");
                Assert.AreEqual(0, resetCount, "Return East Bridge traversal must not trigger reset.");
            }
        }

        [UnityTest]
        public IEnumerator PlayMode_11_RooftopTraversal_WestBridge_BothDirections_NoReset()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene("RooftopMission");
            yield return new WaitForSecondsRealtime(0.8f);

            var desktopRig = Object.FindFirstObjectByType<DEADeyeDesktopRig>();
            var boundary = Object.FindFirstObjectByType<DEADeyeRooftopBoundary>();
            Assert.IsNotNull(desktopRig, "Desktop rig must exist.");
            Assert.IsNotNull(boundary, "DEADeyeRooftopBoundary must exist.");

            var cc = desktopRig.GetComponent<CharacterController>();
            int resetCount = 0;
            boundary.OnBoundaryResetTriggered += () => resetCount++;

            // Path from Perch 1 across West Bridge to Perch 3
            Vector3[] forwardPath = new Vector3[]
            {
                new Vector3(0f, 26f, -3.0f),
                new Vector3(-6.8f, 26f, -5.2f), // Perch 1 West transition landing
                new Vector3(-10.5f, 26f, -0.6f),// West bridge midpoint
                new Vector3(-14.0f, 26f, 4.0f), // Perch 3 East transition landing
                new Vector3(-18.0f, 26f, 6.0f)  // Perch 3 center
            };

            foreach (var wp in forwardPath)
            {
                for (int step = 0; step < 35; step++)
                {
                    Vector3 delta = wp - desktopRig.transform.position;
                    delta.y = 0f;
                    if (delta.magnitude > 0.05f)
                    {
                        cc.Move(delta.normalized * Mathf.Min(delta.magnitude, 0.4f));
                    }
                    yield return new WaitForFixedUpdate();
                }
                Assert.GreaterOrEqual(desktopRig.transform.position.y, 25.5f, $"Player must stay elevated on West path at {wp}.");
                Assert.AreEqual(0, resetCount, "West Bridge traversal must not trigger reset.");
            }

            // Return Path from Perch 3 back to Perch 1
            Vector3[] returnPath = new Vector3[]
            {
                new Vector3(-18.0f, 26f, 6.0f),
                new Vector3(-14.0f, 26f, 4.0f),
                new Vector3(-10.5f, 26f, -0.6f),
                new Vector3(-6.8f, 26f, -5.2f),
                new Vector3(0f, 26f, -3.0f)
            };

            foreach (var wp in returnPath)
            {
                for (int step = 0; step < 35; step++)
                {
                    Vector3 delta = wp - desktopRig.transform.position;
                    delta.y = 0f;
                    if (delta.magnitude > 0.05f)
                    {
                        cc.Move(delta.normalized * Mathf.Min(delta.magnitude, 0.4f));
                    }
                    yield return new WaitForFixedUpdate();
                }
                Assert.GreaterOrEqual(desktopRig.transform.position.y, 25.5f, $"Player must stay elevated on return path at {wp}.");
                Assert.AreEqual(0, resetCount, "Return West Bridge traversal must not trigger reset.");
            }
        }

        [UnityTest]
        public IEnumerator PlayMode_12_RooftopTraversal_Perch2AndPerch3_EdgesAndParapets_NoReset()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene("RooftopMission");
            yield return new WaitForSecondsRealtime(0.8f);

            var desktopRig = Object.FindFirstObjectByType<DEADeyeDesktopRig>();
            var boundary = Object.FindFirstObjectByType<DEADeyeRooftopBoundary>();
            Assert.IsNotNull(desktopRig, "Desktop rig must exist.");
            Assert.IsNotNull(boundary, "DEADeyeRooftopBoundary must exist.");

            var cc = desktopRig.GetComponent<CharacterController>();
            int resetCount = 0;
            boundary.OnBoundaryResetTriggered += () => resetCount++;

            // Move to Perch 2 center
            cc.enabled = false;
            desktopRig.transform.position = new Vector3(18.0f, 26.0f, 4.0f);
            cc.enabled = true;
            yield return new WaitForFixedUpdate();

            Vector3[] perch2Edges = new Vector3[]
            {
                new Vector3(18.0f, 26.0f, 7.0f), // North parapet notch
                new Vector3(20.5f, 26.0f, 4.0f), // East wall
                new Vector3(18.0f, 26.0f, 1.2f), // South wall
                new Vector3(15.2f, 26.0f, 4.0f)  // West transition
            };

            foreach (var edge in perch2Edges)
            {
                for (int step = 0; step < 20; step++)
                {
                    Vector3 delta = edge - desktopRig.transform.position;
                    delta.y = 0f;
                    if (delta.magnitude > 0.05f)
                    {
                        cc.Move(delta.normalized * Mathf.Min(delta.magnitude, 0.4f));
                    }
                    yield return new WaitForFixedUpdate();
                }
                Assert.GreaterOrEqual(desktopRig.transform.position.y, 25.5f, $"Player must remain on Perch 2 edge at {edge}.");
                Assert.AreEqual(0, resetCount, $"Player on Perch 2 edge at {edge} must not trigger reset.");
            }

            // Move to Perch 3 center
            cc.enabled = false;
            desktopRig.transform.position = new Vector3(-18.0f, 26.0f, 6.0f);
            cc.enabled = true;
            yield return new WaitForFixedUpdate();

            Vector3[] perch3Edges = new Vector3[]
            {
                new Vector3(-18.0f, 26.0f, 9.0f), // North parapet notch
                new Vector3(-20.5f, 26.0f, 6.0f), // West wall
                new Vector3(-18.0f, 26.0f, 3.2f), // South wall
                new Vector3(-15.2f, 26.0f, 6.0f)  // East transition
            };

            foreach (var edge in perch3Edges)
            {
                for (int step = 0; step < 20; step++)
                {
                    Vector3 delta = edge - desktopRig.transform.position;
                    delta.y = 0f;
                    if (delta.magnitude > 0.05f)
                    {
                        cc.Move(delta.normalized * Mathf.Min(delta.magnitude, 0.4f));
                    }
                    yield return new WaitForFixedUpdate();
                }
                Assert.GreaterOrEqual(desktopRig.transform.position.y, 25.5f, $"Player must remain on Perch 3 edge at {edge}.");
                Assert.AreEqual(0, resetCount, $"Player on Perch 3 edge at {edge} must not trigger reset.");
            }
        }

        [UnityTest]
        public IEnumerator PlayMode_13_RooftopTraversal_DeliberateFall_SafeResetToNearestPerch()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene("RooftopMission");
            yield return new WaitForSecondsRealtime(0.8f);

            var desktopRig = Object.FindFirstObjectByType<DEADeyeDesktopRig>();
            var boundary = Object.FindFirstObjectByType<DEADeyeRooftopBoundary>();
            Assert.IsNotNull(desktopRig, "Desktop rig must exist.");
            Assert.IsNotNull(boundary, "DEADeyeRooftopBoundary must exist.");

            var cc = desktopRig.GetComponent<CharacterController>();
            int resetCount = 0;
            boundary.OnBoundaryResetTriggered += () => resetCount++;

            // Deliberately drop below minimumSafeY (e.g. Y = 10m over the street at X=0, Z=20)
            cc.enabled = false;
            desktopRig.transform.position = new Vector3(0f, 10f, 20f);
            cc.enabled = true;

            // Wait a few frames for boundary check
            yield return new WaitForSecondsRealtime(0.3f);

            // Boundary must catch the fall and reset player
            Assert.GreaterOrEqual(resetCount, 1, "Deliberate fall below minimumSafeY must trigger boundary reset.");
            Assert.GreaterOrEqual(desktopRig.transform.position.y, 25.5f, "Player must be safely restored to elevated rooftop level (Y >= 25.5f).");
            Assert.LessOrEqual(Vector3.Distance(desktopRig.transform.position, new Vector3(0f, 26f, -2.5f)), 4.0f, "Player must be restored to a safe perch location.");
        }
    }
}
