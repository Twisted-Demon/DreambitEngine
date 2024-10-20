struct PS_INPUT
{
    float4 Position : SV_POSITION;
    float4 Color    : COLOR0;
    float2 TexCoord : TEXCOORD0;
};

sampler2D TextureSampler : register(s0);
float4 ambientColor;

float4 MainPS(PS_INPUT input) : SV_TARGET
{
    float4 fragColor = tex2D(TextureSampler, input.TexCoord) * input.Color;

    float4 ambient = ambientColor;

    return fragColor * ambient;
}

technique MainTechnique
{
    pass P0
    {
        PixelShader = compile ps_3_0 MainPS();
    }
}