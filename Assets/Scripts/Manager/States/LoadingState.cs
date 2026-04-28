using System;

public class LoadingState : IGameState
{
    private readonly GameApp app;
    private readonly string sceneName;
    private readonly Func<IGameState> nextStateFactory;
    private UIManager _ui;
    private SceneController _scene;

    public LoadingState(GameApp app, string sceneName, Func<IGameState> nextStateFactory)
    {
        this.app = app;
        this.sceneName = sceneName;
        this.nextStateFactory = nextStateFactory;
        _ui = ManagerRegistry.Get<UIManager>();
        _scene = ManagerRegistry.Get<SceneController>();
    }

    public void Enter()
    {
        var panel = _ui.Open<LoadingPanel>();

        _scene.LoadScene(
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
        _ui.Close<LoadingPanel>();
    }

    public void Update()
    {
    }
}
