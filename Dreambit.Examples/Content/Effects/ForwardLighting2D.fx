struct PS_INPUT
{
    float4 Position : SV_POSITION;
    float4 Color    : COLOR0;
    float2 TexCoord : TEXCOORD0;
};

sampler2D TextureSampler : register(s0);

// ---- Lighting uniforms ----
float3 AmbientColor;                 // 0..1
int    LightCount;                   // <= MAX_LIGHTS

static const int MAX_LIGHTS = 32;
float2 LightsPos[MAX_LIGHTS];        // screen-space (pixels)
float  LightsRadius[MAX_LIGHTS];     // pixels
float3 LightsColor[MAX_LIGHTS];      // 0..1
float  LightsIntensity[MAX_LIGHTS];  // scalar (e.g., 0.2..4.0)

float smooth01(float t)
{
    return t * t * (3.0 - 2.0 * t);
}

// Smooth falloff: (1 - (d/r)^2)^2, clamped
float Attenuate(float dist, float radius)
{
    float x = saturate(1.0 - (dist * dist) / (radius * radius));
    return x * x;
}

float Attenuate_Soft(float dist, float inner, float outer)
{
    float t = saturate((dist - inner) / max(outer - inner, 1e-5));
    return 1.0 - smooth01(t);
}

float4 MainPS(PS_INPUT input, float2 screenPos : VPOS) : SV_TARGET
{
    float4 baseColor = tex2D(TextureSampler, input.TexCoord) * input.Color;

    // Ambient term (modulates albedo)
    float3 lit = baseColor.rgb * AmbientColor;

    // Accumulate point lights in screen space
    [loop]
    for (int i = 0; i < LightCount; i++)
    {
        float2 L = LightsPos[i] - screenPos;
        float  d = length(L);
        float  a = Attenuate_Soft(d, 0, LightsRadius[i]);           // 0..1
        float3 c = LightsColor[i] * LightsIntensity[i];     // light power

        // Simple 2D lambert (no normals): light scales albedo
        lit += baseColor.rgb * c * a;
    }

    return float4(lit, baseColor.a);
}

technique MainTechnique
{
    pass P0
    {
        PixelShader = compile ps_3_0 MainPS();
        AlphaBlendEnable = TRUE;
    }
}