Texture2D<float4> TextureSampler : register(t0);
SamplerState TextureSamplerState : register(s0);

struct PS_INPUT
{
    float4 Position : SV_Position;
    float4 Color    : COLOR0;
    float2 TexCoord : TEXCOORD0;
};

float2 TexelSize;
float2 Direction;

float4 MainPS(
    PS_INPUT input) : SV_Target0
{
    float2 uv =
        input.TexCoord;

    float2 offset =
        TexelSize *
        Direction;

    // Optimized 9-tap Gaussian blur using bilinear filtering.
    // Five texture samples per pass.
    float3 color =
        TextureSampler.Sample(
            TextureSamplerState,
            uv).rgb *
        0.2270270270f;

    color +=
        TextureSampler.Sample(
            TextureSamplerState,
            uv +
            offset * 1.3846153846f).rgb *
        0.3162162162f;

    color +=
        TextureSampler.Sample(
            TextureSamplerState,
            uv -
            offset * 1.3846153846f).rgb *
        0.3162162162f;

    color +=
        TextureSampler.Sample(
            TextureSamplerState,
            uv +
            offset * 3.2307692308f).rgb *
        0.0702702703f;

    color +=
        TextureSampler.Sample(
            TextureSamplerState,
            uv -
            offset * 3.2307692308f).rgb *
        0.0702702703f;

    return float4(
        color,
        1.0f);
}

technique MainTechnique
{
    pass P0
    {
        PixelShader =
            compile ps_6_0 MainPS();
    }
}