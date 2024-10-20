using PixelariaEngine.Scripting;

namespace PixelariaEngine.Sandbox.Player;

public class InDialogue : State<InDialogue>
{
    
    public override void OnInitialize()
    {
        ScriptingManager.Instance.OnScriptingStart += () =>
        {
            Fsm.SetNextState<InDialogue>();
        };
        
        ScriptingManager.Instance.OnScriptingEnd += () =>
        {
            Fsm.GoToDefaultState();
        };
    }

    public override void OnEnter()
    {
    }

    public override void OnEnd()
    {
    }

    public override void OnExecute()
    {
        
    }
}