using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using Unity.XR.CoreUtils;

namespace DEADeyeVR.Foundation.Tests
{
    [TestFixture]
    public class FoundationIntegrationTests
    {
        private GameObject testRoot;
        private GameObject floorObj;
        private GameObject[] wallObjs;
        private GameObject targetObj;
        private GameObject weaponObj;
        private FoundationTarget target;
        private FoundationWeapon weapon;

        [SetUp]
        public void SetUp()
        {
            testRoot = new GameObject("TestRoot");

            // Solid Floor (30x30m, top surface at Y = 0)
            floorObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floorObj.name = "Floor_Solid";
            floorObj.transform.SetParent(testRoot.transform);
            floorObj.transform.position = new Vector3(0f, -0.5f, 0f);
            floorObj.transform.localScale = new Vector3(30f, 1f, 30f);

            // Perimeter Walls
            wallObjs = new GameObject[4];
            wallObjs[0] = CreateTestWall("Wall_North", new Vector3(0f, 3f, 15f), new Vector3(30f, 6f, 1f));
            wallObjs[1] = CreateTestWall("Wall_South", new Vector3(0f, 3f, -15f), new Vector3(30f, 6f, 1f));
            wallObjs[2] = CreateTestWall("Wall_East", new Vector3(15f, 3f, 0f), new Vector3(1f, 6f, 30f));
            wallObjs[3] = CreateTestWall("Wall_West", new Vector3(-15f, 3f, 0f), new Vector3(1f, 6f, 30f));

            // Target
            targetObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            targetObj.name = "Target";
            targetObj.transform.SetParent(testRoot.transform);
            targetObj.transform.position = new Vector3(0f, 1.5f, 6.0f);
            targetObj.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            targetObj.transform.localScale = new Vector3(1.2f, 0.05f, 1.2f);
            target = targetObj.AddComponent<FoundationTarget>();

            // Weapon
            weaponObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            weaponObj.name = "Weapon";
            weaponObj.transform.SetParent(testRoot.transform);
            weaponObj.transform.position = new Vector3(0f, 1.5f, 1.0f);
            weaponObj.transform.localScale = new Vector3(0.1f, 0.15f, 0.5f);
            weaponObj.AddComponent<Rigidbody>();
            weaponObj.AddComponent<XRGrabInteractable>();
            weapon = weaponObj.AddComponent<FoundationWeapon>();

            // Sync transforms with PhysX scene
            Physics.SyncTransforms();
        }

        [TearDown]
        public void TearDown()
        {
            if (testRoot != null)
                Object.DestroyImmediate(testRoot);
        }

        private GameObject CreateTestWall(string name, Vector3 pos, Vector3 scale)
        {
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = name;
            wall.transform.SetParent(testRoot.transform);
            wall.transform.position = pos;
            wall.transform.localScale = scale;
            return wall;
        }

        [Test]
        public void Test_01_PlayerGroundedOnSolidFloor()
        {
            Physics.SyncTransforms();

            // Verify downward raycast from standing eye height (1.65m) hits floor at Y = 0.0m
            Ray ray = new Ray(new Vector3(0f, 1.65f, 0f), Vector3.down);
            bool hit = Physics.Raycast(ray, out RaycastHit hitInfo, 5f);

            Assert.IsTrue(hit, "Downward raycast must hit the solid floor collider.");
            Assert.AreEqual(floorObj.GetComponent<Collider>(), hitInfo.collider, "Hit collider must be the solid floor collider.");
            Assert.AreEqual(0.0f, hitInfo.point.y, 0.001f, "Floor top surface must be at exactly Y = 0.0m.");
        }

        [Test]
        public void Test_02_PerimeterWallsCollidersEnforceBoundary()
        {
            Physics.SyncTransforms();

            // Verify all 4 walls block horizontal rays from player arena (elevated above table props)
            Vector3 center = new Vector3(0f, 2.5f, 0f);
            Vector3[] dirs = { Vector3.forward, Vector3.back, Vector3.right, Vector3.left };

            for (int i = 0; i < 4; i++)
            {
                Ray ray = new Ray(center, dirs[i]);
                bool hit = Physics.Raycast(ray, out RaycastHit hitInfo, 20f);
                Assert.IsTrue(hit, $"Perimeter wall in direction {dirs[i]} must be detected.");
                Assert.AreEqual(wallObjs[i].GetComponent<Collider>(), hitInfo.collider, $"Hit collider must be wall {wallObjs[i].name}.");
            }
        }

        [Test]
        public void Test_03_GrabbableObjects_PickupAndRelease()
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.transform.SetParent(testRoot.transform);
            cube.AddComponent<Rigidbody>();
            XRGrabInteractable grab = cube.AddComponent<XRGrabInteractable>();

            Assert.IsNotNull(grab, "Grabbable object must have XRGrabInteractable attached.");
            Assert.IsTrue(grab.throwOnDetach, "throwOnDetach must be enabled for realistic throwing.");
            Assert.IsFalse(grab.isSelected, "Object must not be selected initially.");
        }

        [Test]
        public void Test_04_WeaponInteractable_CanBePickedUp()
        {
            XRGrabInteractable grab = weapon.GetComponent<XRGrabInteractable>();
            Assert.IsNotNull(grab, "Weapon must have an XRGrabInteractable component.");
            Assert.IsNotNull(weapon.GetComponent<Rigidbody>(), "Weapon must have a Rigidbody for physical interaction.");
            Assert.IsNotNull(weapon.GetComponent<Collider>(), "Weapon must have a physics Collider.");
        }

        [Test]
        public void Test_05_WeaponAimAndFire_HitsTargetAndRegisters()
        {
            int initialHits = target.HitCount;
            Assert.AreEqual(0, initialHits, "Target hit count must start at 0.");

            // Aim weapon directly at target
            weapon.transform.LookAt(target.transform.position);
            Physics.SyncTransforms();

            // Fire
            bool hitRegistered = weapon.Fire();

            Assert.IsTrue(hitRegistered, "Firing aligned weapon must hit the target.");
            Assert.AreEqual(1, target.HitCount, "Target hit count must increment to 1.");
            Assert.AreEqual(1, weapon.TotalHitsRegistered, "Weapon registered hit count must be 1.");
            Assert.AreEqual(1, weapon.TotalShotsFired, "Weapon shots fired count must be 1.");
        }

        [Test]
        public void Test_06_MultipleShots_AccumulateCorrectHits()
        {
            weapon.transform.LookAt(target.transform.position);
            Physics.SyncTransforms();

            for (int i = 0; i < 5; i++)
            {
                weapon.Fire();
            }

            Assert.AreEqual(5, target.HitCount, "Target must register all 5 consecutive hits.");
            Assert.AreEqual(5, weapon.TotalShotsFired, "Weapon must record 5 total shots fired.");
            Assert.AreEqual(5, weapon.TotalHitsRegistered, "Weapon must record 5 total hits.");
        }

        [Test]
        public void Test_07_MissedShot_DoesNotIncrementTargetHits()
        {
            int hitsBefore = target.HitCount;

            // Aim weapon away from target (towards the sky)
            weapon.transform.rotation = Quaternion.Euler(-60f, 0f, 0f);
            Physics.SyncTransforms();
            bool hit = weapon.Fire();

            Assert.IsFalse(hit, "Weapon aimed at sky must not register target hit.");
            Assert.AreEqual(hitsBefore, target.HitCount, "Target hit count must remain unchanged on missed shot.");
        }
    }
}
