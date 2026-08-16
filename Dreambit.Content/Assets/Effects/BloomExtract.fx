Texture2D<float4> TextureSampler : register(t0);
SamplerState TextureSamplerState : register(s0);

struct PS_INPUT
{
    float4 Position : SV_Position;
    float4 Color : COLOR0;
    float2 TexCoord : TEXCOORD0;
};

float Threshold;
float SoftKnee;

float4 MainPS(PS_INPUT input) : SV_Target0
{
    float4 color = TextureSampler.Sample(TextureSamplerState, input.TexCoord);

    color *= input.Color;

    float brightness = max(max(color.r, color.g), color.b);

    float threshold = max(Threshold, 0.0f);

    float knee = max(threshold * saturate(SoftKnee), 0.00001f);

    float soft = brightness - threshold + knee;

    soft = clamp(soft, 0.0f, 2.0f * knee);

    soft = soft * soft / (4.0f * knee + 0.00001f);

    float contribution = max(brightness - threshold, soft);

    contribution /= max(brightness, 0.00001f);

    return float4(color.rgb * contribution, 1.0f);
}

technique MainTechnique
{
    pass P0
    {
        PixelShader =
            compile ps_6_0 MainPS();
    }
}