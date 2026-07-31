using System;

namespace Dreambit;

public static class RandomExtensions
{
    public static float NextFloat(this Random random, float min, float max)
    {
        return (random.NextSingle() + Mathf.Epsilon) * (max - min) + min;
    }
}