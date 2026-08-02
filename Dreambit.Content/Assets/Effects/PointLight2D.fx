// Despite the historical WS suffix, this position is supplied in
// render-target pixel coordinates because SV_Position is screen-space.
float2 LightingPositionWS;

float3 LightColor;
float LightRadius;
float LightIntensity;

float3 AmbientColor;
float SpecularPower;
float SpecularStrength;

struct PS_INPUT
{
    float4 Position : SV_Position;
    float4 Color    : COLOR0;
    float2 TexCoord : TEXCOORD0;
};

float SmoothFalloff(
    float distanceToLight,
    float radius)
{
    float normalizedDistance = saturate(
        distanceToLight / max(radius, 0.00001f)
    );

    float falloff = 1.0f - normalizedDistance;

    return falloff * falloff *
        (3.0f - 2.0f * falloff);
}

float4 MainPS(PS_INPUT input) : SV_Target0
{
    float distanceToLight = length(
        input.Position.xy - LightingPositionWS
    );

    float attenuation = SmoothFalloff(
        distanceToLight,
        LightRadius
    );

    float highlightPower = max(
        SpecularPower,
        1.0f
    );

    float highlight = pow(
        saturate(attenuation),
        highlightPower
    );

    float shapedIntensity =
        attenuation +
        highlight * max(SpecularStrength, 0.0f);

    float3 light =
        AmbientColor +
        LightColor *
        LightIntensity *
        shapedIntensity;

    // Use SpriteBatch's vertex color so the COLOR0 interface remains
    // consistent across the Vulkan shader stages.
    light *= input.Color.rgb;

    return float4(
        light,
        attenuation * input.Color.a
    );
}

technique MainTechnique
{
    pass P0
    {
        PixelShader = compile ps_6_0 MainPS();
    }
}