using UnityEngine;

public class GameplayState : IGameState
{
    private readonly GameApp app;
    private UIManager _ui;
    private AudioManager _audio;

    public GameplayState(GameApp app)
    {
        this.app = app;
        _ui = ManagerRegistry.Get<UIManager>();
        _audio = ManagerRegistry.Get<AudioManager>();
    }

    public void Enter()
    {
        Time.timeScale = 1f;

        _ui.Close<LoadingPanel>();
        _ui.Close<MainMenuPanel>();

        _audio.PlayBGM("Gameplay");
    }

    public void Exit()
    {
    }

    public void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            app.TogglePause();
        }
    }
}
