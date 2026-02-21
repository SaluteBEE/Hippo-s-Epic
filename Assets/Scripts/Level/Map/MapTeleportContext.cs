/// <summary>
/// 传送上下文数据类，用于传递传送目标信息。
/// </summary>
public class MapTeleportContext
{
    /// <summary>
    /// 目标地图的ID（字符串标识符，如"Town_01", "Dungeon_02"等）。
    /// </summary>
    public string TargetMapId { get; set; }

    /// <summary>
    /// 目标地图中进入点的索引，用于定位玩家传送后的位置。
    /// </summary>
    public int TargetTeleportPointIndex { get; set; }

    /// <summary>
    /// 无参构造函数。
    /// </summary>
    public MapTeleportContext()
    {
    }

    /// <summary>
    /// 使用目标地图ID和进入点索引初始化上下文。
    /// </summary>
    /// <param name="targetMapId">目标地图ID</param>
    /// <param name="targetTeleportPointIndex">目标地图进入点索引</param>
    public MapTeleportContext(string targetMapId, int targetTeleportPointIndex)
    {
        TargetMapId = targetMapId;
        TargetTeleportPointIndex = targetTeleportPointIndex;
    }
}