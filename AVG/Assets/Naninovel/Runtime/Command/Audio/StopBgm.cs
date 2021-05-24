// Copyright 2017-2021 Elringus (Artyom Sovetnikov). All rights reserved.

using UniRx.Async;

namespace Naninovel.Commands
{
    /// <summary>
    /// Stops playing a BGM (background music) track with the provided name.
    /// </summary>
    /// <remarks>
    /// When music track name (BgmPath) is not specified, will stop all the currently played tracks.
    /// </remarks>
    public class StopBgm : AudioCommand
    {
        /// <summary>
        /// Path to the music track to stop.
        /// </summary>
        [ParameterAlias(NamelessParameterAlias), IDEResource(AudioConfiguration.DefaultAudioPathPrefix)]
        public StringParameter BgmPath;
        /// <summary>
        /// Duration of the volume fade-out before stopping playback, in seconds (0.35 by default).
        /// </summary>
        [ParameterAlias("fade"), ParameterDefaultValue("0.35")]
        public DecimalParameter FadeOutDuration = 0.35f;

        public override async UniTask ExecuteAsync (CancellationToken cancellationToken = default)
        {
            if (Assigned(BgmPath)) await AudioManager.StopBgmAsync(BgmPath, FadeOutDuration, cancellationToken);
            else await AudioManager.StopAllBgmAsync(FadeOutDuration, cancellationToken);
        }
    } 
}
