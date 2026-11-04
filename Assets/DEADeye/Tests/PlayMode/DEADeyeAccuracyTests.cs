using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using DEADeye;

namespace DEADeye.Tests
{
    /// <summary>
    /// Mandatory PlayMode Accuracy Verification Test Suite:
    /// 1. Crosshair directly on enemy body: hit registers.
    /// 2. Crosshair directly on enemy head: headshot registers.
    /// 3. Crosshair between two enemies: correct target registers.
    /// 4. Target at different distances: hits register.
    /// 5. Target behind building: shot is blocked.
    /// 6. Scope centre on target: hit registers.
    /// 7. Muzzle obstruction: shot does not pass through nearby geometry.
    /// 8. Miss: no damage or score is awarded.
    /// </summary>
    public class DEADeyeAccuracyTests
    {
        [UnitySetUp]
        public IEnumerator Setup()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene("RooftopMission");
            yield return new WaitForSeconds(0.6f);

            // Ensure mission is active and score is clean
            if (DEADeyeGameManager.Instance != null)
            {
                DEADeyeGameManager.Instance.SetGameState(DEADeyeGameState.MissionActive);
            }
            if (DEADeyeScoreManager.Instance != null)
            {
                DEADeyeScoreManager.Instance.ResetMetrics();
            }
            if (DEADeyeWeaponManager.Instance != null)
            {
                DEADeyeWeaponManager.Instance.ResetAmmo();
            }

            // Sniper rifle requires scope active to fire
            var rig = Object.FindFirstObjectByType<DEADeyeDesktopRig>();
            if (rig != null)
            {
                rig.SetAiming(true);
            }
            var weapon = Object.FindFirstObjectByType<DEADeyeSniperWeapon>();
            if (weapon != null && weapon.ScopeOptic != null)
            {
                weapon.ScopeOptic.SetScopeActive(true);
            }
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Time.timeScale = 1f;
            var rig = Object.FindFirstObjectByType<DEADeyeDesktopRig>();
            if (rig != null)
            {
                rig.ClearAimOverride();
                rig.SetAiming(false);
            }
            yield return null;
        }

        private DEADeyeSniperWeapon GetWeapon()
        {
            var weapon = Object.FindFirstObjectByType<DEADeyeSniperWeapon>();
            Assert.IsNotNull(weapon, "DEADeyeSniperWeapon must be present in the scene.");
            weapon.EnableDebugDiagnostics = true;
            return weapon;
        }

        private DEADeyeDesktopRig GetDesktopRig()
        {
            var rig = Object.FindFirstObjectByType<DEADeyeDesktopRig>();
            Assert.IsNotNull(rig, "DEADeyeDesktopRig must be present in the scene.");
            return rig;
        }

        [UnityTest]
        public IEnumerator Accuracy_01_CrosshairDirectlyOnEnemyBody_HitRegisters()
        {
            var weapon = GetWeapon();
            var rig = GetDesktopRig();
            var scoreMgr = DEADeyeScoreManager.Instance;

            // Spawn test enemy at 25m in front of player
            GameObject enemyGO = new GameObject("TestEnemy_Body");
            Vector3 enemyPos = rig.transform.position + rig.transform.forward * 25f;
            enemyPos.y = 26.0f; // on the same rooftop level for clean direct testing
            enemyGO.transform.position = enemyPos;

            var ai = enemyGO.AddComponent<DEADeyeHostileHumanoid>();
            ai.InitializeVariant(HumanoidVariant.Enforcer);

            yield return null;

            // Aim camera directly at enemy torso (Y = 27.05m)
            Vector3 torsoPos = enemyPos + new Vector3(0f, 1.05f, 0f);
            rig.LookAtTarget(torsoPos);

            int healthBefore = ai.CurrentHealth;
            int scoreBefore = scoreMgr.TotalScore;

            bool fired = weapon.TryFire();
            Assert.IsTrue(fired, "Weapon must fire successfully.");

            yield return new WaitForSeconds(0.1f);

            Assert.Less(ai.CurrentHealth, healthBefore, "Check 1: Enemy must take damage from body shot.");
            Assert.GreaterOrEqual(scoreMgr.TotalScore - scoreBefore, 100, "Check 1: Score must increase by at least 100 for body hit.");
            Assert.AreEqual(1, scoreMgr.TargetHits, "Check 1: Target hits count must increment.");

            Object.Destroy(enemyGO);
        }

