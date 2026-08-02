static const int MAX_LIGHTS = 32;

Texture2D<float4> TextureSampler : register(t0);
SamplerState TextureSamplerState : register(s0);

struct PS_INPUT
{
    float4 Position : SV_Position;
    float4 Color    : COLOR0;
    float2 TexCoord : TEXCOORD0;
};

// Light positions and radii are expressed in render-target pixels.
float3 AmbientColor;
int LightCount;

float2 LightsPos[MAX_LIGHTS];
float LightsRadius[MAX_LIGHTS];
float3 LightsColor[MAX_LIGHTS];
float LightsIntensity[MAX_LIGHTS];

float Smooth01(float value)
{
    return value * value * (3.0f - 2.0f * value);
}

float AttenuateSoft(
    float distanceToLight,
    float innerRadius,
    float outerRadius)
{
    float width = max(
        outerRadius - innerRadius,
        0.00001f
    );

    float t = saturate(
        (distanceToLight - innerRadius) / width
    );

    return 1.0f - Smooth01(t);
}

float4 MainPS(PS_INPUT input) : SV_Target0
{
    float4 baseColor = TextureSampler.Sample(
        TextureSamplerState,
        input.TexCoord
    );

    baseColor *= input.Color;

    float3 litColor = baseColor.rgb * AmbientColor;

    int activeLightCount = clamp(
        LightCount,
        0,
        MAX_LIGHTS
    );

    // SV_Position is in render-target pixel coordinates here.
    float2 screenPosition = input.Position.xy;

    [loop]
    for (int i = 0; i < activeLightCount; ++i)
    {
        float radius = max(
            LightsRadius[i],
            0.00001f
        );

        float distanceToLight = length(
            LightsPos[i] - screenPosition
        );

        float attenuation = AttenuateSoft(
            distanceToLight,
            0.0f,
            radius
        );

        float3 lightPower =
            LightsColor[i] *
            LightsIntensity[i];

        litColor +=
            baseColor.rgb *
            lightPower *
            attenuation;
    }

    return float4(
        litColor,
        baseColor.a
    );
}

technique MainTechnique
{
    pass P0
    {
        PixelShader = compile ps_6_0 MainPS();
    }
}