// Copyright 2017-2021 Elringus (Artyom Sovetnikov). All rights reserved.

using UniRx.Async;

namespace Naninovel.Commands
{
    /// <summary>
    /// Closes an [@if] conditional execution block.
    /// For usage examples see [conditional execution](/guide/naninovel-scripts.md#conditional-execution) guide.
    /// </summary>
    public class EndIf : Command
    {
        public override UniTask ExecuteAsync (CancellationToken cancellationToken = default) => UniTask.CompletedTask;
    }
}
