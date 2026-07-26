using UnityEngine;

public class BattleGameState : IGameState
{
    private readonly GameApp _app;
    private InputManager _inputManager;
    private Rigidbody2D _playerRb;
    private Vector2 _savedVelocity;

    public BattleGameState(GameApp app)
    {
        _app = app;
        _inputManager = ManagerRegistry.Get<InputManager>();
    }

    public void Enter()
    {
        if (_inputManager != null)
            _inputManager.EnableOnlyUI();

        var player = LevelController.Instance?.PlayerCharacter;
        if (player != null && player.rigidbody2D != null)
        {
            _playerRb = player.rigidbody2D;
            _savedVelocity = _playerRb.velocity;
            _playerRb.velocity = Vector2.zero;
            _playerRb.isKinematic = true;
        }

        Debug.Log("[BattleGameState] 进入战斗状态");
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

        Debug.Log("[BattleGameState] 退出战斗状态");
    }

    public void Update()
    {
    }
}
