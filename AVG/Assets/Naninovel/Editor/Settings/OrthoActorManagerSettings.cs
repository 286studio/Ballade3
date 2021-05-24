// Copyright 2017-2021 Elringus (Artyom Sovetnikov). All rights reserved.

using System;
using System.Collections.Generic;
using UnityEditor;

namespace Naninovel
{
    public abstract class OrthoActorManagerSettings<TConfig, TActor, TMeta> : ActorManagerSettings<TConfig, TActor, TMeta>
        where TConfig : OrthoActorManagerConfiguration<TMeta>
        where TActor : IActor
        where TMeta : OrthoActorMetadata
    {
        protected override Dictionary<string, Action<SerializedProperty>> OverrideMetaDrawers ()
        {
            var drawers = base.OverrideMetaDrawers();
            drawers[nameof(OrthoActorMetadata.Pivot)] = p => { if (ResourcesTypeConstraint != null) EditorGUILayout.PropertyField(p); };
            drawers[nameof(OrthoActorMetadata.PixelsPerUnit)] = p => { if (ResourcesTypeConstraint != null) EditorGUILayout.PropertyField(p); };
            drawers[nameof(OrthoActorMetadata.EnableDepthPass)] = p => { if (ResourcesTypeConstraint != null) EditorGUILayout.PropertyField(p); };
            drawers[nameof(OrthoActorMetadata.DepthAlphaCutoff)] = p => { if (ResourcesTypeConstraint != null && EditedMetadata.EnableDepthPass) EditorGUILayout.PropertyField(p); };
            drawers[nameof(OrthoActorMetadata.CustomTextureShader)] = p => { if (ResourcesTypeConstraint != null && !typeof(GenericActorBehaviour).IsAssignableFrom(ResourcesTypeConstraint)) EditorGUILayout.PropertyField(p); };
            drawers[nameof(OrthoActorMetadata.CustomSpriteShader)] = p => { if (ResourcesTypeConstraint != null && !typeof(GenericActorBehaviour).IsAssignableFrom(ResourcesTypeConstraint) && !EditedMetadata.RenderTexture) EditorGUILayout.PropertyField(p); };
            drawers[nameof(OrthoActorMetadata.RenderTexture)] = p => { if (ResourcesTypeConstraint != null && ResourcesTypeConstraint != typeof(GenericActorBehaviour)) EditorGUILayout.PropertyField(p); };
            drawers[nameof(OrthoActorMetadata.CorrectRenderAspect)] = p => { if (ResourcesTypeConstraint != typeof(GenericActorBehaviour) && EditedMetadata.RenderTexture) EditorGUILayout.PropertyField(p); };
            return drawers;
        }
    }
}
