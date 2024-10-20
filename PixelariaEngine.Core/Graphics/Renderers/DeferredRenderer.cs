using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework.Graphics;

namespace PixelariaEngine;

public class DeferredRenderer(Scene scene) : Renderer(scene)
{
    private List<RenderTarget2D> _albedoRenderTargets = [];

    private Effect _geometryPassEffect;
    private Effect _lightingPassEffect;

    public override void Initialize()
    {
        _geometryPassEffect = Resources.LoadAsset<Effect>("Shaders/DeferredGeometry");
        _lightingPassEffect = Resources.LoadAsset<Effect>("Shaders/DeferredLighting");
    }

    private void PreRender()
    {
        var drawables = Scene.Drawables.GetAllDrawables()
            .Where(x => x.Enabled && x.Entity.Enabled && x.DrawLayer != RenderLayers.LightLayer);

        foreach (var drawable in drawables)
            drawable.OnPreDraw();
    }
}