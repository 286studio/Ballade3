// Copyright 2017-2021 Elringus (Artyom Sovetnikov). All rights reserved.

using System;
using System.IO;
using UnityEditor;

namespace Naninovel
{
    public class ScriptAssetPostprocessor : AssetPostprocessor
    {
        private static ScriptsConfiguration configuration = default;
        private static EditorResources editorResources = default;

        private static void OnPostprocessAllAssets (string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            if (BuildProcessor.Building) return;

            var modifiedResources = false;

            foreach (string assetPath in importedAssets)
            {
                if (AssetDatabase.GetMainAssetTypeAtPath(assetPath) != typeof(Script)) continue;

                if (configuration is null)
                    configuration = ProjectConfigurationProvider.LoadOrDefault<ScriptsConfiguration>();
                if (editorResources is null)
                    editorResources = EditorResources.LoadOrDefault();

                HandleAutoAdd(assetPath, ref modifiedResources);
            }

            if (modifiedResources)
            {
                EditorUtility.SetDirty(editorResources);
                AssetDatabase.SaveAssets();
            }
        }

        private static void HandleAutoAdd (string assetPath, ref bool modifiedResources)
        {
            if (!configuration.AutoAddScripts) return;

            var guid = AssetDatabase.AssetPathToGUID(assetPath);
            var name = Path.GetFileNameWithoutExtension(assetPath);

            // Don't add the script if it's already added.
            if (guid is null || editorResources.GetPathByGuid(guid) != null) return;

            // Add only new scripts created via context menu (will always have a @stop at second line).
            var linesEnum = File.ReadLines(assetPath).GetEnumerator();
            var secondLine = (linesEnum.MoveNext() && linesEnum.MoveNext()) ? linesEnum.Current : null;
            linesEnum.Dispose(); // Release the file.
            if (!secondLine?.EqualsFast(AssetMenuItems.DefaultScriptContent.GetAfterFirst(Environment.NewLine)) ?? true) return;

            editorResources.AddRecord(configuration.Loader.PathPrefix, configuration.Loader.PathPrefix, name, guid);
            modifiedResources = true;
        }
    }
}
