using UnityEngine;

/// <summary>
/// Level的Unity生命周期方法和输入系统。
/// </summary>
public partial class Level : MonoBehaviour
{
    private void Awake()
    {
        // 1. 获取组件引用
        if (currentMap == null)
            currentMap = transform.GetComponentInChildren<MapBehaviour>();

        if (playerCharacter == null)
            playerCharacter = transform.GetComponentInChildren<PlayerCharacter>();

        // 2. 验证引用有效性
        if (currentMap == null)
            throw new System.Exception("MapBehaviour not found in children");

        if (playerCharacter == null)
            throw new System.Exception("PlayerCharacter not found in children");

        // 3. 初始化地图Prefab字典（临时方案）
        InitializeMapPrefabDict();

        // 4. 初始化地图
        InitializeMap(currentMap);

        // 5. 初始化玩家
        playerCharacter.Initialize();

        // 6. 订阅事件
        SubscribeToMapTeleportEvent(currentMap);

        // 7. 启用玩家控制
        isPlayerControlEnabled = true;
    }

    /// <summary>
    /// Level的Input系统：处理玩家输入。
    /// </summary>
    private void Update()
    {
        if (!isPlayerControlEnabled) return;

        // 处理玩家移动输入
        HandleMovementInput();

        // 处理交互输入
        HandleInteractionInput();
    }

    /// <summary>
    /// 处理WASD移动输入。
    /// </summary>
    private void HandleMovementInput()
    {
        // WASD移动
        float horizontal = Input.GetAxis("Horizontal"); // A/D 或 左/右箭头
        float vertical = Input.GetAxis("Vertical");     // W/S 或 上/下箭头

        Vector2 moveDirection = new Vector2(horizontal, vertical);

        // 归一化对角线移动，避免对角线移动速度大于单轴移动
        if (moveDirection.sqrMagnitude > 1f)
        {
            moveDirection = moveDirection.normalized;
        }
        // 将输入转换为PlayerCharacter的速度命令
        // TODO: 添加移动速度配置
        playerCharacter.SetVelocity(moveDirection);
    }

    /// <summary>
    /// 处理交互按键输入（如E键）。
    /// </summary>
    private void HandleInteractionInput()
    {
        // 交互按键（E键）
        if (Input.GetKeyDown(KeyCode.E))
        {
            // 检查是否有可用的交互对象
            if (currentInteractable != null)
            {
                // 处理传送点交互
                if (currentInteractable is MapTeleportPoint teleportPoint)
                {
                    teleportPoint.Teleport();
                }
                // TODO: 处理其他交互对象（NPC对话等）
                // else if (currentInteractable is NPCObject npc)
                // {
                //     npc.Interact();
                // }
            }
            // 没有可用交互对象时，不做任何反应（不输出警告）
        }
    }
}