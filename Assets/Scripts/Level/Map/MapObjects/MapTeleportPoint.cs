using System;
using UnityEngine;

/// <summary>
/// 地图传送点。
/// 继承自MapObject，无碰撞体，仅有触发器（IsTrigger = true）。
/// 主要负责检测玩家进入触发区，当玩家按E键时触发传送。
/// </summary>
public class MapTeleportPoint : MapObject
{
    [Header("Teleport Target")]
    [SerializeField] private string targetMapId;        // 目标地图ID
    [SerializeField] private int targetTeleportIndex;   // 目标地图的进入点索引

    [Header("Trigger Settings")]
    private Collider2D triggerCollider;

    /// <summary>
    /// 玩家是否在传送点区域内。
    /// </summary>
    private bool isPlayerInTrigger;

    /// <summary>
    /// 传送触发事件，传递MapTeleportContext。
    /// 由MapBehaviour订阅。
    /// </summary>
    public event Action<MapTeleportContext> TeleportTriggered;

    /// <summary>
    /// 进入传送点区域事件（用于通知Level更新当前激活的传送点）。
    /// </summary>
    public event Action<MapTeleportPoint> PlayerEntered;

    /// <summary>
    /// 离开传送点区域事件（用于通知Level清除当前激活的传送点）。
    /// </summary>
    public event Action<MapTeleportPoint> PlayerExited;

    /// <summary>
    /// 目标地图ID。
    /// </summary>
    public string TargetMapId => targetMapId;

    /// <summary>
    /// 目标地图的进入点索引。
    /// </summary>
    public int TargetTeleportIndex => targetTeleportIndex;

    /// <summary>
    /// 玩家是否在传送点区域内。
    /// </summary>
    public bool IsPlayerInTrigger => isPlayerInTrigger;

    public override void Initialize()
    {
        base.Initialize();

        // 获取触发器
        triggerCollider = GetComponent<Collider2D>();

        if (triggerCollider == null)
        {
            Debug.LogError($"{name} (MapTeleportPoint) is missing Collider2D component");
            return;
        }

        // 确保为触发器
        if (!triggerCollider.isTrigger)
        {
            Debug.LogWarning($"{name} (MapTeleportPoint) Collider2D is not a trigger. Setting it now.");
            triggerCollider.isTrigger = true;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            // 玩家进入触发区，记录状态并通知Level
            isPlayerInTrigger = true;
            PlayerEntered?.Invoke(this);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            // 玩家离开触发区，清除状态并通知Level
            isPlayerInTrigger = false;
            PlayerExited?.Invoke(this);
        }
    }

    /// <summary>
    /// 触发传送（由Level在玩家按E键交互时调用）。
    /// </summary>
    public void Teleport()
    {
        // 触发传送事件
        var context = new MapTeleportContext(targetMapId, targetTeleportIndex);
        TeleportTriggered?.Invoke(context);
    }
}
