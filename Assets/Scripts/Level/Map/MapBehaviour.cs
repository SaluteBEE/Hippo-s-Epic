using System;
using System.Linq;
using UnityEngine;

/// <summary>
/// 地图行为管理器，负责管理地图中的所有对象和传送点。
/// </summary>
public class MapBehaviour : MonoBehaviour
{
    private bool isMapEntered;

    /// <summary>
    /// 地图中的所有MapObject。
    /// </summary>
    public MapObject[] MapObjects;

    /// <summary>
    /// 地图中的传送点数组。
    /// </summary>
    private MapTeleportPoint[] teleportPoints;

    /// <summary>
    /// 地图传送进入点位置数组（玩家传送到本地图时的出现位置）。
    /// 索引对应MapTeleportContext.TargetTeleportPointIndex。
    /// </summary>
    [Header("Teleport Entry Points")]
    [SerializeField] private Vector2[] teleportEntryPoints = new Vector2[0];

    /// <summary>
    /// 地图传送事件，当玩家触发传送点时由MapBehaviour转发给Level。
    /// </summary>
    public event Action<MapTeleportContext> MapTeleported;

    /// <summary>
    /// 传送点进入事件，当玩家进入传送点区域时由MapBehaviour转发给Level。
    /// </summary>
    public event Action<MapTeleportPoint> TeleportPointEntered;

    /// <summary>
    /// 传送点离开事件，当玩家离开传送点区域时由MapBehaviour转发给Level。
    /// </summary>
    public event Action<MapTeleportPoint> TeleportPointExited;

    /// <summary>
    /// 获取指定索引的传送点GameObject。
    /// </summary>
    /// <param name="index">传送点索引</param>
    /// <returns>MapTeleportPoint，索引无效时返回null</returns>
    public MapTeleportPoint GetTeleportPoint(int index)
    {
        if (teleportPoints == null || index < 0 || index >= teleportPoints.Length)
        {
            Debug.LogError($"MapBehaviour: Teleport point index {index} is out of range. " +
                $"Available: {(teleportPoints != null ? teleportPoints.Length : 0)}");
            return null;
        }
        return teleportPoints[index];
    }

    /// <summary>
    /// 获取指定索引的传送进入点位置（玩家传送到本地图时的出现位置）。
    /// </summary>
    /// <param name="index">进入点索引</param>
    /// <param name="position">输出的进入点位置</param>
    /// <returns>是否成功获取</returns>
    public bool GetTeleportEntryPoint(int index, out Vector2 position)
    {
        if (teleportEntryPoints == null || index < 0 || index >= teleportEntryPoints.Length)
        {
            Debug.LogError($"MapBehaviour '{name}': Teleport entry point index {index} is out of range. " +
                $"Available: {(teleportEntryPoints != null ? teleportEntryPoints.Length : 0)}");
            position = Vector2.zero;
            return false;
        }
        position = teleportEntryPoints[index];
        return true;
    }

    public void OnLevelInitialized()
    {
        Initialize();
    }

    /// <summary>
    /// 初始化地图：获取所有MapObject，按类型进行特殊处理和事件订阅。
    /// </summary>
    public void Initialize()
    {
        // 遍历所有子物体，找到所有 MapObject 组件
        MapObjects = GetComponentsInChildren<MapObject>();

        // 遍历MapObject，根据类型做特殊处理
        foreach (var mapObject in MapObjects)
        {
            // 初始化每个MapObject
            // 注意：部分MapObject在Initialize()时可能获取全局信息
            // 例如：某些物体根据存档Flag决定是否出现
            // TODO: 实现全局信息获取机制（暂不实现）
            mapObject.Initialize();

            // 如果是MapTeleportPoint，订阅其传送事件
            if (mapObject is MapTeleportPoint teleportPoint)
            {
                teleportPoint.TeleportTriggered += OnTeleportPointTriggered;
                teleportPoint.PlayerEntered += OnTeleportPointEntered;
                teleportPoint.PlayerExited += OnTeleportPointExited;
            }

            // 可以在这里添加其他类型的特殊处理
            // 例如：NPC、可破坏物体等
            // 根据类型进行检测并执行相应的订阅或配置
        }

        // 缓存传送点数组
        teleportPoints = MapObjects.OfType<MapTeleportPoint>().ToArray();
    }

    public void EnterMap()
    {
        if (!isMapEntered)
        {
            isMapEntered = true;

            OnMapEntered();
            return;
        }
        throw new Exception("Map has already been entered. Multiple entries are not allowed.");
    }

    public void ExitMap()
    {
        if (isMapEntered)
        {
            isMapEntered = false;

            // 取消订阅所有传送点事件
            foreach (var mapObject in MapObjects)
            {
                if (mapObject is MapTeleportPoint teleportPoint)
                {
                    teleportPoint.TeleportTriggered -= OnTeleportPointTriggered;
                    teleportPoint.PlayerEntered -= OnTeleportPointEntered;
                    teleportPoint.PlayerExited -= OnTeleportPointExited;
                }
            }

            OnMapExited();
            return;
        }
        throw new Exception("Map has not been entered yet. Cannot exit.");
    }

    /// <summary>
    /// 销毁地图（定义方法框架，暂不实现内部逻辑）。
    /// </summary>
    public void DestroyMap()
    {
        // TODO: 实现地图销毁逻辑
        // - 清理地图对象引用
        // - 销毁GameObject
        // - 释放资源
    }

    /// <summary>
    /// 传送点触发时的回调，转发传送事件给Level。
    /// </summary>
    private void OnTeleportPointTriggered(MapTeleportContext context)
    {
        MapTeleported?.Invoke(context);
    }

    /// <summary>
    /// 传送点进入时的回调，转发进入事件给Level。
    /// </summary>
    private void OnTeleportPointEntered(MapTeleportPoint teleportPoint)
    {
        TeleportPointEntered?.Invoke(teleportPoint);
    }

    /// <summary>
    /// 传送点离开时的回调，转发离开事件给Level。
    /// </summary>
    private void OnTeleportPointExited(MapTeleportPoint teleportPoint)
    {
        TeleportPointExited?.Invoke(teleportPoint);
    }

    private void OnMapEntered()
    {
        foreach (var mapObject in MapObjects)
        {
            mapObject.OnMapEntered();
        }
    }

    private void OnMapExited()
    {
        foreach (var mapObject in MapObjects)
        {
            mapObject.OnMapExited();
        }
    }
}