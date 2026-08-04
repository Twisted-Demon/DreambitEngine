using System;
using Microsoft.Xna.Framework.Graphics;

namespace Dreambit.ECS;

public abstract class DrawableComponent : Component
{
    private int _drawLayer;
    public abstract RectangleF Bounds { get; }

    public Effect Effect { get; set; } = null;

    public bool UsesEffect => Effect != null;

    public virtual int DrawLayer
    {
        get => _drawLayer;
        set => OnDrawLayerChanged(value);
    }

    public void Draw()
    {
        if (IsFaulted() && !Enabled) return;

        try
        {
            OnDraw();
        }
        catch (Exception exception)
        {
            HandleCallbackException(nameof(Draw), exception);
        }
    }

    protected virtual void OnDraw()
    {
    }

    public virtual void OnPreDraw()
    {
    }

    public virtual void OnPostDraw()
    {
    }

    public void DrawUi()
    {
        if (IsFaulted() && !Enabled) return;

        try
        {
            OnDrawUi();
        }
        catch (Exception exception)
        {
            HandleCallbackException(nameof(OnDrawUi), exception);
        }
    }

    protected virtual void OnDrawUi()
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

    public virtual bool IsVisibleFromCamera(RectangleF cameraBounds)
    {
        return cameraBounds.Intersects(Bounds);
    }
}

public abstract class DrawableComponent<T> : DrawableComponent where T : DrawableComponent
{
    protected readonly Logger<T> Logger = new();
}