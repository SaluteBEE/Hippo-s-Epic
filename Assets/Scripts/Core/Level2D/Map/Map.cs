using UnityEngine;

namespace Core.Level2D.Maps
{
    public sealed class Map : MonoBehaviour
    {
        /// <summary>
        /// 默认入口
        /// </summary>
        public Vector2 mainEntrance;

        /// <summary>
        /// 摄像机 X 轴位移限制（x 为最小值，y 为最大值）
        /// </summary>
        public Vector2 cameraClampX;

        /// <summary>
        /// 摄像机 Y 轴位移限制（x 为最小值，y 为最大值）
        /// </summary>
        public Vector2 cameraClampY;

        public void Initialize()
        {
            MapObject[] mapObjects = GetComponentsInChildren<MapObject>();
            for (int i = mapObjects.Length - 1; i >= 0; i--)
            {
                mapObjects[i].Initialize();
            }
        }

        public void Dispose()
        {
            Destroy(gameObject);
        }
    }
}