using PixelariaEngine.ECS;

namespace PixelariaEngine.Scripting;

public class EnableEntityScript : ScriptAction
{
    private string _entityName;
    
    public EnableEntityScript(string entity)
    {
        _entityName = entity;
    }
    
    public override void OnUpdate()
    {
        var e = Entity.FindByName(_entityName);
        if (e != null)
            e.Enabled = true;

        IsComplete = true;
    }
}