using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace DEADeye.Editor
{
    public static class DEADeyeTMPSetup
    {
        [MenuItem("DEADeye/Setup/Import TMP Essentials")]
        public static void ImportTMPEssentials()
        {
            string packagePath = "Library/PackageCache/com.unity.ugui@c0ef89c047f6/Package Resources/TMP Essential Resources.unitypackage";
            if (!File.Exists(packagePath))
            {
                // Fallback search
                var files = Directory.GetFiles("Library/PackageCache", "TMP Essential Resources.unitypackage", SearchOption.AllDirectories);
                if (files.Length > 0)
                {
                    packagePath = files[0];
                }
            }

            Debug.Log($"[DEADeyeTMPSetup] Importing TMP Essential Resources from: {packagePath}");
            AssetDatabase.ImportPackage(packagePath, false);
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
            Debug.Log("[DEADeyeTMPSetup] Successfully imported TMP Essential Resources!");
        }
    }
}
