namespace PixelariaEngine.Sandbox.Player;

public class InDialogue : State<InDialogue>
{
    private SandboxController _controller;

    public override void OnInitialize()
    {
        _controller = Fsm.Entity.GetComponent<SandboxController>();
    }

    public override void OnEnter()
    {
        _controller.Enabled = false;
    }

    public override void OnEnd()
    {
        _controller.Enabled = true;
    }

    public override void OnExecute()
    {
        
    }
}