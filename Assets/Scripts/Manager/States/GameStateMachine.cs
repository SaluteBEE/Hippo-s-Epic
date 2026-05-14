public interface IGameState
{
    void Enter();
    void Exit();
    void Update();
}
public class GameStateMachine
{
    public IGameState Current { get; private set; }

    public event System.Action<IGameState> OnStateChanged;

    public void ChangeState(IGameState newState)
    {
        if (newState == null)
            return;

        Current?.Exit();
        Current = newState;
        Current.Enter();
        OnStateChanged?.Invoke(newState);
    }

    public void Update()
    {
        Current?.Update();
    }
}