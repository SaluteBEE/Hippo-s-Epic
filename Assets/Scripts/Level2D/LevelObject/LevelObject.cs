using UnityEngine;


    public class LevelObject : MonoBehaviour
    {
        private Vector2 _position2D;

        public void SetPosition(Vector2 vector2)
        {
            _position2D = vector2;
            transform.position = new Vector3(vector2.x, vector2.y, vector2.y);
        }
    }
