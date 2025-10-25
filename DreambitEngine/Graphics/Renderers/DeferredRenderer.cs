using System.Linq;
using Dreambit.ECS;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Dreambit;

public class DeferredRenderer(Scene scene) : Renderer(scene)
{
    private RenderTarget2D BasePassRenderTarget { get; set; }
    private RenderTarget2D LightingPassRenderTarget { get; set; }

    private Effect _combinePassesEffect;
    
    public override void OnDraw()
    {
        PreRender();
        RenderBasePass();
        CombinePasses();
    }

    private void PreRender()
    {
        var drawables = Scene.Drawables.GetAllDrawables()
            .Where(x => x.Enabled && x.Entity.Enabled && x.DrawLayer != DrawLayers.LightLayer);
        
        foreach(var drawable in drawables)
            drawable.OnPreDraw();
    }

    private void RenderBasePass()
    {
        var drawLayers = Scene.Drawables.GetDrawLayers();
        var layerOrder = drawLayers.Keys.OrderBy(x => x).ToList();
        var cameraMatrix = Scene.MainCamera.TransformMatrix;
        
        Device.SetRenderTarget(BasePassRenderTarget);
        Device.Clear(Color.Transparent);
        
        //lighting
        
        for (var i = 0; i < layerOrder.Count; i++)
        {
            var drawables = drawLayers[layerOrder[i]]
                .Where(x => x.Enabled && x.Entity.Enabled && x.DrawLayer != DrawLayers.LightLayer)
                .ToList();

            var visibleDrawables = drawables
                .Where(d => d.IsVisibleFromCamera(Scene.MainCamera.Bounds))
                .ToList();

            if (visibleDrawables.Count == 0) continue;

            var sortedDrawables = visibleDrawables
                .OrderBy(d => d.Transform.WorldPosition.Y)
                .ThenBy(d => d.UsesEffect ? d.Effect : DefaultEffect)
                .ToList();

            Effect currentEffect = null;

            Core.SpriteBatch.Begin(
                transformMatrix: cameraMatrix,
                samplerState: SamplerState.PointClamp,
                sortMode: SpriteSortMode.Deferred,
                blendState: BlendState.AlphaBlend,
                effect: DefaultEffect
            );

            foreach (var drawable in sortedDrawables)
            {
                var drawableEffect = drawable.UsesEffect ? drawable.Effect : DefaultEffect;

                if (drawableEffect != currentEffect)
                {
                    Core.SpriteBatch.End();
                    Core.SpriteBatch.Begin(
                        transformMatrix: cameraMatrix,
                        samplerState: SamplerState.PointClamp,
                        sortMode: SpriteSortMode.Deferred,
                        blendState: BlendState.AlphaBlend,
                        effect: drawableEffect
                    );
                    currentEffect = drawableEffect;
                }

                drawable.OnDraw();
            }

            Core.SpriteBatch.End();
        }
    }

    private void CombinePasses()
    {
        Device.SetRenderTarget(FinalRenderTarget);
        Device.Clear(Color.Transparent);
        
        Core.SpriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Opaque, SamplerState.PointClamp, effect: _combinePassesEffect);

        _combinePassesEffect.Parameters["AlbedoRT"].SetValue(BasePassRenderTarget);
        _combinePassesEffect.Parameters["LightingRT"].SetValue(LightingPassRenderTarget);
        
        var rect = new Rectangle(0, 0, Window.Width, Window.Height);
        
        Core.SpriteBatch.DrawFilledRectangle(rect, Color.White);
        
        Core.SpriteBatch.End();
    }

    protected override void OnWindowResized(object sender, WindowEventArgs args)
    {
        base.OnWindowResized(sender, args);

        CreateBaseAndLightingRenderTargets();
    }

    public override void Initialize()
    {
        base.Initialize();
        CreateBaseAndLightingRenderTargets();

        _combinePassesEffect = Resources.LoadAsset<Effect>("Effects/DefferedRenderCombine");
    }

    protected override void OnCleanUp()
    {
        base.OnCleanUp();
        CleanUpBaseAndLightingRenderTargets();
    }

    private void CreateBaseAndLightingRenderTargets()
    {
        // set the base render target
        BasePassRenderTarget?.Dispose();
        BasePassRenderTarget = CreateRenderTarget();
        
        // set the lighting render target
        LightingPassRenderTarget?.Dispose();
        LightingPassRenderTarget = CreateRenderTarget();
    }

    private void CleanUpBaseAndLightingRenderTargets()
    {
        BasePassRenderTarget?.Dispose();
        LightingPassRenderTarget?.Dispose();
    }
}