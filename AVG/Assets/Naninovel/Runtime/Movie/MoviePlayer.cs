// Copyright 2017-2021 Elringus (Artyom Sovetnikov). All rights reserved.

using System;
using System.Threading;
using UniRx.Async;
using UnityEngine;
using UnityEngine.Video;

namespace Naninovel
{
    /// <inheritdoc cref="IMoviePlayer"/>
    [InitializeAtRuntime]
    public class MoviePlayer : IMoviePlayer
    {
        public event Action OnMoviePlay;
        public event Action OnMovieStop;
        public event Action<Texture> OnMovieTextureReady;

        public virtual MoviesConfiguration Configuration { get; }
        public virtual bool Playing => playCTS != null && !playCTS.IsCancellationRequested;
        public virtual Texture2D FadeTexture { get; }

        protected virtual VideoPlayer Player { get; private set; }

        private readonly IInputManager inputManager;
        private readonly IResourceProviderManager providerManager;
        private readonly ILocalizationManager localeManager;
        private LocalizableResourceLoader<VideoClip> videoLoader;
        private CancellationTokenSource playCTS;
        private string playedMovieName;
        private IInputSampler cancelInput;
        // ReSharper disable once NotAccessedField.Local (Used in WebGL pragma)
        private string streamExtension;

        public MoviePlayer (MoviesConfiguration config, IResourceProviderManager providerManager, ILocalizationManager localeManager, IInputManager inputManager)
        {
            this.Configuration = config;
            this.providerManager = providerManager;
            this.localeManager = localeManager;
            this.inputManager = inputManager;

            FadeTexture = config.CustomFadeTexture ? config.CustomFadeTexture : Engine.LoadInternalResource<Texture2D>("Textures/Black");
        }

        public virtual UniTask InitializeServiceAsync ()
        {
            videoLoader = Configuration.Loader.CreateLocalizableFor<VideoClip>(providerManager, localeManager);
            streamExtension = Engine.GetConfiguration<ResourceProviderConfiguration>().VideoStreamExtension;
            cancelInput = inputManager.GetCancel();

            Player = Engine.CreateObject<VideoPlayer>(nameof(MoviePlayer));
            Player.playOnAwake = false;
            Player.skipOnDrop = Configuration.SkipFrames;
            #if UNITY_WEBGL && !UNITY_EDITOR
            Player.source = VideoSource.Url;
            #else
            Player.source = VideoSource.VideoClip;
            #endif
            Player.renderMode = VideoRenderMode.APIOnly;
            Player.isLooping = false;
            Player.audioOutputMode = VideoAudioOutputMode.Direct;
            Player.loopPointReached += HandleLoopPointReached;

            if (Configuration.SkipOnInput && cancelInput != null)
                cancelInput.OnStart += Stop;

            return UniTask.CompletedTask;
        }

        public virtual void ResetService ()
        {
            if (Playing) Stop();
            videoLoader?.ReleaseAll(this);
        }

        public virtual void DestroyService ()
        {
            if (Playing) Stop();
            if (Player != null) Player.loopPointReached -= HandleLoopPointReached;
            if (cancelInput != null) cancelInput.OnStart -= Stop;
            videoLoader?.ReleaseAll(this);
        }

        public virtual async UniTask PlayAsync (string movieName, CancellationToken cancellationToken = default)
        {
            if (Playing) Stop();

            playedMovieName = movieName;
            playCTS = cancellationToken.CreateLinkedTokenSource();

            OnMoviePlay?.Invoke();
            await UniTask.Delay(TimeSpan.FromSeconds(Configuration.FadeDuration));
            if (cancellationToken.CancelASAP) return;

            #if UNITY_WEBGL && !UNITY_EDITOR
            Player.url = PathUtils.Combine(Application.streamingAssetsPath, $"{Configuration.Loader.PathPrefix}/{movieName}") + streamExtension;
            #else
            var videoClipResource = await videoLoader.LoadAndHoldAsync(movieName, this);
            if (cancellationToken.CancelASAP) return;
            if (!videoClipResource.Valid) throw new Exception($"Failed to load `{movieName}` movie.");
            Player.clip = videoClipResource;
            #endif

            Player.Prepare();
            while (!Player.isPrepared) await AsyncUtils.WaitEndOfFrame;
            if (cancellationToken.CancelASAP) return;
            OnMovieTextureReady?.Invoke(Player.texture);

            Player.Play();
            while (Playing) await AsyncUtils.WaitEndOfFrame;
        }

        public virtual void Stop ()
        {
            if (Player) Player.Stop();
            playCTS?.Cancel();
            playCTS?.Dispose();
            playCTS = null;

            videoLoader?.Release(playedMovieName, this);
            playedMovieName = null;

            OnMovieStop?.Invoke();
        }

        #if UNITY_WEBGL && !UNITY_EDITOR
        public virtual UniTask HoldResourcesAsync (string movieName, object holder) => UniTask.CompletedTask;
        #else
        public virtual async UniTask HoldResourcesAsync (string movieName, object holder)
        {
            await videoLoader.LoadAndHoldAsync(movieName, holder);
        }
        #endif

        public virtual void ReleaseResources (string movieName, object holder)
        {
            #if UNITY_WEBGL && !UNITY_EDITOR
            return;
            #else
            videoLoader?.Release(movieName, holder);
            #endif
        }

        private void HandleLoopPointReached (VideoPlayer source) => Stop();
    }
}
