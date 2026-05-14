using UnityEngine;

public class GameplayState : IGameState
{
    private readonly GameApp app;
    private UIManager _ui;
    private AudioManager _audio;
    private InputManager _inputManager;

    public GameplayState(GameApp app)
    {
        this.app = app;
        _ui = ManagerRegistry.Get<UIManager>();
        _audio = ManagerRegistry.Get<AudioManager>();
        _inputManager = ManagerRegistry.Get<InputManager>();
    }

    public void Enter()
    {
        Time.timeScale = 1f;

        _ui.Close<LoadingPanel>();
        _ui.Close<MainMenuPanel>();

        _audio.PlayBGM("Gameplay");

        if (_inputManager != null)
            _inputManager.EnablePlayerAndUI();
    }

    public void Exit()
    {
    }

    public void Update()
    {
        if (_inputManager != null && _inputManager.GameInput.Player.Pause.WasPressedThisFrame())
        {
            app.TogglePause();
        }
    }
}
