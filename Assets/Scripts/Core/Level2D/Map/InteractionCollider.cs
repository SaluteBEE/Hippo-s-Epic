using UnityEngine;

namespace Core.Level2D.Maps
{
    /// <summary>
    /// Map Object 触发器组件，用于玩家角色的点检测
    /// </summary>
    public abstract class InteractionCollider : MonoBehaviour
    {
        public abstract void OnPlayerEnter();
        public abstract void OnPlayerExit();
        public abstract void OnPlayerStay();
        public abstract void OnPlayerExecute();
    }
}