// Copyright 2017-2021 Elringus (Artyom Sovetnikov). All rights reserved.

using UniRx.Async;

namespace Naninovel.Commands
{
    /// <summary>
    /// Allows to enable or disable script player "skip" mode.
    /// </summary>
    public class Skip : Command
    {
        /// <summary>
        /// Whether to enable (default) or disable the skip mode.
        /// </summary>
        [ParameterAlias(NamelessParameterAlias), ParameterDefaultValue("true")]
        public BooleanParameter Enable = true;

        public override UniTask ExecuteAsync (CancellationToken cancellationToken = default)
        {
            var scriptPlayer = Engine.GetService<IScriptPlayer>();
            scriptPlayer.SetSkipEnabled(Enable);
            return UniTask.CompletedTask;
        }
    }
}
