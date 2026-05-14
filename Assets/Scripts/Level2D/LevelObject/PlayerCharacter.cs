using System;
using UnityEngine;


    [AddComponentMenu("Level 2D/Player Character")]
    public class PlayerCharacter : LevelObject, IFocusTarget
    {
        public new Rigidbody2D rigidbody2D;
        public float MoveSpeed = 2.5f;

        [Header("Flip")]
        [SerializeField] private bool faceRightByDefault = true;

        private Interactable _currentInteractable;
        private Vector3 _originalScale;

        public Interactable CurrentInteractable => _currentInteractable;

        public void SetCurrentInteractable(Interactable interactable)
        {
            _currentInteractable = interactable;
        }

        public void ClearCurrentInteractable()
        {
            _currentInteractable = null;
        }

        private void Awake()
        {
            _originalScale = transform.localScale;
        }

        public void SetMoveInput(Vector2 vector2)
        {
            rigidbody2D.velocity = vector2 * MoveSpeed;

            // 根据移动方向左右转向
            if (vector2.x > 0.01f)
            {
                SetFacing(true);
            }
            else if (vector2.x < -0.01f)
            {
                SetFacing(false);
            }
        }

        private void SetFacing(bool faceRight)
        {
            Vector3 scale = _originalScale;

            if (faceRightByDefault)
            {
                scale.x = faceRight ? Mathf.Abs(scale.x) : -Mathf.Abs(scale.x);
            }
            else
            {
                scale.x = faceRight ? -Mathf.Abs(scale.x) : Mathf.Abs(scale.x);
            }

            transform.localScale = scale;
        }

        private void Update()
        {
            Vector3 position = transform.position;
            Vector2 position2D = new Vector2(position.x, position.y);
            transform.position = new Vector3(position2D.x, position2D.y, position2D.y);
            Moved?.Invoke(position2D);
        }

        public void PlayerExecute(int buttonIndex = 0)
        {
            if (_currentInteractable != null)
            {
                _currentInteractable.OnPlayerExecute(buttonIndex);
            }
        }

        public event Action<Vector2> Moved;
    }
