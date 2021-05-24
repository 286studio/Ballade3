// Copyright 2017-2021 Elringus (Artyom Sovetnikov). All rights reserved.

using UniRx.Async;

namespace Naninovel.Commands
{
    /// <summary>
    /// Instantiates a prefab or a [special effect](/guide/special-effects.md);
    /// when performed over an already spawned object, will update the spawn parameters instead.
    /// </summary>
    /// <remarks>
    /// If prefab has a `MonoBehaviour` component attached the root object, and the component implements
    /// a `IParameterized` interface, will pass the specified `params` values after the spawn;
    /// if the component implements `IAwaitable` interface, command execution will wait for
    /// the async completion task returned by the implementation.
    /// </remarks>
    public class Spawn : Command, Command.IPreloadable
    {
        public interface IParameterized { void SetSpawnParameters (string[] parameters); }
        public interface IAwaitable { UniTask AwaitSpawnAsync (CancellationToken cancellationToken = default); }

        /// <summary>
        /// Name (path) of the prefab resource to spawn.
        /// </summary>
        [ParameterAlias(NamelessParameterAlias), RequiredParameter, IDEResource(SpawnConfiguration.DefaultPathPrefix)]
        public StringParameter Path;
        /// <summary>
        /// Parameters to set when spawning the prefab.
        /// Requires the prefab to have a `IParameterized` component attached the root object.
        /// </summary>
        public StringListParameter Params;

        protected virtual ISpawnManager SpawnManager => Engine.GetService<ISpawnManager>();

        public async UniTask PreloadResourcesAsync ()
        {
            if (!Assigned(Path) || Path.DynamicValue || string.IsNullOrWhiteSpace(Path)) return;
            await SpawnManager.HoldResourcesAsync(Path, this);
        }

        public void ReleasePreloadedResources ()
        {
            if (!Assigned(Path) || Path.DynamicValue || string.IsNullOrWhiteSpace(Path)) return;
            SpawnManager.ReleaseResources(Path, this);
        }

        public override async UniTask ExecuteAsync (CancellationToken cancellationToken = default)
        {
            if (SpawnManager.IsObjectSpawned(Path)) // Update params if already spawned.
                await SpawnManager.UpdateSpawnedAsync(Path, cancellationToken, Params);
            else await SpawnManager.SpawnAsync(Path, cancellationToken, Params);
        }
    }
}
