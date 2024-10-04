// Basic custom effect for sprite rendering
// This shader applies the world, view, and projection transformations
// and renders the texture to the screen

// Matrices for transforming the vertices
matrix World;
matrix View;
matrix Projection;

// The texture to be rendered
Texture2D SpriteTexture;
sampler TextureSampler : register(s0);

// Input structure for vertex shader (from CPU-side code)
struct VertexInput
{
    float3 Position : POSITION0;  // Vertex position
    float4 Color : COLOR0;        // Vertex color
    float2 TexCoord : TEXCOORD0;  // UV coordinates
};

// Output structure for the pixel shader
struct VertexOutput
{
    float4 Position : SV_POSITION; // Transformed position (for rasterization)
    float4 Color : COLOR0;         // Color passed to pixel shader
    float2 TexCoord : TEXCOORD0;   // UV coordinates for texturing
};

// Vertex shader: transforms the vertices with World, View, Projection matrices
VertexOutput VS_Main(VertexInput input)
{
    VertexOutput output;

    // Apply World, View, and Projection transformations to the vertex position
    float4 worldPosition = mul(float4(input.Position, 1.0), World);
    float4 viewPosition = mul(worldPosition, View);
    output.Position = mul(viewPosition, Projection);

    // Pass through the color and texture coordinates
    output.Color = input.Color;
    output.TexCoord = input.TexCoord;

    return output;
}

// Pixel shader: applies the texture and the color to each pixel
float4 PS_Main(VertexOutput input) : SV_Target
{
    // Sample the texture at the given UV coordinates
    float4 textureColor = tex2D(TextureSampler, input.TexCoord);

    // Multiply the texture color by the vertex color (for tinting)
    return textureColor * input.Color;
}

// Define the techniques and passes
technique BasicTechnique
{
    pass P0
    {
        // Specify the shaders to use
        VertexShader = compile vs_3_0 VS_Main();
        PixelShader  = compile ps_3_0 PS_Main();
    }
}