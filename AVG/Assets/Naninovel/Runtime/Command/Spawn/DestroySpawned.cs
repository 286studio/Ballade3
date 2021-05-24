// Copyright 2017-2021 Elringus (Artyom Sovetnikov). All rights reserved.

using UniRx.Async;

namespace Naninovel.Commands
{
    /// <summary>
    /// Destroys an object spawned with [@spawn] command.
    /// </summary>
    /// <remarks>
    /// If prefab has a `MonoBehaviour` component attached the root object, and the component implements
    /// a `IParameterized` interface, will pass the specified `params` values before destroying the object;
    /// if the component implements `IAwaitable` interface, command execution will wait for
    /// the async completion task returned by the implementation before destroying the object.
    /// </remarks>
    [CommandAlias("despawn")]
    public class DestroySpawned : Command
    {
        public interface IParameterized { void SetDestroyParameters (string[] parameters); }
        public interface IAwaitable { UniTask AwaitDestroyAsync (CancellationToken cancellationToken = default); }

        /// <summary>
        /// Name (path) of the prefab resource to destroy.
        /// A [@spawn] command with the same parameter is expected to be executed before.
        /// </summary>
        [ParameterAlias(NamelessParameterAlias), RequiredParameter]
        public StringParameter Path;
        /// <summary>
        /// Parameters to set before destroying the prefab.
        /// Requires the prefab to have a `IParameterized` component attached the root object.
        /// </summary>
        public StringListParameter Params;

        protected virtual ISpawnManager SpawnManager => Engine.GetService<ISpawnManager>();

        public override async UniTask ExecuteAsync (CancellationToken cancellationToken = default)
        {
            if (!SpawnManager.IsObjectSpawned(Path))
            {
                LogWarningWithPosition($"Failed to destroy spawned object '{Path}': the object is not found.");
                return;
            }

            await SpawnManager.DestroySpawnedAsync(Path, cancellationToken, Params);
        }
    }
}
