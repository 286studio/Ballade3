// Copyright 2017-2021 Elringus (Artyom Sovetnikov). All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Naninovel
{
    /// <summary>
    /// Derive from this class to create custom editors for <see cref="Configuration"/> assets of <see cref="IActorManager"/> services.
    /// </summary>
    /// <typeparam name="TConfig">Type of the configuration asset this editor is built for.</typeparam>
    /// <typeparam name="TActor">Type of the actor this editor is built for.</typeparam>
    /// <typeparam name="TMeta">Type of the actor meta the actor manager uses.</typeparam>
    public abstract class ActorManagerSettings<TConfig, TActor, TMeta> : ResourcefulSettings<TConfig> 
        where TConfig : Configuration 
        where TActor : IActor
        where TMeta : ActorMetadata
    {
        protected SerializedProperty MetadataMapProperty { get; private set; }
        protected string EditedActorId => EditingMetadata ? MetadataMapEditor.SelectedActorId : null;
        protected TMeta EditedMetadata => EditingMetadata ? MetadataMapEditor.EditedMetadataProperty.GetGenericValue<TMeta>() : DefaultMetadata;
        protected TMeta DefaultMetadata { get; private set; }
        protected bool EditingMetadata => MetadataMapEditor.EditedMetadataProperty != null;
        protected GUIContent FromMetaButtonLabel { get; private set; }
        protected MetadataMapEditor MetadataMapEditor { get; private set; }
        protected virtual string MetadataMapPropertyName => "Metadata";
        protected virtual string DefaultMetadataPropertyName => "DefaultMetadata";
        protected virtual HashSet<string> LockedActorIds => null;
        protected virtual bool AllowMultipleResources => implementationToResourcesAttribute.TryGetValue(EditedMetadata.Implementation, out var attr) && attr.AllowMultiple;
        protected override Type ResourcesTypeConstraint => implementationToResourcesAttribute.TryGetValue(EditedMetadata.Implementation, out var attr) ? attr.TypeConstraint : null;
        protected override string ResourcesPathPrefix => AllowMultipleResources ? $"{EditedMetadata.Loader.PathPrefix}/{EditedActorId}" : EditedMetadata.Loader.PathPrefix;
        protected override string ResourcesCategoryId => MetadataToResourcesCategoryId(EditedMetadata);
        protected override string ResourceName => AllowMultipleResources ? null : EditedActorId;

        private static readonly List<string> actorImplementations = new List<string>();
        private static readonly string[] actorImplementationLabels;
        private static readonly Dictionary<string, Type> implementationToCustomDataType = new Dictionary<string, Type>();
        private static readonly Dictionary<string, ActorResourcesAttribute> implementationToResourcesAttribute = new Dictionary<string, ActorResourcesAttribute>();

        private Dictionary<string, Action<SerializedProperty>> overrideDrawers;
        private HashSet<string> lockedActorIds;

        static ActorManagerSettings ()
        {
            foreach (var type in Engine.Types)
            {
                if (type.IsAbstract || type.IsGenericType) continue;
                
                // Collect implementation types.
                if (typeof(TActor).IsAssignableFrom(type))
                { 
                    actorImplementations.Add(type.AssemblyQualifiedName);
                    if (type.GetCustomAttribute<ActorResourcesAttribute>() is ActorResourcesAttribute resourcesAttribute)
                        implementationToResourcesAttribute[type.AssemblyQualifiedName] = resourcesAttribute;
                    continue; 
                }

                // Collect custom metadata types.
                if (!typeof(CustomMetadata).IsAssignableFrom(type)) continue;
                var customMetaImplType = default(string);
                var curType = type;
                while (curType.BaseType != null)
                {
                    if (curType.IsGenericType && curType.GetGenericTypeDefinition() == typeof(CustomMetadata<>))
                    {
                        customMetaImplType = curType.GetGenericArguments()[0].AssemblyQualifiedName;
                        break;
                    }
                    curType = curType.BaseType;
                }
                if (!string.IsNullOrEmpty(customMetaImplType))
                    implementationToCustomDataType[customMetaImplType] = type;
            }

            actorImplementationLabels = actorImplementations.Select(s => s.GetBefore(",")).ToArray();
        }

        public override void OnActivate (string searchContext, VisualElement rootElement)
        {
            base.OnActivate(searchContext, rootElement);
            
            overrideDrawers = OverrideMetaDrawers();
            lockedActorIds = LockedActorIds;
            FromMetaButtonLabel = new GUIContent ($"< Back To {EditorTitle} List");

            MetadataMapProperty = SerializedObject.FindProperty(MetadataMapPropertyName);
            MetadataMapEditor = new MetadataMapEditor(SerializedObject, MetadataMapProperty, typeof(TMeta), EditorTitle, lockedActorIds);
            DefaultMetadata = SerializedObject.FindProperty(DefaultMetadataPropertyName).GetGenericValue<TMeta>();

            MetadataMapEditor.OnElementModified += HandleMetadataElementModified;
        }

        public override void OnDeactivate ()
        {
            base.OnDeactivate();

            if (MetadataMapEditor != null)
                MetadataMapEditor.OnElementModified -= HandleMetadataElementModified;
        }
        
        protected override Dictionary<string, Action<SerializedProperty>> OverrideConfigurationDrawers ()
        {
            var drawers = base.OverrideConfigurationDrawers();
            drawers[MetadataMapPropertyName] = null;
            drawers[DefaultMetadataPropertyName] = property =>
            {
                var label = EditorGUI.BeginProperty(Rect.zero, null, property);
                property.isExpanded = EditorGUILayout.Foldout(property.isExpanded, label, true);
                if (!property.isExpanded) return;
                EditorGUI.indentLevel++;
                DrawDefaultMetaEditor(property);
                EditorGUI.indentLevel--;
            };
            return drawers;
        }
        
        protected virtual Dictionary<string, Action<SerializedProperty>> OverrideMetaDrawers ()
        {
            return new Dictionary<string, Action<SerializedProperty>>
            {
                [nameof(ActorMetadata.Loader)] = property => { if (ResourcesTypeConstraint != null) EditorGUILayout.PropertyField(property); }
            };
        }

        protected virtual string MetadataToResourcesCategoryId (ActorMetadata metadata) => $"{metadata.Loader.PathPrefix}/{metadata.Guid}";

        protected override void DrawConfigurationEditor ()
        {
            if (ShowResourcesEditor)
            {
                if (EditingMetadata)
                {
                    if (GUILayout.Button(FromMetaButtonLabel, GUIStyles.NavigationButton))
                        MetadataMapEditor.ResetEditedMetadata();
                    else
                    {
                        EditorGUILayout.Space();
                        DrawMetaEditor(MetadataMapEditor.EditedMetadataProperty);
                    }
                }
                else
                {
                    if (GUILayout.Button(FromResourcesButtonContent, GUIStyles.NavigationButton))
                        ShowResourcesEditor = false;
                    else
                    {
                        EditorGUILayout.Space();
                        MetadataMapEditor.DrawGUILayout();
                    }
                }
            }
            else
            {
                DrawDefaultEditor();

                EditorGUILayout.Space();
                if (GUILayout.Button(ToResourcesButtonContent, GUIStyles.NavigationButton))
                    ShowResourcesEditor = true;
            }
        }

        protected virtual void DrawMetaEditor (SerializedProperty metaProperty)
        {
            var actorTitle = MetadataMapEditor.SelectedActorId.InsertCamel();

            EditorGUILayout.LabelField($"{actorTitle} Metadata", EditorStyles.boldLabel);
            DrawDefaultMetaEditor(metaProperty);

            EditorGUILayout.Space();

            if (ResourcesTypeConstraint != null)
            {
                EditorGUILayout.LabelField(actorTitle + (AllowMultipleResources ? " Resources" : " Resource"), EditorStyles.boldLabel);
                ResourcesEditor.DrawGUILayout(ResourcesCategoryId, AllowRename, ResourcesPathPrefix, ResourceName, ResourcesTypeConstraint, ResourcesSelectionTooltip);
            }

            // Return to meta list when pressing return key and no text fields are edited.
            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Backspace && !EditorGUIUtility.editingTextField)
            {
                MetadataMapEditor.ResetEditedMetadata();
                Event.current.Use();
            }
        }

        protected void DrawDefaultMetaEditor (SerializedProperty metaProperty)
        {
            var property = metaProperty.Copy();
            var endProperty = property.GetEndProperty();
            
            property.NextVisible(true);
            do
            {
                if (SerializedProperty.EqualContents(property, endProperty))
                    break;

                if (overrideDrawers != null)
                {
                    var localPath = property.propertyPath.Replace(metaProperty.propertyPath + ".", string.Empty);
                    if (overrideDrawers.ContainsKey(localPath))
                    {
                        overrideDrawers[localPath]?.Invoke(property);
                        continue;
                    }
                }

                if (property.propertyPath.EndsWithFast(nameof(ActorMetadata.Implementation)))
                {
                    var label = EditorGUI.BeginProperty(Rect.zero, null, property);
                    var curIndex = actorImplementations.IndexOf(property.stringValue ?? string.Empty);
                    var newIndex = EditorGUILayout.Popup(label, curIndex, actorImplementationLabels);
                    property.stringValue = actorImplementations.IsIndexValid(newIndex) ? actorImplementations[newIndex] : string.Empty;
                    continue;
                }

                if (property.propertyPath.EndsWithFast("customData"))
                {
                    if (!implementationToCustomDataType.TryGetValue(EditedMetadata.Implementation, out var customDataType)) continue;

                    if (!property.hasVisibleChildren || property.managedReferenceFullTypename?.GetAfter(" ")?.Replace("/", "+") != customDataType.FullName)
                        property.managedReferenceValue = Activator.CreateInstance(customDataType);

                    var customMetaProperty = property.Copy();
                    var endCustomMetaProperty = customMetaProperty.GetEndProperty();
                    customMetaProperty.NextVisible(true);
                    do
                    {
                        if (SerializedProperty.EqualContents(customMetaProperty, endCustomMetaProperty))
                            break;
                        EditorGUILayout.PropertyField(customMetaProperty, true);
                    }
                    while (customMetaProperty.NextVisible(false));
                    
                    continue;
                }

                EditorGUILayout.PropertyField(property, true);
            }
            while (property.NextVisible(false));
        }

        protected virtual void HandleMetadataElementModified (MetadataMapEditor.ElementModifiedArgs args)
        {
            // Remove resources category associated with the removed actor.
            if (args.ModificationType == MetadataMapEditor.ElementModificationType.Remove)
            {
                var categoryId = MetadataToResourcesCategoryId(args.Metadata);
                ResourcesEditor.RemoveCategory(categoryId);
            }
        }
    }
}
