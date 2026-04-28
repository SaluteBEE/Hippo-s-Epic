using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class LevelController : MonoBehaviour
{
    public static LevelController Instance { get; private set; }

    [Header("References")]
    [SerializeField] private PlayerCharacter playerCharacter;
    [SerializeField] private MapManager mapManager;

    public PlayerCharacter PlayerCharacter => playerCharacter;
    public MapManager MapManager => mapManager;

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

        InitializePlayer();
    }

    private void Update()
    {
        HandleInput(out LevelInput levelInput);

        if (playerCharacter != null)
        {
            playerCharacter.SetMoveInput(levelInput.PlayerControl);

            if (levelInput.IsPlayerInteractionTriggered)
            {
                playerCharacter.PlayerExecute();
            }
        }
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
        var handle = Addressables.LoadAssetAsync<GameObject>("prefabs/player/PlayerCharacter2D");
        yield return handle;

        if (handle.Status != AsyncOperationStatus.Succeeded)
        {
            Debug.LogError($"[LevelController] 加载玩家 Prefab 失败：{handle.OperationException}");
            yield break;
        }

        var player = Instantiate(handle.Result, Vector3.zero, Quaternion.identity)
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

        mapManager.Initialize(playerCharacter);
    }

    private void HandleInput(out LevelInput levelInput)
    {
        levelInput = new LevelInput();

        Vector2 playerMoveInput = Vector2.zero;

        bool moveLeft = Input.GetKey(KeyCode.A);
        bool moveRight = Input.GetKey(KeyCode.D);
        bool moveUp = Input.GetKey(KeyCode.W);
        bool moveDown = Input.GetKey(KeyCode.S);

        if (moveLeft)
            playerMoveInput.x = -1f;
        else if (moveRight)
            playerMoveInput.x = 1f;

        if (moveUp)
            playerMoveInput.y = 1f;
        else if (moveDown)
            playerMoveInput.y = -1f;

        levelInput.SetPlayerControl(playerMoveInput);

        if (Input.GetKeyDown(KeyCode.E))
        {
            levelInput.IsPlayerInteractionTriggered = true;
        }
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

public struct LevelInput
{
    [Obsolete("请使用 PlayerControl 属性读取，使用 SetPlayerControl 方法设置输入。")]
    public Vector2 playerControl;

    private Vector2 _playerControl;

    public Vector2 PlayerControl => _playerControl;

    public bool IsPlayerInteractionTriggered;

    public void SetPlayerControl(Vector2 input)
    {
        _playerControl = input.normalized;
    }
}