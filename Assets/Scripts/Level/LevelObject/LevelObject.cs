using UnityEngine;

public abstract class LevelObject : MonoBehaviour
{
    /// <summary>
    /// 初始化关卡对象。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 此方法用于在关卡对象开始参与游戏逻辑之前进行初始化操作，
    /// 例如缓存组件引用、注册事件或根据关卡数据配置对象状态。
    /// </para>
    /// <para>
    /// 通常由关卡管理器或关卡加载流程在合适的时机调用，而不是直接由外部脚本任意调用。
    /// 继承自 <see cref="LevelObject"/> 的子类可以重写此方法以添加自定义初始化逻辑；
    /// 如需保留基类行为，请在重写方法中显式调用 <c>base.Initialize()</c>。
    /// </para>
    /// </remarks>
    public virtual void Initialize() { }
}