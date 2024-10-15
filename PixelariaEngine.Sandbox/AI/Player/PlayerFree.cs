namespace PixelariaEngine.Sandbox.Player;

public class PlayerFree : State<PlayerFree>
{
    private SandboxController _controller;
    
    public override void OnInitialize()
    {
        _controller = Fsm.Entity.GetComponent<SandboxController>();
    }

    public override void OnEnter()
    {
        _controller.Enabled = true;
    }

    public override void OnExecute()
    {
    }
}