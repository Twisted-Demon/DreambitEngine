using System;
using Dreambit.ECS;
using Microsoft.Xna.Framework.Input;

namespace Dreambit.Sandbox;

public class InteractionListener : Component
{
    public Action OnPlayerInRange;
    public Action OnPlayerOutOfRange;
    
    public Action OnPlayerInteract;

    public float InteractionRange = 10f;

    private Entity _playerEntity;
    private bool _playerInRange;

    public override void OnAddedToEntity()
    {
        _playerEntity = Entity.FindByName("player");
    }

    public override void OnUpdate()
    {
        if (_playerEntity == null)
            return;

        var pos = Transform.WorldPosition;
        var playerPos = _playerEntity.Transform.WorldPosition;

        var distance = (playerPos - pos).Length();

        if (distance <= InteractionRange)
        {
            if(!_playerInRange)
                OnPlayerInRange?.Invoke();
            
            _playerInRange = true;
        }
        else
        {
            if(_playerInRange)
                OnPlayerOutOfRange?.Invoke();
            
            _playerInRange = false;
        }

        if (!_playerInRange)
            return;
        
        if(Input.IsKeyPressed(Keys.E))
            OnPlayerInteract?.Invoke();
    }
}