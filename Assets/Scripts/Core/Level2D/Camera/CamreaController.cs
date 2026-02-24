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

        public void SetPosition(Vector2 vector2)
        {
            // TODO: Clamp camera position
            transform.position = new Vector3(0.0f, 0.0f, -10f) + (Vector3)vector2;
        }
    }

    public interface IFocusTarget
    {
        public event Action<Vector2> Moved;
    }
}