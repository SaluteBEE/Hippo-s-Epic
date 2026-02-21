using Core.Level2D.Maps;
using UnityEngine;

namespace Core.Level2D.LevelObjects
{
    [AddComponentMenu("Level 2D/Player Character")]
    public class PlayerCharacter : LevelObject
    {
        public new Rigidbody2D rigidbody2D;
        public float MoveSpeed = 2.5f;

        public LayerMask interactionLayerMask;

        private InteractionCollider _currentInteractionCollider;
        public void SetMoveInput(Vector2 vector2)
        {
            rigidbody2D.velocity = vector2 * MoveSpeed;
        }

        private void Update()
        {
            Vector3 position = transform.position;
            Vector2 position2D = new Vector2(position.x, position.y);
            transform.position = new Vector3(position2D.x, position2D.y, position2D.y);

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
        }

        public void PlayerExecute()
        {
            Collider2D collider2D = Physics2D.OverlapPoint(new Vector2(transform.position.x, transform.position.y), interactionLayerMask);
            if (collider2D != null)
            {
                if (collider2D.TryGetComponent(out InteractionCollider interactionCollider))
                {
                    interactionCollider.OnPlayerExecute();
                }
            }
        }
    }
}