static const int MAX_LIGHTS = 32;

// Temporary global tuning value.
// This is measured in the same units as SortDepth.
// Once the behavior feels right, move this to PointLight2D
// so each light can have its own softness.
static const float DEPTH_SOFTNESS = 2.0f;

Texture2D<float4> TextureSampler : register(t0);
Texture2D<float> DepthTexture : register(t1);

SamplerState TextureSamplerState : register(s0);

struct PS_INPUT
{
    float4 Position : SV_Position;
    float4 Color    : COLOR0;
    float2 TexCoord : TEXCOORD0;
};

float3 AmbientColor;
int LightCount;

float2 LightsPos[MAX_LIGHTS];
float LightsRadius[MAX_LIGHTS];
float3 LightsColor[MAX_LIGHTS];
float LightsIntensity[MAX_LIGHTS];
float LightsDepth[MAX_LIGHTS];

float AttenuatePointLight(
    float distanceToLight,
    float radius)
{
    float normalizedDistance =
        distanceToLight /
        max(radius, 0.00001f);

    if (normalizedDistance >= 1.0f)
        return 0.0f;

    float distanceSq =
        normalizedDistance *
        normalizedDistance;

    float distanceFourth =
        distanceSq *
        distanceSq;

    float window =
        saturate(
            1.0f - distanceFourth);

    window *= window;

    float inverseFalloff =
        1.0f /
        (1.0f + 4.0f * distanceSq);

    return
        window *
        inverseFalloff;
}

float CalculateDepthInfluence(
    float lightDepth,
    float receiverDepth,
    float depthSoftness)
{
    // Larger SortDepth values are rendered in front.
    //
    // Negative:
    // receiver is behind the light.
    //
    // Zero:
    // receiver is at the same depth as the light.
    //
    // Positive:
    // receiver is in front of the light.
    float depthDelta =
        receiverDepth -
        lightDepth;

    // Anything behind the light, or at the same
    // depth, receives the full light.
    if (depthDelta <= 0.0f)
        return 1.0f;

    // Objects slightly in front of the light still
    // receive some illumination.
    //
    // As the receiver gets farther in front,
    // smoothly fade the light contribution to zero.
    return
        1.0f -
        smoothstep(
            0.0f,
            max(
                depthSoftness,
                0.00001f),
            depthDelta);
}

float4 MainPS(
    PS_INPUT input) : SV_Target0
{
    float4 baseColor =
        TextureSampler.Sample(
            TextureSamplerState,
            input.TexCoord);

    baseColor *=
        input.Color;

    float3 lighting =
        AmbientColor;

    int activeLightCount =
        clamp(
            LightCount,
            0,
            MAX_LIGHTS);

    float2 screenPosition =
        input.Position.xy;

    // DepthRt contains one raw SortDepth value
    // for each rendered pixel.
    //
    // Use Load instead of Sample so depth values
    // are never interpolated between neighboring
    // drawable depths.
    int2 pixelPosition =
        int2(
            input.Position.xy);

    float receiverDepth =
        DepthTexture.Load(
            int3(
                pixelPosition,
                0));

    [loop]
    for (int i = 0;
         i < activeLightCount;
         ++i)
    {
        float radius =
            max(
                LightsRadius[i],
                0.00001f);

        float2 toLight =
            LightsPos[i] -
            screenPosition;

        float distanceToLight =
            length(
                toLight);

        float attenuation =
            AttenuatePointLight(
                distanceToLight,
                radius);

        float3 radiance =
            LightsColor[i] *
            max(
                LightsIntensity[i],
                0.0f);

        float depthInfluence =
            CalculateDepthInfluence(
                LightsDepth[i],
                receiverDepth,
                DEPTH_SOFTNESS);

        lighting +=
            radiance *
            attenuation *
            depthInfluence;
    }

    float3 litColor =
        baseColor.rgb *
        lighting;

    return float4(
        litColor,
        baseColor.a);
}

technique MainTechnique
{
    pass P0
    {
        PixelShader =
            compile ps_6_0 MainPS();
    }
}