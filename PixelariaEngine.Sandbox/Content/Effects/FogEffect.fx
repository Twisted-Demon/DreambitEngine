sampler2D TextureSampler : register(s0);
sampler2D NoiseSampler : register(s1);

struct PS_INPUT
{
    float4 Position : SV_POSITION;
    float4 Color    : COLOR0;
    float2 TexCoord : TEXCOORD0;
};

float fogStart;
float fogEnd;
float4 fogColor;
float2 noiseScale;
float2 noiseOffset;

float4 MainPS(PS_INPUT input) : SV_TARGET
{
    float depth = input.Position.y; // Using y-position as depth
    float fogFactor = saturate((fogEnd - depth) / (fogEnd - fogStart));

    // Sample the main texture
    float4 textureColor = tex2D(TextureSampler, input.TexCoord) * input.Color;

    // Sample the noise texture
    float2 noiseTexCoord = input.TexCoord * noiseScale + noiseOffset;
    float noiseValue = tex2D(NoiseSampler, noiseTexCoord).r; // Use the alpha channel

    // Modulate the fog factor with the noise value
    fogFactor *= noiseValue;

    // Since we're using premultiplied alpha, we need to handle blending accordingly
    // Blend the fog color with the texture color, considering premultiplied alpha
    float3 finalRGB = lerp(fogColor.rgb * textureColor.a, textureColor.rgb, fogFactor);

    // The alpha is already premultiplied
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