using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Level主控制器（事件订阅、地图切换逻辑、输入系统）。
/// </summary>
public partial class Level : MonoBehaviour
{
    [SerializeField]
    private MapBehaviour currentMap;
    [SerializeField]
    private PlayerCharacter playerCharacter;

    /// <summary>
    /// 玩家控制是否启用。
    /// </summary>
    private bool isPlayerControlEnabled;

    /// <summary>
    /// 当前可交互对象（玩家正在其区域内的交互对象，如传送点、NPC等）。
    /// TODO: 后续可改为IInteractable接口以支持多种交互对象类型。
    /// </summary>
    private MapTeleportPoint currentInteractable;

    /// <summary>
    /// 临时：地图Prefab数组（后续用MapContainer替代）。
    /// </summary>
    [Header("Map Prefabs (临时方案，后续用MapContainer替代)")]
    [SerializeField] private GameObject[] mapPrefabs;

    /// <summary>
    /// 临时：地图Prefab字典，基于MapBehaviour的name或mapId索引（后续用MapContainer替代）。
    /// </summary>
    private Dictionary<string, GameObject> mapPrefabDict;

    /// <summary>
    /// 初始化当前地图。
    /// </summary>
    /// <param name="map">要初始化的地图</param>
    private void InitializeMap(MapBehaviour map)
    {
        // 1. 调用地图初始化
        map.Initialize();

        // 2. 触发地图进入逻辑
        map.EnterMap();
    }

    /// <summary>
    /// 初始化地图Prefab字典。
    /// </summary>
    private void InitializeMapPrefabDict()
    {
        mapPrefabDict = new Dictionary<string, GameObject>();
        if (mapPrefabs == null) return;

        foreach (var prefab in mapPrefabs)
        {
            if (prefab == null) continue;
            // 使用prefab名称作为地图ID
            string mapId = prefab.name;
            if (!mapPrefabDict.ContainsKey(mapId))
            {
                mapPrefabDict.Add(mapId, prefab);
            }
            else
            {
                Debug.LogWarning($"Level: Duplicate map prefab ID '{mapId}'. Skipping.");
            }
        }
    }

    /// <summary>
    /// 地图传送事件回调：执行完整的地图切换逻辑。
    /// </summary>
    private void OnMapTeleported(MapTeleportContext context)
    {
        // 1. 禁用玩家控制
        isPlayerControlEnabled = false;
        playerCharacter.EnableControl(false);

        // 2. 取消对当前地图的事件订阅
        UnsubscribeFromMapTeleportEvent(currentMap);

        // 3. 退出当前地图
        currentMap.ExitMap();

        // 4. 加载目标地图（根据地图ID）
        MapBehaviour targetMap = LoadMap(context.TargetMapId);

        // 5. 初始化目标地图
        InitializeMap(targetMap);

        // 6. 订阅新地图事件
        SubscribeToMapTeleportEvent(targetMap);

        // 7. 获取进入点位置并传送玩家
        if (targetMap.GetTeleportEntryPoint(context.TargetTeleportPointIndex, out Vector2 entryPosition))
        {
            // Z轴高度默认为0（后续可扩展为配置项）
            playerCharacter.TeleportTo(entryPosition, 0f);
        }
        else
        {
            Debug.LogWarning($"Level: Failed to get teleport entry point {context.TargetTeleportPointIndex} " +
                $"for map '{context.TargetMapId}'. Player position not updated.");
        }

        // 8. 销毁旧地图（定义方法，暂不实现内部逻辑）
        DestroyOldMap(currentMap);

        // 9. 更新当前地图引用
        currentMap = targetMap;

        // 10. 恢复玩家控制
        isPlayerControlEnabled = true;
        playerCharacter.EnableControl(true);
    }

    /// <summary>
    /// 加载新地图（临时实现：从mapPrefabDict字典实例化，后续将使用MapContainer替代）。
    /// </summary>
    /// <param name="mapId">地图ID</param>
    /// <returns>实例化的MapBehaviour</returns>
    private MapBehaviour LoadMap(string mapId)
    {
        if (!mapPrefabDict.ContainsKey(mapId))
        {
            throw new Exception(
                $"Map with ID '{mapId}' not found in map prefab dictionary");
        }

        GameObject mapPrefab = mapPrefabDict[mapId];
        GameObject mapInstance = Instantiate(mapPrefab, transform);

        MapBehaviour mapBehaviour = mapInstance.GetComponent<MapBehaviour>();

        if (mapBehaviour == null)
        {
            throw new Exception(
                $"Map prefab '{mapId}' does not have MapBehaviour component");
        }

        return mapBehaviour;
    }

    /// <summary>
    /// 销毁旧地图（定义方法框架，暂不实现内部逻辑）。
    /// </summary>
    /// <param name="map">要销毁的地图</param>
    private void DestroyOldMap(MapBehaviour map)
    {
        // TODO: 实现地图销毁逻辑
        // - 清理地图对象引用
        // - 销毁GameObject
        // - 释放资源
    }

    /// <summary>
    /// 辅助方法：简化地图切换流程（可在非传送场景下使用，如关卡加载）。
    /// </summary>
    private void SwitchToMap(MapBehaviour newMap, int teleportPointIndex)
    {
        // 辅助方法：简化地图切换流程
        // 可在非传送场景下使用（如关卡加载）
    }

    private void SubscribeToMapTeleportEvent(MapBehaviour map)
    {
        map.MapTeleported += OnMapTeleported;
        map.TeleportPointEntered += OnInteractableEntered;
        map.TeleportPointExited += OnInteractableExited;
    }

    private void UnsubscribeFromMapTeleportEvent(MapBehaviour map)
    {
        map.MapTeleported -= OnMapTeleported;
        map.TeleportPointEntered -= OnInteractableEntered;
        map.TeleportPointExited -= OnInteractableExited;
    }

    /// <summary>
    /// 交互对象进入回调：记录当前可交互对象（传送点、NPC等）。
    /// </summary>
    private void OnInteractableEntered(MapTeleportPoint interactable)
    {
        currentInteractable = interactable;
        // TODO: 显示UI提示（如"按E键传送"、"按E键对话"等）
    }

    /// <summary>
    /// 交互对象离开回调：清除当前可交互对象。
    /// </summary>
    private void OnInteractableExited(MapTeleportPoint interactable)
    {
        if (currentInteractable == interactable)
        {
            currentInteractable = null;
            // TODO: 隐藏UI提示
        }
    }
}
