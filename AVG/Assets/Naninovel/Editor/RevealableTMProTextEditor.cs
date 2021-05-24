// Copyright 2017-2021 Elringus (Artyom Sovetnikov). All rights reserved.

using TMPro.EditorUtilities;
using UnityEditor;

namespace Naninovel
{
    [CustomEditor(typeof(UI.RevealableTMProText), true)]
    [CanEditMultipleObjects]
    public class RevealableTMProTextEditor : TMP_EditorPanelUI
    {
        private SerializedProperty revealFadeWidth;
        private SerializedProperty slideClipRect;
        private SerializedProperty italicSlantAngle;
        private SerializedProperty rubyVerticalOffset;
        private SerializedProperty rubySizeScale;
        private SerializedProperty unlockTipsOnPrint;
        private SerializedProperty tipTemplate;
        private SerializedProperty onTipClicked;
        private SerializedProperty fixArabicText;
        private SerializedProperty fixArabicFarsi;
        private SerializedProperty fixArabicTextTags;
        private SerializedProperty fixArabicPreserveNumbers;

        protected override void OnEnable ()
        {
            base.OnEnable();

            revealFadeWidth = serializedObject.FindProperty("revealFadeWidth");
            slideClipRect = serializedObject.FindProperty("slideClipRect");
            italicSlantAngle = serializedObject.FindProperty("italicSlantAngle");
            rubyVerticalOffset = serializedObject.FindProperty("rubyVerticalOffset");
            rubySizeScale = serializedObject.FindProperty("rubySizeScale");
            unlockTipsOnPrint = serializedObject.FindProperty("unlockTipsOnPrint");
            tipTemplate = serializedObject.FindProperty("tipTemplate");
            onTipClicked = serializedObject.FindProperty("onTipClicked");
            fixArabicText = serializedObject.FindProperty("fixArabicText");
            fixArabicFarsi = serializedObject.FindProperty("fixArabicFarsi");
            fixArabicTextTags = serializedObject.FindProperty("fixArabicTextTags");
            fixArabicPreserveNumbers = serializedObject.FindProperty("fixArabicPreserveNumbers");
        }

        public override void OnInspectorGUI ()
        {
            base.OnInspectorGUI();

            serializedObject.Update();

            EditorGUILayout.LabelField("Revealing", EditorStyles.boldLabel);
            ++EditorGUI.indentLevel;
            {
                EditorGUILayout.PropertyField(revealFadeWidth);
                EditorGUILayout.PropertyField(slideClipRect);
                EditorGUILayout.PropertyField(italicSlantAngle);
            }
            --EditorGUI.indentLevel;

            EditorGUILayout.LabelField("Ruby Text", EditorStyles.boldLabel);
            ++EditorGUI.indentLevel;
            {
                EditorGUILayout.PropertyField(rubyVerticalOffset);
                EditorGUILayout.PropertyField(rubySizeScale);
            }
            --EditorGUI.indentLevel;

            EditorGUILayout.LabelField("Tips", EditorStyles.boldLabel);
            ++EditorGUI.indentLevel;
            {
                EditorGUILayout.PropertyField(unlockTipsOnPrint);
                EditorGUILayout.PropertyField(tipTemplate);
                EditorGUILayout.PropertyField(onTipClicked);
            }
            --EditorGUI.indentLevel;

            EditorGUILayout.LabelField("Arabic Text Support", EditorStyles.boldLabel);
            ++EditorGUI.indentLevel;
            {
                EditorGUILayout.PropertyField(fixArabicText);
                EditorGUI.BeginDisabledGroup(!fixArabicText.boolValue);
                EditorGUILayout.PropertyField(fixArabicFarsi);
                EditorGUILayout.PropertyField(fixArabicTextTags);
                EditorGUILayout.PropertyField(fixArabicPreserveNumbers);
                EditorGUI.EndDisabledGroup();
            }
            --EditorGUI.indentLevel;

            serializedObject.ApplyModifiedProperties();
        }
    } 
}
