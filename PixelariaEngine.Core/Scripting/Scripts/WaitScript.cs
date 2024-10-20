namespace PixelariaEngine.Scripting;

public class WaitScript(float duration) : ScriptAction
{
    private readonly Logger<WaitScript> _logger = new();
    private float _elapsedTime;

    public override void OnUpdate()
    {
        _elapsedTime += Time.DeltaTime;

        if (!(_elapsedTime >= duration)) return;

        IsComplete = true;
        _logger.Debug("Wait Complete");
    }
}