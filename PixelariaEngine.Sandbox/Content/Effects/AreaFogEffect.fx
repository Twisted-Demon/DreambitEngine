sampler2D TextureSampler : register(s0);
sampler2D NoiseSampler1 : register(s1);   // First noise texture
sampler2D NoiseSampler2 : register(s2);   // Second noise texture (optional, for extra detail)

struct PS_INPUT
{
    float4 Position : SV_POSITION;
    float4 Color    : COLOR0;
    float2 TexCoord : TEXCOORD0;
};

float fogStart;        // Fog depth start
float fogEnd;          // Fog depth end
float4 fogColor;       // Fog color
float2 noiseScale1;    // Scale for the first noise texture
float2 noiseScale2;    // Scale for the second noise texture
float2 noiseOffset1;   // Offset for the first noise texture (for scrolling)
float2 noiseOffset2;   // Offset for the second noise texture (for scrolling)
float2 windDirection;  // Wind direction
float time;            // Time for scrolling animations
float globalFogIntensity; // Controls overall fog intensity

float edgeSoftness;    // Controls the fade-out towards the edges of the quad

float4 MainPS(PS_INPUT input) : SV_TARGET
{
    // Calculate the depth-based fog factor
    float depth = input.Position.y; // Using y-position as depth
    float fogFactor = saturate((fogEnd - depth) / (fogEnd - fogStart));

    // Sample the main texture (this is your white texture)
    float4 textureColor = tex2D(TextureSampler, input.TexCoord) * input.Color;

    // Scroll the noise textures over time to simulate wind
    float2 windOffset1 = windDirection * time * 0.05; // Adjust speed of first noise
    float2 windOffset2 = windDirection * time * 0.1;  // Adjust speed of second noise

    // Sample noise textures at different scales for dynamic fog
    float2 noiseTexCoord1 = input.TexCoord * noiseScale1 + noiseOffset1 + windOffset1;
    float2 noiseTexCoord2 = input.TexCoord * noiseScale2 + noiseOffset2 + windOffset2;

    float noiseValue1 = tex2D(NoiseSampler1, noiseTexCoord1).r; // Sample first noise texture
    float noiseValue2 = tex2D(NoiseSampler2, noiseTexCoord2).r; // Sample second noise texture

    // Combine the two noise layers to create a more complex fog effect
    float combinedNoiseValue = (noiseValue1 * 0.7 + noiseValue2 * 0.3) * globalFogIntensity;

    // Modulate the fog factor with the combined noise value
    fogFactor *= combinedNoiseValue;

    // Calculate the edge softness factor based on the texture coordinates
    // This will cause the fog to fade out towards the edges of the quad
    float2 distanceFromCenter = abs(input.TexCoord - float2(0.5, 0.5)); // Distance from the center of the quad
    float edgeFactor = saturate(1.0 - max(distanceFromCenter.x, distanceFromCenter.y) * edgeSoftness);

    // Combine the edge factor with the fog factor to ensure smooth fading at the edges
    fogFactor *= edgeFactor;

    // Adjust the fog alpha based on the combined fog factor
    float fogAlpha = fogFactor * textureColor.a;

    // Blend the fog color with the texture color using the fog factor
    float3 finalRGB = lerp(fogColor.rgb * fogAlpha, textureColor.rgb, fogFactor);

    return float4(finalRGB, fogAlpha); // Return final color with modified alpha
}

technique MainTechnique
{
    pass P0
    {
        PixelShader = compile ps_3_0 MainPS();
    }
}