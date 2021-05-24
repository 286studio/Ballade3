// Copyright 2017-2021 Elringus (Artyom Sovetnikov). All rights reserved.

using UniRx.Async;

namespace Naninovel.Commands
{
    /// <summary>
    /// Stops playback of the currently played voice clip.
    /// </summary>
    public class StopVoice : AudioCommand
    {
        public override UniTask ExecuteAsync (CancellationToken cancellationToken = default)
        {
            AudioManager.StopVoice();
            return UniTask.CompletedTask;
        }
    } 
}
