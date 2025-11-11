using System;
using System.Collections.Generic;
using Dreambit.ECS;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Dreambit;

public static class LightingUniforms
{
    public const int MaxLights = 32;

    private static readonly Vector2[] LightPos = new Vector2[MaxLights];
    private static readonly float[] LightRadius = new float[MaxLights];
    private static readonly Vector3[] LightColor = new Vector3[MaxLights];
    private static readonly float[] LightIntensity = new float[MaxLights];

    public static void Apply(Effect fx, IReadOnlyList<PointLight2D> lights, Camera2D camera, Vector3 ambient)
    {
        var count = 0;
        for (var i = 0; i < lights.Count && count < MaxLights; i++)
        {
            var light = lights[i];
            if (!light.Enabled) continue;

            var screen = Vector2.Transform(light.Position, camera.TransformMatrix);

            LightPos[count] = screen;
            LightRadius[count] = MathF.Max(1f, light.Radius * camera.Scale);
            LightColor[count] = light.Color.ToVector3();
            LightIntensity[count] = light.Intensity;
            count++;
        }

        fx.Parameters["AmbientColor"]?.SetValue(ambient);
        fx.Parameters["LightCount"]?.SetValue(count);

        fx.Parameters["LightsPos"]?.SetValue(LightPos);
        fx.Parameters["LightsRadius"]?.SetValue(LightRadius);
        fx.Parameters["LightsColor"]?.SetValue(LightColor);
        fx.Parameters["LightsIntensity"]?.SetValue(LightIntensity);
    }
}