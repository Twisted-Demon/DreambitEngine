sampler2D TextureSampler : register(s0);

struct PS_INPUT
{
    float4 Position : SV_POSITION;
    float4 Color    : COLOR0;
    float2 TexCoord : TEXCOORD0;
};

float fogDensity;
float fogStart;
float fogEnd;
float4 fogColor;

float4 MainPS(PS_INPUT input) : SV_TARGET
{
    float4 sceneColor = tex2D(TextureSampler, input.TexCoord) * input.Color;
    float4 pos = input.Position;

    float distanceFromCamera = input.TexCoord.y * 100.0;
    float fogFactor = clamp((distanceFromCamera - fogStart) / (fogEnd - fogStart), 0.0, 1.0);

    float4 blendedColor = fogColor * fogFactor * fogDensity;
    return blendedColor + sceneColor * (1.0 - blendedColor.a);
}

technique MainTechnique
{
    pass P0
    {
        PixelShader = compile ps_3_0 MainPS();
    }
}