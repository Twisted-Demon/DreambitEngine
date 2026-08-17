Texture2D<float4> TextureSampler : register(t0);
SamplerState TextureSamplerState : register(s0);

struct PS_INPUT
{
    float4 Position : SV_Position;
    float4 Color    : COLOR0;
    float2 TexCoord : TEXCOORD0;
};

float Exposure;

// ============================================================
// Tone Mapping
// ============================================================

static const int TONEMAPPER_NONE              = 0;
static const int TONEMAPPER_REINHARD          = 1;
static const int TONEMAPPER_REINHARD_EXTENDED = 2;
static const int TONEMAPPER_HABLE             = 3;
static const int TONEMAPPER_ACES              = 4;
static const int TONEMAPPER_LOTTES            = 5;
static const int TONEMAPPER_UCHIMURA          = 6;
static const int TONEMAPPER_AGX               = 7;

int ToneMapper;


// ============================================================
// None / Clamp
// ============================================================

float3 ToneMapNone(float3 color)
{
    return saturate(color);
}


// ============================================================
// Reinhard
// ============================================================

float3 ToneMapReinhard(float3 color)
{
    return color /
        (1.0f + color);
}


// ============================================================
// Extended Reinhard
// ============================================================

float3 ToneMapReinhardExtended(
    float3 color)
{
    // HDR input value treated as reference white.
    const float whitePoint = 4.0f;

    float whiteSquared =
        whitePoint *
        whitePoint;

    return saturate(
        (
            color *
            (
                1.0f +
                color /
                whiteSquared
            )
        ) /
        (
            1.0f +
            color
        )
    );
}


// ============================================================
// Hable / Uncharted 2 Filmic
// ============================================================

float3 HableCurve(float3 color)
{
    const float A = 0.15f;
    const float B = 0.50f;
    const float C = 0.10f;
    const float D = 0.20f;
    const float E = 0.02f;
    const float F = 0.30f;

    return
        (
            (
                color *
                (
                    A * color +
                    C * B
                ) +
                D * E
            ) /
            (
                color *
                (
                    A * color +
                    B
                ) +
                D * F
            )
        ) -
        E / F;
}

float3 ToneMapHable(float3 color)
{
    const float exposureBias =
        2.0f;

    const float whitePoint =
        11.2f;

    color *=
        exposureBias;

    float3 mapped =
        HableCurve(color);

    float3 white =
        float3(
            whitePoint,
            whitePoint,
            whitePoint);

    float3 whiteScale =
        1.0f /
        HableCurve(white);

    return saturate(
        mapped *
        whiteScale);
}


// ============================================================
// ACES
//
// Narkowicz ACES filmic approximation.
// This is the tone mapper Dreambit currently uses.
// ============================================================

float3 ToneMapACES(float3 color)
{
    const float a = 2.51f;
    const float b = 0.03f;
    const float c = 2.43f;
    const float d = 0.59f;
    const float e = 0.14f;

    return saturate(
        (
            color *
            (
                a * color +
                b
            )
        ) /
        (
            color *
            (
                c * color +
                d
            ) +
            e
        )
    );
}


// ============================================================
// Lottes
// ============================================================

float3 ToneMapLottes(float3 color)
{
    const float a =
        1.6f;

    const float d =
        0.977f;

    const float hdrMax =
        8.0f;

    const float midIn =
        0.18f;

    const float midOut =
        0.267f;

    float hdrMaxA =
        pow(
            hdrMax,
            a);

    float hdrMaxAD =
        pow(
            hdrMax,
            a * d);

    float midInA =
        pow(
            midIn,
            a);

    float midInAD =
        pow(
            midIn,
            a * d);

    float b =
        (
            -midInA +
            hdrMaxA *
            midOut
        ) /
        (
            (
                hdrMaxAD -
                midInAD
            ) *
            midOut
        );

    float c =
        (
            hdrMaxAD *
            midInA -

            hdrMaxA *
            midInAD *
            midOut
        ) /
        (
            (
                hdrMaxAD -
                midInAD
            ) *
            midOut
        );

    float3 numerator =
        pow(
            max(
                color,
                0.0f),
            a);

    float3 denominator =
        pow(
            max(
                color,
                0.0f),
            a * d) *
        b +
        c;

    return saturate(
        numerator /
        denominator);
}


// ============================================================
// Uchimura
//
// Gran Turismo style tone mapping.
//
// Has an explicit:
// toe
// linear section
// shoulder
// ============================================================

