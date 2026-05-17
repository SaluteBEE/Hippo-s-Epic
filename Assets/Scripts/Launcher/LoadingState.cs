using System;

public class LoadingState : IGameState
{
    private readonly GameApp app;
    private readonly string sceneName;
    private readonly Func<IGameState> nextStateFactory;
    private UIManager _ui;
    private SceneController _scene;
    private InputManager _inputManager;

    public LoadingState(GameApp app, string sceneName, Func<IGameState> nextStateFactory)
    {
        this.app = app;
        this.sceneName = sceneName;
        this.nextStateFactory = nextStateFactory;
        _ui = ManagerRegistry.Get<UIManager>();
        _scene = ManagerRegistry.Get<SceneController>();
        _inputManager = ManagerRegistry.Get<InputManager>();
    }

    public void Enter()
    {
        if (_inputManager != null)
            _inputManager.DisableAll();

        _ui.Open<LoadingPanel>(onReady: panel =>
        {
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
