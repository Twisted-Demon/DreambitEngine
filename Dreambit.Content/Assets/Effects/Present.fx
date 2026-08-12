Texture2D<float4> TextureSampler : register(t0);
SamplerState TextureSamplerState : register(s0);

struct PS_INPUT
{
    float4 Position : SV_Position;
    float4 Color    : COLOR0;
    float2 TexCoord : TEXCOORD0;
};

float3 LinearToSrgb(float3 linearColor)
{
    float3 low = linearColor * 12.92f;
    float3 high = 1.055f * pow(max(linearColor, 0.0f), 1.0f / 2.4f) - 0.055f;
    return lerp(high, low, step(linearColor, 0.0031308f));
}

float4 MainPS(PS_INPUT input) : SV_Target0
{
    float4 color = TextureSampler.Sample(TextureSamplerState, input.TexCoord);
    color *= input.Color;
    return float4(LinearToSrgb(max(color.rgb, 0.0f)), color.a);
}

technique MainTechnique
{
    pass P0
    {
        PixelShader = compile ps_6_0 MainPS();
    }
}
