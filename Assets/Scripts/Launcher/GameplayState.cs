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
        if (_bagOpen)
        {
            _bagOpen = false;
            Time.timeScale = 1f;
            _ui.Close<BagPanel>();
        }
    }

    private bool _bagOpen;

    public void Update()
    {
        if (_inputManager == null) return;

        if (_bagOpen)
        {
            if (_inputManager.GameInput.UI.Cancel.WasPressedThisFrame())
            {
                if (_ui.HasPopupOpen())
                    _ui.CloseTopmost();
                else
                    CloseBag();
            }
            else if (_inputManager.GameInput.UI.Bag.WasPressedThisFrame())
            {
                CloseBag();
            }
            return;
        }

        if (_inputManager.GameInput.Player.Pause.WasPressedThisFrame())
        {
            app.TogglePause();
        }

        if (_inputManager.GameInput.UI.Bag.WasPressedThisFrame())
        {
            OpenBag();
        }
        else if (_inputManager.GameInput.UI.Cancel.WasPressedThisFrame())
        {
            _ui.CloseTopmost();
        }
    }

    private void OpenBag()
    {
        _bagOpen = true;
        Time.timeScale = 0f;
        _inputManager.DisablePlayer();
        _inputManager.EnableUI();
        _ui.Open<BagPanel>();
    }

    private void CloseBag()
    {
        _bagOpen = false;
        Time.timeScale = 1f;
        _inputManager.EnablePlayerAndUI();
        _ui.Close<UseItemTip>();
        _ui.Close<BagPanel>();
    }
}
