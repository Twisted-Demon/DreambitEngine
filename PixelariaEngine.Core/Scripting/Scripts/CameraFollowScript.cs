using PixelariaEngine.ECS;

namespace PixelariaEngine.Scripting;

public class CameraFollowScript : Script
{
    private readonly Entity _entityToFollow;
    
    public CameraFollowScript(string entity)
    {
        _entityToFollow = Scene.Instance.FindEntity(entity);
    }
    
    
    public override void OnUpdate()
    {
        if (_entityToFollow is null)
        {
            IsComplete = true;
            return;
        }
        
        Scene.Instance.MainCamera.IsFollowing = true;
        Scene.Instance.MainCamera.TransformToFollow = _entityToFollow.Transform;

        IsComplete = true;
    }
}