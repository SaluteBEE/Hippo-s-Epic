using System;
using Core.Level2D.Camera;
using Core.Level2D.Maps;
using UnityEngine;

namespace Core.Level2D.LevelObjects
{
    [AddComponentMenu("Level 2D/Player Character")]
    public class PlayerCharacter : LevelObject, IFocusTarget
    {
        public new Rigidbody2D rigidbody2D;
        public float MoveSpeed = 2.5f;

        public LayerMask interactionLayerMask;

        private InteractionCollider _currentInteractionCollider;

        private  AnimationController currentAnimationController;
        private bool isMove = false;
        private void Awake()
        {
            currentAnimationController = GetComponentInChildren<AnimationController>();
        }

        public void SetMoveInput(Vector2 vector2)
        {
            rigidbody2D.velocity = vector2 * MoveSpeed;
            if (rigidbody2D.velocity.x > 0)
            {
                currentAnimationController.transform.localScale = new Vector3(-1, 1, 1);    
            }
            else
            {
                currentAnimationController.transform.localScale = new Vector3(1, 1, 1);    
            }
            
            float speed = rigidbody2D.velocity.sqrMagnitude;
            bool curState = speed > 0;

            if (curState != isMove)
            {
                currentAnimationController.PlayComposition(speed>0?"walk2":"idle");
                isMove = curState;
            }
            
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

        public event Action<Vector2> Moved;
    }
}