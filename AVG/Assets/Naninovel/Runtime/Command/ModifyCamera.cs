// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UniRx.Async;
using UnityEngine;

namespace Naninovel.Commands
{
    /// <summary>
    /// Modifies the main camera, changing offset, zoom level and rotation over time.
    /// Check [this video](https://youtu.be/zy28jaMss8w) for a quick demonstration of the command effect.
    /// </summary>
    /// <example>
    /// ; Offset over X-axis (pan) the camera by -3 units and offset over Y-axis by 1.5 units
    /// @camera offset:-3,1.5
    /// 
    /// ; Set camera in perspective mode, zoom-in by 50% and move back by 5 units
    /// @camera ortho:false offset:,,-5 zoom:0.5
    /// 
    /// ; Set camera in orthographic mode and roll by 10 degrees clock-wise
    /// @camera ortho:true roll:10
    /// 
    /// ; Offset, zoom and roll simultaneously animated over 5 seconds
    /// @camera offset:-3,1.5 zoom:0.5 roll:10 time:5
    /// 
    /// ; Instantly reset camera to the default state
    /// @camera offset:0,0 zoom:0 rotation:0,0,0 time:0
    /// 
    /// ; Toggle `FancyCameraFilter` and `Bloom` components attached to the camera
    /// @camera toggle:FancyCameraFilter,Bloom
    /// </example>
    [CommandAlias("camera")]
    public class ModifyCamera : Command
    {
        /// <summary>
        /// Local camera position offset in units by X,Y,Z axes.
        /// </summary>
        public DecimalListParameter Offset;
        /// <summary>
        /// Local camera rotation by Z-axis in angle degrees (0.0 to 360.0 or -180.0 to 180.0).
        /// The same as third component of `rotation` parameter; ignored when `rotation` is specified.
        /// </summary>
        public DecimalParameter Roll;
        /// <summary>
        /// Local camera rotation over X,Y,Z-axes in angle degrees (0.0 to 360.0 or -180.0 to 180.0).
        /// </summary>
        public DecimalListParameter Rotation;
        /// <summary>
        /// Relatize camera zoom (orthographic size or field of view, depending on the render mode), in 0.0 (no zoom) to 1.0 (full zoom) range.
        /// </summary>
        public DecimalParameter Zoom;
        /// <summary>
        /// Whether the camera should render in orthographic (true) or perspective (false) mode.
        /// </summary>
        [ParameterAlias("ortho")]
        public BooleanParameter Orthographic;
        /// <summary>
        /// Names of the components to toggle (enable if disabled and vice-versa). The components should be attached to the same gameobject as the camera.
        /// This can be used to toggle [custom post-processing effects](/guide/special-effects.md#camera-effects).
        /// </summary>
        [ParameterAlias("toggle")]
        public StringListParameter ToggleTypeNames;
        /// <summary>
        /// Name of the easing function to use for the modification.
        /// <br/><br/>
        /// Available options: Linear, SmoothStep, Spring, EaseInQuad, EaseOutQuad, EaseInOutQuad, EaseInCubic, EaseOutCubic, EaseInOutCubic, EaseInQuart, EaseOutQuart, EaseInOutQuart, EaseInQuint, EaseOutQuint, EaseInOutQuint, EaseInSine, EaseOutSine, EaseInOutSine, EaseInExpo, EaseOutExpo, EaseInOutExpo, EaseInCirc, EaseOutCirc, EaseInOutCirc, EaseInBounce, EaseOutBounce, EaseInOutBounce, EaseInBack, EaseOutBack, EaseInOutBack, EaseInElastic, EaseOutElastic, EaseInOutElastic.
        /// <br/><br/>
        /// When not specified, will use a default easing function set in the camera configuration settings.
        /// </summary>
        [ParameterAlias("easing")]
        public StringParameter EasingTypeName;
        /// <summary>
        /// Duration (in seconds) of the modification. Default value: 0.35 seconds.
        /// </summary>
        [ParameterAlias("time")]
        public DecimalParameter Duration = .35f;

        protected ICameraManager CameraManager => Engine.GetService<ICameraManager>();

        public override async UniTask ExecuteAsync (CancellationToken cancellationToken = default)
        {
            var easingType = CameraManager.Configuration.DefaultEasing;
            if (Assigned(EasingTypeName) && !Enum.TryParse(EasingTypeName, true, out easingType))
                LogWarningWithPosition($"Failed to parse `{EasingTypeName}` easing.");

            if (Assigned(Orthographic))
                CameraManager.Camera.orthographic = Orthographic;

            if (Assigned(ToggleTypeNames))
                foreach (var name in ToggleTypeNames)
                    ToggleComponent(name, CameraManager.Camera.gameObject);

            var tasks = new List<UniTask>();

            if (Assigned(Offset)) tasks.Add(CameraManager.ChangeOffsetAsync(ArrayUtils.ToVector3(Offset, Vector3.zero), Duration, easingType, cancellationToken));
            if (Assigned(Rotation)) tasks.Add(CameraManager.ChangeRotationAsync(Quaternion.Euler(
                Rotation.ElementAtOrDefault(0) ?? CameraManager.Rotation.eulerAngles.x,
                Rotation.ElementAtOrDefault(1) ?? CameraManager.Rotation.eulerAngles.y,
                Rotation.ElementAtOrDefault(2) ?? CameraManager.Rotation.eulerAngles.z), Duration, easingType, cancellationToken));
            else if (Assigned(Roll)) tasks.Add(CameraManager.ChangeRotationAsync(Quaternion.Euler(
                CameraManager.Rotation.eulerAngles.x, 
                CameraManager.Rotation.eulerAngles.y, 
                Roll), Duration, easingType, cancellationToken));
            if (Assigned(Zoom)) tasks.Add(CameraManager.ChangeZoomAsync(Zoom, Duration, easingType, cancellationToken));

            await UniTask.WhenAll(tasks);
        }

        private void ToggleComponent (string componentName, GameObject obj)
        {
            var cmp = obj.GetComponent(componentName) as MonoBehaviour;
            if (!cmp)
            {
                LogWithPosition($"Failed to toggle `{componentName}` camera component; the component is not found on the camera's gameobject.");
                return;
            }
            cmp.enabled = !cmp.enabled;
        }
    }
}
