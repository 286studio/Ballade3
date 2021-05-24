// Copyright 2017-2021 Elringus (Artyom Sovetnikov). All rights reserved.

using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// A <see cref="TransitionalRenderer"/> implementation, that outputs the result to a render texture.
    /// </summary>
    public class TransitionalTextureRenderer : TransitionalRenderer
    {
        /// <summary>
        /// Render texture to output the render result.
        /// </summary>
        public virtual RenderTexture RenderTexture { get; set; }
        /// <summary>
        /// Whether to resize source texture when it has different aspect with the render texture.
        /// </summary>
        public virtual bool CorrectAspect { get; set; }

        /// <inheritdoc cref="TransitionalRenderer.Initialize"/>
        public new void Initialize (Shader customShader = default)
        {
            base.Initialize(customShader);
        }

        protected virtual void Update ()
        {
            if (ShouldRender()) 
                RenderToTexture(RenderTexture, CorrectAspect);
        }
    } 
}
