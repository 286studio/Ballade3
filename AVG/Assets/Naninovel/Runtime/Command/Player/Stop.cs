// Copyright 2017-2021 Elringus (Artyom Sovetnikov). All rights reserved.

using UniRx.Async;

namespace Naninovel.Commands
{
    /// <summary>
    /// Stops the naninovel script execution.
    /// </summary>
    public class Stop : Command
    {
        public override UniTask ExecuteAsync (CancellationToken cancellationToken = default)
        {
            Engine.GetService<IScriptPlayer>().Stop();

            return UniTask.CompletedTask;
        }
    } 
}
