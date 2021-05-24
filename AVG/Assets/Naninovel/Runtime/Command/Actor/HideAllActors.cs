// Copyright 2017-2021 Elringus (Artyom Sovetnikov). All rights reserved.

using System.Linq;
using UniRx.Async;

namespace Naninovel.Commands
{
    /// <summary>
    /// Hides (removes) all the actors (eg characters, backgrounds, text printers, choice handlers, etc) on scene.
    /// </summary>
    [CommandAlias("hideAll")]
    public class HideAllActors : Command
    {
        /// <summary>
        /// Duration (in seconds) of the fade animation. Default value: 0.35 seconds.
        /// </summary>
        [ParameterAlias("time"), ParameterDefaultValue("0.35")]
        public DecimalParameter Duration = .35f;

        public override async UniTask ExecuteAsync (CancellationToken cancellationToken = default)
        {
            var managers = Engine.GetAllServices<IActorManager>();
            await UniTask.WhenAll(managers.SelectMany(m => m.GetAllActors()).Select(a => a.ChangeVisibilityAsync(false, Duration, cancellationToken: cancellationToken)));
        }
    } 
}
