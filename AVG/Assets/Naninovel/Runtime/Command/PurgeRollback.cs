// Copyright 2017-2021 Elringus (Artyom Sovetnikov). All rights reserved.

using UniRx.Async;

namespace Naninovel.Commands
{
    /// <summary>
    /// Prevents player from rolling back to the previous state snapshots.
    /// </summary>
    public class PurgeRollback : Command
    {
        public override UniTask ExecuteAsync (CancellationToken cancellationToken = default)
        {
            Engine.GetService<IStateManager>()?.PurgeRollbackData();
            return UniTask.CompletedTask;
        }
    }
}
