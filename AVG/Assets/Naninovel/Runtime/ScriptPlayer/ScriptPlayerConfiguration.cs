// Copyright 2017-2021 Elringus (Artyom Sovetnikov). All rights reserved.

using UnityEngine;

namespace Naninovel
{
    [EditInProjectSettings]
    public class ScriptPlayerConfiguration : Configuration
    {
        [Tooltip("Time scale to use when in skip (fast-forward) mode."), Range(1f, 100f)]
        public float SkipTimeScale = 10f;
        [Tooltip("Minimum seconds to wait before executing next command while in auto play mode.")]
        public float MinAutoPlayDelay = 3f;
        [Tooltip("Whether to instantly complete blocking (`wait:true`) commands performed over time (eg, animations, hide/reveal, tint changes, etc) when `Continue` input is activated.")]
        public bool CompleteOnContinue = true;
        [Tooltip("Whether to show player debug window on engine initialization.")]
        public bool ShowDebugOnInit = false;
    }
}
