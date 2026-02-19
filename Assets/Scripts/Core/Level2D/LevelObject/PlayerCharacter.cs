using UnityEngine;

namespace Core.Level2D.LevelObjects
{
    [AddComponentMenu("Level 2D/Player Character")]
    public class PlayerCharacter : LevelObject
    {
        public new Rigidbody2D rigidbody2D;
        public float MoveSpeed = 2.5f;
        public void SetMoveInput(Vector2 vector2)
        {
            rigidbody2D.velocity = vector2 * MoveSpeed;
        }

        private void Update()
        {
            Vector3 position = transform.position;
            Vector2 position2D = new Vector2(position.x, position.y);
            transform.position = new Vector3(position2D.x, position2D.y, position2D.y);
        }
    }
}