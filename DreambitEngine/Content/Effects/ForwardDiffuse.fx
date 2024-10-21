struct PS_INPUT
{
    float4 Position : SV_POSITION;
    float4 Color    : COLOR0;
    float2 TexCoord : TEXCOORD0;
};

sampler2D TextureSampler : register(s0);

float4 MainPS(PS_INPUT input) : SV_TARGET
{
    return tex2D(TextureSampler, input.TexCoord) * input.Color;
}

technique MainTechnique
{
    pass P0
    {
        PixelShader = compile ps_3_0 MainPS();
    }
}