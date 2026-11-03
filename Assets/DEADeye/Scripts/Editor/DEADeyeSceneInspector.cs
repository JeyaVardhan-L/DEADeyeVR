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
                Debug.Log($"Root GO: {root.name}, Active: {root.activeSelf}");
                foreach (Transform child in root.transform)
                {
                    Debug.Log($"  Child: {child.name}");
                }
            }
        }
    }
}
#endif
