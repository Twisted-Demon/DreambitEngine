// WorldRing.fx
// MonoGame 3.8.5 DesktopVK / Vulkan
//
// RadiusWorld, ThicknessWorld, QuadSizeWorld and SoftnessWorld
// are all measured in world units.
//
// The shader performs 4x4 subpixel sampling: 16 circle tests per pixel.

#define TWO_PI 6.28318530717958647692f
#define SUBPIXEL_GRID 4

float2 QuadSizeWorld;

float RadiusWorld;
float ThicknessWorld;

float4 RingColor;
float Opacity;

// Optional physical feathering in world units.
// Keep at 0 for a clean, naturally antialiased ring.
float SoftnessWorld;

// Dashing:
//
// DashCount <= 0.5: solid ring.
// DashFill = 0.5: equal dash and gap.
// DashFill = 1.0: no visible gaps.
float DashCount;
float DashFill;

// Rotates the dash pattern around the circle.
float DashOffsetRadians;

// 0 = square dash ends.
// 1 = rounded dash ends.
float RoundDashCaps;


// Signed distance to a rectangular shape.
//
// Negative: inside.
// Zero: boundary.
// Positive: outside.
float RectangleDistance(float2 position, float2 halfSize)
{
    float2 delta = abs(position) - halfSize;

    return length(max(delta, 0.0f))
         + min(max(delta.x, delta.y), 0.0f);
}


// Signed distance to a capsule aligned along the X axis.
float CapsuleDistance(
    float tangentialPosition,
    float radialPosition,
    float halfDashLength,
    float halfThickness)
{
    // The line portion excludes the circular end caps.
    float halfLineLength =
        max(halfDashLength - halfThickness, 0.0f);

    float2 capsulePosition = float2(
        max(abs(tangentialPosition) - halfLineLength, 0.0f),
        radialPosition
    );

    return length(capsulePosition) - halfThickness;
}


// Returns the signed distance to the complete ring or dashed ring.
float RingDistance(float2 localPositionWorld)
{
    float distanceFromCenter =
        length(localPositionWorld);

    float radialPosition =
        distanceFromCenter - RadiusWorld;

    float halfThickness =
        max(ThicknessWorld * 0.5f, 0.00001f);

    // Ordinary solid ring.
    if (DashCount <= 0.5f || DashFill >= 0.9999f)
    {
        return abs(radialPosition) - halfThickness;
    }

    float safeDashCount =
        max(DashCount, 1.0f);

    float safeRadius =
        max(RadiusWorld, 0.0001f);

    // Angle around the center of the circle.
    float angle =
        atan2(
            localPositionWorld.y,
            localPositionWorld.x
        ) + DashOffsetRadians;

    // Physical world-space length of one dash-and-gap segment
    // along the centerline of the ring.
    float segmentLengthWorld =
        (TWO_PI * safeRadius) / safeDashCount;

    // Convert the angle into a position centered inside the
    // current repeating segment.
    //
    // Result:
    // -segmentLength / 2 ... +segmentLength / 2
    float segmentPhase =
        frac(
            angle * safeDashCount / TWO_PI + 0.5f
        ) - 0.5f;

    float tangentialPositionWorld =
        segmentPhase * segmentLengthWorld;

    float halfDashLengthWorld =
        segmentLengthWorld *
        saturate(DashFill) *
        0.5f;

    // Square-ended dash.
    float squareDistance = RectangleDistance(
        float2(
            tangentialPositionWorld,
            radialPosition
        ),
        float2(
            halfDashLengthWorld,
            halfThickness
        )
    );

    // Rounded-ended dash.
    float roundedDistance = CapsuleDistance(
        tangentialPositionWorld,
        radialPosition,
        halfDashLengthWorld,
        halfThickness
    );

    return lerp(
        squareDistance,
        roundedDistance,
        saturate(RoundDashCaps)
    );
}


// Converts the signed distance into one subpixel coverage sample.
float DistanceToCoverage(float signedDistanceWorld)
{
    // With no added softness, each subpixel is either covered
    // or uncovered. Averaging all sixteen samples produces the
    // antialiasing.
    if (SoftnessWorld <= 0.000001f)
    {
        return signedDistanceWorld <= 0.0f
            ? 1.0f
            : 0.0f;
    }

    // Optional additional physical feathering.
    return saturate(
        0.5f -
        signedDistanceWorld /
        (2.0f * SoftnessWorld)
    );
}


float4 PS_WorldRing(
    float4 screenPosition : SV_Position,
    float4 vertexColor    : COLOR0,
    float2 uv             : TEXCOORD0
) : SV_Target0
{
    // Convert the quad's normalized UV coordinates into a
    // position measured directly in world units.
    //
    // UV (0.5, 0.5) is the ring center.
    float2 localPositionWorld =
        (uv - float2(0.5f, 0.5f)) *
        QuadSizeWorld;

    // Determine how far one output pixel travels across the
    // quad in its local world-space coordinates.
    //
    // These derivatives are only used to position the subpixel
    // samples. They do not generate smoothstep-style blur.
    float2 pixelStepXWorld =
        ddx(localPositionWorld);

    float2 pixelStepYWorld =
        ddy(localPositionWorld);

    float coverage = 0.0f;

    // Evaluate the circle at sixteen locations inside this pixel.
    [unroll]
    for (int sampleY = 0; sampleY < SUBPIXEL_GRID; sampleY++)
    {
        [unroll]
        for (int sampleX = 0; sampleX < SUBPIXEL_GRID; sampleX++)
        {
            // Produces positions from approximately
            // -0.375 to +0.375 inside the current pixel.
            float2 subpixelOffset =
                (
                    float2(
                        (float)sampleX + 0.5f,
                        (float)sampleY + 0.5f
                    ) / (float)SUBPIXEL_GRID
                ) - 0.5f;

            float2 samplePositionWorld =
                localPositionWorld +
                pixelStepXWorld * subpixelOffset.x +
                pixelStepYWorld * subpixelOffset.y;

            float signedDistanceWorld =
                RingDistance(samplePositionWorld);

            coverage += DistanceToCoverage(
                signedDistanceWorld
            );
        }
    }

    coverage /= (float)(
        SUBPIXEL_GRID *
        SUBPIXEL_GRID
    );

    float4 tint =
        RingColor * vertexColor;

    float finalAlpha =
        saturate(tint.a * Opacity) *
        coverage;

    // MonoGame's ordinary AlphaBlend uses premultiplied alpha.
    return float4(tint.rgb * finalAlpha, finalAlpha);
}


technique WorldRing
{
    pass P0
    {
        PixelShader = compile ps_6_0 PS_WorldRing();
    }
}