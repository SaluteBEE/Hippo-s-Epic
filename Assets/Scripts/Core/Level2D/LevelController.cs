using Core.Level2D.Camera;
using Core.Level2D.LevelObjects;
using Core.Level2D.Maps;
using UnityEngine;

namespace Core.Level2D
{
    public class LevelController : MonoBehaviour
    {
        [SerializeField]
        private PlayerCharacter _playerCharacter;
        public PlayerCharacter PlayerCharacter
        {
            get => _playerCharacter;
        }

        // [SerializeField]
        private Map _map;
        public Map Map
        {
            get => _map;
            private set => _map = value;
        }

        [SerializeField]
        private CameraController _cameraController;

        private void Awake()
        {
            LevelManager.SetLevelController(this);
            Map map = GetComponentInChildren<Map>();
            if (map != null)
            {
                SetMap(map);
                // Instantiate Player Character
                GameObject playerCharacterPrefab = Resources.Load<GameObject>("Level2D/Player Character 2D");
                Vector2 mainEntrance = Map.mainEntrance;
                Vector3 position = new Vector3(mainEntrance.x, mainEntrance.y, mainEntrance.y);
                SetPlayerCharacter(Instantiate(playerCharacterPrefab, position, Quaternion.identity).GetComponent<PlayerCharacter>());
            }
        }

        private void Update()
        {
            HandleInput(out LevelInput levelInput);

            if(_playerCharacter != null)
            {
                _playerCharacter.SetMoveInput(levelInput.playerControl);
            }
        }

        /// <summary>
        /// 处理 Level 在每一帧计算中所需要的所有输入指令
        /// </summary>
        /// <param name="levelInput"></param>
        private void HandleInput(out LevelInput levelInput)
        {
            levelInput = new LevelInput();

            // Move

            // set flags

            Vector2 playerMoveInput = Vector2.zero;
            bool flagMoveLeft = Input.GetKey(KeyCode.A);
            bool flagMoveRight = Input.GetKey(KeyCode.D);
            bool flagMoveUp = Input.GetKey(KeyCode.W);
            bool flagMoveDown = Input.GetKey(KeyCode.S);

            // calculate

            if (flagMoveLeft)
                playerMoveInput.x = -1f;
            else if (flagMoveRight)
                playerMoveInput.x = 1f;

            if (flagMoveUp)
                playerMoveInput.y = 1f;
            else if (flagMoveDown)
                playerMoveInput.y = -1f;

            // Normalize

            playerMoveInput.Normalize();

            // Apply

            levelInput.playerControl = playerMoveInput;

            // Interactive

            if (Input.GetKeyDown(KeyCode.E))
            {
                _playerCharacter.PlayerExecute();
            }

        }

        /// <summary>
        /// 用于通过脚本挂载玩家角色，建议在实例化关卡后实例化玩家角色并挂载，同时执行其初始化脚本（基于存档或 Level Flag 计算玩家状态）
        /// TODO: 实现方法
        /// </summary>
        /// <param name="playerCharacter"></param>
        public void SetPlayerCharacter(PlayerCharacter playerCharacter)
        {
            if (_playerCharacter == null && playerCharacter != null)
            {
                _playerCharacter = playerCharacter;
                _cameraController.SetFocusTarget(playerCharacter);
                return;
            }
            Debug.Log("Level2D: Set Player Character failed.");
        }

        /// <summary>
        /// 设置地图，释放现有地图并初始化新地图
        /// </summary>
        /// <param name="map"></param>
        public void SetMap(Map map)
        {
            if (_map != null)
            {
                _map.Dispose();
            }
            _map = map;
            map.Initialize();
            if (_cameraController != null)
            {
                _cameraController.SetCameraClamp(map.cameraClampX, map.cameraClampY);
            }
        }

        /// <summary>
        /// 实例化一个 Level 的 GameObject 并挂载空 Level Controller 组件
        /// </summary>
        /// <returns></returns>
        public static LevelController InstantiateLevelController()
        {
            GameObject levelGameObject = new GameObject("Level");
            LevelController levelController = levelGameObject.AddComponent<LevelController>();
            return levelController;
        }
    }

    /// <summary>
    /// Level Controller 在每一帧计算中需要的输入行为
    /// </summary>
    public struct LevelInput
    {
        /// <summary>
        /// 用户的二维输入向量，需要被归一化
        /// </summary>
        public Vector2 playerControl;

        /// <summary>
        /// 玩家触发交互
        /// </summary>
        public bool isPlayerInteractionTriggered;
    }
}