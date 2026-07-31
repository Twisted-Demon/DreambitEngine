using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Dreambit.ECS;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Dreambit;

public class Basic2dLightingRenderPass : RenderPass
{
    private Effect LightingFx { get; set; }

    private RenderTarget2D AlbedoRt { get; set; }

    private readonly List<int> _sortedLayerBuffer = new(16);
    private readonly List<DrawableSortEntry> _drawableSortBuffer = new(512);

    private static readonly DrawableSortEntryComparer SortComparer = new();

    public override void Initialize()
    {
        base.Initialize();
        
        CreateAlbedoRenderTarget();
        
        LightingFx = Resources.LoadAsset<Effect>("Effects/ForwardLighting2D");

    }

    public override void OnDraw()
    {
        RenderDrawables();
        RenderLighting();
    }

    private void RenderDrawables()
    {
        var drawLayers = Drawables.GetDrawLayers();
        
        var camera = Scene.MainCamera;
        var cameraBounds = camera.BoundsF;
        var cameraMatrix = camera.TransformMatrix;
        
        BuildSortedLayerBuffer(drawLayers);

        Device.SetRenderTarget(AlbedoRt);
        Device.Clear(Color.Transparent);

        for (var layerIndex = 0;
             layerIndex < _sortedLayerBuffer.Count;
             layerIndex++)
        {
            var layer = _sortedLayerBuffer[layerIndex];
            var layerDrawables = drawLayers[layer];

            BuildDrawableSortBuffer(
                layerDrawables,
                cameraBounds);

            if (_drawableSortBuffer.Count == 0)
                continue;

            _drawableSortBuffer.Sort(SortComparer);

            RenderSortedDrawables(cameraMatrix);
        }
    }

    private void RenderLighting()
    {
        var lights = Drawables.GetAllDrawablesByType<PointLight2D>()
            .Where(x => x.IsVisibleFromCamera(Scene.MainCamera.BoundsF)).ToList();

        var ambientLight = Drawables.GetAllDrawablesByType<AmbientLight2D>().FirstOrDefault();

        var ambientColor = ambientLight != null ? ambientLight.Color : Color.Black;

        LightingUniforms.Apply(LightingFx, lights, Scene.MainCamera, ambientColor.ToVector3());

        Device.SetRenderTarget(RenderPipeline.SceneRenderTarget);
        Device.Clear(Color.Transparent);

        Core.SpriteBatch.Begin(
            SpriteSortMode.Immediate,
            BlendState.Opaque,
            Scene.RenderingOptions.SamplerState,
            DepthStencilState.None,
            RasterizerState.CullNone,
            LightingFx,
            Matrix.Identity
        );

        Core.SpriteBatch.Draw(
            AlbedoRt,
            new Rectangle(
                0,
                0,
                RenderPipeline.SceneRenderTarget.Width,
                RenderPipeline.SceneRenderTarget.Height
            ),
            Color.White
        );

        Core.SpriteBatch.End();
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

private void BuildDrawableSortBuffer(List<DrawableComponent> layerDrawables, RectangleF cameraBounds)
{
    _drawableSortBuffer.Clear();

    for (var i = 0; i < layerDrawables.Count; i++)
    {
        var drawable = layerDrawables[i];

        if (!drawable.Enabled ||
            !drawable.Entity.Enabled)
        {
            continue;
        }

        if (!drawable.IsVisibleFromCamera(cameraBounds))
            continue;

        var effect =
            drawable.Effect ?? DefaultEffect;

        // Snapshot WorldPosition.Y once.
        //
        // Without this, the comparer can evaluate WorldPosition
        // many times during an O(n log n) sort.
        var worldY =
            drawable.Transform.WorldPosition.Y;

        _drawableSortBuffer.Add(
            new DrawableSortEntry(
                drawable,
                effect,
                worldY));
    }
}

private void RenderSortedDrawables(Matrix cameraMatrix)
{
    Effect currentEffect = null;
    var batchStarted = false;

    for (var i = 0;
         i < _drawableSortBuffer.Count;
         i++)
    {
        var entry = _drawableSortBuffer[i];

        if (!batchStarted ||
            !ReferenceEquals(
                entry.Effect,
                currentEffect))
        {
            if (batchStarted)
                Core.SpriteBatch.End();

            Core.SpriteBatch.Begin(
                SpriteSortMode.Deferred,
                Scene.RenderingOptions.BlendState,
                Scene.RenderingOptions.SamplerState,
                DepthStencilState.None,
                RasterizerState.CullNone,
                entry.Effect,
                cameraMatrix
            );

            currentEffect = entry.Effect;
            batchStarted = true;
        }

        entry.Drawable.OnDraw();
    }

    if (batchStarted)
        Core.SpriteBatch.End();
}

    protected override void OnWindowResized(object sender, WindowResizedEventArgs args)
    {
        base.OnWindowResized(sender, args);

        CreateAlbedoRenderTarget();
    }

    protected override void OnDisposing()
    {
        base.OnDisposing();
        CleanupAlbedoRenderTarget();
        Resources.UnloadAsset(LightingFx.Name);
    }

    private void CreateAlbedoRenderTarget()
    {
        AlbedoRt?.Dispose();
        AlbedoRt = RenderPipeline.CreateRenderTarget();
    }

    private void CleanupAlbedoRenderTarget()
    {
        AlbedoRt?.Dispose();
        AlbedoRt = null;
    }

    private readonly struct DrawableSortEntry
    {
        public DrawableSortEntry(DrawableComponent drawable, Effect effect, float worldY)
        {
            Drawable = drawable;
            Effect = effect;
            WorldY = worldY;

            // Calculate this once instead of during every comparison.
            EffectKey = RuntimeHelpers.GetHashCode(effect);
        }

        public DrawableComponent Drawable { get; }

        public Effect Effect { get; }

        public float WorldY { get; }

        public int EffectKey { get; }
    }

    private sealed class DrawableSortEntryComparer : IComparer<DrawableSortEntry>
    {
        public int Compare(DrawableSortEntry left, DrawableSortEntry right)
        {
            var yComparison = left.WorldY.CompareTo(right.WorldY);

            if (yComparison != 0)
                return yComparison;

            /*
             * Only group by effect when the two drawables have the
             * same Y position. This preserves strict Y ordering.
             */
            if (!ReferenceEquals(left.Effect, right.Effect))
            {
                var effectComparison = left.EffectKey.CompareTo(right.EffectKey);

                if (effectComparison != 0)
                    return effectComparison;
            }

            /*
             * List.Sort is unstable, so provide a tie-breaker.
             * This helps prevent equally positioned sprites from
             * changing order unexpectedly.
             */
            var entityComparison = left.Drawable.Entity.Id.CompareTo(right.Drawable.Entity.Id);

            if (entityComparison != 0)
                return entityComparison;

            // Handles multiple drawables belonging to the same entity.
            return RuntimeHelpers
                .GetHashCode(left.Drawable)
                .CompareTo(RuntimeHelpers.GetHashCode(right.Drawable));
        }
    }
}