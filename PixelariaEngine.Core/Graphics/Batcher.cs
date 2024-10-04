using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace PixelariaEngine.Graphics;

public class Batcher
{
    private readonly GraphicsDevice _graphicsDevice;
    private Effect _effect;
    private readonly Effect _defaultEffect;
    private readonly Dictionary<string, EffectParameter> _shaderParameters = [];
    private bool _isBatchActive;

    private List<SpriteData> _spriteQueue = [];

    private struct SpriteData
    {
        public Texture2D Texture;
        public Vector3 Position;
        public Rectangle? SourceRectangle;
        public Color Color;
        public Vector3 Scale;
        public Vector3 Rotation;
        public Vector2 Origin;
    }

    public Batcher()
    {
        _graphicsDevice = Core.Instance.GraphicsDevice;
        _defaultEffect = Core.Instance.Content.Load<Effect>("Effects/DefaultEffect");
        _effect = _defaultEffect;
        CacheShaderParameters();
    }

    private void CacheShaderParameters()
    {
        foreach(var param in _effect.Parameters)
            _shaderParameters[param.Name] = param;
    }

    public void SetShaderParameter(string paramName, object value)
    {
        if (!_shaderParameters.TryGetValue(paramName, out var param))
        {
            Log.Warn($"Batcher: Param {paramName} does not exist");
            return;
        }

        switch (value)
        {
            case Matrix matrix:
                param.SetValue(matrix);
                break;
            case Matrix[] matArray:
                param.SetValue(matArray);
                break;
            case bool boolean:
                param.SetValue(boolean);
                break;
            case int integer:
                param.SetValue(integer);
                break;
            case int[] intArray:
                param.SetValue(intArray);
                break;
            case Quaternion quaternion:
                param.SetValue(quaternion);
                break;
            case float floatingPoint:
                param.SetValue(floatingPoint);
                break;
            case float[] floatArray:
                param.SetValue(floatArray);
                break;
            case Texture texture:
                param.SetValue(texture);
                break;
            case Vector2 vector2:
                param.SetValue(vector2);
                break;
            case Vector3 vector3:
                param.SetValue(vector3);
                break;
            case Vector4 vector4:
                param.SetValue(vector4);
                break;
            case Vector2[] vector2Array:
                param.SetValue(vector2Array);
                break;
            case Vector3[] vector3Array:
                param.SetValue(vector3Array);
                break;
            case Vector4[] vector4Array:
                param.SetValue(vector4Array);
                break;
            default:
                Log.Error($"Batcher: shader parameter {value.GetType().ToString()} unsupported");
                break;
        }
    }

    public void Begin(Matrix viewMatrix, Matrix projectionMatrix, Effect effect = null, SamplerState samplerState = null)
    {
        if(_isBatchActive)
            throw new InvalidOperationException("Batcher is already active");
        
        //dispose of the old effect
        _effect = null;
        
        //set the new effect, or use default if we did not specify one
        _effect = effect ?? _defaultEffect;
        CacheShaderParameters();
        
        //set the sampler state
        samplerState ??= SamplerState.LinearClamp;
        _graphicsDevice.SamplerStates[0] = samplerState;
        
        SetShaderParameter("View", viewMatrix);
        SetShaderParameter("Projection", projectionMatrix);
        
        _isBatchActive = true;
    }

    public void End()
    {
        if(!_isBatchActive)
            throw new InvalidOperationException("Batcher is not active");
        
        _spriteQueue.Sort((a,b) => a.Position.Z.CompareTo(b.Position.Z));
        

        foreach (var sprite in _spriteQueue)
        {
            DrawSpriteToScreen(sprite);
        }
        
        _spriteQueue.Clear();
        _isBatchActive = false;
    }

    public void Draw(Texture2D texture, Vector3 position, Rectangle? sourceRectangle
        , Color? color = null, Vector3? scale = null, Vector3? rotation = null, Vector2? origin = null)
    {
        _spriteQueue.Add(new SpriteData
        {
            Texture = texture,
            Position = position,
            SourceRectangle = sourceRectangle,
            Color = color ?? Color.White,
            Scale = scale ?? Vector3.One,
            Rotation = rotation ?? Vector3.Zero,
            Origin = origin ?? Vector2.Zero
        });
    }


    private void DrawSpriteToScreen(SpriteData sprite)
    {
        //Calculate texture size and scaled size
        var textureSize = sprite.SourceRectangle?.Size.ToVector2() ?? sprite.Texture.Bounds.Size.ToVector2();
        var scaledSize = new Vector2(textureSize.X * sprite.Scale.X, textureSize.Y * sprite.Scale.Y);
        
        //build the transformation matrix for the spite(translation, scaling, rotation)
        var transformation = Matrix.CreateTranslation(-new Vector3(sprite.Origin, 0)) *
                             Matrix.CreateScale(sprite.Scale) *
                             Matrix.CreateRotationX(sprite.Rotation.X) *
                             Matrix.CreateRotationY(sprite.Rotation.Y) *
                             Matrix.CreateRotationZ(sprite.Rotation.Z) *
                             Matrix.CreateTranslation(sprite.Position);
        
        //vertex positions after transformation
        var topLeft = Vector3.Transform(new Vector3(0,0,0), transformation);
        var topRight = Vector3.Transform(new Vector3(scaledSize.X, 0, 0), transformation);
        var bottomLeft = Vector3.Transform(new Vector3(0, scaledSize.Y, 0), transformation);
        var bottomRight = Vector3.Transform(new Vector3(scaledSize.X, scaledSize.Y, 0), transformation);
        
        //UV coordinates
        var uvTopLeft = sprite.SourceRectangle.HasValue
            ? new Vector2(sprite.SourceRectangle.Value.X / (float)sprite.Texture.Width, 
                sprite.SourceRectangle.Value.Y / (float) sprite.Texture.Height)
            : Vector2.Zero;
        
        var uvBottomRight = sprite.SourceRectangle.HasValue
            ? new Vector2((sprite.SourceRectangle.Value.Right) / (float)sprite.Texture.Width
                , (sprite.SourceRectangle.Value.Bottom) / (float)sprite.Texture.Height)
            : Vector2.One;
        
        //set up vertex data
        var vertices = new VertexPositionColorTexture[6]
        {
            new VertexPositionColorTexture(topLeft, sprite.Color, uvTopLeft),
            new VertexPositionColorTexture(bottomLeft, sprite.Color, new Vector2(uvTopLeft.X, uvBottomRight.Y)),
            new VertexPositionColorTexture(topRight, sprite.Color, new Vector2(uvBottomRight.X, uvTopLeft.Y)),
            
            new VertexPositionColorTexture(topRight, sprite.Color, new Vector2(uvBottomRight.X, uvTopLeft.Y)),
            new VertexPositionColorTexture(bottomRight, sprite.Color, new Vector2(uvTopLeft.X, uvBottomRight.Y)),
            new VertexPositionColorTexture(bottomRight, sprite.Color, uvBottomRight),
        };
        
        //set the shader parameters
        SetShaderParameter("World", Matrix.Identity); //optional per-objnect world transformation
        SetShaderParameter("TextureSampler", sprite.Texture);

        //apply shader passes
        foreach (var pass in _effect.CurrentTechnique.Passes)
        {
            pass.Apply();
            _graphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleList, vertices, 0, 2);
        }
    }
}