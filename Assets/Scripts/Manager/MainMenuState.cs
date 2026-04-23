namespace Manager
{
    public class MainMenuState
    {
        
    }
}public class MainMenuState : IGameState
{
    private readonly GameApp app;

    public MainMenuState(GameApp app)
    {
        this.app = app;
    }

    public void Enter()
    {
        app.UI.Close<LoadingPanel>();

        app.UI.Open<MainMenuPanel>();
        app.Audio.PlayBGM("MainMenu");
    }

    public void Exit()
    {
        app.UI.Close<MainMenuPanel>();
    }

    public void Update()
    {
    }
}