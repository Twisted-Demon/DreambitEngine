using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Dreambit.ECS;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Dreambit;

public class Basic2dLightingRenderPass : RenderPass
{
    private static readonly DrawableSortEntryComparer SortComparer = new();
    private readonly List<DrawableSortEntry> _drawableSortBuffer = new(512);

    private readonly List<int> _sortedLayerBuffer = new(16);
    private Effect LightingFx { get; set; }

    private RenderTarget2D AlbedoRt { get; set; }

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

        var ambientColor = ambientLight != null
            ? ambientLight.Color.ToVector3() * ambientLight.Intensity
            : Vector3.Zero;

        LightingUniforms.Apply(LightingFx, lights, Scene.MainCamera, ambientColor);

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

    private void BuildDrawableSortBuffer(
        List<DrawableComponent> layerDrawables,
        RectangleF cameraBounds)
    {
        _drawableSortBuffer.Clear();

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

            var sortDepth =
                drawable.SortDepth;

            _drawableSortBuffer.Add(
                new DrawableSortEntry(
                    drawable,
                    effect,
                    sortDepth));
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

            entry.Drawable.Draw();
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
        public DrawableSortEntry(
            DrawableComponent drawable,
            Effect effect,
            float sortDepth)
        {
            Drawable = drawable;
            Effect = effect;
            SortDepth = sortDepth;

            EffectKey =
                RuntimeHelpers.GetHashCode(effect);
        }

        public DrawableComponent Drawable { get; }

        public Effect Effect { get; }

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
