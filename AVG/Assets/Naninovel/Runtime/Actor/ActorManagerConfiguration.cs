// Copyright 2017-2021 Elringus (Artyom Sovetnikov). All rights reserved.

using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// Asset used to configure <see cref="IActorManager"/> services.
    /// </summary>
    public abstract class ActorManagerConfiguration : Configuration
    {
        [Tooltip("Easing function to use by default for all the actor modification animations (changing appearance, position, tint, etc).")]
        public EasingType DefaultEasing = EasingType.Linear;
        [Tooltip("Whether to automatically reveal (show) an actor when executing modification commands.")]
        public bool AutoShowOnModify = true;

        /// <summary>
        /// Attempts to retrieve metadata of an actor with the provided ID;
        /// when not found, will return a default metadata.
        /// </summary>
        public ActorMetadata GetMetadataOrDefault (string actorId) => GetMetadataNonGeneric(actorId);

        protected abstract ActorMetadata GetMetadataNonGeneric (string actorId);
    }

    /// <summary>
    /// Asset used to configure <see cref="IActorManager"/> services.
    /// </summary>
    /// <typeparam name="TMeta">Type of actor metadata configured service operates with.</typeparam>
    public abstract class ActorManagerConfiguration<TMeta> : ActorManagerConfiguration
        where TMeta : ActorMetadata
    {
        protected abstract TMeta DefaultActorMetadata { get; }
        protected abstract ActorMetadataMap<TMeta> ActorMetadataMap { get; }

        /// <summary>
        /// Attempts to retrieve metadata of an actor with the provided ID;
        /// when not found, will return a default metadata.
        /// </summary>
        public new TMeta GetMetadataOrDefault (string actorId)
        {
            return ActorMetadataMap.ContainsId(actorId) ? ActorMetadataMap[actorId] : DefaultActorMetadata;
        }

        protected override ActorMetadata GetMetadataNonGeneric (string actorId) => GetMetadataOrDefault(actorId);
    }
}
