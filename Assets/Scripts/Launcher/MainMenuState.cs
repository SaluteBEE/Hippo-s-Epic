public class MainMenuState : IGameState
{
    private readonly GameApp app;
    private UIManager _ui;
    private AudioManager _audio;

    public MainMenuState(GameApp app)
    {
        this.app = app;
        _ui = ManagerRegistry.Get<UIManager>();
        _audio = ManagerRegistry.Get<AudioManager>();
    }

    public void Enter()
    {
        _ui.Close<LoadingPanel>();

        _ui.Open<MainMenuPanel>();
        _audio.PlayBGM("MainMenu");
    }

    public void Exit()
    {
        _ui.Close<MainMenuPanel>();
    }

    public void Update()
    {
    }
}
