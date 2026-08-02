Texture2D<float4> TextureSampler : register(t0);
SamplerState TextureSamplerState : register(s0);

float hueShift;
float saturation;

struct PS_INPUT
{
    float4 Position : SV_Position;
    float4 Color    : COLOR0;
    float2 TexCoord : TEXCOORD0;
};

float3 RGBtoHSV(float3 rgb)
{
    float maximum = max(max(rgb.r, rgb.g), rgb.b);
    float minimum = min(min(rgb.r, rgb.g), rgb.b);
    float delta = maximum - minimum;

    float hue = 0.0f;

    if (delta > 0.00001f)
    {
        if (maximum == rgb.r)
        {
            hue = (rgb.g - rgb.b) / delta;
        }
        else if (maximum == rgb.g)
        {
            hue = ((rgb.b - rgb.r) / delta) + 2.0f;
        }
        else
        {
            hue = ((rgb.r - rgb.g) / delta) + 4.0f;
        }

        hue -= 6.0f * floor(hue / 6.0f);
    }

    float sat = maximum <= 0.00001f
        ? 0.0f
        : delta / maximum;

    return float3(hue, sat, maximum);
}

float3 HSVtoRGB(float3 hsv)
{
    float hue = hsv.x - 6.0f * floor(hsv.x / 6.0f);
    float chroma = hsv.z * hsv.y;
    float x = chroma * (1.0f - abs(fmod(hue, 2.0f) - 1.0f));
    float match = hsv.z - chroma;

    float3 rgb;

    if (hue < 1.0f)
    {
        rgb = float3(chroma, x, 0.0f);
    }
    else if (hue < 2.0f)
    {
        rgb = float3(x, chroma, 0.0f);
    }
    else if (hue < 3.0f)
    {
        rgb = float3(0.0f, chroma, x);
    }
    else if (hue < 4.0f)
    {
        rgb = float3(0.0f, x, chroma);
    }
    else if (hue < 5.0f)
    {
        rgb = float3(x, 0.0f, chroma);
    }
    else
    {
        rgb = float3(chroma, 0.0f, x);
    }

    return rgb + match;
}

float4 MainPS(PS_INPUT input) : SV_Target0
{
    float4 color = TextureSampler.Sample(
        TextureSamplerState,
        input.TexCoord
    );

    // Preserve the color supplied by SpriteBatch.Draw().
    color *= input.Color;

    float3 hsv = RGBtoHSV(color.rgb);

    hsv.x += hueShift;
    hsv.x -= 6.0f * floor(hsv.x / 6.0f);
    hsv.y = saturate(hsv.y * saturation);

    color.rgb = HSVtoRGB(hsv);

    return color;
}

technique MainTechnique
{
    pass P0
    {
        PixelShader = compile ps_6_0 MainPS();
    }
}