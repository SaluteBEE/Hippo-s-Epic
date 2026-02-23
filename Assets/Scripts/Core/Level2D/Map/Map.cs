using System;
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
        /// 摄像机位移限制
        /// </summary>
        public Vector2 cameraClamp;

        public void Initialize()
        {
            MapObject[] mapObjects = GetComponentsInChildren<MapObject>();
            for (int i = mapObjects.Length - 1; i >= 0; i--)
            {
                mapObjects[i].Initialize();
            }

            // TODO: 通知 Camera Controller，钳制摄像机位移
        }

        public void Dispose()
        {
            Destroy(gameObject);
        }
    }
}