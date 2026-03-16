using System;
using UnityEngine;


    [AddComponentMenu("Level 2D/Player Character")]
    public class PlayerCharacter : LevelObject, IFocusTarget
    {
        public new Rigidbody2D rigidbody2D;
        public float MoveSpeed = 2.5f;

        public LayerMask interactionLayerMask;

        [Header("Flip")]
        [SerializeField] private bool faceRightByDefault = true;

        private InteractionCollider _currentInteractionCollider;
        private Vector3 _originalScale;

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

            // Interaction Check
            Vector2 vector2 = new Vector2(transform.position.x, transform.position.y);

            Collider2D collider2D = Physics2D.OverlapPoint(vector2, interactionLayerMask);
            if (collider2D != null)
            {
                if (collider2D.TryGetComponent(out InteractionCollider interactionCollider))
                {
                    if (interactionCollider != _currentInteractionCollider)
                    {
                        _currentInteractionCollider?.OnPlayerExit();
                        _currentInteractionCollider = interactionCollider;
                        _currentInteractionCollider.OnPlayerEnter();
                    }
                }
            }
            else
            {
                if (_currentInteractionCollider != null)
                {
                    _currentInteractionCollider.OnPlayerExit();
                    _currentInteractionCollider = null;
                }
            }
        }

        public void PlayerExecute()
        {
            Collider2D collider2D = Physics2D.OverlapPoint(
                new Vector2(transform.position.x, transform.position.y),
                interactionLayerMask
            );

            if (collider2D != null)
            {
                if (collider2D.TryGetComponent(out InteractionCollider interactionCollider))
                {
                    interactionCollider.OnPlayerExecute();
                }
            }
        }

        public event Action<Vector2> Moved;
    }
