using Microsoft.Xna.Framework.Graphics;

namespace PixelariaEngine.ECS;

public abstract class DrawableComponent : Component
{
    private int _drawLayer;

    public int DrawLayer
    {
        get => _drawLayer;
        set => OnDrawLayerChanged(value);
    }

    public abstract void OnDraw();

    private void OnDrawLayerChanged(int newDrawLayer)
    {
        if (_drawLayer == newDrawLayer)
            return;

        var oldDrawLayer = _drawLayer;
        _drawLayer = newDrawLayer;

        Scene.Drawables.UpdateDrawableDrawLayer(this, oldDrawLayer, newDrawLayer);
    }
}