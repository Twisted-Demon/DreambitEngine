// Maximum number of lights
#define MAX_LIGHTS 8

// Structures
struct PS_INPUT
{
    float4 Position : SV_POSITION;
    float4 Color    : COLOR0;
    float2 TexCoord : TEXCOORD0;
};

sampler2D TextureSampler : register(s0);

// Light data
float Lights[7 * MAX_LIGHTS]; // Each light has 7 floats
int LightCount;

// Pixel Shader
float4 MainPS(PS_INPUT input) : SV_TARGET
{
    // Sample the texture
    float4 color = tex2D(TextureSampler, input.TexCoord) * input.Color;


    // Convert pixel position to normalized screen coordinates (0 to 1)
    float2 screenPos = input.Position.xy;

    // Ambient light factor
    float ambientFactor = 0.1;
    float3 finalColor = color.rgb * ambientFactor;

    // Apply lighting from each light source
    for (int i = 0; i < LightCount; i++)
    {
        int index = i * 7;
        float2 lightPos = float2(Lights[index], Lights[index + 1]);
        float3 lightColor = float3(Lights[index + 2], Lights[index + 3], Lights[index + 4]);
        float lightIntensity = Lights[index + 5];
        float lightRadius = Lights[index + 6];

        // Calculate distance from the light to the pixel
        float2 toLight = lightPos - screenPos;
        float distance = length(toLight);

        // Linear attenuation
        float attenuation = saturate(1.0 - (distance / lightRadius));

        // Accumulate the light's contribution
        finalColor += color.rgb * lightColor * attenuation * lightIntensity;
    }

    return float4(finalColor, color.a);
}

technique MainTechnique
{
    pass P0
    {
        PixelShader = compile ps_3_0 MainPS();
    }
}