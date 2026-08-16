Texture2D<float4> TextureSampler : register(t0);
Texture2D<float4> BloomTexture : register(t1);

SamplerState TextureSamplerState : register(s0);

struct PS_INPUT
{
    float4 Position : SV_Position;
    float4 Color    : COLOR0;
    float2 TexCoord : TEXCOORD0;
};

float BloomIntensity;

float4 MainPS(
    PS_INPUT input) : SV_Target0
{
    // The scene copy is full resolution, so fetch it exactly.
    // This prevents the bloom's linear filtering from softening
    // pixel-art scene color.
    int2 pixelPosition =
        int2(input.Position.xy);

    float4 scene =
        TextureSampler.Load(
            int3(
                pixelPosition,
                0));

    // Bloom is intentionally sampled linearly because its render
    // target is half resolution and should be smooth.
    float3 bloom =
        BloomTexture.Sample(
            TextureSamplerState,
            input.TexCoord).rgb;

    float3 result =
        scene.rgb +
        bloom *
        max(
            BloomIntensity,
            0.0f);

    return float4(
        result,
        scene.a);
}

technique MainTechnique
{
    pass P0
    {
        PixelShader =
            compile ps_6_0 MainPS();
    }
}