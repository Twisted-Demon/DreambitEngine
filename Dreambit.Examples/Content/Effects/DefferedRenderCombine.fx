texture AlbedoRT : register(t0);
texture LightingRT : register(t1);

sampler AlbedoS = sampler_state {
    Texture = <AlbedoRT>;
    MinFilter = Linear; MagFilter = Linear; MipFilter = Linear;
    AddressU = Clamp; AddressV = Clamp;
};

sampler LightingS = sampler_state {
    Texture = <LightingRT>;
    MinFilter = Linear; MagFilter = Linear; MipFilter = Linear;
    AddressU = Clamp; AddressV = Clamp;
};

struct VSIn  { float4 Position : POSITION0; float2 TexCoord : TEXCOORD0; };
struct VSOut { float4 Position : POSITION0; float2 TexCoord : TEXCOORD0; };

VSOut VS(VSIn i)
{
    VSOut o;
    o.Position = i.Position;
    o.TexCoord = i.TexCoord;
    return o;
}

float4 PS(VSOut i) : COLOR0
{
    float4 albedo   = tex2D(AlbedoS, i.TexCoord);     // linear color
    float3 lighting = float3(1.0, 1.0, 1.0); // ambient + lights
    float3 outRGB   = albedo.rgb * lighting;
    return float4(outRGB, albedo.a);
}

technique Composite
{
    pass P0
    {
        VertexShader = compile vs_3_0 VS();
        PixelShader  = compile ps_3_0 PS();
    }
}