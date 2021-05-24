// Copyright 2017-2021 Elringus (Artyom Sovetnikov). All rights reserved.

using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// Uses file system watcher to track changes to `.nani` files in the project directory.
    /// </summary>
    public static class ScriptFileWatcher
    {
        /// <summary>
        /// Invoked when a <see cref="Script"/> asset is created or modified; returns modified script asset path.
        /// </summary>
        public static event Action<string> OnModified;

        private static ConcurrentQueue<string> modifiedScriptPaths = new ConcurrentQueue<string>();

        [InitializeOnLoadMethod]
        private static void Initialize ()
        {
            var config = ProjectConfigurationProvider.LoadOrDefault<ScriptsConfiguration>();
            if (!config.WatchScripts) return;
            EditorApplication.update += Update;
            var dataPath = string.IsNullOrEmpty(config.WatchedDirectory) || !Directory.Exists(config.WatchedDirectory) ? Application.dataPath : config.WatchedDirectory;
            Task.Run(() => StartWatcher(dataPath))
                .ContinueWith(StopWatcher, TaskScheduler.FromCurrentSynchronizationContext());
        }
        
        private static void Update ()
        {
            if (modifiedScriptPaths.Count == 0) return;
            if (!modifiedScriptPaths.TryDequeue(out var fullPath)) return;
            if (!File.Exists(fullPath)) return;
            
            var assetPath = PathUtils.AbsoluteToAssetPath(fullPath);
            AssetDatabase.ImportAsset(assetPath);
            OnModified?.Invoke(assetPath);
            
            // Required to rebuild script when editor is not in focus, because script view
            // delays rebuild, but delayed call is not invoked while editor is not in focus.
            if (!InternalEditorUtility.isApplicationActive)
                EditorApplication.delayCall?.Invoke();
        }

        private static FileSystemWatcher StartWatcher (string path)
        {
            var watcher = new FileSystemWatcher();
            watcher.Path = path;
            watcher.IncludeSubdirectories = true;
            watcher.NotifyFilter = NotifyFilters.LastWrite; 
            watcher.Filter = "*.nani";
            watcher.Changed += (_, e) => modifiedScriptPaths.Enqueue(e.FullPath);
            watcher.EnableRaisingEvents = true;
            return watcher;
        }

        private static void StopWatcher (Task<FileSystemWatcher> startTask)
        {
            try
            {
                var watcher = startTask.Result;
                AppDomain.CurrentDomain.DomainUnload += (EventHandler)((_, __) => { watcher.Dispose(); });
            }
            catch (Exception e) { Debug.LogError($"Failed to stop script file watcher: {e.Message}"); }
        }
    }
}
