// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using System;
using UnityEditor;
using UnityEditor.ProjectWindowCallback;
using UnityEngine;

namespace Naninovel
{
    public static class AssetMenuItems
    {
        private class DoCopyAsset : EndNameEditAction
        {
            public override void Action (int instanceId, string targetPath, string sourcePath)
            {
                AssetDatabase.CopyAsset(sourcePath, targetPath);
                var newAsset = AssetDatabase.LoadAssetAtPath<GameObject>(targetPath);
                ProjectWindowUtil.ShowCreatedAsset(newAsset);
            }
        }

        public static readonly string DefaultScriptContent = $"{Environment.NewLine}@stop";
        public static readonly string DefaultManagedTextContent = $"Item1Path: Item 1 Value{Environment.NewLine}Item2Path: Item 2 Value";

        private static void CreateResourceCopy (string resourcePath, string copyName)
        {
            var assetPath = PathUtils.AbsoluteToAssetPath(PathUtils.Combine(PackagePath.RuntimeResourcesPath, $"{resourcePath}.prefab"));
            CreateAssetCopy(assetPath, copyName);
        }

        private static void CreatePrefabCopy (string prefabPath, string copyName)
        {
            var assetPath = PathUtils.AbsoluteToAssetPath(PathUtils.Combine(PackagePath.PrefabsPath, $"{prefabPath}.prefab"));
            CreateAssetCopy(assetPath, copyName);
        }

        private static void CreateAssetCopy (string assetPath, string copyName)
        {
            var targetPath = $"{copyName}.prefab";
            var endAction = ScriptableObject.CreateInstance<DoCopyAsset>();
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(0, endAction, targetPath, null, assetPath);
        }

        [MenuItem("Assets/Create/Naninovel/Naninovel Script", priority = -4)]
        private static void CreateScript () => ProjectWindowUtil.CreateAssetWithContent("NewScript.nani", DefaultScriptContent);

        [MenuItem("Assets/Create/Naninovel/Managed Text", priority = -3)]
        private static void CreateManagedText () => ProjectWindowUtil.CreateAssetWithContent("NewManagedText.txt", DefaultManagedTextContent);

        [MenuItem("Assets/Create/Naninovel/Custom UI", priority = -2)]
        private static void CreateCustomUI () => CreatePrefabCopy("Templates/CustomUI", "NewCustomUI");

        [MenuItem("Assets/Create/Naninovel/Choice Button", priority = -1)]
        private static void CreateChoiceButton () => CreatePrefabCopy("ChoiceHandlers/ChoiceHandlerButton", "NewChoiceButton");

        [MenuItem("Assets/Create/Naninovel/Character/Generic")]
        private static void CreateCharacterGeneric () => CreatePrefabCopy("Templates/GenericCharacter", "NewGenericCharacter");
        [MenuItem("Assets/Create/Naninovel/Character/Layered")]
        private static void CreateCharacterLayered() => CreatePrefabCopy("Templates/Layered", "NewLayeredCharacter");

        [MenuItem("Assets/Create/Naninovel/Background/Generic")]
        private static void CreateBackgroundGeneric () => CreatePrefabCopy("Templates/GenericBackground", "NewGenericBackground");
        [MenuItem("Assets/Create/Naninovel/Background/Layered")]
        private static void CreateBackgroundLayered () => CreatePrefabCopy("Templates/Layered", "NewLayeredBackground");

        [MenuItem("Assets/Create/Naninovel/Text Printer/Dialogue")]
        private static void CreatePrinterDialogue () => CreatePrefabCopy("TextPrinters/Dialogue", "NewDialoguePrinter");
        [MenuItem("Assets/Create/Naninovel/Text Printer/Fullscreen")]
        private static void CreatePrinterFullscreen () => CreatePrefabCopy("TextPrinters/Fullscreen", "NewFullscreenPrinter");
        [MenuItem("Assets/Create/Naninovel/Text Printer/Wide")]
        private static void CreatePrinterWide () => CreatePrefabCopy("TextPrinters/Wide", "NewWidePrinter");
        [MenuItem("Assets/Create/Naninovel/Text Printer/Chat")]
        private static void CreatePrinterChat () => CreatePrefabCopy("TextPrinters/Chat", "NewChatPrinter");
        [MenuItem("Assets/Create/Naninovel/Text Printer/Bubble")]
        private static void CreatePrinterBubble () => CreatePrefabCopy("TextPrinters/Bubble", "NewBubblePrinter");
        [MenuItem("Assets/Create/Naninovel/Text Printer/TMProDialogue")]
        private static void CreatePrinterTMProDialogue () => CreatePrefabCopy("TextPrinters/TMProDialogue", "NewTMProDialoguePrinter");
        [MenuItem("Assets/Create/Naninovel/Text Printer/TMProFullscreen")]
        private static void CreatePrinterTMProFullscreen () => CreatePrefabCopy("TextPrinters/TMProFullscreen", "NewTMProFullscreenPrinter");
        [MenuItem("Assets/Create/Naninovel/Text Printer/TMProWide")]
        private static void CreatePrinterTMProWide () => CreatePrefabCopy("TextPrinters/TMProWide", "NewTMProWidePrinter");
        [MenuItem("Assets/Create/Naninovel/Text Printer/TMProBubble")]
        private static void CreatePrinterTMProBubble () => CreatePrefabCopy("TextPrinters/TMProBubble", "NewTMProBubblePrinter");

        [MenuItem("Assets/Create/Naninovel/Choice Handler/ButtonList")]
        private static void CreateChoiceButtonList () => CreatePrefabCopy("ChoiceHandlers/ButtonList", "NewButtonListChoiceHandler");
        [MenuItem("Assets/Create/Naninovel/Choice Handler/ButtonArea")]
        private static void CreateChoiceButtonArea () => CreatePrefabCopy("ChoiceHandlers/ButtonArea", "NewButtonAreaChoiceHandler");
    }
}
