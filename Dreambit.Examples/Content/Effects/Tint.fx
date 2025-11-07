sampler2D TextureSampler : register(s0);

float4 tintColor;

float4 MainPS(float2 texCoord : TEXCOORD0) : COLOR
{
    // sample the texture
    float4 color = tex2D(TextureSampler, texCoord);

    color.rgb *= tintColor.rgb; // tint the rgb channels

    return color;
}

technique MainTechnique
{
    pass P0
    {
        PixelShader = compile ps_3_0 MainPS();
    }
}