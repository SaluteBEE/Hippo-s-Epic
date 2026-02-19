using UnityEngine;

namespace Core.Level2D.Maps
{
    public abstract class InteractionCollider : MonoBehaviour
    {
        public abstract void OnPlayerEnter();
        public abstract void OnPlayerExit();
        public abstract void OnPlayerExecute();
    }
}