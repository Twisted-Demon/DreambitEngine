using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Dreambit.ECS;

public abstract class DrawableComponent : Component
{
    private int _drawLayer;
    public abstract Rectangle Bounds { get; }

    public Effect Effect { get; set; }

    public bool UsesEffect => Effect != null;

    public int DrawLayer
    {
        get => _drawLayer;
        set => OnDrawLayerChanged(value);
    }

    public virtual void OnDraw()
    {
    }

    public virtual void OnPreDraw()
    {
    }

    public virtual void OnPostDraw()
    {
    }

    private void OnDrawLayerChanged(int newDrawLayer)
    {
        if (_drawLayer == newDrawLayer)
            return;

        var oldDrawLayer = _drawLayer;
        _drawLayer = newDrawLayer;

        Scene.Drawables.UpdateDrawableDrawLayer(this, oldDrawLayer, newDrawLayer);
    }

    public virtual bool IsVisibleFromCamera(Rectangle cameraBounds)
    {
        return cameraBounds.Intersects(Bounds);
    }
}

public abstract class DrawableComponent<T> : DrawableComponent where T : DrawableComponent
{
    protected readonly Logger<T> Logger = new();
}