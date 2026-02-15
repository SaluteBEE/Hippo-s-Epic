using UnityEngine;

/// <summary>
/// 视觉装饰物体。
/// 用于地面、天花板、背景等无逻辑的装饰性对象。
/// 支持子类扩展以添加视觉效果。
/// </summary>
public class VisualObject : MapObject
{
    public override void Initialize()
    {
        base.Initialize();
        // 可在子类中添加视觉效果初始化
    }
}
