float2 LightingPositionWS;
float3 LightColor;
float LightRadius;
float LightIntensity;

float3 AmbientColor;
float SpecularPower = 32;
float SpecularStrength = 0.0;

struct VSInput
{
    float4 Position : POSITION0;
    float2 TexCoord : TEXCOORD0;
};

struct VSOutput
{
    float4 Position : SV_Position;
    float2 TexCoord : TEXCOORD0;
};

float4 MainPS(VSInput input) : SV_TARGET
{
    return float4(0, 0, 0, 0);
}

technique MainTechnique
{
    pass P0
    {
        PixelShader = compile ps_3_0 MainPS();
    }
}

