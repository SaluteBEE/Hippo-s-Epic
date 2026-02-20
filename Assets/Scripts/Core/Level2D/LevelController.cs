using Core.Level2D.LevelObjects;
using UnityEngine;

namespace Core.Level2D
{
    public class LevelController : MonoBehaviour
    {
        [SerializeField]
        private PlayerCharacter _playerCharacter;

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

        public void SetPlayerCharacter(PlayerCharacter playerCharacter)
        {
            if (_playerCharacter == null && playerCharacter != null)
            {
                _playerCharacter = playerCharacter;

                return;
            }
            Debug.Log("Level2D: ");
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