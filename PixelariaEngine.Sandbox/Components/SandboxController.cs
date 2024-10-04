using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using PixelariaEngine.ECS;

namespace PixelariaEngine.Sandbox.Components;

public class SandboxController : Component
{
    private Vector3 _moveDir;
    private const float Speed = 75f;

    public override void OnUpdate()
    {
        _moveDir = Vector3.Zero;
        
        _moveDir.X += Input.IsKeyHeld(Keys.D) ? 1 : 0;
        _moveDir.X -= Input.IsKeyHeld(Keys.A)  ? 1 : 0;
        
        _moveDir.Y += Input.IsKeyHeld(Keys.S)  ? 1 : 0;
        _moveDir.Y -= Input.IsKeyHeld(Keys.W)  ? 1 : 0;
        
        if(_moveDir != Vector3.Zero)
            _moveDir.Normalize();
        
        Transform.Position += _moveDir * Speed * Time.DeltaTime;
        Transform.Rotation.Z += Time.DeltaTime;
        
        if(Input.IsKeyPressed(Keys.Escape))
            Core.Instance.Exit();
    }
}