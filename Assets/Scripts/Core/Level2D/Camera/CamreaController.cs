using System;
using UnityEngine;

namespace Core.Level2D.Camera
{
    public class CameraController : MonoBehaviour
    {
        private IFocusTarget _focusTarget;
        public IFocusTarget FocusTarget
        {
            get => _focusTarget;
            private set => _focusTarget = value;
        }

        private Vector2 _clampX;
        private Vector2 _clampY;
        private bool _isClamped;

        public void SetFocusTarget(IFocusTarget focusTarget)
        {
            if (FocusTarget != null)
            {
                // Unsubscribe
                FocusTarget.Moved -= SetPosition;
            }

            if (focusTarget != null)
            {
                // Subscribe
                FocusTarget.Moved += SetPosition;
            }
        }

        /// <summary>
        /// 设置摄像机位移的钳制范围
        /// </summary>
        /// <param name="clampX">X 轴钳制范围（x 为最小值，y 为最大值）</param>
        /// <param name="clampY">Y 轴钳制范围（x 为最小值，y 为最大值）</param>
        public void SetCameraClamp(Vector2 clampX, Vector2 clampY)
        {
            _clampX = clampX;
            _clampY = clampY;
            _isClamped = true;
        }

        public void SetPosition(Vector2 vector2)
        {
            if (_isClamped)
            {
                vector2.x = Mathf.Clamp(vector2.x, _clampX.x, _clampX.y);
                vector2.y = Mathf.Clamp(vector2.y, _clampY.x, _clampY.y);
            }
            transform.position = new Vector3(0.0f, 0.0f, -10f) + (Vector3)vector2;
        }
    }

    public interface IFocusTarget
    {
        public event Action<Vector2> Moved;
    }
}