        [UnityTest]
        public IEnumerator Accuracy_02_CrosshairDirectlyOnEnemyHead_HeadshotRegisters()
        {
            var weapon = GetWeapon();
            var rig = GetDesktopRig();
            var scoreMgr = DEADeyeScoreManager.Instance;

            // Spawn test enemy at 25m
            GameObject enemyGO = new GameObject("TestEnemy_Head");
            Vector3 enemyPos = rig.transform.position + rig.transform.forward * 25f;
            enemyPos.y = 26.0f;
            enemyGO.transform.position = enemyPos;

            var ai = enemyGO.AddComponent<DEADeyeHostileHumanoid>();
            ai.InitializeVariant(HumanoidVariant.Enforcer);

            yield return null;

            // Aim camera directly at enemy head (Y = 27.62m)
            Vector3 headPos = enemyPos + new Vector3(0f, 1.62f, 0f);
            rig.LookAtTarget(headPos);

            int scoreBefore = scoreMgr.TotalScore;
            int headshotsBefore = scoreMgr.Headshots;

            bool fired = weapon.TryFire();
            Assert.IsTrue(fired, "Weapon must fire successfully.");

            yield return new WaitForSeconds(0.1f);

            Assert.AreEqual(1, scoreMgr.Headshots - headshotsBefore, "Check 2: Headshot count must increment.");
            Assert.GreaterOrEqual(scoreMgr.TotalScore - scoreBefore, 300, "Check 2: Score must increase by at least 300 for precision headshot.");
            Assert.IsTrue(ai.IsEliminated, "Check 2: Enemy must be eliminated by precision headshot (300 damage).");

            Object.Destroy(enemyGO);
        }

        [UnityTest]
        public IEnumerator Accuracy_03_CrosshairBetweenTwoEnemies_CorrectTargetRegisters()
        {
            var weapon = GetWeapon();
            var rig = GetDesktopRig();

            Vector3 basePos = rig.transform.position + rig.transform.forward * 30f;
            basePos.y = 26.0f;

            // Enemy A on the left
            GameObject enemyA = new GameObject("Enemy_Left");
            enemyA.transform.position = basePos - rig.transform.right * 1.8f;
            var aiA = enemyA.AddComponent<DEADeyeHostileHumanoid>();
            aiA.InitializeVariant(HumanoidVariant.Enforcer);

            // Enemy B on the right
            GameObject enemyB = new GameObject("Enemy_Right");
            enemyB.transform.position = basePos + rig.transform.right * 1.8f;
            var aiB = enemyB.AddComponent<DEADeyeHostileHumanoid>();
            aiB.InitializeVariant(HumanoidVariant.Enforcer);

            yield return null;

            // Aim camera specifically at Enemy A's torso
            Vector3 targetPosA = enemyA.transform.position + new Vector3(0f, 1.05f, 0f);
            rig.LookAtTarget(targetPosA);

            weapon.TryFire();
            yield return new WaitForSeconds(0.1f);

            Assert.Less(aiA.CurrentHealth, 100, "Check 3: Targeted Enemy A must take damage.");
            Assert.AreEqual(100, aiB.CurrentHealth, "Check 3: Non-targeted Enemy B must remain undamaged.");

            Object.Destroy(enemyA);
            Object.Destroy(enemyB);
        }

        [UnityTest]
        public IEnumerator Accuracy_04_TargetAtDifferentDistances_HitsRegister()
        {
            var weapon = GetWeapon();
            var rig = GetDesktopRig();
            float[] distances = new float[] { 20f, 45f, 75f };

            foreach (float dist in distances)
            {
                // Reset cooldown
                yield return new WaitForSeconds(0.9f);

                GameObject enemyGO = new GameObject($"Enemy_{dist}m");
                Vector3 enemyPos = rig.transform.position + rig.transform.forward * dist;
                enemyPos.y = 26.0f;
                enemyGO.transform.position = enemyPos;

                var ai = enemyGO.AddComponent<DEADeyeHostileHumanoid>();
                ai.InitializeVariant(HumanoidVariant.Infiltrator);
                yield return null;

                rig.LookAtTarget(enemyPos + new Vector3(0f, 1.0f, 0f));

                int hitsBefore = DEADeyeScoreManager.Instance.TargetHits;
                weapon.TryFire();
                yield return new WaitForSeconds(0.1f);

                Assert.AreEqual(hitsBefore + 1, DEADeyeScoreManager.Instance.TargetHits, $"Check 4: Hit must register accurately at {dist}m distance.");
                Object.Destroy(enemyGO);
            }
        }

        [UnityTest]
        public IEnumerator Accuracy_05_TargetBehindBuilding_ShotIsBlocked()
        {
            var weapon = GetWeapon();
            var rig = GetDesktopRig();

            // Spawn target at 40m
            GameObject enemyGO = new GameObject("Enemy_BehindWall");
            Vector3 enemyPos = rig.transform.position + rig.transform.forward * 40f;
            enemyPos.y = 26.0f;
            enemyGO.transform.position = enemyPos;

            var ai = enemyGO.AddComponent<DEADeyeHostileHumanoid>();
            ai.InitializeVariant(HumanoidVariant.Enforcer);

            // Aim camera directly at enemy torso first
            Vector3 targetPos = enemyPos + new Vector3(0f, 1.05f, 0f);
            rig.LookAtTarget(targetPos);
            yield return null;

            // Spawn intermediate solid building obstacle directly between player camera and enemy
            GameObject wallGO = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wallGO.name = "Building_Obstacle";
            wallGO.transform.position = Vector3.Lerp(rig.DesktopCamera.transform.position, targetPos, 0.5f);
            wallGO.transform.localScale = new Vector3(10f, 10f, 4f);
            Physics.SyncTransforms();
            yield return null;

            int healthBefore = ai.CurrentHealth;
            int hitsBefore = DEADeyeScoreManager.Instance.TargetHits;

            weapon.TryFire();
            yield return new WaitForSeconds(0.1f);

            Assert.AreEqual(healthBefore, ai.CurrentHealth, "Check 5: Enemy behind building must NOT take damage.");
            Assert.AreEqual(hitsBefore, DEADeyeScoreManager.Instance.TargetHits, "Check 5: Target hits must NOT increment when shot is blocked by building.");

            Object.Destroy(enemyGO);
            Object.Destroy(wallGO);
        }

