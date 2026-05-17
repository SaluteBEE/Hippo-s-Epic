using UnityEngine;

public class PauseState : IGameState
{
    private readonly GameApp app;
    private InputManager _inputManager;

    public PauseState(GameApp app)
    {
        this.app = app;
        _inputManager = ManagerRegistry.Get<InputManager>();
    }

    public void Enter()
    {
        Time.timeScale = 0f;

        if (_inputManager != null)
            _inputManager.EnableOnlyUI();
    }

    public void Exit()
    {
        Time.timeScale = 1f;
    }

    public void Update()
    {
        if (_inputManager != null && _inputManager.GameInput.UI.Cancel.WasPressedThisFrame())
        {
            app.ResumeGame();
        }
    }
}