// Copyright 2017-2021 Elringus (Artyom Sovetnikov). All rights reserved.

using UniRx.Async;

namespace Naninovel
{
    /// <summary>
    /// A <see cref="ICharacterActor"/> implementation using <see cref="LayeredActorBehaviour"/> to represent the actor.
    /// </summary>
    [ActorResources(typeof(LayeredCharacterBehaviour), false)]
    public class LayeredCharacter : LayeredActor<LayeredCharacterBehaviour, CharacterMetadata>, ICharacterActor, Commands.LipSync.IReceiver
    {
        public CharacterLookDirection LookDirection
        {
            get => TransitionalRenderer.GetLookDirection(ActorMetadata.BakedLookDirection);
            set => TransitionalRenderer.SetLookDirection(value, ActorMetadata.BakedLookDirection);
        }

        private CharacterLipSyncer lipSyncer;

        public LayeredCharacter (string id, CharacterMetadata metadata)
            : base(id, metadata) { }

        public override async UniTask InitializeAsync ()
        {
            await base.InitializeAsync();

            lipSyncer = new CharacterLipSyncer(Id, Behaviour.NotifyIsSpeakingChanged);
        }

        public UniTask ChangeLookDirectionAsync (CharacterLookDirection lookDirection, float duration,
            EasingType easingType = default, CancellationToken cancellationToken = default)
        {
            return TransitionalRenderer.ChangeLookDirectionAsync(lookDirection,
                ActorMetadata.BakedLookDirection, duration, easingType, cancellationToken);
        }

        public override void Dispose ()
        {
            base.Dispose();

            lipSyncer?.Dispose();
        }

        public void AllowLipSync (bool active) => lipSyncer.SyncAllowed = active;
    }
}