        [UnityTest]
        public IEnumerator Accuracy_06_ScopeCentreOnTarget_HitRegisters()
        {
            var weapon = GetWeapon();
            var rig = GetDesktopRig();

            // Spawn target at 50m
            GameObject enemyGO = new GameObject("Enemy_ScopedAim");
            Vector3 enemyPos = rig.transform.position + rig.transform.forward * 50f;
            enemyPos.y = 26.0f;
            enemyGO.transform.position = enemyPos;

            var ai = enemyGO.AddComponent<DEADeyeHostileHumanoid>();
            ai.InitializeVariant(HumanoidVariant.Commander);

            yield return null;

            // Activate scope
            weapon.ScopeOptic.SetScopeActive(true);
            Assert.IsTrue(weapon.ScopeOptic.IsAiming, "Check 6: Scope must be active.");

            // Align weapon & scope camera directly with enemy torso
            Vector3 targetPos = enemyPos + new Vector3(0f, 1.10f, 0f);
            rig.LookAtTarget(targetPos);
            if (weapon.ScopeOptic.ScopeCamera != null)
            {
                weapon.ScopeOptic.ScopeCamera.transform.LookAt(targetPos);
            }

            int healthBefore = ai.CurrentHealth;
            weapon.TryFire();
            yield return new WaitForSeconds(0.1f);

            Assert.Less(ai.CurrentHealth, healthBefore, "Check 6: Hit must register when fired through active optical scope.");
            weapon.ScopeOptic.SetScopeActive(false);

            Object.Destroy(enemyGO);
        }

        [UnityTest]
        public IEnumerator Accuracy_07_MuzzleObstruction_ShotDoesNotPassThroughNearbyGeometry()
        {
            var weapon = GetWeapon();
            var rig = GetDesktopRig();

            // Spawn target at 30m
            GameObject enemyGO = new GameObject("Enemy_Target");
            Vector3 enemyPos = rig.transform.position + rig.transform.forward * 30f;
            enemyPos.y = 26.0f;
            enemyGO.transform.position = enemyPos;

            var ai = enemyGO.AddComponent<DEADeyeHostileHumanoid>();
            ai.InitializeVariant(HumanoidVariant.Enforcer);

            // Aim camera at enemy first
            Vector3 targetPos = enemyPos + new Vector3(0f, 1.05f, 0f);
            rig.LookAtTarget(targetPos);
            yield return null;

            // Place an obstruction directly blocking the lower rifle muzzle path without blocking upper camera sightline
            Transform muzzle = weapon.MuzzlePoint;
            Vector3 muzzlePos = muzzle != null ? muzzle.position : rig.DesktopCamera.transform.position;
            Vector3 muzzleDir = (targetPos - muzzlePos).normalized;

            GameObject closeObstacle = GameObject.CreatePrimitive(PrimitiveType.Cube);
            closeObstacle.name = "Nearby_MuzzleObstruction";
            closeObstacle.transform.position = muzzlePos + muzzleDir * 2.0f;
            closeObstacle.transform.localScale = new Vector3(2.0f, 0.6f, 0.4f);
            Physics.SyncTransforms();
            yield return null;

            int healthBefore = ai.CurrentHealth;
            weapon.TryFire();
            yield return new WaitForSeconds(0.1f);

            Assert.AreEqual(healthBefore, ai.CurrentHealth, "Check 7: Shot must NOT pass through geometry obstructing the rifle muzzle.");

            Object.Destroy(enemyGO);
            Object.Destroy(closeObstacle);
        }

        [UnityTest]
        public IEnumerator Accuracy_08_Miss_NoDamageOrScoreAwarded()
        {
            var weapon = GetWeapon();
            var rig = GetDesktopRig();
            var scoreMgr = DEADeyeScoreManager.Instance;

            // Aim camera high into empty sky (pitch = -60°)
            rig.SetLookRotation(-60f, 0f);

            int scoreBefore = scoreMgr.TotalScore;
            int hitsBefore = scoreMgr.TargetHits;
            int missesBefore = scoreMgr.Misses;

            weapon.TryFire();
            yield return new WaitForSeconds(0.1f);

            Assert.AreEqual(scoreBefore, scoreMgr.TotalScore, "Check 8: Score must NOT increase on a miss.");
            Assert.AreEqual(hitsBefore, scoreMgr.TargetHits, "Check 8: Target hits must NOT increase on a miss.");
            Assert.AreEqual(missesBefore + 1, scoreMgr.Misses, "Check 8: Misses count must increment.");
        }
    }
}

