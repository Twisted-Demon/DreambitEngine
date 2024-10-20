using Microsoft.Xna.Framework;
using PixelariaEngine.ECS;

namespace PixelariaEngine.Scripting;

public class MoveScript : Script
{
    private string _entityName;
    private float _speed;
    private Vector2 _moveTo;
    private Entity _entity;
    private Mover _mover;
    
    public MoveScript(string entity, float speed, Vector2 moveTo)
    {
        _entityName = entity;
        _speed = speed;
        _moveTo = moveTo;
    }

    public override void OnStart()
    {
        _entity = Entity.FindByName(_entityName);
        _mover = _entity.AttachComponent<Mover>();
        _mover.Velocity = Vector3.Zero;
    }

    public override void OnUpdate()
    {
        if (_mover.MoveTo(_moveTo.ToVector3(), _speed))
        {
            IsComplete = true;
        }
    }

    public override void OnCompleted()
    {
        _mover.Velocity = Vector3.Zero;
    }
}