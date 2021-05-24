// Copyright 2017-2021 Elringus (Artyom Sovetnikov). All rights reserved.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Naninovel
{
    public class IDEMetadataWindow : EditorWindow
    {
        protected string OutputPath
        {
            get => PlayerPrefs.GetString(GetPrefName());
            set
            {
                PlayerPrefs.SetString(GetPrefName(), value);
                ValidateOutputPath();
            }
        }

        private static readonly GUIContent generateCommandsContent = new GUIContent("Generate Commands", "Whether to generate metadata for the custom commands.");
        private static readonly GUIContent generateResourcesContent = new GUIContent("Generate Resources", "Whether to generate metadata for the project resources.");
        private static readonly GUIContent generateActorsContent = new GUIContent("Generate Actors", "Whether to generate metadata for the actors.");
        private static readonly GUIContent outputPathContent = new GUIContent("Output Path", $"Path to `{outputFolderName}` folder of the target Naninovel IDE extension.");

        private const string outputFolderName = "server";
        private const string fileName = "customMetadata.json";
        private const string contentTemplate = "{\"commands\": [\n\n{0}]}";

        private bool outputPathValid = false;

        [MenuItem("Naninovel/Tools/IDE Metadata")]
        private static void OpenWindow ()
        {
            var position = new Rect(100, 100, 500, 135);
            GetWindowWithRect<IDEMetadataWindow>(position, true, "IDE Metadata", true);
        }

        private static string GetPrefName ([CallerMemberName] string name = "") => $"Naninovel.{nameof(IDEMetadataWindow)}.{name}";

        private void OnEnable ()
        {
            ValidateOutputPath();
        }

        private void ValidateOutputPath ()
        {
            outputPathValid = OutputPath?.EndsWith(outputFolderName) ?? false;
        }

        private void OnGUI ()
        {
            EditorGUILayout.LabelField("Naninovel IDE Metadata", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("The tool to generate IDE metadata; see `IDE Extension` guide for usage instructions.", EditorStyles.wordWrappedMiniLabel);

            EditorGUILayout.Space();

            using (new EditorGUILayout.HorizontalScope())
            {
                OutputPath = EditorGUILayout.TextField(outputPathContent, OutputPath);
                if (GUILayout.Button("Select", EditorStyles.miniButton, GUILayout.Width(65)))
                    OutputPath = EditorUtility.OpenFolderPanel("Output Path", "", "");
            }

            GUILayout.FlexibleSpace();

            if (!outputPathValid)
                EditorGUILayout.HelpBox($"Output path is not valid. Make sure it points to a `{outputFolderName}` folder under a Naninovel IDE extension installation directory.", MessageType.Error);
            else if (GUILayout.Button("Generate IDE Metadata", GUIStyles.NavigationButton))
                GenerateCustomMetadata();

            EditorGUILayout.Space();
        }

        private void GenerateCustomMetadata ()
        {
            try
            {
                var projectMeta = new ProjectMetadata();
                UpdateProgressBar("Processing commands...", 0);
                GenerateCustomCommandsMetadata(projectMeta);
                UpdateProgressBar("Processing resources...", .25f);
                GenerateResourcesMetadata(projectMeta);
                UpdateProgressBar("Processing actors...", .50f);
                GenerateActorsMetadata(projectMeta);
                UpdateProgressBar("Processing predefined variables...", .75f);
                GeneratePredefinedVariablesMetadata(projectMeta);
                UpdateProgressBar("Processing expression functions...", .95f);
                GenerateExpressionFunctionsMetadata(projectMeta);
                WriteFile(projectMeta, Path.Combine(OutputPath, fileName));
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                Repaint();
            }
        }

        private static void UpdateProgressBar (string info, float progress) => EditorUtility.DisplayProgressBar("Generating IDE Metadata", info, progress);

        private static void WriteFile (ProjectMetadata projectMeta, string filePath)
        {
            var json = JsonUtility.ToJson(projectMeta, true);
            File.WriteAllText(filePath, json);
        }

        private static void GenerateResourcesMetadata (ProjectMetadata projectMeta)
        {
            var resources = new List<ProjectMetadata.ProjectResource>();
            var editorResources = EditorResources.LoadOrDefault();
            var records = editorResources.GetAllRecords();
            foreach (var kv in records)
            {
                var record = editorResources.GetRecordByGuid(kv.Value);
                if (!record.HasValue) continue;
                var resource = new ProjectMetadata.ProjectResource {
                    pathPrefix = record.Value.PathPrefix,
                    name = record.Value.Name
                };
                resources.Add(resource);
            }
            projectMeta.resources = resources;
        }

        private static void GenerateActorsMetadata (ProjectMetadata projectMeta)
        {
            var actors = new List<ProjectMetadata.ProjectActor>();
            var editorResources = EditorResources.LoadOrDefault();
            var allResources = editorResources.GetAllRecords().Keys.ToArray();
            var chars = ProjectConfigurationProvider.LoadOrDefault<CharactersConfiguration>().Metadata.ToDictionary();
            foreach (var kv in chars)
            {
                var charActor = new ProjectMetadata.ProjectActor {
                    id = kv.Key,
                    displayName = kv.Value.DisplayName,
                    pathPrefix = kv.Value.Loader.PathPrefix,
                    appearances = FindAppearances(kv.Key, kv.Value.Loader.PathPrefix, kv.Value.Implementation)
                };
                actors.Add(charActor);
            }
            var backs = ProjectConfigurationProvider.LoadOrDefault<BackgroundsConfiguration>().Metadata.ToDictionary();
            foreach (var kv in backs)
            {
                var backActor = new ProjectMetadata.ProjectActor {
                    id = kv.Key,
                    pathPrefix = kv.Value.Loader.PathPrefix,
                    appearances = FindAppearances(kv.Key, kv.Value.Loader.PathPrefix, kv.Value.Implementation)
                };
                actors.Add(backActor);
            }
            var choiceHandlers = ProjectConfigurationProvider.LoadOrDefault<ChoiceHandlersConfiguration>().Metadata.ToDictionary();
            foreach (var kv in choiceHandlers)
            {
                var choiceHandlerActor = new ProjectMetadata.ProjectActor {
                    id = kv.Key,
                    pathPrefix = kv.Value.Loader.PathPrefix
                };
                actors.Add(choiceHandlerActor);
            }
            var printers = ProjectConfigurationProvider.LoadOrDefault<TextPrintersConfiguration>().Metadata.ToDictionary();
            foreach (var kv in printers)
            {
                var printerActor = new ProjectMetadata.ProjectActor {
                    id = kv.Key,
                    pathPrefix = kv.Value.Loader.PathPrefix
                };
                actors.Add(printerActor);
            }
            projectMeta.actors = actors;

            List<string> FindAppearances (string actorId, string pathPrefix, string actorImplementation)
            {
                var prefabPath = allResources.FirstOrDefault(p => p.EndsWithFast($"{pathPrefix}/{actorId}"));
                var assetGUID = prefabPath != null ? editorResources.GetGuidByPath(prefabPath) : null;
                var assetPath = assetGUID != null ? AssetDatabase.GUIDToAssetPath(assetGUID) : null;
                var prefabAsset = assetPath != null ? AssetDatabase.LoadMainAssetAtPath(assetPath) : null;
                if (prefabAsset != null && actorImplementation.Contains("Layered"))
                {
                    var layeredBehaviour = (prefabAsset as GameObject)?.GetComponent<LayeredActorBehaviour>();
                    return layeredBehaviour != null ? layeredBehaviour.GetCompositionMap().Keys.ToList() : new List<string>();
                }
                else if (prefabAsset != null && (actorImplementation.Contains("Generic") || actorImplementation.Contains("Live2D")))
                {
                    var animator = (prefabAsset as GameObject)?.GetComponent<Animator>();
                    var controller = animator != null ? animator.runtimeAnimatorController as AnimatorController : null;
                    return controller != null
                        ? controller.parameters
                            .Where(p => p.type == AnimatorControllerParameterType.Trigger).Select(p => p.name).ToList()
                        : new List<string>();
                }
                #if SPRITE_DICING_AVAILABLE
                else if (prefabAsset != null && actorImplementation.Contains("Diced"))
                {
                    return (prefabAsset as SpriteDicing.DicedSpriteAtlas)?.GetAllSprites().Select(s => s.name).ToList() ?? new List<string>();
                }
                #endif
                else
                {
                    var multiplePrefix = $"{pathPrefix}/{actorId}/";
                    return allResources.Where(p => p.Contains(multiplePrefix)).Select(p => p.GetAfter(multiplePrefix)).ToList();
                }
            }
        }

        private static void GeneratePredefinedVariablesMetadata (ProjectMetadata projectMeta)
        {
            var config = ProjectConfigurationProvider.LoadOrDefault<CustomVariablesConfiguration>();
            projectMeta.predefinedVariables = config.PredefinedVariables.Select(p => p.Name).ToList();
        }

        private static void GenerateExpressionFunctionsMetadata (ProjectMetadata projectMeta)
        {
            projectMeta.expressionFunctions = Engine.Types
                .Where(t => t.Namespace != typeof(ExpressionFunctions).Namespace && t.IsDefined(typeof(ExpressionFunctionsAttribute)))
                .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Static)).Select(m => m.Name).Distinct().ToList();
        }

        private static void GenerateCustomCommandsMetadata (ProjectMetadata projectMeta)
        {
            var customCommands = Command.CommandTypes.Values.Where(t => t.Namespace != Command.DefaultNamespace).ToList();
            GenerateCommandsMetadata(projectMeta, customCommands, ResolveCustomCommandDocs, ResolveCustomCommandParameterSummary);

            (string, string, string) ResolveCustomCommandDocs (Type commandType) =>
                (commandType.CustomAttributes.FirstOrDefault(a => a.AttributeType == typeof(DocumentationAttribute))?.ConstructorArguments[0].Value as string,
                    commandType.CustomAttributes.FirstOrDefault(a => a.AttributeType == typeof(DocumentationAttribute))?.ConstructorArguments[1].Value as string, null);

            string ResolveCustomCommandParameterSummary (FieldInfo parameterFieldInfo) =>
                parameterFieldInfo.CustomAttributes.FirstOrDefault(a => a.AttributeType == typeof(DocumentationAttribute))?.ConstructorArguments[0].Value as string;
        }

        public static void GenerateCommandsMetadata (ProjectMetadata projectMeta, IReadOnlyCollection<Type> commands,
            Func<Type, (string summary, string remarks, string examples)> commandDocsResolver, Func<FieldInfo, string> parameterSummaryResolver)
        {
            var commandsMeta = new List<ProjectMetadata.CommandMetadata>();
            foreach (var commandType in commands)
            {
                (string summary, string remarks, string examples) = commandDocsResolver(commandType);
                var metadata = new ProjectMetadata.CommandMetadata {
                    id = commandType.Name,
                    alias = commandType.CustomAttributes.FirstOrDefault(a => a.AttributeType == typeof(Command.CommandAliasAttribute))?.ConstructorArguments[0].Value as string,
                    localizable = typeof(Command.ILocalizable).IsAssignableFrom(commandType),
                    summary = summary,
                    remarks = remarks,
                    examples = examples,
                    @params = ExtractParamsMeta(commandType, parameterSummaryResolver)
                };
                commandsMeta.Add(metadata);
            }
            projectMeta.commands = commandsMeta.OrderBy(c => string.IsNullOrEmpty(c.alias) ? c.id : c.alias).ToList();

            List<ProjectMetadata.ParameterMetadata> ExtractParamsMeta (Type commandType, Func<FieldInfo, string> summaryResolver)
            {
                var result = new List<ProjectMetadata.ParameterMetadata>();
                var fieldInfos = commandType.GetFields(BindingFlags.Public | BindingFlags.Instance)
                    .Where(x => !x.IsSpecialName && !x.GetCustomAttributes<ObsoleteAttribute>().Any())
                    .Where(f => f.FieldType.GetInterface(nameof(ICommandParameter)) != null).ToArray();

                foreach (var fieldInfo in fieldInfos)
                {
                    // Extracting parameter properties.
                    var id = fieldInfo.Name;
                    var alias = fieldInfo.CustomAttributes.FirstOrDefault(a => a.AttributeType == typeof(Command.ParameterAliasAttribute))?.ConstructorArguments[0].Value as string;
                    var nameless = alias == string.Empty;
                    var required = fieldInfo.CustomAttributes.Any(a => a.AttributeType == typeof(Command.RequiredParameterAttribute));
                    var summary = summaryResolver(fieldInfo);
                    var resourcePathPrefix = fieldInfo.CustomAttributes.FirstOrDefault(a => a.AttributeType == typeof(IDEResourceAttribute))?.ConstructorArguments[0].Value as string;
                    var resourcePathPrefixNamedId = resourcePathPrefix != null ? (int)fieldInfo.CustomAttributes.First(a => a.AttributeType == typeof(IDEResourceAttribute)).ConstructorArguments[1].Value : -1;
                    var actorPathPrefix = fieldInfo.CustomAttributes.FirstOrDefault(a => a.AttributeType == typeof(IDEActorAttribute))?.ConstructorArguments[0].Value as string;
                    var actorPathPrefixNamedId = actorPathPrefix != null ? (int)fieldInfo.CustomAttributes.First(a => a.AttributeType == typeof(IDEActorAttribute)).ConstructorArguments[1].Value : -1;
                    var appearance = fieldInfo.CustomAttributes.Any(a => a.AttributeType == typeof(IDEAppearanceAttribute));
                    var appearanceNamedId = appearance ? (int)fieldInfo.CustomAttributes.First(a => a.AttributeType == typeof(IDEAppearanceAttribute)).ConstructorArguments[0].Value : -1;
                    var constant = fieldInfo.CustomAttributes.FirstOrDefault(a => a.AttributeType == typeof(IDEConstantAttribute))?.ConstructorArguments[0].Value as string;
                    var constantNamedId = constant != null ? (int)fieldInfo.CustomAttributes.First(a => a.AttributeType == typeof(IDEConstantAttribute)).ConstructorArguments[1].Value : -1;
                    var defaultValue = fieldInfo.CustomAttributes.FirstOrDefault(a => a.AttributeType == typeof(Command.ParameterDefaultValueAttribute))?.ConstructorArguments[0].Value as string;
                    var localizable = fieldInfo.CustomAttributes.Any(a => a.AttributeType == typeof(Command.LocalizableParameterAttribute));

                    // Extracting parameter value type.
                    string ResolveValueType (Type type)
                    {
                        var valueTypeName = type.GetInterface("INullable`1")?.GetGenericArguments()[0].Name;
                        switch (valueTypeName)
                        {
                            case "String":
                            case "NullableString": return "string";
                            case "Int32":
                            case "NullableInteger": return "int";
                            case "Single":
                            case "NullableFloat": return "float";
                            case "Boolean":
                            case "NullableBoolean": return "bool";
                        }
                        return null;
                    }

                    var dataType = new ProjectMetadata.DataType();
                    var paramType = fieldInfo.FieldType;
                    var isLiteral = ResolveValueType(paramType) != null;
                    if (isLiteral)
                    {
                        dataType.kind = "literal";
                        dataType.contentType = ResolveValueType(paramType);
                    }
                    else if (paramType.GetInterface("IEnumerable") != null)
                    {
                        var elementType = paramType.GetInterface("INullable`1").GetGenericArguments()[0].GetGenericArguments()[0];
                        var namedElementType = elementType.BaseType?.GetGenericArguments()[0];
                        if (namedElementType?.GetInterface("INamedValue") != null) // Treating arrays of named liters as maps for the parser.
                        {
                            dataType.kind = "map";
                            dataType.contentType = ResolveValueType(namedElementType.GetInterface("INamed`1").GetGenericArguments()[0]);
                        }
                        else
                        {
                            dataType.kind = "array";
                            dataType.contentType = ResolveValueType(elementType);
                        }
                    }
                    else
                    {
                        dataType.kind = "namedLiteral";
                        dataType.contentType = ResolveValueType(paramType.GetInterface("INullable`1").GetGenericArguments()[0].GetInterface("INamed`1").GetGenericArguments()[0]);
                    }

                    result.Add(new ProjectMetadata.ParameterMetadata {
                        id = id,
                        alias = alias,
                        nameless = nameless,
                        required = required,
                        dataType = dataType,
                        localizable = localizable,
                        defaultValue = defaultValue,
                        summary = summary,
                        resourcePathPrefix = resourcePathPrefix,
                        resourcePathPrefixNamedId = resourcePathPrefixNamedId,
                        actorPathPrefix = actorPathPrefix,
                        actorPathPrefixNamedId = actorPathPrefixNamedId,
                        appearance = appearance,
                        appearanceNamedId = appearanceNamedId,
                        constant = constant,
                        constantNamedId = constantNamedId
                    });
                }

                return result;
            }
        }
    }
}
