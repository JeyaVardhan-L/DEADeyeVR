using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace DEADeye.Editor
{
    /// <summary>
    /// Automated Android build pipeline for DEADeye VR targeting Meta Quest 2 / 3 / Pro.
    /// Configures IL2CPP, ARM64 architecture, OpenXR XR Plugin settings, and outputs a release APK.
    /// </summary>
    public static class DEADeyeAndroidBuilder
    {
        private const string APK_OUTPUT_PATH = "Builds/Android/DEADeye_Release.apk";

        private static readonly string[] BUILD_SCENES = new string[]
        {
            "Assets/DEADeye/Scenes/RooftopMission.unity"
        };

        [MenuItem("DEADeye/Build/Build Android Quest APK")]
        public static void BuildQuestApk()
        {
            Debug.Log("[DEADeyeAndroidBuilder] Starting Meta Quest Android Build Process...");

            string outputDir = Path.GetDirectoryName(APK_OUTPUT_PATH);
            if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
            {
                Directory.CreateDirectory(outputDir);
                Debug.Log($"[DEADeyeAndroidBuilder] Created output directory: {outputDir}");
            }

            // 1. Target and Architecture Configuration
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.deadeye.vr");
            PlayerSettings.productName = "DEADeye";
            PlayerSettings.companyName = "DEADeye Studios";
            PlayerSettings.bundleVersion = "1.0.0";
            PlayerSettings.Android.bundleVersionCode = 1;

            // Meta Quest IL2CPP + 64-bit ARM
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel29;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
            PlayerSettings.colorSpace = ColorSpace.Linear;

            // 2. Validate Scene Assets
            foreach (string scenePath in BUILD_SCENES)
            {
                if (!File.Exists(scenePath))
                {
                    throw new FileNotFoundException($"[DEADeyeAndroidBuilder] Required build scene missing: {scenePath}");
                }
            }

            // 3. Build Player Configuration
            BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions
            {
                scenes = BUILD_SCENES,
                locationPathName = APK_OUTPUT_PATH,
                target = BuildTarget.Android,
                targetGroup = BuildTargetGroup.Android,
                options = BuildOptions.None
            };

            Debug.Log($"[DEADeyeAndroidBuilder] Executing BuildPlayer to: {APK_OUTPUT_PATH}...");
            BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
            BuildSummary summary = report.summary;

            Debug.Log($"[DEADeyeAndroidBuilder] Build Completed with Result: {summary.result}");
            Debug.Log($"[DEADeyeAndroidBuilder] Total Time: {summary.totalTime.TotalSeconds:F1}s | Size: {summary.totalSize / (1024f * 1024f):F2} MB");
            Debug.Log($"[DEADeyeAndroidBuilder] Errors: {summary.totalErrors} | Warnings: {summary.totalWarnings}");

            if (summary.result != BuildResult.Succeeded)
            {
                throw new Exception($"[DEADeyeAndroidBuilder] Android Build Failed! Total Errors: {summary.totalErrors}");
            }

            Debug.Log($"[DEADeyeAndroidBuilder] BUILD SUCCEEDED. Artifact generated: {Path.GetFullPath(APK_OUTPUT_PATH)}");
        }
    }
}

