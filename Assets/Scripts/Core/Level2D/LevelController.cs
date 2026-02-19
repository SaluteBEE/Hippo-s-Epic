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

        private void HandleInput(out LevelInput levelInput)
        {
            levelInput = new LevelInput();

            Vector2 playerMoveInput = Vector2.zero;
            bool flagMoveLeft = Input.GetKey(KeyCode.A);
            bool flagMoveRight = Input.GetKey(KeyCode.D);
            bool flagMoveUp = Input.GetKey(KeyCode.W);
            bool flagMoveDown = Input.GetKey(KeyCode.S);

            if (flagMoveLeft)
                playerMoveInput.x = -1f;
            else if (flagMoveRight)
                playerMoveInput.x = 1f;

            if (flagMoveUp)
                playerMoveInput.y = 1f;
            else if (flagMoveDown)
                playerMoveInput.y = -1f;

            playerMoveInput.Normalize();

            levelInput.playerControl = playerMoveInput;
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

        public static LevelController InstantiateLevelController()
        {
            GameObject levelGameObject = new GameObject("Level");
            LevelController levelController = levelGameObject.AddComponent<LevelController>();
            return levelController;
        }
    }

    public struct LevelInput
    {
        public Vector2 playerControl;
    }
}