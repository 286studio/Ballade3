// Copyright 2017-2021 Elringus (Artyom Sovetnikov). All rights reserved.

using UniRx.Async;

namespace Naninovel.Commands
{
    /// <summary>
    /// Holds script execution until user activates a `continue` input.
    /// Shortcut for `@wait i`.
    /// </summary>
    [CommandAlias("i")]
    public class WaitForInput : Command, Command.IForceWait
    {
        public override async UniTask ExecuteAsync (CancellationToken cancellationToken = default)
        {
            var waitCommand = new Wait { PlaybackSpot = PlaybackSpot };
            waitCommand.WaitMode = Commands.Wait.InputLiteral;
            await waitCommand.ExecuteAsync(cancellationToken);
        }
    }
}
