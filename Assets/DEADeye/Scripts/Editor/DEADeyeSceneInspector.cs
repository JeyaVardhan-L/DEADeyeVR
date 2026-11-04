#if UNITY_EDITOR
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DEADeye.Editor
{
    public static class DEADeyeSceneInspector
    {
        [MenuItem("DEADeye/Debug/Inspect Traversal And Boundaries")]
        public static void InspectTraversalAndBoundaries()
        {
            var scene = EditorSceneManager.OpenScene("Assets/DEADeye/Scenes/RooftopMission.unity");
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"=== SCENE TRAVERSAL & BOUNDARY INSPECTION: {scene.name} ===");

            // Inspect RooftopSafetyBoundary root object
            var boundaryRoot = GameObject.Find("RooftopSafetyBoundary");
            if (boundaryRoot != null)
            {
                sb.AppendLine($"\n*** FOUND RooftopSafetyBoundary ***");
                sb.AppendLine($"Position: {boundaryRoot.transform.position}, Rotation: {boundaryRoot.transform.eulerAngles}, Scale: {boundaryRoot.transform.lossyScale}");
                foreach (var comp in boundaryRoot.GetComponents<Component>())
                {
                    sb.AppendLine($"  Component: {comp.GetType().FullName}");
                    if (comp is Collider c)
                    {
                        sb.AppendLine($"    Collider bounds: {c.bounds.min} to {c.bounds.max}, center={c.bounds.center}, size={c.bounds.size}, isTrigger={c.isTrigger}");
                    }
                    if (comp is MonoBehaviour mb)
                    {
                        var so = new SerializedObject(mb);
                        var sp = so.GetIterator();
                        while (sp.NextVisible(true))
                        {
                            sb.AppendLine($"    Prop: {sp.name} = {sp.propertyType}");
                        }
                    }
                }
                foreach (Transform child in boundaryRoot.transform)
                {
                    sb.AppendLine($"  Child: {child.name}, pos={child.position}, scale={child.lossyScale}");
                    foreach (var comp in child.GetComponents<Component>())
                    {
                        sb.AppendLine($"    Child Comp: {comp.GetType().FullName}");
                        if (comp is Collider cc)
                        {
                            sb.AppendLine($"      Child Col: bounds={cc.bounds.min} to {cc.bounds.max}, isTrigger={cc.isTrigger}");
                        }
                    }
                }
            }
            else
            {
                sb.AppendLine("\n*** RooftopSafetyBoundary NOT FOUND by GameObject.Find ***");
            }

            // 1. Inspect Player and CharacterController
            var player = Object.FindFirstObjectByType<DEADeyePlayerController>();
            var desktopRig = Object.FindFirstObjectByType<DEADeyeDesktopRig>();
            if (desktopRig != null)
            {
                var cc = desktopRig.GetComponent<CharacterController>();
                sb.AppendLine($"\n--- PLAYER DESKTOP RIG ---");
                sb.AppendLine($"Rig Position: {desktopRig.transform.position}");
                if (cc != null)
                {
                    sb.AppendLine($"CharacterController: center={cc.center}, radius={cc.radius}, height={cc.height}, stepOffset={cc.stepOffset}, slopeLimit={cc.slopeLimit}, skinWidth={cc.skinWidth}, minMoveDistance={cc.minMoveDistance}");
                    sb.AppendLine($"CC World Bounds: center={cc.bounds.center}, min={cc.bounds.min}, max={cc.bounds.max}, size={cc.bounds.size}");
                }
            }

            // 2. Inspect DEADeyeRooftopBoundary
            var boundaryComp = Object.FindFirstObjectByType<DEADeyeRooftopBoundary>();
            if (boundaryComp != null)
            {
                sb.AppendLine($"\n--- DEADeyeRooftopBoundary ---");
                var so = new SerializedObject(boundaryComp);
                sb.AppendLine($"networkCenter: {so.FindProperty("networkCenter").vector3Value}");
                sb.AppendLine($"networkSize: {so.FindProperty("networkSize").vector3Value}");
                sb.AppendLine($"minimumSafeY: {so.FindProperty("minimumSafeY").floatValue}");
                sb.AppendLine($"perch1SafePos: {so.FindProperty("perch1SafePos").vector3Value}");
                sb.AppendLine($"perch2SafePos: {so.FindProperty("perch2SafePos").vector3Value}");
                sb.AppendLine($"perch3SafePos: {so.FindProperty("perch3SafePos").vector3Value}");
            }

            // 3. Inspect All Floors & Bridges
            sb.AppendLine($"\n--- FLOORS & BRIDGES ---");
            var allCols = Object.FindObjectsByType<Collider>(FindObjectsSortMode.None);
            foreach (var col in allCols)
            {
                string name = col.gameObject.name;
                if (name.Contains("Floor") || name.Contains("Bridge") || name.Contains("Perch") || name.Contains("Boundary") || name.Contains("Parapet") || name.Contains("Rail") || name.Contains("Rest"))
                {
                    sb.AppendLine($"[{col.gameObject.name}] parent={col.transform.parent?.name}, isTrigger={col.isTrigger}, layer={LayerMask.LayerToName(col.gameObject.layer)}, pos={col.transform.position}, bounds={col.bounds.min} to {col.bounds.max}, center={col.bounds.center}, size={col.bounds.size}");
                }
            }

            // 4. Test Raycasts across bridges at 0.5m intervals from Perch 1 to Perch 2 & 3
            sb.AppendLine($"\n--- BRIDGE TRAVERSAL SAMPLING (Perch 1 -> East Perch 2) ---");
            Vector3 p1 = new Vector3(0f, 26.0f, -2.5f);
            Vector3 p2 = new Vector3(18f, 26.0f, 4.0f);
            Vector3[] waypointsEast = new Vector3[]
            {
                new Vector3(0f, 26.0f, -2.5f),
                new Vector3(6.5f, 26.0f, -5.2f),
                new Vector3(7.5f, 26.0f, -4.5f),
                new Vector3(10.0f, 26.0f, -1.5f),
                new Vector3(13.5f, 26.0f, 1.5f),
                new Vector3(15.0f, 26.0f, 2.5f),
                new Vector3(18.0f, 26.0f, 4.0f)
            };

            for (int i = 0; i < waypointsEast.Length; i++)
            {
                Vector3 pt = waypointsEast[i];
                Ray ray = new Ray(pt + Vector3.up * 1.0f, Vector3.down);
                if (Physics.Raycast(ray, out RaycastHit hit, 5.0f))
                {
                    sb.AppendLine($"Waypoint East[{i}] {pt}: Hit '{hit.collider.name}' (layer {LayerMask.LayerToName(hit.collider.gameObject.layer)}) at Y={hit.point.y:F2} (dist={hit.distance:F2})");
                }
                else
                {
                    sb.AppendLine($"Waypoint East[{i}] {pt}: NO FLOOR HIT! (GAP DETECTED!)");
                }
            }

            sb.AppendLine($"\n--- BRIDGE TRAVERSAL SAMPLING (Perch 1 -> West Perch 3) ---");
            Vector3[] waypointsWest = new Vector3[]
            {
                new Vector3(0f, 26.0f, -2.5f),
                new Vector3(-6.5f, 26.0f, -5.2f),
                new Vector3(-7.5f, 26.0f, -4.5f),
                new Vector3(-10.0f, 26.0f, -1.0f),
                new Vector3(-13.5f, 26.0f, 3.0f),
                new Vector3(-15.0f, 26.0f, 4.5f),
                new Vector3(-18.0f, 26.0f, 6.0f)
            };

            for (int i = 0; i < waypointsWest.Length; i++)
            {
                Vector3 pt = waypointsWest[i];
                Ray ray = new Ray(pt + Vector3.up * 1.0f, Vector3.down);
                if (Physics.Raycast(ray, out RaycastHit hit, 5.0f))
                {
                    sb.AppendLine($"Waypoint West[{i}] {pt}: Hit '{hit.collider.name}' (layer {LayerMask.LayerToName(hit.collider.gameObject.layer)}) at Y={hit.point.y:F2} (dist={hit.distance:F2})");
                }
                else
                {
                    sb.AppendLine($"Waypoint West[{i}] {pt}: NO FLOOR HIT! (GAP DETECTED!)");
                }
            }

            sb.AppendLine($"\n--- WEAPON INSPECTION ---");
            var weapon = Object.FindFirstObjectByType<DEADeyeSniperWeapon>();
            if (weapon != null)
            {
                sb.AppendLine($"Weapon GameObject: {weapon.gameObject.name}, pos={weapon.transform.position}, rot={weapon.transform.eulerAngles}, scale={weapon.transform.lossyScale}");
                foreach (var comp in weapon.GetComponentsInChildren<Component>(true))
                {
                    sb.AppendLine($"  Comp: {comp.GetType().FullName} on {comp.gameObject.name}");
                    if (comp is Renderer r)
                    {
                        foreach (var m in r.sharedMaterials)
                        {
                            sb.AppendLine($"    Mat: {m?.name}, Shader: {m?.shader?.name}");
                        }
                    }
                }
            }

            File.WriteAllText("traversal_inspection.txt", sb.ToString());
            Debug.Log(sb.ToString());
        }
    }
}
#endif
