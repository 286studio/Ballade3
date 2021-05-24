// Copyright 2017-2021 Elringus (Artyom Sovetnikov). All rights reserved.

using System.Linq;
using UniRx.Async;
using UnityEngine;

namespace Naninovel.FX
{
    /// <summary>
    /// Shakes a <see cref="ICharacterActor"/> with provided name or a random visible one.
    /// </summary>
    public class ShakeCharacter : ShakeTransform
    {
        [SerializeField] private bool preventPositiveYOffset = true;

        protected override Transform GetShakenTransform ()
        {
            var manager = Engine.GetService<ICharacterManager>();
            var id = string.IsNullOrEmpty(ObjectName) ? manager.GetAllActors().FirstOrDefault(a => a.Visible)?.Id : ObjectName;
            var go = GameObject.Find(id);
            return ObjectUtils.IsValid(go) ? go.transform : null;
        }

        protected override async UniTask ShakeSequenceAsync (CancellationToken cancellationToken)
        {
            if (!preventPositiveYOffset)
            {
                await base.ShakeSequenceAsync(cancellationToken);
                return;
            }

            var amplitude = DeltaPos + DeltaPos * Random.Range(-AmplitudeVariation, AmplitudeVariation);
            var duration = ShakeDuration + ShakeDuration * Random.Range(-DurationVariation, DurationVariation);

            await MoveAsync(InitialPos - amplitude * .5f, duration * .5f, cancellationToken);
            await MoveAsync(InitialPos, duration * .5f, cancellationToken);
        }
    }
}
