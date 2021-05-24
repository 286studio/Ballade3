// Copyright 2017-2021 Elringus (Artyom Sovetnikov). All rights reserved.

using System;
using System.Collections.Generic;
using System.Threading;
using UniRx.Async;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Naninovel
{
    /// <inheritdoc cref="IInputSampler"/>
    public class InputSampler : IInputSampler
    {
        public event Action OnStart;
        public event Action OnEnd;

        public virtual InputBinding Binding { get; }
        public virtual bool Enabled { get; set; } = true;
        public virtual bool Active => Value != 0;
        public virtual float Value { get; private set; }
        public virtual bool StartedDuringFrame => Active && Time.frameCount == lastActiveFrame;
        public virtual bool EndedDuringFrame => !Active && Time.frameCount == lastActiveFrame;

        private readonly InputConfiguration config;
        private readonly HashSet<GameObject> objectTriggers;
        private UniTaskCompletionSource<bool> onInputTCS;
        private UniTaskCompletionSource onInputStartTCS, onInputEndTCS;
        private CancellationTokenSource onInputStartCTS, onInputEndCTS;
        private int lastActiveFrame;
        private float lastTouchTime;
        private Vector2 lastTouchBeganPosition;

        #if ENABLE_INPUT_SYSTEM && INPUT_SYSTEM_AVAILABLE
        private readonly UnityEngine.InputSystem.InputAction inputAction;
        #endif

        /// <param name="config">Input manager configuration asset.</param>
        /// <param name="binding">Binding to trigger input.</param>
        /// <param name="objectTriggers">Objects to trigger input.</param>
        public InputSampler (InputConfiguration config, InputBinding binding, IEnumerable<GameObject> objectTriggers)
        {
            Binding = binding;
            this.config = config;
            this.objectTriggers = objectTriggers != null ? new HashSet<GameObject>(objectTriggers) : new HashSet<GameObject>();

            #if ENABLE_INPUT_SYSTEM && INPUT_SYSTEM_AVAILABLE
            if (ObjectUtils.IsValid(config.InputActions))
                inputAction = config.InputActions.FindActionMap("Naninovel")?.FindAction(binding.Name);
            inputAction?.Enable();
            #endif
        }

        public virtual void AddObjectTrigger (GameObject obj) => objectTriggers.Add(obj);

        public virtual void RemoveObjectTrigger (GameObject obj) => objectTriggers.Remove(obj);

        public virtual async UniTask<bool> WaitForInputAsync ()
        {
            if (onInputTCS is null) onInputTCS = new UniTaskCompletionSource<bool>();
            return await onInputTCS.Task;
        }

        public virtual async UniTask WaitForInputStartAsync ()
        {
            if (onInputStartTCS is null) onInputStartTCS = new UniTaskCompletionSource();
            await onInputStartTCS.Task;
        }

        public virtual async UniTask WaitForInputEndAsync ()
        {
            if (onInputEndTCS is null) onInputEndTCS = new UniTaskCompletionSource();
            await onInputEndTCS.Task;
        }

        public virtual System.Threading.CancellationToken GetInputStartCancellationToken ()
        {
            if (onInputStartCTS is null) onInputStartCTS = new CancellationTokenSource();
            return onInputStartCTS.Token;
        }

        public virtual System.Threading.CancellationToken GetInputEndCancellationToken ()
        {
            if (onInputEndCTS is null) onInputEndCTS = new CancellationTokenSource();
            return onInputEndCTS.Token;
        }

        public virtual void Activate (float value) => SetInputValue(value);

        /// <summary>
        /// Performs the sampling, updating the input status; expected to be invoked on each render loop update.
        /// </summary>
        public virtual void SampleInput ()
        {
            if (!Enabled) return;

            #if ENABLE_LEGACY_INPUT_MANAGER
            if (config.ProcessLegacyBindings && Binding.Keys?.Count > 0)
                SampleKeys();

            if (config.ProcessLegacyBindings && Binding.Axes?.Count > 0)
                SampleAxes();

            if (Input.touchSupported && Binding.Swipes?.Count > 0)
                SampleSwipes();

            if (objectTriggers.Count > 0)
                SampleObjectTriggers();

            void SampleKeys ()
            {
                foreach (var key in Binding.Keys)
                {
                    if (Input.GetKeyDown(key)) SetInputValue(1);
                    if (Input.GetKeyUp(key)) SetInputValue(0);
                }
            }
            
            void SampleAxes ()
            {
                var maxValue = 0f;
                foreach (var axis in Binding.Axes)
                {
                    var axisValue = axis.Sample();
                    if (Mathf.Abs(axisValue) > Mathf.Abs(maxValue))
                        maxValue = axisValue;
                }
                if (!Mathf.Approximately(maxValue, Value))
                    SetInputValue(maxValue);
            }

            void SampleSwipes ()
            {
                if (!Input.touchSupported) return;
                var swipeRegistered = false;
                foreach (var swipe in Binding.Swipes)
                    if (swipe.Sample()) { swipeRegistered = true; break; }
                if (swipeRegistered != Active) SetInputValue(swipeRegistered ? 1 : 0);
            }

            void SampleObjectTriggers ()
            {
                if (!Triggered()) return;
                var hoveredObject = EventSystem.current.GetHoveredGameObject();
                if (hoveredObject && objectTriggers.Contains(hoveredObject))
                    if (!hoveredObject.TryGetComponent<IInputTrigger>(out var trigger) || trigger.CanTriggerInput())
                        SetInputValue(1f);
                
                bool Triggered ()
                {
                    if (!Input.touchSupported || Input.touchCount == 0) 
                        return Input.GetMouseButtonDown(0);
                    
                    var touch = Input.GetTouch(0);
                    if (touch.phase == TouchPhase.Began)
                    {
                        lastTouchBeganPosition = touch.position;
                        return false;
                    }

                    if (touch.phase != TouchPhase.Ended) return false;
                    
                    var cooldown = Time.unscaledTime - lastTouchTime <= config.TouchFrequencyLimit;
                    if (cooldown) return false;
                    
                    var withinDistanceLimit = Vector2.Distance(touch.position, lastTouchBeganPosition) < config.TouchDistanceLimit;
                    if (!withinDistanceLimit) return false;
                    
                    lastTouchTime = Time.unscaledTime;
                    return true;
                }
            }
            #endif

            #if ENABLE_INPUT_SYSTEM && INPUT_SYSTEM_AVAILABLE
            if (inputAction != null)
            {
                if (inputAction.type == UnityEngine.InputSystem.InputActionType.Value)
                {
                    var value = inputAction.ReadValue<float>();
                    SetInputValue(value);
                }
                else if (inputAction.WasPressedThisFrame())
                    SetInputValue(1);
                else if (inputAction.WasReleasedThisFrame())
                    SetInputValue(0);
            }
            #endif
        }

        private void SetInputValue (float value)
        {
            Value = value;
            lastActiveFrame = Time.frameCount;

            onInputTCS?.TrySetResult(Active);
            onInputTCS = null;
            if (Active)
            {
                onInputStartTCS?.TrySetResult();
                onInputStartTCS = null;
                onInputStartCTS?.Cancel();
                onInputStartCTS?.Dispose();
                onInputStartCTS = null;
            }
            else
            {
                onInputEndTCS?.TrySetResult();
                onInputEndTCS = null;
                onInputEndCTS?.Cancel();
                onInputEndCTS?.Dispose();
                onInputEndCTS = null;
            }
           
            if (Active) OnStart?.Invoke();
            else OnEnd?.Invoke();
        }
    }
}
