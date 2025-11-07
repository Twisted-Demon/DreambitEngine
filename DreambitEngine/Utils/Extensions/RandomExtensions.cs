using System;
using Microsoft.Xna.Framework;

namespace Dreambit;

public static class RandomExtensions
{
    public static float Next(this Random random, float min, float max)
    {
        return random.NextSingle() * (max - min) + min;
    }


}