// Copyright 2017-2021 Elringus (Artyom Sovetnikov). All rights reserved.

using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Naninovel
{
    [CustomEditor(typeof(LayeredActorBehaviour), true)]
    public class LayeredActorBehaviourEditor : Editor
    {
        private const string mapFieldName = "compositionMap";

        private void OnEnable ()
        {
            EditorApplication.contextualPropertyMenu += HandlePropertyContextMenu;
        }

        private void OnDisable ()
        {
            EditorApplication.contextualPropertyMenu -= HandlePropertyContextMenu;
        }

        private void HandlePropertyContextMenu (GenericMenu menu, SerializedProperty property)
        {
            if (property.propertyType != SerializedPropertyType.Generic ||
                !property.propertyPath.Contains($"{mapFieldName}.Array.data[")) return;

            var propertyCopy = property.Copy();
            menu.AddItem(new GUIContent("Preview Composition"), false, () =>
            {
                var targetObj = propertyCopy.serializedObject.targetObject as LayeredActorBehaviour;
                if (targetObj == null) return;
                var index = propertyCopy.propertyPath.GetAfterFirst($"{mapFieldName}.Array.data[").GetBefore("]").AsInvariantInt();
                if (index != null)
                {
                    targetObj.RebuildLayers();
                    var composition = targetObj.GetCompositionMap().Values.ElementAt(index.Value);
                    targetObj.ApplyComposition(composition);
                }

                EditorUtility.SetDirty(propertyCopy.serializedObject.targetObject);
            });
        }
    }
}
