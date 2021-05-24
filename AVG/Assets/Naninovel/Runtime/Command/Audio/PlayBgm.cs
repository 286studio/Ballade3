// Copyright 2017-2021 Elringus (Artyom Sovetnikov). All rights reserved.

using System.Linq;
using UniRx.Async;

namespace Naninovel.Commands
{
    /// <summary>
    /// Plays or modifies currently played [BGM (background music)](/guide/audio.md#background-music) track with the provided name.
    /// </summary>
    /// <remarks>
    /// Music tracks are looped by default.
    /// When music track name (BgmPath) is not specified, will affect all the currently played tracks.
    /// When invoked for a track that is already playing, the playback won't be affected (track won't start playing from the start),
    /// but the specified parameters (volume and whether the track is looped) will be applied.
    /// </remarks>
    [CommandAlias("bgm")]
    public class PlayBgm : AudioCommand, Command.IPreloadable
    {
        /// <summary>
        /// Path to the music track to play.
        /// </summary>
        [ParameterAlias(NamelessParameterAlias), IDEResource(AudioConfiguration.DefaultAudioPathPrefix)]
        public StringParameter BgmPath;
        /// <summary>
        /// Path to the intro music track to play once before the main track (not affected by the loop parameter).
        /// </summary>
        [ParameterAlias("intro"), IDEResource(AudioConfiguration.DefaultAudioPathPrefix)]
        public StringParameter IntroBgmPath;
        /// <summary>
        /// Volume of the music track.
        /// </summary>
        [ParameterDefaultValue("1")]
        public DecimalParameter Volume = 1f;
        /// <summary>
        /// Whether to play the track from beginning when it finishes.
        /// </summary>
        [ParameterDefaultValue("true")]
        public BooleanParameter Loop = true;
        /// <summary>
        /// Duration of the volume fade-in when starting playback, in seconds (0.0 by default); 
        /// doesn't have effect when modifying a playing track.
        /// </summary>
        [ParameterAlias("fade"), ParameterDefaultValue("0")]
        public DecimalParameter FadeInDuration = 0f;
        /// <summary>
        /// Audio mixer [group path](https://docs.unity3d.com/ScriptReference/Audio.AudioMixer.FindMatchingGroups) that should be used when playing the audio.
        /// </summary>
        [ParameterAlias("group")]
        public StringParameter GroupPath;
        /// <summary>
        /// Duration (in seconds) of the modification. Default value: 0.35 seconds.
        /// </summary>
        [ParameterAlias("time"), ParameterDefaultValue("0.35")]
        public DecimalParameter Duration = .35f;

        public async UniTask PreloadResourcesAsync ()
        {
            if (!Assigned(BgmPath) || BgmPath.DynamicValue) return;
            await AudioManager.AudioLoader.LoadAndHoldAsync(BgmPath, this);

            if (!Assigned(IntroBgmPath) || IntroBgmPath.DynamicValue) return;
            await AudioManager.AudioLoader.LoadAndHoldAsync(IntroBgmPath, this);
        }

        public void ReleasePreloadedResources ()
        {
            if (!Assigned(BgmPath) || BgmPath.DynamicValue) return;
            AudioManager?.AudioLoader?.Release(BgmPath, this);

            if (!Assigned(IntroBgmPath) || IntroBgmPath.DynamicValue) return;
            AudioManager?.AudioLoader?.Release(IntroBgmPath, this);
        }

        public override async UniTask ExecuteAsync (CancellationToken cancellationToken = default)
        {
            if (Assigned(BgmPath)) await PlayOrModifyTrackAsync(AudioManager, BgmPath, Volume, Loop, Duration, FadeInDuration, IntroBgmPath, GroupPath, cancellationToken);
            else await UniTask.WhenAll(AudioManager.GetPlayedBgmPaths().ToList().Select(path => PlayOrModifyTrackAsync(AudioManager, path, Volume, Loop, Duration, FadeInDuration, IntroBgmPath, null, cancellationToken)));
        }

        private static async UniTask PlayOrModifyTrackAsync (IAudioManager manager, string path, float volume, bool loop, float time, float fade, string introPath, string group, CancellationToken cancellationToken)
        {
            if (manager.IsBgmPlaying(path)) await manager.ModifyBgmAsync(path, volume, loop, time, cancellationToken);
            else await manager.PlayBgmAsync(path, volume, fade, loop, introPath, group, cancellationToken);
        }
    } 
}
