// Copyright 2017-2021 Elringus (Artyom Sovetnikov). All rights reserved.

using System.Collections.Generic;
using System.Linq;
using UniRx.Async;
using UnityEngine;

namespace Naninovel
{
    /// <inheritdoc cref="ISpawnManager"/>
    [InitializeAtRuntime]
    public class SpawnManager : IStatefulService<GameStateMap>, ISpawnManager
    {
        [System.Serializable]
        public class GameState 
        { 
            public List<SpawnedObjectState> SpawnedObjects; 
        }

        private class SpawnedObject 
        { 
            public GameObject Object; 
            public SpawnedObjectState State; 
        }

        public virtual SpawnConfiguration Configuration { get; }

        private readonly List<SpawnedObject> spawnedObjects = new List<SpawnedObject>();
        private readonly IResourceProviderManager providersManager;
        private ResourceLoader<GameObject> loader;

        public SpawnManager (SpawnConfiguration config, IResourceProviderManager providersManager)
        {
            Configuration = config;
            this.providersManager = providersManager;
        }

        public virtual UniTask InitializeServiceAsync ()
        {
            loader = Configuration.Loader.CreateFor<GameObject>(providersManager);
            return UniTask.CompletedTask;
        }

        public virtual void ResetService ()
        {
            DestroyAllSpawnedObjects();
        }

        public virtual void DestroyService ()
        {
            DestroyAllSpawnedObjects();
        }

        public virtual void SaveServiceState (GameStateMap stateMap)
        {
            var state = new GameState {
                SpawnedObjects = spawnedObjects.Select(o => o.State).ToList()
            };
            stateMap.SetState(state);
        }

        public virtual UniTask LoadServiceStateAsync (GameStateMap stateMap)
        {
            var state = stateMap.GetState<GameState>();
            if (state?.SpawnedObjects?.Count > 0)
            {
                if (spawnedObjects.Count > 0)
                    foreach (var obj in spawnedObjects.ToList())
                        if (!state.SpawnedObjects.Exists(o => o.Path.EqualsFast(obj.State.Path)))
                            DestroySpawnedObject(obj.State.Path);

                foreach (var objState in state.SpawnedObjects)
                    if (!IsObjectSpawned(objState.Path))
                        SpawnAsync(objState.Path, CancellationToken.LazyCanceled, objState.Parameters).Forget();
                    else UpdateSpawnedAsync(objState.Path, CancellationToken.LazyCanceled, objState.Parameters).Forget();
            }
            else if (spawnedObjects.Count > 0) DestroyAllSpawnedObjects();
            return UniTask.CompletedTask;
        }

        public virtual async UniTask HoldResourcesAsync (string path, object holder)
        {
            var resourcePath = SpawnConfiguration.ProcessInputPath(path, out _);
            await loader.LoadAndHoldAsync(resourcePath, holder);
        }

        public virtual void ReleaseResources (string path, object holder)
        {
            var resourcePath = SpawnConfiguration.ProcessInputPath(path, out _);
            if (!loader.IsLoaded(resourcePath)) return;

            loader.Release(resourcePath, holder, false);
            if (loader.CountHolders(resourcePath) == 0)
            {
                if (IsObjectSpawned(path))
                    DestroySpawnedObject(path);
                loader.Release(resourcePath, holder);
            }
        }

        public virtual async UniTask SpawnAsync (string path, CancellationToken cancellationToken = default, params string[] parameters)
        {
            if (IsObjectSpawned(path))
            {
                Debug.LogWarning($"Object `{path}` is already spawned and can't be spawned again before it's destroyed.");
                return;
            }

            var resourcePath = SpawnConfiguration.ProcessInputPath(path, out _);
            var prefabResource = await loader.LoadAndHoldAsync(resourcePath, this);
            if (cancellationToken.CancelASAP) return;
            if (!prefabResource.Valid)
            {
                Debug.LogWarning($"Failed to spawn `{resourcePath}`: resource is not valid.");
                return;
            }

            var obj = Engine.Instantiate(prefabResource.Object, path);

            var spawnedObj = new SpawnedObject { Object = obj, State = new SpawnedObjectState(path, parameters) };
            spawnedObjects.Add(spawnedObj);

            var parameterized = obj.GetComponent<Commands.Spawn.IParameterized>();
            parameterized?.SetSpawnParameters(parameters);

            var awaitable = obj.GetComponent<Commands.Spawn.IAwaitable>();
            if (awaitable != null) await awaitable.AwaitSpawnAsync(cancellationToken);
        }

        public virtual async UniTask UpdateSpawnedAsync (string path, CancellationToken cancellationToken = default, params string[] parameters)
        {
            if (!IsObjectSpawned(path)) return;

            var spawnedData = GetSpawnedObject(path);
            spawnedData.State = new SpawnedObjectState(path, parameters);

            var parameterized = spawnedData.Object.GetComponent<Commands.Spawn.IParameterized>();
            parameterized?.SetSpawnParameters(parameters);

            var awaitable = spawnedData.Object.GetComponent<Commands.Spawn.IAwaitable>();
            if (awaitable != null) await awaitable.AwaitSpawnAsync(cancellationToken);
        }

        public virtual async UniTask<bool> DestroySpawnedAsync (string path, CancellationToken cancellationToken = default, params string[] parameters)
        {
            var spawnedObj = GetSpawnedObject(path);
            if (spawnedObj is null)
            {
                Debug.LogWarning($"Failed to destroy spawned object `{path}`: the object is not found.");
                return false;
            }

            var parameterized = spawnedObj.Object.GetComponent<Commands.DestroySpawned.IParameterized>();
            parameterized?.SetDestroyParameters(parameters);

            var awaitable = spawnedObj.Object.GetComponent<Commands.DestroySpawned.IAwaitable>();
            if (awaitable != null) await awaitable.AwaitDestroyAsync(cancellationToken);
            if (cancellationToken.CancelASAP) return false;

            return DestroySpawnedObject(path);
        }

        public virtual bool DestroySpawnedObject (string path)
        {
            var spawnedObj = GetSpawnedObject(path);
            if (spawnedObj is null)
            {
                Debug.LogWarning($"Failed to destroy spawned object `{path}`: the object is not found.");
                return false;
            }

            var removed = spawnedObjects?.Remove(spawnedObj);
            ObjectUtils.DestroyOrImmediate(spawnedObj.Object);

            var resourcePath = SpawnConfiguration.ProcessInputPath(path, out _);
            loader.Release(resourcePath, this);

            return removed ?? false;
        }

        public virtual void DestroyAllSpawnedObjects ()
        {
            foreach (var spawnedObj in spawnedObjects)
                ObjectUtils.DestroyOrImmediate(spawnedObj.Object);
            spawnedObjects.Clear();

            loader?.ReleaseAll(this);
        }

        public virtual bool IsObjectSpawned (string path)
        {
            return spawnedObjects?.Exists(o => o.State.Path.EqualsFast(path)) ?? false;
        }

        private SpawnedObject GetSpawnedObject (string path)
        {
            return spawnedObjects?.FirstOrDefault(o => o.State.Path.EqualsFast(path));
        }
    }
}
