// Copyright 2017-2021 Elringus (Artyom Sovetnikov). All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UniRx.Async;
using UnityEngine;

namespace Naninovel
{
    /// <inheritdoc cref="ICameraManager"/>
    /// <remarks>Initialization order lowered, so the user could see something while waiting for the engine initialization.</remarks>
    [InitializeAtRuntime(-1)] 
    public class CameraManager : ICameraManager, IStatefulService<GameStateMap>, IStatefulService<SettingsStateMap>
    {
        [Serializable]
        public class Settings
        {
            public int QualityLevel = -1;
        }

        [Serializable]
        public class GameState
        {
            public Vector3 Offset = Vector3.zero;
            public Quaternion Rotation = Quaternion.identity;
            public float Zoom = 0f;
            public bool Orthographic = true;
            public CameraLookState LookMode = default;
            public CameraComponentState[] CameraComponents;
            public bool RenderUI = true;
        }

        public virtual CameraConfiguration Configuration { get; }
        public virtual Camera Camera { get; protected set; }
        public virtual Camera UICamera { get; protected set; }
        public virtual bool RenderUI
        {
            get => Configuration.UseUICamera ? UICamera.enabled : MaskUtils.GetLayer(Camera.cullingMask, uiLayer);
            set { if (Configuration.UseUICamera) UICamera.enabled = value; else Camera.cullingMask = MaskUtils.SetLayer(Camera.cullingMask, uiLayer, value); }
        }
        public virtual Vector3 Offset
        {
            get => offset;
            set { CompleteOffsetTween(); offset = value; ApplyOffset(value); }
        }
        public virtual Quaternion Rotation
        {
            get => rotation;
            set { CompleteRotationTween(); rotation = value; ApplyRotation(value); }
        }
        public virtual float Zoom
        {
            get => zoom;
            set { CompleteZoomTween(); zoom = value; ApplyZoom(value); }
        }
        public virtual bool Orthographic
        {
            get => Camera.orthographic;
            set { Camera.orthographic = value; Zoom = Zoom; }
        }
        public virtual int QualityLevel { get => QualitySettings.GetQualityLevel(); set => QualitySettings.SetQualityLevel(value, true); }

        protected virtual CameraLookController LookController { get; private set; }

        private readonly IInputManager inputManager;
        private readonly IEngineBehaviour engineBehaviour;
        private readonly RenderTexture thumbnailRenderTexture;
        private readonly List<MonoBehaviour> cameraComponentsCache = new List<MonoBehaviour>();
        private readonly Tweener<VectorTween> offsetTweener = new Tweener<VectorTween>();
        private readonly Tweener<VectorTween> rotationTweener = new Tweener<VectorTween>();
        private readonly Tweener<FloatTween> zoomTweener = new Tweener<FloatTween>();
        private GameObject serviceObject;
        private Transform lookContainer;
        private Vector3 offset = Vector3.zero;
        private Quaternion rotation = Quaternion.identity;
        private float zoom = 0f;
        private float initialOrthoSize, initialFOV;
        private int uiLayer;

        public CameraManager (CameraConfiguration config, IInputManager inputManager, IEngineBehaviour engineBehaviour)
        {
            Configuration = config;
            this.inputManager = inputManager;
            this.engineBehaviour = engineBehaviour;
            
            thumbnailRenderTexture = new RenderTexture(config.ThumbnailResolution.x, config.ThumbnailResolution.y, 24);
        }

