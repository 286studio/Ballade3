// Copyright 2017-2021 Elringus (Artyom Sovetnikov). All rights reserved.

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Naninovel
{
    public class EngineSettings : ConfigurationSettings<EngineConfiguration>
    {
        protected override Dictionary<string, Action<SerializedProperty>> OverrideConfigurationDrawers ()
        {
            var drawers = base.OverrideConfigurationDrawers();
            drawers[nameof(EngineConfiguration.CustomInitializationUI)] = p => { if (Configuration.ShowInitializationUI) EditorGUILayout.PropertyField(p); };
            drawers[nameof(EngineConfiguration.ObjectsLayer)] = property => {
                if (!Configuration.OverrideObjectsLayer) return;
                var label = EditorGUI.BeginProperty(Rect.zero, null, property);
                property.intValue = EditorGUILayout.LayerField(label, property.intValue);
            };
            drawers[nameof(EngineConfiguration.ToggleConsoleKey)] = p => { if (Configuration.EnableDevelopmentConsole) EditorGUILayout.PropertyField(p); };
            return drawers;
        }
    }
}
