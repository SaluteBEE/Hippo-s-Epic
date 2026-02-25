using UnityEngine;

namespace Core.Level2D.Maps
{
    /// <summary>
    /// 基于自身 Transform Z 轴 作为系数，在 Camera Moved 回调中乘算
    /// </summary>
    public class ParallaxLayer
    {
        private readonly Transform transform;
        private readonly float scaleFactor;

        public ParallaxLayer(Transform transform)
        {
            this.transform = transform;
            scaleFactor = transform.position.z;
        }

        public void OnCameraMoved(Vector2 cameraOffset)
        {
            Vector2 layerOffset = cameraOffset * scaleFactor;
            transform.position = (Vector3)layerOffset + new Vector3(0, 0, scaleFactor);
        }
    }
}