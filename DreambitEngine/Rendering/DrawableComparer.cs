using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Dreambit.ECS;
using Microsoft.Xna.Framework.Graphics;

namespace Dreambit;

public sealed class DrawableComparer : IComparer<DrawableComponent>
{
    private readonly Effect _defaultEffect;

    public DrawableComparer(Effect defaultEffect)
    {
        _defaultEffect = defaultEffect;
    }

    public int Compare(
        DrawableComponent x,
        DrawableComponent y)
    {
        if (ReferenceEquals(x, y))
            return 0;

        if (x is null)
            return -1;

        if (y is null)
            return 1;

        var depthComparison =
            x.SortDepth.CompareTo(y.SortDepth);

        if (depthComparison != 0)
            return depthComparison;

        var effectX = x.UsesEffect
            ? x.Effect
            : _defaultEffect;

        var effectY = y.UsesEffect
            ? y.Effect
            : _defaultEffect;

        if (ReferenceEquals(effectX, effectY))
            return 0;

        return RuntimeHelpers.GetHashCode(effectX)
            .CompareTo(RuntimeHelpers.GetHashCode(effectY));
    }
}
