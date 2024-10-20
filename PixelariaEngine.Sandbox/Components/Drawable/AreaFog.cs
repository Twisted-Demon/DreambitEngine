using System.Reflection.PortableExecutable;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PixelariaEngine.ECS;
using PixelariaEngine.Graphics;
using PixelariaEngine.Sandbox.Drawable;

namespace PixelariaEngine.Sandbox;

public class AreaFogComponent : DrawableComponent<ScreenFog>
{
    public override Rectangle Bounds => GetBounds();
    
    public int Width = 32;
    public int Height = 32;
    public PivotType PivotType { get; set; } = PivotType.Center;
    public Vector2 Pivot { get; set; }
    private Texture2D _fogNoiseTexture;
    public Vector2 NoiseOffset;
    public Vector2 NoiseScale = Vector2.One;

    public override void OnCreated()
    {
        Effect = Resources.LoadAsset<Effect>("Effects/AreaFogEffect");
        _fogNoiseTexture = Resources.LoadAsset<Texture2D>("Textures/pnoise16x512");
        DrawLayer = 5;
    }

    public override void OnPreDraw()
    {
        Effect.Parameters["edgeSoftness"].SetValue(2.0f);
        Effect.Parameters["time"].SetValue(Time.DeltaTime);
        
        // Set the wind direction (e.g., diagonal wind towards top-right)
        Effect.Parameters["windDirection"].SetValue(new Vector2(0.5f, 0.3f));
        
        Effect.Parameters["fogColor"].SetValue(new Vector4(1f, 1f, 1f, 1.0f));
        
        Effect.Parameters["noiseScale1"].SetValue(NoiseScale);
        Effect.Parameters["noiseScale2"].SetValue(NoiseScale * 2);
        
        Effect.Parameters["fogStart"].SetValue(0);
        Effect.Parameters["fogEnd"].SetValue(Window.Height);
        
        Effect.Parameters["globalFogIntensity"].SetValue(2.0f);
        
        var noiseSpeedX = 0.05f;
        var noiseSpeedY = 0.04f;

        NoiseOffset.X += noiseSpeedX * Time.DeltaTime;
        NoiseOffset.Y += noiseSpeedY * Time.DeltaTime;
        
        Effect.Parameters["NoiseSampler1"].SetValue(_fogNoiseTexture);
        Effect.Parameters["NoiseSampler2"].SetValue(_fogNoiseTexture);
        
        Effect.Parameters["noiseOffset1"].SetValue(NoiseOffset);
        Effect.Parameters["noiseOffset1"].SetValue(NoiseOffset / 2);
        
    }

    public override void OnDraw()
    {
        Core.SpriteBatch.DrawFilledRectangle(Bounds, Color.DarkGray);
    }
    
    private Rectangle GetBounds()
    {
        var pivotToUse = Transform.WorldPosToVec2;
        
        switch (PivotType)
        {
            case PivotType.Custom:
                pivotToUse -= Pivot;
                break;
            default:
                var pivotOffset = PivotHelper.GetRelativePivot(PivotType);
                pivotToUse -= new Vector2(pivotOffset.X * Width, pivotOffset.Y * Height);
                break;
        }
        
        var bounds = new Rectangle(
            (int)pivotToUse.X, 
            (int)pivotToUse.Y, 
            Width, 
            Height);

        return bounds;
    }
}