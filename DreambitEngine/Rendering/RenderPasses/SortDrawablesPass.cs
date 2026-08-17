using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Dreambit.ECS;
using Microsoft.Xna.Framework.Graphics;

namespace Dreambit;

public class SortDrawablesPass : RenderPass
{
    private static readonly DrawableSortEntryComparer SortComparer = new();
    private readonly List<DrawableSortEntry> _layerSortBuffer = new(128);

    private readonly List<DrawableSortEntry> _sceneRenderList = new(512);
    private readonly List<int> _sortedLayerBuffer = new(16);

    public IReadOnlyList<DrawableSortEntry> SceneRenderList => _sceneRenderList;

    public override void OnDraw()
    {
        BuildSceneRenderList();
    }

    private void BuildSceneRenderList()
    {
        _sceneRenderList.Clear();

        var drawLayers = Drawables.GetDrawLayers();
        var cameraBounds = RenderCamera.BoundsF;

        BuildSortedLayerBuffer(drawLayers);

        for (var layerIndex = 0; layerIndex < _sortedLayerBuffer.Count; layerIndex++)
        {
            var drawLayer = _sortedLayerBuffer[layerIndex];
            var layerDrawables = drawLayers[drawLayer];

            BuildLayerSortBuffer(drawLayer, layerDrawables, cameraBounds);

            if (_layerSortBuffer.Count == 0)
                continue;

            _layerSortBuffer.Sort(SortComparer);

            _sceneRenderList.AddRange(
                _layerSortBuffer);
        }
    }

    private void BuildLayerSortBuffer(
        int drawLayer,
        List<DrawableComponent> layerDrawables,
        RectangleF cameraBounds)
    {
        _layerSortBuffer.Clear();

        for (var i = 0; i < layerDrawables.Count; i++)
        {
            var drawable = layerDrawables[i];

            if (!drawable.Enabled ||
                !drawable.Entity.Enabled)
                continue;

            if (!drawable.IsVisibleFromCamera(cameraBounds))
                continue;

            var effect =
                drawable.Effect ?? DefaultEffect;

            _layerSortBuffer.Add(
                new DrawableSortEntry(
                    drawable,
                    effect,
                    drawLayer,
                    drawable.SortDepth));
        }
    }

    private void BuildSortedLayerBuffer(
        Dictionary<int, List<DrawableComponent>> drawLayers)
    {
        _sortedLayerBuffer.Clear();

        foreach (var layer in drawLayers.Keys)
        {
            // Point lights are processed separately in RenderLighting().
            if (layer == DrawLayers.LightLayer)
                continue;

            _sortedLayerBuffer.Add(layer);
        }

        _sortedLayerBuffer.Sort();
    }

    public readonly struct DrawableSortEntry
    {
        public DrawableSortEntry(
            DrawableComponent drawable,
            Effect effect,
            int drawLayer,
            float sortDepth)
        {
            Drawable = drawable;
            Effect = effect;
            DrawLayer = drawLayer;
            SortDepth = sortDepth;

            EffectKey =
                RuntimeHelpers.GetHashCode(effect);
        }

        public DrawableComponent Drawable { get; }

        public Effect Effect { get; }

        public int DrawLayer { get; }

        public float SortDepth { get; }

        public int EffectKey { get; }
    }

    private sealed class DrawableSortEntryComparer :
        IComparer<DrawableSortEntry>
    {
        public int Compare(
            DrawableSortEntry left,
            DrawableSortEntry right)
        {
            var depthComparison =
                left.SortDepth.CompareTo(
                    right.SortDepth);

            if (depthComparison != 0)
                return depthComparison;

            if (!ReferenceEquals(
                    left.Effect,
                    right.Effect))
            {
                var effectComparison =
                    left.EffectKey.CompareTo(
                        right.EffectKey);

                if (effectComparison != 0)
                    return effectComparison;
            }

            var entityComparison =
                left.Drawable.Entity.Id.CompareTo(
                    right.Drawable.Entity.Id);

            if (entityComparison != 0)
                return entityComparison;

            return RuntimeHelpers
                .GetHashCode(left.Drawable)
                .CompareTo(
                    RuntimeHelpers.GetHashCode(
                        right.Drawable));
        }
    }
}