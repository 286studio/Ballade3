// Copyright 2017-2021 Elringus (Artyom Sovetnikov). All rights reserved.

using UnityEngine;

namespace Naninovel
{
    public abstract class OrthoActorManagerConfiguration<TMeta> : ActorManagerConfiguration<TMeta>
        where TMeta : ActorMetadata
    {
        [Tooltip("Origin point used for reference when positioning actors on scene.")]
        public Vector2 SceneOrigin = new Vector2(.5f, 0f);
        [Tooltip("Initial Z-axis offset (depth) from actors to the camera to set when the actors are created.")]
        public float ZOffset = 100;
        [Tooltip("Distance by Z-axis to set between the actors when they are created; used to prevent z-fighting issues.")]
        public float ZStep = .1f;
    }
}
