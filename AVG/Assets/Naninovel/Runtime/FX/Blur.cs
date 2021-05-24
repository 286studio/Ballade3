// Copyright 2017-2021 Elringus (Artyom Sovetnikov). All rights reserved.

using System.Linq;
using Naninovel.Commands;
using UniRx.Async;
using UnityEngine;

namespace Naninovel.FX
{
    public class Blur : MonoBehaviour, Spawn.IParameterized, Spawn.IAwaitable, DestroySpawned.IParameterized, DestroySpawned.IAwaitable
    {
        public interface IBlurable
        {
            UniTask BlurAsync (float intensity, float duration, EasingType easingType = default, CancellationToken cancellationToken = default);
        }

        protected string ActorId { get; private set; }
        protected float Intensity { get; private set; }
        protected float Duration { get; private set; }
        protected float StopDuration { get; private set; }

        [SerializeField] private string defaultActorId = "MainBackground";
        [SerializeField] private float defaultIntensity = .5f;
        [SerializeField] private float defaultDuration = 1f;

        public virtual void SetSpawnParameters (string[] parameters)
        {
            ActorId = parameters?.ElementAtOrDefault(0) ?? defaultActorId;
            Intensity = Mathf.Abs(parameters?.ElementAtOrDefault(1)?.AsInvariantFloat() ?? defaultIntensity);
            Duration = Mathf.Abs(parameters?.ElementAtOrDefault(2)?.AsInvariantFloat() ?? defaultDuration);
        }

        public async UniTask AwaitSpawnAsync (CancellationToken cancellationToken = default)
        {
            var actor = FindActor(ActorId);
            if (actor is null) return;
            var duration = cancellationToken.CancelLazy ? 0 : Duration;
            await actor.BlurAsync(Intensity, duration, EasingType.SmoothStep, cancellationToken);
        }

        public void SetDestroyParameters (string[] parameters)
        {
            StopDuration = Mathf.Abs(parameters?.ElementAtOrDefault(0)?.AsInvariantFloat() ?? defaultDuration);
        }

        public async UniTask AwaitDestroyAsync (CancellationToken cancellationToken = default)
        {
            var actor = FindActor(ActorId);
            if (actor is null) return;
            var duration = cancellationToken.CancelLazy ? 0 : StopDuration;
            await actor.BlurAsync(0, duration, EasingType.SmoothStep, cancellationToken);
        }

        private void OnDestroy () // Required to disable the effect on rollback.
        {
            FindActor(ActorId, false)?.BlurAsync(0, 0);
        }

        private static IBlurable FindActor (string actorId, bool logError = true)
        {
            var manager = Engine.GetAllServices<IActorManager>(c => c.ActorExists(actorId)).FirstOrDefault();
            if (manager is null)
            {
                if (logError) Debug.LogError($"Failed to apply blur effect: Can't find `{actorId}` actor");
                return null;
            }
            var blurable = manager.GetActor(actorId) as IBlurable;
            if (blurable is null)
            {
                if (logError) Debug.LogError($"Failed to apply blur effect: `{actorId}` actor doesn't support blur effect.");
                return null;
            }
            return blurable;
        }
    }
}
