using UnityEngine;

namespace Map
{
    public abstract class MapObject : MonoBehaviour
    {
        public abstract void OnMapEntered();
        public abstract void OnMapExited();
    
    }
}