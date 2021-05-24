// Copyright 2017-2021 Elringus (Artyom Sovetnikov). All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Video;

namespace Naninovel
{
    /// <remarks>
    /// On build pre-process: 
    ///   - When addressable provider is used: assign an addressable address and label to the assets referenced in <see cref="EditorResources"/>;
    ///   - Otherwise: copy the <see cref="EditorResources"/> assets to a temp `Resources` folder (except the assets already stored in `Resources` folders).
    /// On build post-process or build fail: 
    ///   - restore any affected assets and delete the created temporary `Resources` folder.
    /// </remarks>
    public static class BuildProcessor
    {
        public const string TempResourcesPath = "Assets/TEMP_NANINOVEL/Resources";
        public const string TempStreamingPath = "Assets/StreamingAssets";

        public static bool Building { get; private set; }

        private static ResourceProviderConfiguration config;
        private static bool useAddressables;

        [InitializeOnLoadMethod]
        private static void Initialize ()
        {
            if (ProjectConfigurationProvider.LoadOrDefault<ResourceProviderConfiguration>().EnableBuildProcessing)
                BuildPlayerWindow.RegisterBuildPlayerHandler(BuildHandler);

            ProjectResourcesBuildProcessor.TempFolderPath = TempResourcesPath;
        }

        private static void BuildHandler (BuildPlayerOptions options)
        {
            try
            {
                Building = true;
                PreprocessBuild(options);
                BuildPlayerWindow.DefaultBuildMethods.BuildPlayer(options);
            }
            finally
            {
                PostprocessBuild();
                Building = false;
            }
        }

        public static void PreprocessBuild (BuildPlayerOptions options)
        {
            config = ProjectConfigurationProvider.LoadOrDefault<ResourceProviderConfiguration>();

            useAddressables = AddressableHelper.Available && config.UseAddressables;
            if (!useAddressables) Debug.Log("Consider installing Addressable Asset System and enabling `Use Addressables` in the Naninovel's `Resource Provider` configuration menu. When the system is not available, all the assets assigned as Naninovel resources and not stored in `Resources` folders will be copied and re-imported when building the player, which could significantly increase build time.");

            if (useAddressables) AddressableHelper.RemovePreviousEntries();

            EditorUtils.CreateFolderAsset(TempResourcesPath);

            var records = EditorResources.LoadOrDefault().GetAllRecords();
            var projectResources = ProjectResources.Get().GetAllResources();
            var progress = 0;
            foreach (var record in records)
            {
                progress++;

                var resourcePath = record.Key;
                var assetGuid = record.Value;
                var assetPath = AssetDatabase.GUIDToAssetPath(assetGuid);
                if (string.IsNullOrEmpty(assetPath) || !EditorUtils.AssetExistsByPath(assetPath))
                {
                    Debug.LogWarning($"Failed to resolve `{resourcePath}` asset path from GUID stored in `EditorResources` asset. The resource won't be included to the build.");
                    continue;
                }

                if (EditorUtility.DisplayCancelableProgressBar("Processing Naninovel Resources", $"Processing '{assetPath}' asset...", progress / (float)records.Count))
                {
                    PostprocessBuild(); // Remove temporary assets.
                    throw new OperationCanceledException("Build was cancelled by the user.");
                }

                var resourceType = AssetDatabase.GetMainAssetTypeAtPath(assetPath);
                if (resourceType is null)
                {
                    Debug.LogWarning($"Failed to evaluate type of `{resourcePath}` asset. The resource won't be included to the build.");
                    continue;
                }

                if (resourceType == typeof(SceneAsset))
                    ProcessSceneResource(resourcePath, assetPath);
                else if (resourceType == typeof(VideoClip) && options.target == BuildTarget.WebGL)
                    ProcessVideoResourceForWebGL(resourcePath, assetPath);
                else ProcessResourceAsset(assetGuid, resourcePath, assetPath, projectResources);
            }

            AssetDatabase.SaveAssets();

            if (useAddressables && config.AutoBuildBundles)
            {
                EditorUtility.DisplayProgressBar("Processing Naninovel Resources", "Building asset bundles...", 1f);
                AddressableHelper.RebuildPlayerContent();
            }
        }

        public static void PostprocessBuild ()
        {
            AssetDatabase.DeleteAsset(TempResourcesPath.GetBeforeLast("/"));
            AssetDatabase.SaveAssets();
            EditorUtility.ClearProgressBar();
        }

        private static void ProcessResourceAsset (string assetGuid, string resourcePath, string assetPath, IReadOnlyDictionary<string, Type> projectResources)
        {
            if (projectResources.Keys.Contains(resourcePath)) // Handle assets stored in `Resources`.
            {
                var otherAsset = Resources.Load(resourcePath, typeof(UnityEngine.Object));
                AssetDatabase.TryGetGUIDAndLocalFileIdentifier(otherAsset, out var otherGuid, out long _);
                if (otherAsset && otherGuid != assetGuid) // Check if a different asset is available under the same resources path.
                {
                    var otherPath = AssetDatabase.GetAssetPath(otherAsset);
                    PostprocessBuild();
                    EditorUtility.ClearProgressBar();
                    throw new Exception($"Resource conflict detected: asset stored at `{otherPath}` conflicts with `{resourcePath}` Naninovel resource; rename or move the conflicting asset and rebuild the player.");
                }
                return;
            }

            if (useAddressables)
            {
                if (!AddressableHelper.CheckAssetConflict(assetGuid, resourcePath, out var conflictAddress))
                {
                    AddressableHelper.CreateOrUpdateAddressableEntry(assetGuid, resourcePath, config.GroupByCategory);
                    return;
                }
                Debug.Log($"Asset assigned as a Naninovel `{resourcePath}` resource is already registered in the Addressable Asset System as `{conflictAddress}`. It will be copied to prevent conflicts.");
            }

            var tempPath = string.IsNullOrWhiteSpace(config.ProjectRootPath) ? PathUtils.Combine(TempResourcesPath, resourcePath) : PathUtils.Combine(TempResourcesPath, config.ProjectRootPath, resourcePath);
            if (assetPath.Contains(".")) tempPath += $".{assetPath.GetAfter(".")}";

            EditorUtils.CreateFolderAsset(tempPath.GetBeforeLast("/"));
            AssetDatabase.CopyAsset(assetPath, tempPath);
        }

        /// <summary>
        /// Make sure the scene is included to the build settings.
        /// </summary>
        private static void ProcessSceneResource (string path, string assetPath)
        {
            var currentScenes = EditorBuildSettings.scenes.ToList();
            if (string.IsNullOrEmpty(assetPath) || currentScenes.Exists(s => s.path == assetPath)) return;
            currentScenes.Add(new EditorBuildSettingsScene(assetPath, true));
            EditorBuildSettings.scenes = currentScenes.ToArray();
        }

        /// <summary>
        /// Copy video to `StreamingAssets` folder for streaming on WebGL (built-in videos are not supported on the platform).
        /// </summary>
        private static void ProcessVideoResourceForWebGL (string path, string assetPath)
        {
            var streamingPath = PathUtils.Combine(TempStreamingPath, path);
            if (assetPath.Contains(".")) streamingPath += $".{assetPath.GetAfter(".")}";
            if (assetPath.EndsWithFast(streamingPath))
                return; // The file is already in a streaming assets folder.
            EditorUtils.CreateFolderAsset(streamingPath.GetBeforeLast("/"));
            AssetDatabase.CopyAsset(assetPath, streamingPath);
        }
    }
}
