using UnityEngine;

public class GameplayState : IGameState
{
    private readonly GameApp app;

    public GameplayState(GameApp app)
    {
        this.app = app;
    }

    public void Enter()
    {
        Time.timeScale = 1f;

        app.UI.Close<LoadingPanel>();
        app.UI.Close<MainMenuPanel>();
        app.UI.Close<PausePanel>();

        app.UI.Open<HUDPanel>();
        app.Audio.PlayBGM("Gameplay");
    }

    public void Exit()
    {
        app.UI.Close<HUDPanel>();
    }

    public void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            app.TogglePause();
        }
    }
}