using System;
using UnityEngine;

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
        if (FocusTarget == focusTarget)
            return;

        if (FocusTarget != null)
            FocusTarget.Moved -= SetPosition;

        FocusTarget = focusTarget;

        if (FocusTarget != null)
            FocusTarget.Moved += SetPosition;
    }

    /// <summary>
    /// 设置摄像机位移的钳制范围
    /// </summary>
    public void SetCameraClamp(Vector2 clampX, Vector2 clampY)
    {
        _clampX = clampX;
        _clampY = clampY;
        _isClamped = true;
    }

    /// <summary>
    /// 清除边界限制（如果某些地图不需要限制时可用）
    /// </summary>
    public void ClearCameraClamp()
    {
        _isClamped = false;
    }

    public void SetPosition(Vector2 pos)
    {
        if (_isClamped)
        {
            pos.x = Mathf.Clamp(pos.x, _clampX.x, _clampX.y);
            pos.y = Mathf.Clamp(pos.y, _clampY.x, _clampY.y);
        }

        transform.position = new Vector3(pos.x, pos.y, -10f);
        Moved?.Invoke(pos);
    }

    /// <summary>
    /// 在更新边界或切换目标后，立刻同步摄像机到目标当前位置
    /// </summary>
    public void SnapToFocusTarget()
    {
        if (FocusTarget is MonoBehaviour target)
        {
            Vector3 p = target.transform.position;
            SetPosition(new Vector2(p.x, p.y));
        }
    }

    public event Action<Vector2> Moved;
}

public interface IFocusTarget
{
    event Action<Vector2> Moved;
}