using UnityEngine;

/// <summary>
/// 所有关卡对象的抽象基类。
/// 提供统一的位置和运动控制接口。
/// </summary>
/// <remarks>
/// 2D物理在X-Y平面直接运作，Z轴作为独立高度层（用于渲染排序等）。
/// 摄像机采用俯视视角模拟侧视卷轴效果。
/// </remarks>
public abstract class LevelObject : MonoBehaviour
{
    /// <summary>
    /// 2D刚体组件，用于物理计算。
    /// </summary>
    protected Rigidbody2D rb2D;

    /// <summary>
    /// 缓存的Transform引用，避免重复访问。
    /// </summary>
    protected Transform cachedTransform;

    /// <summary>
    /// 当前高度（Z轴），独立于X-Y物理平面。
    /// </summary>
    protected float currentHeight;

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
    public virtual void Initialize()
    {
        rb2D = GetComponent<Rigidbody2D>();
        cachedTransform = transform;

        // 初始化高度为当前Z位置
        currentHeight = cachedTransform.position.z;
    }

    /// <summary>
    /// 设置物体在X-Y平面的位置，以及独立的Z轴高度。
    /// </summary>
    /// <param name="position">X-Y平面位置（2D物理平面，用户空间：+Y=向上）</param>
    /// <param name="height">Z轴高度（独立于物理平面），默认0.0f</param>
    public void SetPosition(Vector2 position, float height = 0.0f)
    {
        currentHeight = height;

        // Y轴反向：用户空间 → Unity空间
        // 用户期望+Y是「向上」，但Unity俯视角中需要-Y才能实现向上移动
        cachedTransform.position = new Vector3(
            position.x,    // X: 水平位置
            -position.y,   // Y: 纵向位置（反向以适配俯视角）
            height         // Z: 独立高度层
        );

        // 同步Rigidbody2D位置（同样需要Y轴反向）
        if (rb2D != null)
        {
            rb2D.position = new Vector2(position.x, -position.y);
        }
    }

    /// <summary>
    /// 设置物体速度（X-Y平面）。
    /// </summary>
    /// <param name="velocity">X-Y平面速度（用户空间：+Y=向上）</param>
    public void SetVelocity(Vector2 velocity)
    {
        if (rb2D != null)
        {
            // Y轴反向：用户空间 → Unity空间
            // 用户按W键期望+Y速度（向上），但Unity俯视角需要-Y速度
            rb2D.velocity = new Vector2(velocity.x, -velocity.y);
        }
    }
}