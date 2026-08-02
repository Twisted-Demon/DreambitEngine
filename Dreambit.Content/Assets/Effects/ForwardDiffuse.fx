Texture2D<float4> TextureSampler : register(t0);
SamplerState TextureSamplerState : register(s0);

struct PS_INPUT
{
    float4 Position : SV_Position;
    float4 Color    : COLOR0;
    float2 TexCoord : TEXCOORD0;
};

float4 MainPS(PS_INPUT input) : SV_Target0
{
    float4 textureColor = TextureSampler.Sample(
        TextureSamplerState,
        input.TexCoord
    );

    return textureColor * input.Color;
}

technique MainTechnique
{
    pass P0
    {
        PixelShader = compile ps_6_0 MainPS();
    }
}