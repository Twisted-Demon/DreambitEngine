using System;
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

    private readonly List<DrawableSortEntry> _sceneRenderList = new(512);
    private readonly List<DrawableSortEntry> _layerSortBuffer = new(128);
    private readonly List<int> _sortedLayerBuffer = new(16);
    private DreambitEffect LightingFx { get; set; }
    private DreambitEffect DepthFx { get; set; }

    private RenderTarget2D AlbedoRt { get; set; }
    private RenderTarget2D DepthRt { get; set; }

    public override void Initialize()
    {
        base.Initialize();

        CreateAlbedoRenderTarget();
        CreateDepthRenderTarget();

        LightingFx = Resources.LoadAsset<DreambitEffect>("Effects/ForwardLighting2D");
        DepthFx = Resources.LoadAsset<DreambitEffect>("Effects/Depth2D");
    }

    public override void OnDraw()
    {
        BuildSceneRenderList();
        RenderDrawables();
        RenderDepth();
        RenderLighting();
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

    private void RenderDrawables()
    {
        Device.SetRenderTarget(AlbedoRt);
        Device.Clear(Color.Transparent);

        RenderSceneAlbedo(
            RenderCamera.TransformMatrix);
    }

    private void RenderDepth()
    {
        Device.SetRenderTarget(DepthRt);
        Device.Clear(Color.Transparent);

        RenderSceneDepth();
    }
    

    private void RenderLighting()
    {
        var lights = Drawables.GetAllDrawablesByType<PointLight2D>()
            .Where(x => x.IsVisibleFromCamera(RenderCamera.BoundsF)).ToList();

        var ambientLight = Drawables.GetAllDrawablesByType<AmbientLight2D>().FirstOrDefault();
        var ambientColor = ambientLight is not null
            ? ambientLight.Color.ToVector3() * ambientLight.Intensity
            : Scene.Settings.AmbientLightColor.ToVector3() *
              Scene.Settings.AmbientLightIntensity;

        LightingUniforms.Apply(LightingFx, lights, RenderCamera, ambientColor);

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
    

    private void RenderSceneAlbedo(
        Matrix cameraMatrix)
    {
        Effect currentEffect = null;

        var currentDrawLayer = 0;
        var batchStarted = false;

        for (var i = 0;
             i < _sceneRenderList.Count;
             i++)
        {
            var entry = _sceneRenderList[i];

            var effectChanged =
                !ReferenceEquals(
                    entry.Effect,
                    currentEffect);

            var layerChanged =
                batchStarted &&
                entry.DrawLayer != currentDrawLayer;

            if (!batchStarted ||
                effectChanged ||
                layerChanged)
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
                    cameraMatrix);

                currentEffect =
                    entry.Effect;

                currentDrawLayer =
                    entry.DrawLayer;

                batchStarted = true;
            }

            entry.Drawable.Draw();
        }

        if (batchStarted)
            Core.SpriteBatch.End();
    }

    private void RenderSceneDepth()
    {
        for (var i = 0; i < _sceneRenderList.Count; i++)
        {
            var entry = _sceneRenderList[i];

            DepthFx.Effect.Parameters["SortDepth"]
                ?.SetValue(entry.SortDepth);


            Core.SpriteBatch.Begin(
                SpriteSortMode.Deferred,
                BlendState.Opaque,
                Scene.RenderingOptions.SamplerState,
                DepthStencilState.None,
                RasterizerState.CullNone,
                DepthFx,
                RenderCamera.TransformMatrix);
            
            entry.Drawable.Draw();
            
            Core.SpriteBatch.End();
        }
    }
    

    protected override void OnViewportResized()
    {
        base.OnViewportResized();

        CreateAlbedoRenderTarget();
        CreateDepthRenderTarget();
    }

    protected override void OnDisposing()
    {
        base.OnDisposing();
        CleanupAlbedoRenderTarget();
        CleanupDepthRenderTarget();
        Resources.UnloadAsset(LightingFx.AssetName);
        Resources.UnloadAsset(DepthFx.AssetName);
    }

    private void CreateAlbedoRenderTarget()
    {
        AlbedoRt?.Dispose();
        AlbedoRt = RenderPipeline.CreateViewportRenderTarget();
    }

    private void CreateDepthRenderTarget()
    {
        DepthRt?.Dispose();
        ArgumentNullException.ThrowIfNull(AlbedoRt);
        DepthRt = new RenderTarget2D(
            Device,
            AlbedoRt.Width,
            AlbedoRt.Height,
            false,
            SurfaceFormat.Single,
            DepthFormat.None);
    }

    private void CleanupAlbedoRenderTarget()
    {
        AlbedoRt?.Dispose();
        AlbedoRt = null;
    }

    private void CleanupDepthRenderTarget()
    {
        DepthRt?.Dispose();
        DepthRt = null;
    }

    private readonly struct DrawableSortEntry
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
