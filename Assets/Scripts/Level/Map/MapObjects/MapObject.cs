/// <summary>
/// 地图对象基类，所有放置在地图中的对象都应继承此类。
/// </summary>
public class MapObject : LevelObject
{
    /// <summary>
    /// 地图进入时调用。子类可重写以添加自定义逻辑。
    /// </summary>
    public virtual void OnMapEntered()
    {
    }

    /// <summary>
    /// 地图退出时调用。子类可重写以添加自定义逻辑。
    /// </summary>
    public virtual void OnMapExited()
    {
    }
}