float3 ToneMapUchimura(float3 color)
{
    const float P = 1.0f;
    const float a = 1.0f;
    const float m = 0.22f;
    const float l = 0.40f;
    const float c = 1.33f;
    const float b = 0.0f;

    float l0 =
        ((P - m) * l) /
        a;

    float L0 =
        m -
        m / a;

    float L1 =
        m +
        (1.0f - m) /
        a;

    float S0 =
        m +
        l0;

    float S1 =
        m +
        a * l0;

    float C2 =
        (a * P) /
        (P - S1);

    float CP =
        -C2 /
        P;

    float3 w0 =
        1.0f -
        smoothstep(
            0.0f,
            m,
            color);

    float3 w2 =
        step(
            m + l0,
            color);

    float3 w1 =
        1.0f -
        w0 -
        w2;

    float3 safeColor =
        max(
            color,
            0.0f);

    float3 T =
        m *
        pow(
            safeColor /
            m,
            c) +
        b;

    float3 S =
        P -
        (
            P -
            S1
        ) *
        exp(
            CP *
            (
                safeColor -
                S0
            )
        );

    float3 L =
        m +
        a *
        (
            safeColor -
            m
        );

    return saturate(
        T * w0 +
        L * w1 +
        S * w2);
}


// ============================================================
// AgX
// ============================================================

float3 AgXContrastApprox(float3 x)
{
    float3 x2 =
        x * x;

    float3 x4 =
        x2 * x2;

    return
        15.5f *
        x4 *
        x2 -

        40.14f *
        x4 *
        x +

        31.96f *
        x4 -

        6.868f *
        x2 *
        x +

        0.4298f *
        x2 +

        0.1191f *
        x -

        0.00232f;
}

float3 ToneMapAgX(float3 color)
{
    // Transform linear sRGB into the AgX working space.
    const float3x3 insetMatrix =
    {
        0.8566271533f, 0.1373189729f, 0.1118982130f,
        0.0951212405f, 0.7612419906f, 0.0767994186f,
        0.0482516061f, 0.1014390365f, 0.8113023688f
    };

    const float3x3 outsetMatrix =
    {
         1.1271005818f, -0.1413297635f, -0.1413297635f,
        -0.1106066431f,  1.1578237022f, -0.1106066431f,
        -0.0164939387f, -0.0164939387f,  1.2519364067f
    };

    color =
        mul(
            insetMatrix,
            color);

    color =
        max(
            color,
            1e-10f);

    // AgX works in logarithmic exposure space.
    const float minEv =
        -12.47393f;

    const float maxEv =
        4.026069f;

    color =
        log2(color);

    color =
        (
            color -
            minEv
        ) /
        (
            maxEv -
            minEv
        );

    color =
        saturate(color);

    color =
        AgXContrastApprox(
            color);

    color =
        mul(
            outsetMatrix,
            color);

    color =
        max(
            color,
            0.0f);

    return saturate(color);
}


// ============================================================
// Dispatcher
// ============================================================

float3 ApplyToneMapping(
    float3 color)
{
    color =
        max(
            color,
            0.0f);

    switch (ToneMapper)
    {
        case TONEMAPPER_NONE:
            return ToneMapNone(
                color);

        case TONEMAPPER_REINHARD:
            return ToneMapReinhard(
                color);

        case TONEMAPPER_REINHARD_EXTENDED:
            return ToneMapReinhardExtended(
                color);

        case TONEMAPPER_HABLE:
            return ToneMapHable(
                color);

        case TONEMAPPER_ACES:
            return ToneMapACES(
                color);

        case TONEMAPPER_LOTTES:
            return ToneMapLottes(
                color);

        case TONEMAPPER_UCHIMURA:
            return ToneMapUchimura(
                color);

        case TONEMAPPER_AGX:
            return ToneMapAgX(
                color);

        default:
            return ToneMapACES(
                color);
    }
}

float3 LinearToSrgb(float3 linearColor)
{
    linearColor = max(
        linearColor,
        0.0f
    );

    float3 low =
        linearColor * 12.92f;

    float3 high =
        1.055f *
        pow(
            linearColor,
            1.0f / 2.4f
        ) -
        0.055f;

    return lerp(
        high,
        low,
        step(
            linearColor,
            0.0031308f
        )
    );
}

float InterleavedGradientNoise(float2 pixelPosition)
{
    return frac(52.9829189f * frac(dot(pixelPosition, float2(0.06711056f, 0.00583715f))));
}

float3 ApplyOutputDither(float3 srgbColor, float2 pixelPosition)
{
    float noise = InterleavedGradientNoise(pixelPosition) - 0.5f;

    const float ditherStrength = 1.0f / 255.0f;

    return saturate(srgbColor + noise * ditherStrength);
}

float4 MainPS(PS_INPUT input) : SV_Target0
{
    float4 color = TextureSampler.Sample(
        TextureSamplerState,
        input.TexCoord
    );

    color *= input.Color;

    float3 hdrColor =
        max(color.rgb, 0.0f) *
        max(Exposure, 0.0f);

    float3 mapped = //ToneMapFilmicALU(hdrColor);
       ApplyToneMapping(hdrColor);

    float3 srgb =
        LinearToSrgb(mapped);

    srgb =
        ApplyOutputDither(
            srgb,
            input.Position.xy
        );

    return float4(
        srgb,
        color.a
    );
}

technique MainTechnique
{
    pass P0
    {
        PixelShader = compile ps_6_0 MainPS();
    }
}