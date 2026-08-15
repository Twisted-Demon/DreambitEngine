static const int MAX_LIGHTS = 32;

Texture2D<float4> TextureSampler : register(t0);
SamplerState TextureSamplerState : register(s0);

struct PS_INPUT
{
    float4 Position : SV_Position;
    float4 Color    : COLOR0;
    float2 TexCoord : TEXCOORD0;
};

// All light positions/radii are in render-target pixels.
float3 AmbientColor;
int LightCount;

float2 LightsPos[MAX_LIGHTS];
float LightsRadius[MAX_LIGHTS];
float3 LightsColor[MAX_LIGHTS];
float LightsIntensity[MAX_LIGHTS];

// Finite-range attenuation with an inverse-square-like response.
//
// A real inverse-square function has infinite range and becomes singular
// at distance zero, neither of which is particularly useful for authored
// 2D point lights.
//
// Distance is normalized by the authored radius so Intensity remains
// predictable regardless of resolution or camera scale.
float AttenuatePointLight(
    float distanceToLight,
    float radius)
{
    float normalizedDistance =
        distanceToLight / max(radius, 0.00001f);

    if (normalizedDistance >= 1.0f)
        return 0.0f;

    float distanceSq =
        normalizedDistance * normalizedDistance;

    // Smooth finite-radius cutoff.
    //
    // Squaring this window prevents an obvious hard ring at the edge.
    float distanceFourth =
        distanceSq * distanceSq;

    float window =
        saturate(1.0f - distanceFourth);

    window *= window;

    // Inverse-square-inspired decrease without a singularity at the
    // center of the light.
    float inverseFalloff =
        1.0f / (1.0f + 4.0f * distanceSq);

    return window * inverseFalloff;
}

float4 MainPS(PS_INPUT input) : SV_Target0
{
    float4 baseColor = TextureSampler.Sample(
        TextureSamplerState,
        input.TexCoord
    );

    baseColor *= input.Color;

    float3 lighting = AmbientColor;

    int activeLightCount = clamp(
        LightCount,
        0,
        MAX_LIGHTS
    );

    // SpriteBatch's SV_Position is already expressed in render-target
    // pixel coordinates, matching LightingUniforms.
    float2 screenPosition = input.Position.xy;

    [loop]
    for (int i = 0; i < activeLightCount; ++i)
    {
        float radius = max(
            LightsRadius[i],
            0.00001f
        );

        float2 toLight =
            LightsPos[i] - screenPosition;

        float distanceToLight =
            length(toLight);

        float attenuation =
            AttenuatePointLight(
                distanceToLight,
                radius
            );

        float3 radiance =
            LightsColor[i] *
            max(LightsIntensity[i], 0.0f);

        lighting +=
            radiance *
            attenuation;
    }

    // Do not saturate here.
    //
    // SceneRenderTarget is HDR, so values > 1 are deliberately preserved
    // for tone mapping and eventual bloom.
    float3 litColor =
        baseColor.rgb *
        lighting;

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