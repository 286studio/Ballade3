// Copyright 2017-2021 Elringus (Artyom Sovetnikov). All rights reserved.

using System;
using System.Collections.Generic;
using Naninovel.FX;
using UniRx.Async;
using UnityEngine;
using UnityEngine.Video;

namespace Naninovel
{
    /// <summary>
    /// A <see cref="IActor"/> implementation using <see cref="VideoClip"/> to represent the actor.
    /// </summary>
    public abstract class VideoActor<TMeta> : MonoBehaviourActor<TMeta>, Blur.IBlurable
        where TMeta : OrthoActorMetadata
    {
        public override string Appearance { get => appearance; set => SetAppearance(value); }
        public override bool Visible { get => visible; set => SetVisibility(value); }

        protected virtual TransitionalRenderer TransitionalRenderer { get; private set; }
        protected virtual Dictionary<string, VideoPlayer> PlayerMap { get; } = new Dictionary<string, VideoPlayer>();
        protected virtual int TextureDepthBuffer { get; } = 24;
        protected virtual RenderTextureFormat TextureFormat { get; } = RenderTextureFormat.ARGB32;

        // ReSharper disable once NotAccessedField.Local (Used in WebGL pragma)
        private readonly string streamExtension;

        private LocalizableResourceLoader<VideoClip> videoLoader;
        private string appearance;
        private bool visible;

        protected VideoActor (string id, TMeta metadata)
            : base(id, metadata)
        {
            streamExtension = Engine.GetConfiguration<ResourceProviderConfiguration>().VideoStreamExtension;
        }

        public override async UniTask InitializeAsync ()
        {
            await base.InitializeAsync();

            var providerManager = Engine.GetService<IResourceProviderManager>();
            var localizationManager = Engine.GetService<ILocalizationManager>();
            videoLoader = new LocalizableResourceLoader<VideoClip>(
                providerManager.GetProviders(ActorMetadata.Loader.ProviderTypes), providerManager,
                localizationManager, $"{ActorMetadata.Loader.PathPrefix}/{Id}");

            TransitionalRenderer = TransitionalRenderer.CreateFor(ActorMetadata, GameObject);

            SetVisibility(false);
        }

        public UniTask BlurAsync (float intensity, float duration, EasingType easingType = default, CancellationToken cancellationToken = default)
        {
            return TransitionalRenderer.BlurAsync(intensity, duration, easingType, cancellationToken);
        }

        public override async UniTask ChangeAppearanceAsync (string appearance, float duration, EasingType easingType = default,
            Transition? transition = default, CancellationToken cancellationToken = default)
        {
            this.appearance = appearance;

            if (string.IsNullOrEmpty(appearance)) return;

            var videoPlayer = await GetOrLoadVideoAsync(appearance);
            if (cancellationToken.CancelASAP) return;

            if (!videoPlayer.isPrepared)
            {
                videoPlayer.Prepare();
                while (!cancellationToken.CancelASAP && !videoPlayer.isPrepared)
                    await AsyncUtils.WaitEndOfFrame;
                if (cancellationToken.CancelASAP) return;
            }

            var previousTexture = videoPlayer.targetTexture;
            videoPlayer.targetTexture = RenderTexture.GetTemporary((int)videoPlayer.width, (int)videoPlayer.height, TextureDepthBuffer, TextureFormat);
            videoPlayer.Play();

            await TransitionalRenderer.TransitionToAsync(videoPlayer.targetTexture, duration, easingType, transition, cancellationToken);
            if (cancellationToken.CancelASAP) return;

            foreach (var kv in PlayerMap) // Make sure no other videos are playing.
                if (!kv.Key.EqualsFast(appearance))
                    kv.Value.Stop();

            if (previousTexture)
                RenderTexture.ReleaseTemporary(previousTexture);
        }

        public override async UniTask ChangeVisibilityAsync (bool visible, float duration, EasingType easingType = default, CancellationToken cancellationToken = default)
        {
            this.visible = visible;

            await TransitionalRenderer.FadeToAsync(visible ? TintColor.a : 0, duration, easingType, cancellationToken);
        }

        public override async UniTask HoldResourcesAsync (string appearance, object holder)
        {
            if (string.IsNullOrEmpty(appearance)) return;

            await GetOrLoadVideoAsync(appearance);
            videoLoader.Hold(appearance, this);
        }

        public override void ReleaseResources (string appearance, object holder)
        {
            if (string.IsNullOrEmpty(appearance)) return;

            if (!PlayerMap.TryGetValue(appearance, out var player)) return;

            player.Stop();
            RenderTexture.ReleaseTemporary(player.targetTexture);
            ObjectUtils.DestroyOrImmediate(player.gameObject);
            videoLoader.Release(appearance, holder);
            PlayerMap.Remove(appearance);
        }

        public override void Dispose ()
        {
            base.Dispose();

            foreach (var player in PlayerMap.Values)
            {
                if (player == null) continue;
                RenderTexture.ReleaseTemporary(player.targetTexture);
                ObjectUtils.DestroyOrImmediate(player.gameObject);
            }

            PlayerMap.Clear();
            videoLoader?.ReleaseAll(this);
        }

        protected virtual void SetAppearance (string appearance) => ChangeAppearanceAsync(appearance, 0).Forget();

        protected virtual void SetVisibility (bool visible) => ChangeVisibilityAsync(visible, 0).Forget();

        protected override Color GetBehaviourTintColor () => TransitionalRenderer.TintColor;

        protected override void SetBehaviourTintColor (Color tintColor)
        {
            if (!Visible) tintColor.a = TransitionalRenderer.TintColor.a;
            TransitionalRenderer.TintColor = tintColor;
        }

        protected virtual async UniTask<VideoPlayer> GetOrLoadVideoAsync (string videoName)
        {
            if (PlayerMap.ContainsKey(videoName)) return PlayerMap[videoName];

            var videoPlayer = Engine.CreateObject<VideoPlayer>(videoName);

            #if UNITY_WEBGL && !UNITY_EDITOR
            videoPlayer.source = VideoSource.Url;
            videoPlayer.url = PathUtils.Combine(Application.streamingAssetsPath, $"{ActorMetadata.Loader.PathPrefix}/{Id}/{videoName}") + streamExtension;
            await AsyncUtils.WaitEndOfFrame;
            #else
            var videoClip = await videoLoader.LoadAsync(videoName);
            if (!videoClip.Valid) throw new Exception($"Failed to load `{videoName}` resource for `{Id}` video actor. Make sure the video clip is assigned in the actor resources.");
            videoPlayer.source = VideoSource.VideoClip;
            videoPlayer.clip = videoClip;
            #endif

            videoPlayer.playOnAwake = false;
            videoPlayer.isLooping = true;
            videoPlayer.renderMode = VideoRenderMode.RenderTexture;
            videoPlayer.audioOutputMode = VideoAudioOutputMode.None;

            PlayerMap[videoName] = videoPlayer;

            return videoPlayer;
        }
    }
}
