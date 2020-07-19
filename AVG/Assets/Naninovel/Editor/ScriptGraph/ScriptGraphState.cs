// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// Represents serializable state of <see cref="ScriptGraphView"/>.
    /// </summary>
    [System.Serializable]
    public class ScriptGraphState : ScriptableObject
    {
        [System.Serializable]
        public struct NodeState 
        {
            public string ScriptName;
            public Rect Position;
        }

        public List<NodeState> NodesState = new List<NodeState>();

        private static string directoryPath => PathUtils.Combine(Application.dataPath, ConfigurationSettings.GeneratedDataPath);
        private static string assetPath => PathUtils.AbsoluteToAssetPath(PathUtils.Combine(directoryPath, $"{nameof(ScriptGraphState)}.asset"));

        /// <summary>
        /// Loads an existing asset from package data folder or creates a new default instance.
        /// </summary>
        public static ScriptGraphState LoadOrDefault ()
        {
            var obj = AssetDatabase.LoadAssetAtPath<ScriptGraphState>(assetPath);

            if (!ObjectUtils.IsValid(obj))
            {
                obj = CreateInstance<ScriptGraphState>();
                System.IO.Directory.CreateDirectory(directoryPath);
                AssetDatabase.CreateAsset(obj, assetPath);
                AssetDatabase.Refresh();
                AssetDatabase.SaveAssets();
            }

            return obj;
        }
    }
}
