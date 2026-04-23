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

        app.Audio.PlayBGM("Gameplay");
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