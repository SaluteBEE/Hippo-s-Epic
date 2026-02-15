public class MapTeleportContext
{
    /// <summary>
    /// 目标地图的索引。
    /// </summary>
    public int TargetMapIndex { get; set; }

    /// <summary>
    /// 目标地图中传送位置的索引。
    /// </summary>
    public int TargetMapTeleportPositionIndex { get; set; }

    /// <summary>
    /// 无参构造函数，保留与现有代码的兼容性。
    /// </summary>
    public MapTeleportContext()
    {
    }

    /// <summary>
    /// 使用目标地图索引和传送位置索引初始化上下文。
    /// </summary>
    /// <param name="targetMapIndex">目标地图索引。</param>
    /// <param name="targetMapTeleportPositionIndex">目标地图中传送位置索引。</param>
    public MapTeleportContext(int targetMapIndex, int targetMapTeleportPositionIndex)
    {
        TargetMapIndex = targetMapIndex;
        TargetMapTeleportPositionIndex = targetMapTeleportPositionIndex;
    }
}