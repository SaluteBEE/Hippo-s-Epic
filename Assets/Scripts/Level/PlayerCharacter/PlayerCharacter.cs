using UnityEngine;

/// <summary>
/// 玩家角色类，独立于地图层级，参与地图的物理和逻辑运算。
/// </summary>
public class PlayerCharacter : LevelObject
{
    /// <summary>
    /// 控制是否启用。
    /// </summary>
    private bool isControlEnabled;

    /// <summary>
    /// 启用或禁用玩家控制。
    /// </summary>
    /// <param name="enable">是否启用控制</param>
    public void EnableControl(bool enable)
    {
        isControlEnabled = enable;

        // 禁用控制时停止当前运动
        if (!enable)
        {
            SetVelocity(Vector2.zero);
        }
    }

    /// <summary>
    /// 传送玩家到指定位置。
    /// </summary>
    /// <param name="position">目标X-Y平面位置</param>
    /// <param name="height">目标Z轴高度</param>
    public void TeleportTo(Vector2 position, float height = 0.0f)
    {
        SetPosition(position, height);
        SetVelocity(Vector2.zero);
    }

    /// <summary>
    /// 控制是否启用（只读）。
    /// </summary>
    public bool IsControlEnabled => isControlEnabled;

    public override void Initialize()
    {
        base.Initialize();
        isControlEnabled = true;
    }
}