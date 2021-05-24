// Copyright 2017-2021 Elringus (Artyom Sovetnikov). All rights reserved.

using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Naninovel
{
    public static class Upgrader
    {
        [MenuItem("Naninovel/Upgrade/v1.12 to v1.13")]
        private static void Upgrade112To113 ()
        {
            if (!EditorUtility.DisplayDialog("Perform upgrade?",
                "Are you sure you want to perform v1.12-v1.13 upgrade? Configuration assets will be modified. Make sure to perform a backup before confirming.",
                "Upgrade", "Cancel")) return;
            
            // Handle LayeredActorBehaviour replaced with LayeredBackgroundBehaviour and LayeredCharacterBehaviour.  
            try
            {
                const string layeredBehaviourComponentGuid = "0ed8eaa5eef74e849a7f97276a748279";
                const string layeredCharacterComponentGuid = "3645880df9c1965479dfe05f712a1711";
                const string layeredBackgroundComponentGuid = "5fd416f37425423409b956ac79ed74bc";
                var editorResources = EditorResources.LoadOrDefault();
                var records = editorResources.GetAllRecords().ToArray();
                for (int i = 0; i < records.Length; i++)
                {
                    var resourcePath = records[i].Key;
                    var resourceGuid = records[i].Value;
                    var assetPath = AssetDatabase.GUIDToAssetPath(resourceGuid);
                    if (string.IsNullOrEmpty(assetPath) || !File.Exists(assetPath)) continue;
                    if (AssetDatabase.GetMainAssetTypeAtPath(assetPath) != typeof(GameObject)) continue;
                    EditorUtility.DisplayProgressBar("Upgrading project to Naninovel v1.13", $"Processing `{assetPath}`", i / (float)records.Length);
                    var assetText = File.ReadAllText(assetPath);
                    if (!assetText.Contains(layeredBehaviourComponentGuid)) continue;
                    var isCharacter = resourcePath.Contains(CharactersConfiguration.DefaultPathPrefix);
                    var isBackground = resourcePath.Contains(BackgroundsConfiguration.DefaultPathPrefix);
                    if (!isCharacter && !isBackground) continue;
                    assetText = assetText.Replace(layeredBehaviourComponentGuid, isCharacter ? layeredCharacterComponentGuid : layeredBackgroundComponentGuid);
                    File.WriteAllText(assetPath, assetText);
                    Debug.Log($"Upgrader: Replaced `LayeredActorBehaviour` component on `{assetPath}`.");
                }
            }
            finally { EditorUtility.ClearProgressBar(); }

            AssetDatabase.Refresh();
            AssetDatabase.SaveAssets();
        }
        
        [MenuItem("Naninovel/Upgrade/UniTask v1 to v2")]
        private static void UpgradeUniTaskv1Tov2 ()
        {
            if (!EditorUtility.DisplayDialog("Perform upgrade?",
               "Are you sure you want to modify this Unity project to migrate from UniTask v1 to v2?\n\nAll the C# script files in the project containing 'UniRx.Async' will be modified and 'ThirdParty/UniTask' folder inside Naninovel package will be removed. The effect of the upgrade is permanent and can't be undone, so make sure to backup the project before confirming.\n\nAfter the upgrade is complete, install UniTask v2 via UPM (other installation scenarios are not supported).", "Upgrade", "Cancel")) return;

            const string title = "Upgrading to UniTask v2";
            const string v1Using = "UniRx.Async";
            const string v2Using = "Cysharp.Threading.Tasks";

            try
            {
                var uniTaskPath = PathUtils.Combine(PackagePath.PackageRootPath, "ThirdParty/UniTask");
                if (Directory.Exists(uniTaskPath))
                {
                    EditorUtility.DisplayProgressBar(title, $"Deleting `{PathUtils.AbsoluteToAssetPath(uniTaskPath)}`...", 0f);
                    AssetDatabase.DeleteAsset(PathUtils.AbsoluteToAssetPath(uniTaskPath));
                }

                var scriptPaths = AssetDatabase.GetAllAssetPaths().Where(p => Path.GetExtension(p) == ".cs").ToArray();
                for (int i = 0; i < scriptPaths.Length; i++)
                {
                    var path = scriptPaths[i];
                    if (path.EndsWithFast("Upgrader.cs") || !File.Exists(path)) continue;
                    var scriptText = File.ReadAllText(path);
                    if (!scriptText.Contains(v1Using)) continue;
                    EditorUtility.DisplayProgressBar(title, $"Modifying `{PathUtils.AbsoluteToAssetPath(path)}`...", i / (float)scriptPaths.Length);
                    scriptText = scriptText.Replace(v1Using, v2Using);
                    File.WriteAllText(path, scriptText);
                }

                AssetDatabase.Refresh();
                AssetDatabase.SaveAssets();
            }
            catch (Exception e) { UnityEngine.Debug.LogError($"Failed upgrading to UniTask v2: {e.Message}"); }
            finally { EditorUtility.ClearProgressBar(); }
        }
    }
}
