// Copyright 2017-2021 Elringus (Artyom Sovetnikov). All rights reserved.

using System;
using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// Allows scaling an arbitrary value to match current orthographic camera size.
    /// </summary>
    public abstract class CameraMatcher
    {
        /// <summary>
        /// Current match mode.
        /// </summary>
        public CameraMatchMode MatchMode { get; set; }
        /// <summary>
        /// Current match ratio to use in case <see cref="CameraMatchMode.Custom"/> mode is active.
        /// </summary>
        public float CustomMatchRatio { get; set; }

        /// <summary>
        /// Current size (in units) to match against camera.
        /// </summary>
        protected abstract Vector2 ReferenceSize { get; }
        /// <summary>
        /// Whether matching should be performed at this time.
        /// </summary>
        protected virtual bool ShouldMatch => cameraManager?.Camera != null && cameraManager.Camera.orthographic;

        private const float logBase = 2f;

        private readonly Timer timer;
        private readonly UnityEngine.Object target;
        private readonly float updateDelay;
        private readonly ICameraManager cameraManager;

        private Vector2 previousCameraSize;
        private Vector2 previousReferenceSize;

        protected CameraMatcher (ICameraManager cameraManager, float updateDelay, UnityEngine.Object target = default)
        {
            this.cameraManager = cameraManager;
            this.updateDelay = updateDelay;
            this.target = target;
            timer = new Timer(onLoop: Match);
        }

        /// <summary>
        /// Begins the matching process.
        /// </summary>
        public virtual void Start () => timer.Run(updateDelay, true, true, target: target);

        /// <summary>
        /// Stops the matching process.
        /// </summary>
        public virtual void Stop () => timer.Stop();

        /// <summary>
        /// Invoked when reference size should be scaled to match current camera size.
        /// </summary>
        /// <param name="scaleFactor">Modifier to apply for the reference size.</param>
        protected abstract void ApplyScale (float scaleFactor);

        protected virtual void Match ()
        {
            if (!ShouldMatch) return;

            var cameraSize = GetCameraSize();
            if (cameraSize == previousCameraSize &&
                ReferenceSize == previousReferenceSize) return;

            var scaleFactor = 0f;
            switch (MatchMode)
            {
                case CameraMatchMode.Crop:
                    scaleFactor = Mathf.Max(cameraSize.x / ReferenceSize.x, cameraSize.y / ReferenceSize.y);
                    break;
                case CameraMatchMode.Fit:
                    scaleFactor = Mathf.Min(cameraSize.x / ReferenceSize.x, cameraSize.y / ReferenceSize.y);
                    break;
                case CameraMatchMode.Custom:
                    var logWidth = Mathf.Log(cameraSize.x / ReferenceSize.x, logBase);
                    var logHeight = Mathf.Log(cameraSize.y / ReferenceSize.y, logBase);
                    var logWeightedAverage = Mathf.Lerp(logWidth, logHeight, CustomMatchRatio);
                    scaleFactor = Mathf.Pow(logBase, logWeightedAverage);
                    break;
                case CameraMatchMode.Disable:
                    throw new Exception("Shouldn't run matchers when matching is disabled.");
                default:
                    throw new Exception($"Unsupported match mode: `{MatchMode}`.");
            }

            ApplyScale(scaleFactor);

            previousCameraSize = cameraSize;
            previousReferenceSize = ReferenceSize;
        }

        protected virtual Vector2 GetCameraSize ()
        {
            var cameraHeight = cameraManager.Configuration.SceneRect.height;
            var cameraWidth = cameraHeight * cameraManager.Camera.aspect;
            return new Vector2(cameraWidth, cameraHeight);
        }
    }
}
