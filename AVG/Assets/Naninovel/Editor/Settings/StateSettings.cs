// Copyright 2017-2021 Elringus (Artyom Sovetnikov). All rights reserved.

using System;
using System.Collections.Generic;
using UnityEditor;

namespace Naninovel
{
    public class StateSettings : ConfigurationSettings<StateConfiguration>
    {
        private static readonly string[] gameHandlerImplementations, gameHandlerImplementationsLabels;
        private static readonly string[] globalHandlerImplementations, globalHandlerImplementationsLabels;
        private static readonly string[] settingsHandlerImplementations, settingsHandlerImplementationsLabels;

        static StateSettings ()
        {
            InitializeImplementationOptions<ISaveSlotManager<GameStateMap>>(ref gameHandlerImplementations, ref gameHandlerImplementationsLabels);
            InitializeImplementationOptions<ISaveSlotManager<GlobalStateMap>>(ref globalHandlerImplementations, ref globalHandlerImplementationsLabels);
            InitializeImplementationOptions<ISaveSlotManager<SettingsStateMap>>(ref settingsHandlerImplementations, ref settingsHandlerImplementationsLabels);
        }
        
        protected override Dictionary<string, Action<SerializedProperty>> OverrideConfigurationDrawers ()
        {
            var drawers = base.OverrideConfigurationDrawers();
            drawers[nameof(StateConfiguration.StateRollbackSteps)] = p => { if (Configuration.EnableStateRollback) EditorGUILayout.PropertyField(p); };
            drawers[nameof(StateConfiguration.SavedRollbackSteps)] = p => { if (Configuration.EnableStateRollback) EditorGUILayout.PropertyField(p); };
            drawers[nameof(StateConfiguration.GameStateHandler)] = property =>
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Serialization Handlers", EditorStyles.boldLabel);
                DrawImplementationDropdown(property, gameHandlerImplementations, gameHandlerImplementationsLabels);
            };
            drawers[nameof(StateConfiguration.GlobalStateHandler)] = p => DrawImplementationDropdown(p, globalHandlerImplementations, globalHandlerImplementationsLabels);
            drawers[nameof(StateConfiguration.SettingsStateHandler)] = p => DrawImplementationDropdown(p, settingsHandlerImplementations, settingsHandlerImplementationsLabels);
            return drawers;
        }
    }
}
