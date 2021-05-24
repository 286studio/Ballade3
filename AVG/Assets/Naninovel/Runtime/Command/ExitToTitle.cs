// Copyright 2017-2021 Elringus (Artyom Sovetnikov). All rights reserved.

using UniRx.Async;

namespace Naninovel.Commands
{
    /// <summary>
    /// Resets engine state and shows `ITitleUI` UI (main menu).
    /// </summary>
    [CommandAlias("title")]
    public class ExitToTitle : Command, Command.IForceWait
    {
        public override async UniTask ExecuteAsync (CancellationToken cancellationToken = default)
        {
            var gameState = Engine.GetService<IStateManager>();
            var uiManager = Engine.GetService<IUIManager>();

            await gameState.ResetStateAsync();
            // Don't check for the cancellation token, as it's always cancelled after state reset.

            uiManager.GetUI<UI.ITitleUI>()?.Show();
        }
    }
}
