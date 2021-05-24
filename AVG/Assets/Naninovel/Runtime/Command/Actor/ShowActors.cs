// Copyright 2017-2021 Elringus (Artyom Sovetnikov). All rights reserved.

using System.Collections.Generic;
using System.Linq;
using UniRx.Async;

namespace Naninovel.Commands
{
    /// <summary>
    /// Shows (makes visible) actors (character, background, text printer, choice handler, etc) with the specified IDs.
    /// In case multiple actors with the same ID found (eg, a character and a printer), will affect only the first found one.
    /// </summary>
    [CommandAlias("show")]
    public class ShowActors : Command
    {
        /// <summary>
        /// IDs of the actors to show.
        /// </summary>
        [ParameterAlias(NamelessParameterAlias), RequiredParameter, IDEActor]
        public StringListParameter ActorIds;
        /// <summary>
        /// Duration (in seconds) of the fade animation. Default value: 0.35 seconds.
        /// </summary>
        [ParameterAlias("time"), ParameterDefaultValue("0.35")]
        public DecimalParameter Duration = .35f;

        public override async UniTask ExecuteAsync (CancellationToken cancellationToken = default)
        {
            var managers = Engine.GetAllServices<IActorManager>(c => ActorIds.Any(id => c.ActorExists(id)));

            var tasks = new List<UniTask>();
            foreach (var actorId in ActorIds)
                if (managers.FirstOrDefault(m => m.ActorExists(actorId)) is IActorManager manager)
                    tasks.Add(manager.GetActor(actorId).ChangeVisibilityAsync(true, Duration, cancellationToken: cancellationToken));
                else LogErrorWithPosition($"Failed to show `{actorId}` actor: can't find any managers with `{actorId}` actor.");

            await UniTask.WhenAll(tasks);
        }
    } 
}
