using UnityEngine;

public class DialogGameState : IGameState
{
    private readonly GameApp app;
    private InputManager _inputManager;
    private Rigidbody2D _playerRb;
    private Vector2 _savedVelocity;

    public DialogGameState(GameApp app)
    {
        this.app = app;
        _inputManager = ManagerRegistry.Get<InputManager>();
    }

    public void Enter()
    {
        if (_inputManager != null)
            _inputManager.EnableOnlyDialog();

        var player = LevelController.Instance?.PlayerCharacter;
        if (player != null && player.rigidbody2D != null)
        {
            _playerRb = player.rigidbody2D;
            _savedVelocity = _playerRb.velocity;
            _playerRb.velocity = Vector2.zero;
            _playerRb.isKinematic = true;
        }
    }

    public void Exit()
    {
        if (_playerRb != null)
        {
            _playerRb.isKinematic = false;
            _playerRb.velocity = _savedVelocity;
            _playerRb = null;
        }

        if (_inputManager != null)
            _inputManager.EnablePlayerAndUI();
    }

    public void Update()
    {
    }
}