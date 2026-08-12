Texture2D<float4> AlbedoRT : register(t0);
Texture2D<float4> LightingRT : register(t1);
SamplerState AlbedoS : register(s0);
SamplerState LightingS : register(s1);

struct VSIn  { float4 Position : POSITION0; float2 TexCoord : TEXCOORD0; };
struct VSOut { float4 Position : SV_Position; float2 TexCoord : TEXCOORD0; };

VSOut VS(VSIn i)
{
    VSOut o;
    o.Position = i.Position;
    o.TexCoord = i.TexCoord;
    return o;
}

float4 PS(VSOut i) : SV_Target0
{
    float4 albedo   = AlbedoRT.Sample(AlbedoS, i.TexCoord);
    float3 lighting = LightingRT.Sample(LightingS, i.TexCoord).rgb;
    float3 outRGB   = albedo.rgb * lighting;
    return float4(outRGB, albedo.a);
}

technique Composite
{
    pass P0
    {
        VertexShader = compile vs_6_0 VS();
        PixelShader  = compile ps_6_0 PS();
    }
}
