namespace Dreambit.BriGame.Components;

public class GalagaManager : Singleton<GalagaManager>
{
    public GalagaState GameState { get; private set; }

    public void SetGameState(GalagaState state)
    {
        GameState = state;
    }
}

public enum GalagaState
{
    StartMenu,
    StartingGame,
    PlayingGame
}