        public virtual UniTask InitializeServiceAsync ()
        {
            uiLayer = Engine.GetConfiguration<UIConfiguration>().ObjectsLayer;
            serviceObject = Engine.CreateObject(nameof(CameraManager));
            lookContainer = Engine.CreateObject("MainCameraLookContainer", parent: serviceObject.transform).transform;
            InitializeMainCamera();
            lookContainer.position = Configuration.InitialPosition;
            initialOrthoSize = Camera.orthographicSize;
            initialFOV = Camera.fieldOfView;
            InitializeUICamera();
            LookController = new CameraLookController(Camera.transform, inputManager.GetCameraLookX(), inputManager.GetCameraLookY());
            engineBehaviour.OnBehaviourUpdate += LookController.Update;

            return UniTask.CompletedTask;

            void InitializeMainCamera ()
            {
                if (Configuration.CustomCameraPrefab != null)
                {
                    Camera = Engine.Instantiate(Configuration.CustomCameraPrefab, parent: lookContainer);
                    Camera.transform.localPosition = Vector3.zero;
                    return;
                }
                
                Camera = Engine.CreateObject<Camera>("MainCamera", parent: lookContainer);
                Camera.depth = 0;
                Camera.backgroundColor = new Color32(35, 31, 32, 255);
                Camera.orthographic = true;
                Camera.orthographicSize = Configuration.SceneRect.height / 2;
                Camera.fieldOfView = 60f;
                if (!Configuration.UseUICamera)
                    Camera.allowHDR = false; // Otherwise text artifacts appear when printing.
                if (Engine.Configuration.OverrideObjectsLayer) // When culling is enabled, render only the engine object and UI (when not using UI camera) layers.
                    Camera.cullingMask = Configuration.UseUICamera ? 1 << Engine.Configuration.ObjectsLayer : (1 << Engine.Configuration.ObjectsLayer) | (1 << uiLayer);
                else if (Configuration.UseUICamera) Camera.cullingMask = ~(1 << uiLayer);
            }

            void InitializeUICamera ()
            {
                if (!Configuration.UseUICamera) return;
                
                if (Configuration.CustomUICameraPrefab != null)
                {
                    UICamera = Engine.Instantiate(Configuration.CustomUICameraPrefab, parent: serviceObject.transform);
                    UICamera.transform.position = Configuration.InitialPosition;
                    return;
                }
                
                UICamera = Engine.CreateObject<Camera>("UICamera", parent: serviceObject.transform);
                UICamera.depth = 1;
                UICamera.orthographic = true;
                UICamera.allowHDR = false; // Otherwise text artifacts appear when printing.
                UICamera.cullingMask = 1 << uiLayer;
                UICamera.clearFlags = CameraClearFlags.Depth;
                UICamera.transform.position = Configuration.InitialPosition;
            }
        }

        public virtual void ResetService ()
        {
            LookController.Enabled = false;
            Offset = Vector3.zero;
            Rotation = Quaternion.identity;
            Zoom = 0f;
            Orthographic = !Configuration.CustomCameraPrefab || Configuration.CustomCameraPrefab.orthographic;
        }

        public virtual void DestroyService ()
        {
            if (engineBehaviour != null)
                engineBehaviour.OnBehaviourUpdate -= LookController.Update;

            ObjectUtils.DestroyOrImmediate(thumbnailRenderTexture);
            ObjectUtils.DestroyOrImmediate(serviceObject);
        }

        public virtual void SaveServiceState (SettingsStateMap stateMap)
        {
            var settings = new Settings {
                QualityLevel = QualityLevel
            };
            stateMap.SetState(settings);
        }

        public virtual UniTask LoadServiceStateAsync (SettingsStateMap stateMap)
        {
            var settings = stateMap.GetState<Settings>() ?? new Settings();
            if (settings.QualityLevel >= 0 && settings.QualityLevel != QualityLevel)
                QualityLevel = settings.QualityLevel;

            return UniTask.CompletedTask;
        }

        public virtual void SaveServiceState (GameStateMap stateMap)
        {
            Camera.gameObject.GetComponents(cameraComponentsCache);
            var gameState = new GameState {
                Offset = Offset,
                Rotation = Rotation,
                Zoom = Zoom,
                Orthographic = Orthographic,
                LookMode = LookController.GetState(),
                RenderUI = RenderUI,
                // Why zero? Camera is not a MonoBehaviour, so don't count it; others are considered to be custom effect.
                CameraComponents = cameraComponentsCache.Count > 0 ? cameraComponentsCache.Select(c => new CameraComponentState(c)).ToArray() : null
            };
            stateMap.SetState(gameState);
        }

        public virtual UniTask LoadServiceStateAsync (GameStateMap stateMap)
        {
            var state = stateMap.GetState<GameState>();
            if (state is null)
            {
                ResetService();
                return UniTask.CompletedTask;
            }
            
            Offset = state.Offset;
            Rotation = state.Rotation;
            Zoom = state.Zoom;
            Orthographic = state.Orthographic;
            RenderUI = state.RenderUI;
            SetLookMode(state.LookMode.Enabled, state.LookMode.Zone, state.LookMode.Speed, state.LookMode.Gravity);

            if (state.CameraComponents != null)
                foreach (var compState in state.CameraComponents)
                {
                    var comp = Camera.gameObject.GetComponent(compState.TypeName) as MonoBehaviour;
                    if (comp == null) continue;
                    comp.enabled = compState.Enabled;
                }

            return UniTask.CompletedTask;
        }

        public virtual void SetLookMode (bool enabled, Vector2 lookZone, Vector2 lookSpeed, bool gravity)
        {
            LookController.LookZone = lookZone;
            LookController.LookSpeed = lookSpeed;
            LookController.Gravity = gravity;
            LookController.Enabled = enabled;
        }

