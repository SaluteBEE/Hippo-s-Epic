using System;

public class LoadingState : IGameState
{
    private readonly GameApp app;
    private readonly string sceneName;
    private readonly Func<IGameState> nextStateFactory;

    public LoadingState(GameApp app, string sceneName, Func<IGameState> nextStateFactory)
    {
        this.app = app;
        this.sceneName = sceneName;
        this.nextStateFactory = nextStateFactory;
    }

    public void Enter()
    {
        var panel = app.UI.Open<LoadingPanel>();

        app.Scene.LoadScene(
            sceneName,
            progress =>
            {
                if (panel != null)
                    panel.SetProgress(progress);
            },
            () =>
            {
                app.StateMachine.ChangeState(nextStateFactory());
            });
    }

    public void Exit()
    {
        app.UI.Close<LoadingPanel>();
    }

    public void Update()
    {
    }
}