using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PixelariaEngine.ECS;
using PixelariaEngine.Graphics;

namespace PixelariaEngine.Sandbox.Drawable;

public class ScreenFog : DrawableComponent<ScreenFog>
{
    public override Rectangle Bounds => GetBounds();
    public int Width = 32;
    public int Height = 32;
    public PivotType PivotType { get; set; } = PivotType.Center;
    public Vector2 Pivot { get; set; }
    private Texture2D _fogNoiseTexture;
    
    private float _alpha  = .5f;

    private readonly float _maxAlpha = 0.5f;

    public Vector2 NoiseOffset;

    public Vector2 NoiseScale;

    private bool _isDimming = false;

    public override void OnCreated()
    {
        Effect = Resources.LoadAsset<Effect>("Effects/FogEffect");
        _fogNoiseTexture = Resources.LoadAsset<Texture2D>("Textures/pnoise16x512");
        DrawLayer = 5;

        var level = LDtkManager.Instance.CurrentLevel;
        var width = level.PxWid;
        var height = level.PxHei;

        var widthRatio = width / (float)512;
        var heightRatio = height / (float)512;
        
        NoiseScale = new Vector2(widthRatio, heightRatio);
        
    }

    public override void OnUpdate()
    {
        if (_isDimming)
        {
            _alpha -= 0.11f * Time.DeltaTime;
            if (_alpha <= 0.25f)
                _isDimming = false;
        }
        else
        {
            _alpha += 0.0183f * Time.DeltaTime;
            _alpha = Mathf.Clamp(_alpha, 0, _maxAlpha);
        }
    }

    public void DimFog()
    {
        _isDimming = true;
    }

    public override void OnPreDraw()
    {
        Effect.Parameters["fogStart"].SetValue(0f);
        Effect.Parameters["fogEnd"].SetValue(Window.Height);
        Effect.Parameters["fogColor"].SetValue(new Vector4(1f, 1f, 1f, 1f));
        
        Effect.Parameters["noiseScale"].SetValue(NoiseScale * 1.5f);
        
        Effect.Parameters["NoiseSampler"].SetValue(_fogNoiseTexture);
        
        var noiseSpeedX = 0.05f;
        var noiseSpeedY = 0.04f;

        NoiseOffset.X += noiseSpeedX * Time.DeltaTime;
        NoiseOffset.Y += noiseSpeedY * Time.DeltaTime;
        
        Effect.Parameters["noiseOffset"].SetValue(NoiseOffset);
    }

    public override void OnDraw()
    {
        GraphicsUtil.Device.SamplerStates[0] = SamplerState.PointWrap;
        Core.SpriteBatch.DrawFilledRectangle(Bounds, Color.DarkGray * _alpha);
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