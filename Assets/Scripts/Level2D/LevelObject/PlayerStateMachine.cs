using UnityEngine;

public enum PlayerStateType
{
    Idle,
    Walk
}

public interface IPlayerState
{
    PlayerStateType StateType { get; }
    void Enter();
    void Exit();
    void Update(Vector2 moveInput);
}

public class PlayerIdleState : IPlayerState
{
    private PlayerStateMachine _stateMachine;
    private AnimationController _animationController;

    public PlayerStateType StateType => PlayerStateType.Idle;

    public PlayerIdleState(PlayerStateMachine stateMachine, AnimationController animationController)
    {
        _stateMachine = stateMachine;
        _animationController = animationController;
    }

    public void Enter()
    {
        if (_animationController != null)
            _animationController.PlayComposition(CompositionName.Idle);
    }

    public void Exit()
    {
    }

    public void Update(Vector2 moveInput)
    {
        if (moveInput.sqrMagnitude > _stateMachine.Deadzone * _stateMachine.Deadzone)
        {
            _stateMachine.ChangeState(PlayerStateType.Walk);
        }
    }
}

public class PlayerWalkState : IPlayerState
{
    private PlayerStateMachine _stateMachine;
    private AnimationController _animationController;

    public PlayerStateType StateType => PlayerStateType.Walk;

    public PlayerWalkState(PlayerStateMachine stateMachine, AnimationController animationController)
    {
        _stateMachine = stateMachine;
        _animationController = animationController;
    }

    public void Enter()
    {
        if (_animationController != null)
            _animationController.PlayComposition(CompositionName.Walk);
    }

    public void Exit()
    {
    }

    public void Update(Vector2 moveInput)
    {
        if (moveInput.sqrMagnitude <= _stateMachine.Deadzone * _stateMachine.Deadzone)
        {
            _stateMachine.ChangeState(PlayerStateType.Idle);
        }
    }
}

public class PlayerStateMachine
{
    private IPlayerState _currentState;
    private PlayerCharacter _player;
    private AnimationController _animationController;

    private PlayerIdleState _idleState;
    private PlayerWalkState _walkState;

    public float Deadzone { get; } = 0.1f;
    public PlayerStateType CurrentStateType => _currentState?.StateType ?? PlayerStateType.Idle;

    public PlayerStateMachine(PlayerCharacter player, AnimationController animationController)
    {
        _player = player;
        _animationController = animationController;

        _idleState = new PlayerIdleState(this, animationController);
        _walkState = new PlayerWalkState(this, animationController);

        _currentState = _idleState;
    }

    public void Update(Vector2 moveInput)
    {
        _currentState?.Update(moveInput);
    }

    public void ChangeState(PlayerStateType newStateType)
    {
        IPlayerState newState = GetState(newStateType);
        if (newState == null || newState == _currentState)
            return;

        _currentState?.Exit();
        _currentState = newState;
        _currentState.Enter();
    }

    private IPlayerState GetState(PlayerStateType stateType)
    {
        switch (stateType)
        {
            case PlayerStateType.Idle:
                return _idleState;
            case PlayerStateType.Walk:
                return _walkState;
            default:
                return null;
        }
    }

    public void Initialize()
    {
        _currentState?.Enter();
    }
}
