// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using System.IO;
using UnityEditor;
using UnityEngine;

namespace Naninovel
{
    public class AboutWindow : EditorWindow
    {
        public static string InstalledVersion { get => PlayerPrefs.GetString(installedVersionKey); set => PlayerPrefs.SetString(installedVersionKey, value); }

        private const string installedVersionKey = "Naninovel." + nameof(AboutWindow) + "." + nameof(InstalledVersion);
        private const string releaseNotesUri = "https://github.com/Elringus/NaninovelWeb/releases";
        private const string faqUri = "https://naninovel.com/faq/";
        private const string guideUri = "https://naninovel.com/guide/getting-started.html";
        private const string commandsUri = "https://naninovel.com/api/";
        private const string discordUri = "https://discord.gg/BfkNqem";
        private const string supportUri = "https://naninovel.com/support/";
        private const string reviewUri = "https://assetstore.unity.com/packages/templates/systems/naninovel-visual-novel-engine-135453#reviews";

        private EngineVersion engineVersion;
        private GUIContent logoContent;

        private void OnEnable ()
        {
            engineVersion = EngineVersion.LoadFromResources();
            InstalledVersion = engineVersion.Version;
            var logoPath = PathUtils.AbsoluteToAssetPath(Path.Combine(PackagePath.EditorResourcesPath, "NaninovelLogo.png"));
            logoContent = new GUIContent(AssetDatabase.LoadAssetAtPath<Texture2D>(logoPath));
        }

        public void OnGUI ()
        {
            GUILayout.Space(5);

            GUILayout.BeginHorizontal();
            GUILayout.Space(80);
            GUILayout.BeginVertical();
            GUILayout.FlexibleSpace();

            var currentColor = GUI.contentColor;
            GUI.contentColor = EditorGUIUtility.isProSkin ? Color.white : Color.black;
            GUILayout.Label(logoContent, GUIStyle.none);
            GUI.contentColor = currentColor;

            GUILayout.Space(5);
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Space(100);
            EditorGUILayout.SelectableLabel($"{engineVersion.Version} build {engineVersion.Build}");
            GUILayout.EndHorizontal();

            EditorGUILayout.LabelField("Release Notes", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("For the complete list of additions, changes and fixes associated with this Naninovel release, check the release page on GitHub.", EditorStyles.wordWrappedLabel);
            if (GUILayout.Button($"{engineVersion.Version} Release Notes")) Application.OpenURL(releaseNotesUri + $"/tag/{engineVersion.Version}");

            GUILayout.Space(5);

            EditorGUILayout.LabelField("Online Resources", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Check our online resources for the quick start guides and tutorials. Command reference will help you navigate through the script commands and the ways to use them. For the available support options see the support page. Any questions and suggestions are always welcome at our discord server.", EditorStyles.wordWrappedLabel);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("FAQ")) Application.OpenURL(faqUri);
            if (GUILayout.Button("Guide")) Application.OpenURL(guideUri);
            if (GUILayout.Button("Commands")) Application.OpenURL(commandsUri);
            if (GUILayout.Button("Support")) Application.OpenURL(supportUri);
            if (GUILayout.Button("Discord")) Application.OpenURL(discordUri);
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(5);

            EditorGUILayout.LabelField("Rate Naninovel", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("We really hope you like Naninovel! If you feel like it, please leave a review on the Asset Store, that helps us out a lot.", EditorStyles.wordWrappedLabel);
            if (GUILayout.Button("Review On Asset Store")) Application.OpenURL(reviewUri);

            GUILayout.Space(5);
        }

        [InitializeOnLoadMethod]
        private static void FirstTimeSetup ()
        {
            EditorApplication.delayCall += ExecuteFirstTimeSetup;
        }

        private static void ExecuteFirstTimeSetup ()
        {
            // First time ever launch.
            if (string.IsNullOrWhiteSpace(InstalledVersion))
            {
                OpenWindow();
                return;
            }

            // First time after update launch.
            var engineVersion = EngineVersion.LoadFromResources();
            if (engineVersion && engineVersion.Version != InstalledVersion)
                OpenWindow();
        }

        [MenuItem("Naninovel/About", priority = 1)]
        private static void OpenWindow ()
        {
            var position = new Rect(100, 100, 375, 490);
            GetWindowWithRect<AboutWindow>(position, true, "About Naninovel", true);
        }
    }
}