        public virtual Texture2D CaptureThumbnail ()
        {
            if (Configuration.HideUIInThumbnails)
                RenderUI = false;

            // Hide the save-load menu in case it's visible.
            var saveLoadUI = Engine.GetService<IUIManager>()?.GetUI<UI.ISaveLoadUI>();
            var saveLoadUIWasVisible = saveLoadUI?.Visible;
            if (saveLoadUIWasVisible.HasValue && saveLoadUIWasVisible.Value)
                saveLoadUI.Visible = false;

            // Confirmation UI may still be visible here (due to a fade-out time); force-hide it.
            var confirmUI = Engine.GetService<IUIManager>()?.GetUI<UI.IConfirmationUI>();
            var confirmUIWasVisible = confirmUI?.Visible ?? false;
            if (confirmUI != null) confirmUI.Visible = false;

            var initialRenderTexture = Camera.targetTexture;
            Camera.targetTexture = thumbnailRenderTexture;
            ForceTransitionalSpritesUpdate();
            Camera.Render();
            Camera.targetTexture = initialRenderTexture;

            if (RenderUI && Configuration.UseUICamera)
            {
                initialRenderTexture = UICamera.targetTexture;
                UICamera.targetTexture = thumbnailRenderTexture;
                UICamera.Render();
                UICamera.targetTexture = initialRenderTexture;
            }

            var thumbnail = thumbnailRenderTexture.ToTexture2D();

            // Restore the save-load menu and confirmation UI in case we hid them.
            if (saveLoadUIWasVisible.HasValue && saveLoadUIWasVisible.Value)
                saveLoadUI.Visible = true;
            if (confirmUIWasVisible)
                confirmUI.Visible = true;

            if (Configuration.HideUIInThumbnails)
                RenderUI = true;

            return thumbnail;

            void ForceTransitionalSpritesUpdate ()
            {
                var updateMethod = typeof(TransitionalSpriteRenderer).GetMethod("Update", BindingFlags.NonPublic | BindingFlags.Instance);
                if (updateMethod is null) throw new Exception("Failed to locate `Update` method of transitional sprite renderer.");
                var sprites = UnityEngine.Object.FindObjectsOfType<TransitionalSpriteRenderer>();
                foreach (var sprite in sprites)
                    updateMethod.Invoke(sprite, null);
            }
        }

        public virtual async UniTask ChangeOffsetAsync (Vector3 offset, float duration, EasingType easingType = default, CancellationToken cancellationToken = default)
        {
            CompleteOffsetTween();

            if (duration > 0)
            {
                var currentOffset = this.offset;
                this.offset = offset;
                var tween = new VectorTween(currentOffset, offset, duration, ApplyOffset, false, easingType, Camera);
                await offsetTweener.RunAsync(tween, cancellationToken);
            }
            else Offset = offset;
        }

        public virtual async UniTask ChangeRotationAsync (Quaternion rotation, float duration, EasingType easingType = default, CancellationToken cancellationToken = default)
        {
            CompleteRotationTween();

            if (duration > 0)
            {
                var currentRotation = this.rotation;
                this.rotation = rotation;
                var tween = new VectorTween(currentRotation.ClampedEulerAngles(), rotation.ClampedEulerAngles(), duration, ApplyRotation, false, easingType, Camera);
                await rotationTweener.RunAsync(tween, cancellationToken);
            }
            else Rotation = rotation;
        }

        public virtual async UniTask ChangeZoomAsync (float zoom, float duration, EasingType easingType = default, CancellationToken cancellationToken = default)
        {
            CompleteZoomTween();

            if (duration > 0)
            {
                var currentZoom = this.zoom;
                this.zoom = zoom;
                var tween = new FloatTween(currentZoom, zoom, duration, ApplyZoom, false, easingType, Camera);
                await zoomTweener.RunAsync(tween, cancellationToken);
            }
            else Zoom = zoom;
        }

        private void ApplyOffset (Vector3 offset)
        {
            lookContainer.position = Configuration.InitialPosition + offset;
        }

        private void ApplyRotation (Quaternion rotation)
        {
            lookContainer.rotation = rotation;
        }

        private void ApplyRotation (Vector3 rotation)
        {
            lookContainer.rotation = Quaternion.Euler(rotation);
        }

        private void ApplyZoom (float zoom)
        {
            if (Orthographic) Camera.orthographicSize = initialOrthoSize * (1f - Mathf.Clamp(zoom, 0, .99f));
            else Camera.fieldOfView = Mathf.Lerp(5f, initialFOV, 1f - zoom);
        }

        private void CompleteOffsetTween ()
        {
            if (offsetTweener.Running)
                offsetTweener.CompleteInstantly();
        }

        private void CompleteRotationTween ()
        {
            if (rotationTweener.Running)
                rotationTweener.CompleteInstantly();
        }

        private void CompleteZoomTween ()
        {
            if (zoomTweener.Running)
                zoomTweener.CompleteInstantly();
        }
    } 
}
