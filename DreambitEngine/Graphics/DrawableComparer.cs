using System.Collections;
using System.Collections.Generic;
using Dreambit.ECS;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Dreambit;

public sealed class DrawableComparer : IComparer<DrawableComponent>
{
    private readonly Effect _defaultEffect;

    public DrawableComparer(Effect defaultEffect)
    {
        _defaultEffect =  defaultEffect;
    }
    
    public int Compare(DrawableComponent? x, DrawableComponent? y)
    {
        float dy = x.Transform.WorldPosition.Y - y.Transform.WorldPosition.Y;
        if (dy < 0f) return -1;
        if(dy > 0f) return 1;
        
        var ex = x.UsesEffect ? x.Effect : _defaultEffect;
        var ey = y.UsesEffect ? y.Effect : _defaultEffect;

        if (!ReferenceEquals(ex, ey))
        {
            int hx = ex.GetHashCode();
            int hy = ey.GetHashCode();
            if (hx < hy) return -1;
            if (hx > hy) return 1;
        }

        return 0;
    }
}