using System;
using Microsoft.Xna.Framework;

namespace Dreambit;

public static class ColorExt
{
    public static Color WithHueOffset(this Color c, float degrees)
    {
        // Convert to Vector3 0..1
        var r = c.R / 255f; var g = c.G / 255f; var b = c.B / 255f;
        RgbToHsl(r, g, b, out var h, out var s, out var l);
        h = (h + degrees / 360f) % 1f; if (h < 0f) h += 1f;
        HslToRgb(h, s, l, out r, out g, out b);
        return new Color(r, g, b, c.A / 255f);

    }
    
    private static void RgbToHsl(float r, float g, float b, out float h, out float s, out float l)
    {
        float max = MathF.Max(r, MathF.Max(g, b));
        float min = MathF.Min(r, MathF.Min(g, b));
        l = (max + min) * 0.5f;

        if (MathF.Abs(max - min) < 1e-6f) { h = 0f; s = 0f; return; }

        float d = max - min;
        s = l > 0.5f ? d / (2f - max - min) : d / (max + min);

        if (max == r)       h = (g - b) / d + (g < b ? 6f : 0f);
        else if (max == g)  h = (b - r) / d + 2f;
        else                h = (r - g) / d + 4f;
        h /= 6f;
    }
    
    private static void HslToRgb(float h, float s, float l, out float r, out float g, out float b)
    {
        if (s <= 1e-6f) { r = g = b = l; return; }
        float q = l < 0.5f ? l * (1f + s) : l + s - l * s;
        float p = 2f * l - q;
        r = HueToRgb(p, q, h + 1f / 3f);
        g = HueToRgb(p, q, h);
        b = HueToRgb(p, q, h - 1f / 3f);
    }
    
    private static float HueToRgb(float p, float q, float t)
    {
        if (t < 0f) t += 1f; if (t > 1f) t -= 1f;
        if (t < 1f / 6f) return p + (q - p) * 6f * t;
        if (t < 1f / 2f) return q;
        if (t < 2f / 3f) return p + (q - p) * (2f / 3f - t) * 6f;
        return p;
    }
}