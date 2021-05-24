// Copyright 2017-2021 Elringus (Artyom Sovetnikov). All rights reserved.

using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// Allows scaling PPU of a background rendered with <see cref="TransitionalSpriteRenderer"/> to match camera size.
    /// </summary>
    public class BackgroundMatcher : CameraMatcher
    {
        protected override Vector2 ReferenceSize => new Vector2(renderer.MainTexture.width, renderer.MainTexture.height) / metadata.PixelsPerUnit;
        protected override bool ShouldMatch => base.ShouldMatch && renderer != null && renderer.MainTexture != null;

        private const float updateDelay = .1f;

        private readonly TransitionalSpriteRenderer renderer;
        private readonly BackgroundMetadata metadata;

        public BackgroundMatcher (ICameraManager cameraManager, TransitionalSpriteRenderer renderer, BackgroundMetadata metadata)
            : base(cameraManager, updateDelay, renderer)
        {
            this.renderer = renderer;
            this.metadata = metadata;

            MatchMode = metadata.MatchMode;
            CustomMatchRatio = metadata.CustomMatchRatio;
        }

        /// <summary>
        /// Creates the matcher for a background actor with the provided metadata and renderer.
        /// Will return null in case matcher is not required based on the actor configuration.
        /// </summary>
        public static BackgroundMatcher CreateFor (BackgroundMetadata metadata, TransitionalRenderer renderer)
        {
            if (renderer is TransitionalSpriteRenderer spriteRenderer && metadata.MatchMode != CameraMatchMode.Disable)
            {
                var cameraManager = Engine.GetService<ICameraManager>();
                var matcher = new BackgroundMatcher(cameraManager, spriteRenderer, metadata);
                matcher.Start();
                return matcher;
            }
            return null;
        }

        protected override void ApplyScale (float scaleFactor)
        {
            renderer.PixelsPerUnit = Mathf.FloorToInt(metadata.PixelsPerUnit / scaleFactor);
        }

        protected override void Match ()
        {
            if (!ShouldMatch && renderer != null && renderer.PixelsPerUnit != metadata.PixelsPerUnit)
                renderer.PixelsPerUnit = metadata.PixelsPerUnit;

            base.Match();
        }
    }
}
