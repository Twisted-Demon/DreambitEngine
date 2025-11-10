sampler2D TextureSampler : register(s0);

float hueShift;
float saturation;

// Convert RGB to HSV
float3 RGBtoHSV(float3 rgb)
{
    float Cmax = max(max(rgb.r, rgb.g), rgb.b);
    float Cmin = min(min(rgb.r, rgb.g), rgb.b);
    float delta = Cmax - Cmin;

    float h = 0.0f;
    if (delta > 0.0f)
    {
        if (Cmax == rgb.r)
            h = fmod((rgb.g - rgb.b) / delta, 6.0f);
        else if (Cmax == rgb.g)
            h = (rgb.b - rgb.r) / delta + 2.0f;
        else
            h = (rgb.r - rgb.g) / delta + 4.0f;
    }

    float s = (Cmax == 0.0f) ? 0.0f : delta / Cmax;
    float v = Cmax;

    return float3(h, s, v);
}

// Convert HSV back to RGB
float3 HSVtoRGB(float3 hsv)
{
    float C = hsv.z * hsv.y;
    float X = C * (1.0f - abs(fmod(hsv.x, 2.0f) - 1.0f));
    float m = hsv.z - C;

    float3 rgb;

    if (hsv.x < 1.0f) rgb = float3(C, X, 0);
    else if (hsv.x < 2.0f) rgb = float3(X, C, 0);
    else if (hsv.x < 3.0f) rgb = float3(0, C, X);
    else if (hsv.x < 4.0f) rgb = float3(0, X, C);
    else if (hsv.x < 5.0f) rgb = float3(X, 0, C);
    else rgb = float3(C, 0, X);

    rgb += m;
    return rgb;
}

float4 MainPS(float2 texCoord : TEXCOORD0) : COLOR
{
    // Sample the texture
    float4 color = tex2D(TextureSampler, texCoord);

    // Convert to HSV
    float3 hsv = RGBtoHSV(color.rgb);

    // Adjust hue and saturation
    hsv.x = fmod(hsv.x + hueShift, 6.0f); // Shift hue (wrap around 0-6)
    hsv.y = clamp(hsv.y * saturation, 0.0f, 1.0f); // Adjust saturation

    // Convert back to RGB
    color.rgb = HSVtoRGB(hsv);

    return color;
}

technique MainTechnique
{
    pass P0
    {
        PixelShader = compile ps_3_0 MainPS();
    }
}
