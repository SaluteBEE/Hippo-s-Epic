using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.InputSystem;
using UnityEngine.ResourceManagement.AsyncOperations;

public class LevelController : MonoBehaviour
{
    public static LevelController Instance { get; private set; }

    [Header("References")]
    [SerializeField] private PlayerCharacter playerCharacter;
    [SerializeField] private MapManager mapManager;

    public PlayerCharacter PlayerCharacter => playerCharacter;
    public MapManager MapManager => mapManager;

    private GameInput _gameInput;
    private bool _ownsInput;
    private AsyncOperationHandle<GameObject> _playerHandle;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("LevelController already exists, destroy duplicate.");
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (mapManager == null)
        {
            mapManager = GetComponentInChildren<MapManager>(true);
        }

        EnsureInputManager();
        InitializePlayer();
    }

    private void OnDestroy()
    {
        if (Instance != this) return;
        Instance = null;
        if (_ownsInput && _gameInput != null)
        {
            _gameInput.Player.Disable();
            _gameInput.Dispose();
        }
        if (_playerHandle.IsValid())
            Addressables.Release(_playerHandle);
        Interactable.ReleaseHintPrefab();
    }

    private void EnsureInputManager()
    {
        var inputManager = ManagerRegistry.Get<InputManager>();
        if (inputManager != null)
        {
            _gameInput = inputManager.GameInput;
            return;
        }

        _gameInput = new GameInput();
        _ownsInput = true;
        Debug.Log("[LevelController] 无 InputManager，创建独立 GameInput 实例");
    }

    private void Update()
    {
        if (playerCharacter == null) return;
        if (_gameInput == null) return;

        if (!_gameInput.Player.enabled)
            return;

        var move = _gameInput.Player.Move.ReadValue<Vector2>();
        playerCharacter.SetMoveInput(move);

        if (_gameInput.Player.Interact.WasPressedThisFrame())
            playerCharacter.PlayerExecute(0);
        else if (_gameInput.Player.Button2.WasPressedThisFrame())
            playerCharacter.PlayerExecute(1);
        else if (_gameInput.Player.Button3.WasPressedThisFrame())
            playerCharacter.PlayerExecute(2);
        else if (_gameInput.Player.Button4.WasPressedThisFrame())
            playerCharacter.PlayerExecute(3);
        else if (_gameInput.Player.Button5.WasPressedThisFrame())
            playerCharacter.PlayerExecute(4);
        else if (_gameInput.Player.Button1.WasPressedThisFrame())
            playerCharacter.PlayerExecute(0);
    }

    private void InitializePlayer()
    {
        if (playerCharacter != null)
        {
            InitializeMap();
            return;
        }

        StartCoroutine(LoadPlayerAsync());
    }

    private IEnumerator LoadPlayerAsync()
    {
        _playerHandle = Addressables.LoadAssetAsync<GameObject>("prefabs/player/PlayerCharacter2D");
        yield return _playerHandle;

        if (_playerHandle.Status != AsyncOperationStatus.Succeeded)
        {
            Debug.LogError($"[LevelController] 加载玩家 Prefab 失败：{_playerHandle.OperationException}");
            yield break;
        }

        var player = Instantiate(_playerHandle.Result, Vector3.zero, Quaternion.identity)
            .GetComponent<PlayerCharacter>();

        SetPlayerCharacter(player);
        InitializeMap();
    }

    private void InitializeMap()
    {
        if (mapManager == null)
        {
            Debug.LogError("[LevelController] MapManager is null.");
            return;
        }

        SaveManager.Instance.Load();
        mapManager.Initialize(playerCharacter);
    }

    public void SetPlayerCharacter(PlayerCharacter player)
    {
        if (player == null)
        {
            Debug.LogWarning("[LevelController] SetPlayerCharacter failed: player is null.");
            return;
        }

        playerCharacter = player;
    }

    public static LevelController Create()
    {
        if (Instance != null)
            return Instance;

        GameObject levelGameObject = new GameObject("Level");
        return levelGameObject.AddComponent<LevelController>();
    }
}