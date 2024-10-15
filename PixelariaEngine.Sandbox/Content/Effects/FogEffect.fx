sampler2D TextureSampler : register(s0);

struct PS_INPUT
{
    float4 Position : SV_POSITION;
    float4 Color    : COLOR0;
    float2 TexCoord : TEXCOORD0;
};

float fogStart;
float fogEnd;
float4 fogColor;

float4 MainPS(PS_INPUT input) : SV_TARGET
{
    float depth = input.Position.y; //using y-position as depth
    float fogFactor = saturate((fogEnd - depth) / (fogEnd - fogStart));

    float4 textureColor = tex2D(TextureSampler, input.TexCoord) * input.Color;
    float3 finalRGB = lerp(fogColor.rgb, textureColor.rgb, fogFactor);

    float finalAlpha = textureColor.a;

    return float4(finalRGB, finalAlpha);
}

technique MainTechnique
{
    pass P0
    {
        PixelShader = compile ps_3_0 MainPS();
    }
}