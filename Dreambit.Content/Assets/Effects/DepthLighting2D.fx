static const int MAX_LIGHTS = 32;

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

float4 MainPS(
    PS_INPUT input) : SV_Target0
{
    float4 baseColor =
        TextureSampler.Sample(
            TextureSamplerState,
            input.TexCoord);

    baseColor *= input.Color;

    float3 lighting =
        AmbientColor;

    int activeLightCount =
        clamp(
            LightCount,
            0,
            MAX_LIGHTS);

    float2 screenPosition =
        input.Position.xy;

    // DepthRt has exactly one float per viewport pixel.
    // Use Load instead of filtered sampling because interpolating
    // SortDepth values across sprite edges would be meaningless.
    int2 pixelPosition =
        int2(input.Position.xy);

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
            length(toLight);

        float attenuation =
            AttenuatePointLight(
                distanceToLight,
                radius);

        float3 radiance =
            LightsColor[i] *
            max(
                LightsIntensity[i],
                0.0f);

        // Larger SortDepth values render in front.
        //
        // For this first verification step:
        // - surfaces behind/equal to the light receive it
        // - surfaces in front receive none
        float depthInfluence =
            receiverDepth <= LightsDepth[i]
                ? 1.0f
                : 0.0f;

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