// Copyright 2017-2021 Elringus (Artyom Sovetnikov). All rights reserved.

using UniRx.Async;

namespace Naninovel.Commands
{
    /// <summary>
    /// Removes all the messages from [printer backlog](/guide/text-printers.md#printer-backlog).
    /// </summary>
    public class ClearBacklog : Command
    {
        public override UniTask ExecuteAsync (CancellationToken cancellationToken = default)
        {
            Engine.GetService<IUIManager>()?.GetUI<UI.IBacklogUI>()?.Clear();
            return UniTask.CompletedTask;
        }
    }
}
