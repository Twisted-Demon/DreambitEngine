using Dreambit.ECS;

namespace Dreambit.Scripting;

public class CameraFollowScript : ScriptAction
{
    private string _entityName;

    public CameraFollowScript(string entity)
    {
        _entityName = entity;
    }


    public override void OnUpdate()
    {
        var e = Scene.Instance.FindEntity(_entityName);
        if (e is null)
        {
            IsComplete = true;
            return;
        }

        Scene.Instance.MainCamera.IsFollowing = true;
        Scene.Instance.MainCamera.TransformToFollow = e.Transform;

        IsComplete = true;
    }
}