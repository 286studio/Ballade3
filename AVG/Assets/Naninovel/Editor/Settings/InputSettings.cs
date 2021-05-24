// Copyright 2017-2021 Elringus (Artyom Sovetnikov). All rights reserved.

using System;
using System.Collections.Generic;
using UnityEditor;

namespace Naninovel
{
    public class InputSettings : ConfigurationSettings<InputConfiguration>
    {
        protected override Dictionary<string, Action<SerializedProperty>> OverrideConfigurationDrawers ()
        {
            var drawers = base.OverrideConfigurationDrawers();
            drawers[nameof(InputConfiguration.CustomEventSystem)] = p => { if (Configuration.SpawnEventSystem) EditorGUILayout.PropertyField(p); };
            drawers[nameof(InputConfiguration.CustomInputModule)] = p => { if (Configuration.SpawnInputModule) EditorGUILayout.PropertyField(p); };
            return drawers;
        }
    }
}
