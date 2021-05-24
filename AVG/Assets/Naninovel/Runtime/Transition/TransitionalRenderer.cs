// Copyright 2017-2021 Elringus (Artyom Sovetnikov). All rights reserved.

using UniRx.Async;
using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// Allows rendering a texture with <see cref="TransitionalMaterial"/> and transition to another texture with a set of configurable visual effects.
    /// </summary>
    public abstract class TransitionalRenderer : MonoBehaviour
    {
        /// <summary>
        /// Current transition mode data.
        /// </summary>
        public virtual Transition Transition
        {
            get => new Transition(TransitionName, TransitionParams, DissolveTexture);
            set
            {
                TransitionName = value.Name;
                TransitionParams = value.Parameters;
                DissolveTexture = value.DissolveTexture;
            }
        }
        /// <inheritdoc cref="TransitionalMaterial.MainTexture"/>
        public virtual Texture MainTexture { get => Material.MainTexture; set => Material.MainTexture = value; }
        /// <inheritdoc cref="TransitionalMaterial.TransitionTexture"/>
        public virtual Texture TransitionTexture { get => Material.TransitionTexture; set => Material.TransitionTexture = value; }
        /// <inheritdoc cref="TransitionalMaterial.DissolveTexture"/>
        public virtual Texture DissolveTexture { get => Material.DissolveTexture; set => Material.DissolveTexture = value; }
        /// <inheritdoc cref="TransitionalMaterial.TransitionName"/>
        public virtual string TransitionName { get => Material.TransitionName; set => Material.TransitionName = value; }
        /// <inheritdoc cref="TransitionalMaterial.TransitionProgress"/>
        public virtual float TransitionProgress { get => Material.TransitionProgress; set => Material.TransitionProgress = value; }
        /// <inheritdoc cref="TransitionalMaterial.TransitionParams"/>
        public virtual Vector4 TransitionParams { get => Material.TransitionParams; set => Material.TransitionParams = value; }
        /// <inheritdoc cref="TransitionalMaterial.RandomSeed"/>
        public virtual Vector2 RandomSeed { get => Material.RandomSeed; set => Material.RandomSeed = value; }
        /// <inheritdoc cref="TransitionalMaterial.TintColor"/>
        public virtual Color TintColor { get => Material.TintColor; set => Material.TintColor = value; }
        /// <inheritdoc cref="TransitionalMaterial.Opacity"/>
        public virtual float Opacity { get => Material.Opacity; set => Material.Opacity = value; }
        /// <summary>
        /// Whether to flip the content by X-axis.
        /// </summary>
        public virtual bool FlipX { get; set; } = false;
        /// <summary>
        /// Whether to flip the content by Y-axis.
        /// </summary>
        public virtual bool FlipY { get; set; } = false;
        /// <summary>
        /// Intensity of the gaussian blur effect to apply for the rendered target.
        /// </summary>
        public virtual float BlurIntensity { get; set; } = 0f;

        /// <summary>
        /// Material used for rendering the content.
        /// </summary>
        protected virtual TransitionalMaterial Material { get; private set; }

        private readonly Tweener<FloatTween> transitionTweener = new Tweener<FloatTween>();
        private readonly Tweener<ColorTween> colorTweener = new Tweener<ColorTween>();
        private readonly Tweener<FloatTween> fadeTweener = new Tweener<FloatTween>();
        private readonly Tweener<FloatTween> blurTweener = new Tweener<FloatTween>();

        private BlurFilter blurFilter;
        private float opacityLastFrame;

        /// <summary>
        /// Adds a transitional renderer component for the provided actor.
        /// </summary>
        public static TransitionalRenderer CreateFor (OrthoActorMetadata actorMetadata, GameObject actorObject)
        {
            if (actorMetadata.RenderTexture)
            {
                actorMetadata.RenderTexture.Clear();
                var textureRenderer = actorObject.AddComponent<TransitionalTextureRenderer>();
                textureRenderer.Initialize(actorMetadata.CustomTextureShader);
                textureRenderer.RenderTexture = actorMetadata.RenderTexture;
                textureRenderer.CorrectAspect = actorMetadata.CorrectRenderAspect;
                return textureRenderer;
            }
            else
            {
                var spriteRenderer = actorObject.AddComponent<TransitionalSpriteRenderer>();
                spriteRenderer.Initialize(actorMetadata.Pivot, actorMetadata.PixelsPerUnit, actorMetadata.CustomTextureShader, actorMetadata.CustomSpriteShader);
                spriteRenderer.DepthPassEnabled = actorMetadata.EnableDepthPass;
                spriteRenderer.DepthAlphaCutoff = actorMetadata.DepthAlphaCutoff;
                return spriteRenderer;
            }
        }

        /// <summary>
        /// Performs transition from <see cref="TransitionalMaterial.MainTexture"/> to the provided <paramref name="texture"/> over <paramref name="duration"/>.
        /// </summary>
        /// <param name="texture">Texture to transition into.</param>
        /// <param name="duration">Duration of the transition, in seconds.</param>
        /// <param name="easingType">Type of easing to use when applying the transition effect.</param>
        /// <param name="transition">Type of the transition effect to use.</param>
        public virtual async UniTask TransitionToAsync (Texture texture, float duration, EasingType easingType = default,
            Transition? transition = default, CancellationToken cancellationToken = default)
        {
            if (transitionTweener.Running)
            {
                transitionTweener.CompleteInstantly();
                await AsyncUtils.WaitEndOfFrame; // Materials are updated later in render loop, so wait before further modifications.
                if (cancellationToken.CancelASAP) return;
            }

            if (transition.HasValue)
                Transition = transition.Value;

            if (duration <= 0)
            {
                MainTexture = texture;
                TransitionProgress = 0;
                return;
            }
            else
            {
                if (!MainTexture) MainTexture = texture;
                Material.UpdateRandomSeed();
                TransitionTexture = texture;
                var tween = new FloatTween(TransitionProgress, 1, duration, value => TransitionProgress = value, false, easingType, Material);
                await transitionTweener.RunAsync(tween, cancellationToken);
                if (cancellationToken.CancelASAP) return;
                MainTexture = TransitionTexture;
                TransitionProgress = 0;
            }
        }

        /// <summary>
        /// Tints current texture to the provided <param name="color"></param> over <paramref name="duration"/>.
        /// </summary>
        /// <param name="color">Color of the tint.</param>
        /// <param name="duration">Duration of crossfade from current to the target tint color.</param>
        /// <param name="easingType">Type of easing to use when applying the tint.</param>
        public virtual async UniTask TintToAsync (Color color, float duration, EasingType easingType = default, CancellationToken cancellationToken = default)
        {
            if (colorTweener.Running) colorTweener.CompleteInstantly();

            if (duration <= 0)
            {
                TintColor = color;
                return;
            }

            var tween = new ColorTween(TintColor, color, ColorTweenMode.All, duration, value => TintColor = value, false, easingType, Material);
            await colorTweener.RunAsync(tween, cancellationToken);
        }

        /// <summary>
        /// Same as tint, but applies only to the alpha component of the color.
        /// </summary>
        public virtual async UniTask FadeToAsync (float opacity, float duration, EasingType easingType = default, CancellationToken cancellationToken = default)
        {
            if (fadeTweener.Running) fadeTweener.CompleteInstantly();

            if (duration <= 0)
            {
                Opacity = opacity;
                return;
            }

            var tween = new FloatTween(Opacity, opacity, duration, value => Opacity = value, false, easingType, Material);
            await fadeTweener.RunAsync(tween, cancellationToken);
        }

        public virtual async UniTask FadeOutAsync (float duration, EasingType easingType = default, CancellationToken cancellationToken = default)
        {
            await FadeToAsync(0, duration, easingType, cancellationToken);
        }

        public virtual async UniTask FadeInAsync (float duration, EasingType easingType = default, CancellationToken cancellationToken = default)
        {
            await FadeToAsync(1, duration, easingType, cancellationToken);
        }

        public virtual async UniTask BlurAsync (float intensity, float duration, EasingType easingType = default, CancellationToken cancellationToken = default)
        {
            if (blurTweener.Running) blurTweener.CompleteInstantly();

            if (duration <= 0)
            {
                BlurIntensity = intensity;
                return;
            }

            var tween = new FloatTween(BlurIntensity, intensity, duration, value => BlurIntensity = value, false, easingType, this);
            await blurTweener.RunAsync(tween, cancellationToken);
        }

        public virtual async UniTask FlipXAsync (float duration, EasingType easingType = default, CancellationToken cancellationToken = default)
        {
            if (duration <= 0) return;
            Material.FlipMain = true;
            if (transitionTweener.Running)
                while (transitionTweener.Running && !cancellationToken.CancellationRequested)
                    await AsyncUtils.WaitEndOfFrame;
            else await TransitionToAsync(MainTexture, duration, easingType, null, cancellationToken);
            if (cancellationToken.CancelASAP) return;
            Material.FlipMain = false;
        }

        public CharacterLookDirection GetLookDirection (CharacterLookDirection bakedDirection)
        {
            switch (bakedDirection)
            {
                case CharacterLookDirection.Center:
                    return CharacterLookDirection.Center;
                case CharacterLookDirection.Left:
                    return FlipX ? CharacterLookDirection.Right : CharacterLookDirection.Left;
                case CharacterLookDirection.Right:
                    return FlipX ? CharacterLookDirection.Left : CharacterLookDirection.Right;
                default: return default;
            }
        }

        public void SetLookDirection (CharacterLookDirection direction, CharacterLookDirection bakedDirection)
        {
            if (bakedDirection == CharacterLookDirection.Center) return;
            if (direction == CharacterLookDirection.Center)
            {
                FlipX = false;
                return;
            }
            if (direction != GetLookDirection(bakedDirection)) FlipX = !FlipX;
        }

        public async UniTask ChangeLookDirectionAsync (CharacterLookDirection direction, CharacterLookDirection bakedDirection,
            float duration, EasingType easingType = default, CancellationToken cancellationToken = default)
        {
            var prevValue = GetLookDirection(bakedDirection);
            SetLookDirection(direction, bakedDirection);
            if (prevValue != GetLookDirection(bakedDirection) && duration > 0)
                await FlipXAsync(duration, easingType, cancellationToken);
        }

        /// <summary>
        /// Prepares the underlying systems for render.
        /// </summary>
        /// <param name="customShader">Shader to use for rendering; will use a default one when not provided.</param>
        protected virtual void Initialize (Shader customShader = default)
        {
            Material = new TransitionalMaterial(customShader);
            blurFilter = new BlurFilter(2, true);
        }

        protected virtual void OnDestroy ()
        {
            ObjectUtils.DestroyOrImmediate(Material);
            blurFilter?.Dispose();
        }

        protected virtual bool ShouldRender ()
        {
            return Material && MainTexture && opacityLastFrame > 0;
        }

        /// <summary>
        /// Renders <see cref="Material"/> to the provided render texture.
        /// </summary>
        /// <param name="texture">The render target.</param>
        /// <param name="correctAspect">Whether to compensate difference in aspect ratio between rendered content and provided texture.</param>
        protected virtual void RenderToTexture (RenderTexture texture, bool correctAspect)
        {
            if (!Material.SetPass(0)) return;

            Graphics.SetRenderTarget(texture);

            var size = new Vector2(texture.width, texture.height);
            var offset = correctAspect ? GetOffset(MainTexture, texture) : Vector2.zero;
            DrawQuad(size, offset, FlipX, FlipY);

            if (BlurIntensity > 0)
                blurFilter.BlurTexture(texture, BlurIntensity);
        }

        protected virtual (int width, int height) GetPreferredRenderSize ()
        {
            var mainText = MainTexture;
            var transText = TransitionTexture;
            var width = transText && transText.width > mainText.width ? transText.width : mainText.width;
            var height = transText && transText.height > mainText.height ? transText.height : mainText.height;
            return (width, height);
        }

        private static Vector2 GetOffset (Texture source, Texture target)
        {
            var sourceAspect = source.width / (float)source.height;
            var targetAspect = target.width / (float)target.height;
            var adjustedHeight = target.width * (source.height / (float)source.width);
            var adjustedWidth = target.height * (source.width / (float)source.height);
            var offsetX = targetAspect > sourceAspect ? (target.width - adjustedWidth) / 2f : 0;
            var offsetY = targetAspect < sourceAspect ? (target.height - adjustedHeight) / 2f : 0;
            return new Vector2(offsetX, offsetY);
        }

        private static void DrawQuad (Vector2 size, Vector2 offset, bool flipX, bool flipY)
        {
            GL.Clear(true, true, Color.clear);
            GL.PushMatrix();
            GL.LoadPixelMatrix(0, size.x, 0, size.y);
            GL.Begin(GL.QUADS);
            GL.TexCoord2(flipX ? 1 : 0, flipY ? 1 : 0);
            GL.Vertex3(offset.x, offset.y, 0);
            GL.TexCoord2(flipX ? 0 : 1, flipY ? 1 : 0);
            GL.Vertex3(size.x - offset.x, offset.y, 0);
            GL.TexCoord2(flipX ? 0 : 1, flipY ? 0 : 1);
            GL.Vertex3(size.x - offset.x, size.y - offset.y, 0);
            GL.TexCoord2(flipX ? 1 : 0, flipY ? 0 : 1);
            GL.Vertex3(offset.x, size.y - offset.y, 0);
            GL.End();
            GL.PopMatrix();
        }

        private void LateUpdate () => opacityLastFrame = Opacity;
    }
}
