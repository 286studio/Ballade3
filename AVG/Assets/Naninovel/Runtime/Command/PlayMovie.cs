// Copyright 2017-2021 Elringus (Artyom Sovetnikov). All rights reserved.

using UniRx.Async;

namespace Naninovel.Commands
{
    /// <summary>
    /// Plays a movie with the provided name (path).
    /// </summary>
    /// <remarks>
    /// Will fade-out the screen before playing the movie and fade back in after the play.
    /// Playback can be canceled by activating a `cancel` input (`Esc` key by default).
    /// </remarks>
    [CommandAlias("movie")]
    public class PlayMovie : Command, Command.IPreloadable
    {
        /// <summary>
        /// Name of the movie resource to play.
        /// </summary>
        [ParameterAlias(NamelessParameterAlias), RequiredParameter, IDEResource(MoviesConfiguration.DefaultPathPrefix)]
        public StringParameter MovieName;

        protected IMoviePlayer Player => Engine.GetService<IMoviePlayer>();

        public async UniTask PreloadResourcesAsync ()
        {
            if (!Assigned(MovieName) || MovieName.DynamicValue) return;
            await Player.HoldResourcesAsync(MovieName, this);
        }

        public void ReleasePreloadedResources ()
        {
            if (!Assigned(MovieName) || MovieName.DynamicValue) return;
            Player?.ReleaseResources(MovieName, this);
        }

        public override async UniTask ExecuteAsync (CancellationToken cancellationToken = default)
        {
            await Player.PlayAsync(MovieName, cancellationToken);
        }
    }
}
