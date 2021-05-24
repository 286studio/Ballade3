// Copyright 2017-2021 Elringus (Artyom Sovetnikov). All rights reserved.

#if SPRITE_DICING_AVAILABLE

using System;
using System.Collections.Generic;
using System.Linq;
using Naninovel.FX;
using SpriteDicing;
using UniRx.Async;
using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// A <see cref="MonoBehaviourActor{TMeta}"/> using "SpriteDicing" extension to represent the actor.
    /// </summary>
    public abstract class DicedSpriteActor<TMeta> : MonoBehaviourActor<TMeta>, Blur.IBlurable
        where TMeta : OrthoActorMetadata
    {
        public override string Appearance { get => appearance; set => SetAppearance(value); }
        public override bool Visible { get => visible; set => SetVisibility(value); }

        protected virtual TransitionalRenderer TransitionalRenderer { get; private set; }

        private readonly OrthoActorMetadata metadata;
        private readonly Material dicedMaterial;
        private readonly Mesh dicedMesh;
        private readonly Dictionary<object, HashSet<string>> heldAppearances = new Dictionary<object, HashSet<string>>();
        private LocalizableResourceLoader<DicedSpriteAtlas> atlasLoader;
        private RenderTexture appearanceTexture;
        private string appearance;
        private string defaultAppearance;
        private bool visible;

        protected DicedSpriteActor (string id, TMeta metadata)
            : base(id, metadata)
        {
            this.metadata = metadata;

            dicedMaterial = new Material(Shader.Find("Sprites/Default"));
            dicedMaterial.hideFlags = HideFlags.HideAndDontSave;

            dicedMesh = new Mesh();
            dicedMesh.hideFlags = HideFlags.HideAndDontSave;
            dicedMesh.name = $"{id} Diced Sprite Mesh";
        }

        public UniTask BlurAsync (float intensity, float duration, EasingType easingType = default, CancellationToken cancellationToken = default)
        {
            return TransitionalRenderer.BlurAsync(intensity, duration, easingType, cancellationToken);
        }

        public override async UniTask InitializeAsync ()
        {
            await base.InitializeAsync();

            TransitionalRenderer = TransitionalRenderer.CreateFor(ActorMetadata, GameObject);
            SetVisibility(false);

            var providerManager = Engine.GetService<IResourceProviderManager>();
            var localizationManager = Engine.GetService<ILocalizationManager>();
            atlasLoader = metadata.Loader.CreateLocalizableFor<DicedSpriteAtlas>(providerManager, localizationManager);
        }

        public override async UniTask ChangeAppearanceAsync (string appearance, float duration, EasingType easingType = default,
            Transition? transition = default, CancellationToken cancellationToken = default)
        {
            var atlas = await GetOrLoadAtlasAsync(cancellationToken);
            if (cancellationToken.IsCancellationRequested) return;
            appearance = SetAppearance(appearance, atlas);
            var dicedSprite = GetDicedForAppearance(appearance, atlas);
            BuildDicedMeshFrom(dicedSprite);
            var renderTexture = RenderToTemporaryTexture(dicedSprite);

            await TransitionalRenderer.TransitionToAsync(renderTexture, duration, easingType, transition, cancellationToken);
            if (cancellationToken.CancelASAP) return;

            // Release texture with the old appearance.
            if (appearanceTexture)
                RenderTexture.ReleaseTemporary(appearanceTexture);
            appearanceTexture = renderTexture;
        }

        public override async UniTask ChangeVisibilityAsync (bool visible, float duration,
            EasingType easingType = default, CancellationToken cancellationToken = default)
        {
            // When appearance is not set (and default one is not preloaded for some reason, eg when using dynamic parameters) 
            // and revealing the actor — attempt to set default appearance.
            if (!Visible && visible && string.IsNullOrWhiteSpace(Appearance))
                await ChangeAppearanceAsync(defaultAppearance, 0, cancellationToken: cancellationToken);

            this.visible = visible;

            await TransitionalRenderer.FadeToAsync(visible ? TintColor.a : 0, duration, easingType, cancellationToken);
        }

        public override async UniTask HoldResourcesAsync (string appearance, object holder)
        {
            if (!heldAppearances.ContainsKey(holder))
            {
                await atlasLoader.LoadAndHoldAsync(Id, holder);
                heldAppearances.Add(holder, new HashSet<string>());
            }

            heldAppearances[holder].Add(appearance);
        }

        public override void ReleaseResources (string appearance, object holder)
        {
            if (!heldAppearances.ContainsKey(holder)) return;

            heldAppearances[holder].Remove(appearance);
            if (heldAppearances.Count == 0)
            {
                heldAppearances.Remove(holder);
                atlasLoader?.Release(Id, holder);
            }
        }

        public override void Dispose ()
        {
            if (appearanceTexture)
                RenderTexture.ReleaseTemporary(appearanceTexture);
            ObjectUtils.DestroyOrImmediate(dicedMaterial);
            ObjectUtils.DestroyOrImmediate(dicedMesh);

            atlasLoader?.ReleaseAll(this);
            
            base.Dispose();
        }

        protected virtual void SetAppearance (string appearance) => ChangeAppearanceAsync(appearance, 0).Forget();

        protected virtual void SetVisibility (bool visible) => ChangeVisibilityAsync(visible, 0).Forget();

        protected override Color GetBehaviourTintColor () => TransitionalRenderer.TintColor;

        protected override void SetBehaviourTintColor (Color tintColor)
        {
            if (!Visible) // Handle visibility-controlled alpha of the tint color.
                tintColor.a = TransitionalRenderer.TintColor.a;
            TransitionalRenderer.TintColor = tintColor;
        }

        private async UniTask<DicedSpriteAtlas> GetOrLoadAtlasAsync (CancellationToken cancellationToken)
        {
            if (atlasLoader.IsLoaded(Id)) return atlasLoader.GetLoadedOrNull(Id);
            var atlasResource = await atlasLoader.LoadAndHoldAsync(Id, this);
            if (cancellationToken.CancelASAP) return null;
            if (!atlasResource.Valid) throw new Exception($"Failed to load `{Id}` diced sprite atlas.");
            if (atlasResource.Object.SpritesCount == 0)
                throw new Exception($"`{Id}` diced sprite atlas is empty. Add at least one sprite and rebuild the atlas.");
            return atlasResource;
        }

        private string SetAppearance (string appearance, DicedSpriteAtlas atlas)
        {
            if (string.IsNullOrEmpty(defaultAppearance))
                defaultAppearance = GetDefaultSpriteName();
            if (string.IsNullOrEmpty(appearance))
                appearance = defaultAppearance;
            this.appearance = appearance;
            return appearance;

            string GetDefaultSpriteName ()
            {
                var sprites = atlas.GetAllSprites();
                var defaultSprite = sprites.Find(s => s.name.EndsWithFast("Default"));
                return defaultSprite ? defaultSprite.name : sprites.First().name;
            }
        }

        private Sprite GetDicedForAppearance (string appearance, DicedSpriteAtlas atlas)
        {
            // In case user stored source sprites in folders, the diced sprites will have dots in their names.
            var spriteName = appearance.Replace("/", ".");
            var dicedSprite = atlas.GetSprite(spriteName);
            if (dicedSprite is null) throw new Exception($"Failed to get `{spriteName}` diced sprite for `{Id}` actor.");
            return dicedSprite;
        }

        private void BuildDicedMeshFrom (Sprite dicedSprite)
        {
            dicedMesh.Clear();
            dicedMesh.vertices = Array.ConvertAll(dicedSprite.vertices, i => new Vector3(i.x, i.y));
            dicedMesh.uv = dicedSprite.uv;
            dicedMesh.triangles = Array.ConvertAll(dicedSprite.triangles, i => (int)i);
            dicedMaterial.mainTexture = dicedSprite.texture;
        }

        private RenderTexture RenderToTemporaryTexture (Sprite dicedSprite)
        {
            var spriteRect = dicedSprite.GetVerticesRect();
            var renderTexture = GetTexture();
            Graphics.SetRenderTarget(renderTexture);
            GL.Clear(true, true, Color.clear);
            GL.PushMatrix();
            GL.LoadProjectionMatrix(GetOrthoMatrix());
            dicedMaterial.SetPass(0);
            var pivot = dicedSprite.pivot / spriteRect.size / dicedSprite.pixelsPerUnit;
            var drawPos = spriteRect.size * pivot - spriteRect.size / 2;
            Graphics.DrawMeshNow(dicedMesh, drawPos, Quaternion.identity);
            GL.PopMatrix();
            return renderTexture;

            RenderTexture GetTexture ()
            {
                var renderWidth = Mathf.CeilToInt(spriteRect.width * metadata.PixelsPerUnit);
                var renderHeight = Mathf.CeilToInt(spriteRect.height * metadata.PixelsPerUnit);
                return RenderTexture.GetTemporary(renderWidth, renderHeight);
            }

            Matrix4x4 GetOrthoMatrix ()
            {
                var halfSize = spriteRect.size / 2f;
                return Matrix4x4.Ortho(-halfSize.x, halfSize.x, -halfSize.y, halfSize.y, 0f, 100f);
            }
        }
    }
}

#endif
