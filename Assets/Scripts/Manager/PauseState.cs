using UnityEngine;

public class PauseState : IGameState
{
    private readonly GameApp app;

    public PauseState(GameApp app)
    {
        this.app = app;
    }

    public void Enter()
    {
        Time.timeScale = 0f;
        app.UI.Open<PausePanel>();
    }

    public void Exit()
    {
        Time.timeScale = 1f;
        app.UI.Close<PausePanel>();
    }

    public void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            app.ResumeGame();
        }
    }
}