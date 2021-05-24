// Copyright 2017-2021 Elringus (Artyom Sovetnikov). All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace Naninovel
{
    public class LayeredDrawer : IDisposable
    {
        public IReadOnlyCollection<LayeredActorLayer> Layers => layers;

        private readonly Transform transform;
        private readonly Material sharedMaterial;
        private readonly bool reversed;
        private readonly List<LayeredActorLayer> layers = new List<LayeredActorLayer>();
        private readonly CommandBuffer commandBuffer = new CommandBuffer();
        private readonly MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();

        private RenderCanvas renderCanvas;
        private Vector2 canvasSize;
        private Vector2 canvasOffset;

        public LayeredDrawer (Transform transform, Material sharedMaterial = default, bool reversed = false)
        {
            this.transform = transform;
            this.sharedMaterial = sharedMaterial;
            this.reversed = reversed;
            commandBuffer.name = $"Naninovel-DrawLayered-{transform.name}";
            BuildLayers();
        }

        public void Dispose () => ClearLayers();

        public void BuildLayers ()
        {
            ClearLayers();

            var renderers = GetRenderers();
            if (renderers.Count == 0) return;

            foreach (var renderer in renderers)
            {
                if (renderer is SpriteRenderer spriteRenderer)
                {
                    if (!spriteRenderer.sprite) continue;
                    layers.Add(new LayeredActorLayer(spriteRenderer));
                    continue;
                }

                if (!renderer.TryGetComponent<MeshFilter>(out var meshFilter)) continue;
                layers.Add(new LayeredActorLayer(renderer, meshFilter.sharedMesh ? meshFilter.sharedMesh : meshFilter.mesh));
            }

            UpdateCanvas();
        }

        public virtual RenderTexture DrawLayers (int pixelsPerUnit, RenderTexture renderTexture = default)
        {
            if (layers is null || layers.Count == 0)
                throw new Exception($"Can't render layered actor `{transform.name}`: layers data is empty. Make sure the actor prefab contains child objects with at least one renderer.");

            var drawDimensions = canvasSize * pixelsPerUnit;

            if (!renderTexture)
                renderTexture = GetTemporaryTexture(drawDimensions);

            var drawPosition = transform.position + (Vector3)canvasOffset;
            var orthoMin = Vector3.Scale(-drawDimensions / 2f, transform.parent.localScale) + drawPosition * pixelsPerUnit;
            var orthoMax = Vector3.Scale(drawDimensions / 2f, transform.parent.localScale) + drawPosition * pixelsPerUnit;
            var orthoMatrix = Matrix4x4.Ortho(orthoMin.x, orthoMax.x, orthoMin.y, orthoMax.y, float.MinValue, float.MaxValue);
            var rotationMatrix = Matrix4x4.Rotate(Quaternion.Inverse(transform.parent.localRotation));

            PrepareCommandBuffer(renderTexture, orthoMatrix);

            if (reversed)
            {
                for (int i = layers.Count - 1; i >= 0; i--)
                    DrawLayer(layers[i], rotationMatrix, pixelsPerUnit);
            }
            else
            {
                for (int i = 0; i < layers.Count; i++)
                    DrawLayer(layers[i], rotationMatrix, pixelsPerUnit);
            }

            Graphics.ExecuteCommandBuffer(commandBuffer);

            return renderTexture;
        }

        public void DrawGizmos ()
        {
            if (renderCanvas) return; // Render canvas draws its own gizmo.
            if (!Application.isPlaying)
            {
                if (CountRenderers() != layers.Count) BuildLayers();
                else UpdateCanvas();
            }
            Gizmos.DrawWireCube(transform.position, canvasSize);
        }

        private void ClearLayers ()
        {
            foreach (var layer in layers)
                layer.Dispose();
            layers.Clear();
        }

        private IReadOnlyCollection<Renderer> GetRenderers ()
        {
            return transform.GetComponentsInChildren<Renderer>()
                .OrderBy(s => s.sortingOrder)
                .ThenByDescending(s => s.transform.position.z).ToArray();
        }

        private void UpdateCanvas ()
        {
            canvasSize = GetCanvasSize();
            canvasOffset = transform.TryGetComponent<RenderCanvas>(out var canvas) ? canvas.Offset : Vector2.zero;
        }

        private Vector2 GetCanvasSize ()
        {
            if (transform.TryGetComponent<RenderCanvas>(out renderCanvas))
                return renderCanvas.Size;
            if (layers is null || layers.Count == 0) return Vector2.zero;

            var maxPosX = layers.Max(l => Mathf.Max(Mathf.Abs(l.Renderer.bounds.max.x), Mathf.Abs(l.Renderer.bounds.min.x)));
            var maxPosY = layers.Max(l => Mathf.Max(Mathf.Abs(l.Renderer.bounds.max.y), Mathf.Abs(l.Renderer.bounds.min.y)));
            return new Vector2(maxPosX * 2, maxPosY * 2);
        }

        private RenderTexture GetTemporaryTexture (Vector2 drawDimensions)
        {
            var renderTextureSize = new Vector2Int(Mathf.RoundToInt(drawDimensions.x), Mathf.RoundToInt(drawDimensions.y));
            return RenderTexture.GetTemporary(renderTextureSize.x, renderTextureSize.y);
        }

        private void PrepareCommandBuffer (RenderTexture renderTexture, Matrix4x4 orthoMatrix)
        {
            commandBuffer.Clear();
            commandBuffer.SetRenderTarget(renderTexture);
            commandBuffer.ClearRenderTarget(true, true, Color.clear);
            commandBuffer.SetProjectionMatrix(orthoMatrix);
        }

        private void DrawLayer (LayeredActorLayer layer, Matrix4x4 rotationMatrix, int pixelsPerUnit)
        {
            if (!layer.Enabled) return;

            var drawMaterial = sharedMaterial ? sharedMaterial : layer.RenderMaterial;
            var drawPosition = transform.TransformPoint(rotationMatrix // Compensate actor (parent game object) rotation.
                .MultiplyPoint3x4(transform.InverseTransformPoint(layer.Position)));
            var drawTransform = Matrix4x4.TRS(drawPosition * pixelsPerUnit, layer.Rotation, layer.Scale * pixelsPerUnit);
            layer.GetPropertyBlock(propertyBlock);
            commandBuffer.DrawMesh(layer.Mesh, drawTransform, drawMaterial, 0, -1, propertyBlock);
        }

        private int CountRenderers ()
        {
            var result = 0;
            CountIn(transform);
            return result;

            void CountIn (Transform trs)
            {
                for (int i = 0; i < trs.childCount; i++)
                {
                    var child = trs.GetChild(i);
                    if (child.TryGetComponent<SpriteRenderer>(out _) ||
                        child.TryGetComponent<MeshFilter>(out _)) result++;
                    CountIn(child);
                }
            }
        }
    }
}
