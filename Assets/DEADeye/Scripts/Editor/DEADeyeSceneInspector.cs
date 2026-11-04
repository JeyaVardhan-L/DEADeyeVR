#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DEADeye.Editor
{
    public static class DEADeyeSceneInspector
    {
        [MenuItem("DEADeye/Debug/Inspect Scene Hierarchy")]
        public static void InspectScene()
        {
            var scene = EditorSceneManager.OpenScene("Assets/DEADeye/Scenes/RooftopMission.unity");
            Debug.Log($"Opened scene: {scene.name}, Root count: {scene.rootCount}");
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.name.Contains("Sniper") || root.GetComponentInChildren<DEADeyeSniperWeapon>() != null)
                {
                    DumpHierarchy(root.transform, "");
                }
            }
        }

        private static void DumpHierarchy(Transform t, string indent)
        {
            var mf = t.GetComponent<MeshFilter>();
            var mr = t.GetComponent<MeshRenderer>();
            string meshName = mf != null && mf.sharedMesh != null ? mf.sharedMesh.name : "none";
            string matName = mr != null && mr.sharedMaterial != null ? mr.sharedMaterial.name : "none";
            Debug.Log($"{indent}[{t.name}] pos={t.localPosition}, rot={t.localEulerAngles}, scale={t.localScale}, mesh={meshName}, mat={matName}");
            foreach (Transform c in t)
            {
                DumpHierarchy(c, indent + "  ");
            }
        }
    }
}
#endif
