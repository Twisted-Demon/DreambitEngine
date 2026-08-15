Texture2D<float4> TextureSampler : register(t0);
SamplerState TextureSamplerState : register(s0);

struct PS_INPUT
{
    float4 Position : SV_Position;
    float4 Color    : COLOR0;
    float2 TexCoord : TEXCOORD0;
};

float Exposure;

float3 ToneMapACES(float3 color)
{
    // Narkowicz ACES filmic approximation.
    //
    // This is intentionally inexpensive and appropriate for the final
    // presentation pass rather than the lighting pass.
    const float a = 2.51f;
    const float b = 0.03f;
    const float c = 2.43f;
    const float d = 0.59f;
    const float e = 0.14f;

    return saturate(
        (color * (a * color + b)) /
        (color * (c * color + d) + e)
    );
}

float3 LinearToSrgb(float3 linearColor)
{
    linearColor = max(
        linearColor,
        0.0f
    );

    float3 low =
        linearColor * 12.92f;

    float3 high =
        1.055f *
        pow(
            linearColor,
            1.0f / 2.4f
        ) -
        0.055f;

    return lerp(
        high,
        low,
        step(
            linearColor,
            0.0031308f
        )
    );
}

float4 MainPS(PS_INPUT input) : SV_Target0
{
    float4 color = TextureSampler.Sample(
        TextureSamplerState,
        input.TexCoord
    );

    color *= input.Color;

    float3 hdrColor =
        max(color.rgb, 0.0f) *
        max(Exposure, 0.0f);

    float3 mapped =
        ToneMapACES(hdrColor);

    return float4(
        LinearToSrgb(mapped),
        color.a
    );
}

technique MainTechnique
{
    pass P0
    {
        PixelShader = compile ps_6_0 MainPS();
    }
}