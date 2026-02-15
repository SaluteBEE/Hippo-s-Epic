# Level系统实现需求文档

## 文档信息

- **创建日期**: 2026年2月15日
- **版本**: v1.1
- **状态**: 已审阅，已修改

---

## 1. 现有架构分析

### 1.1 现有类结构

```text
LevelObject (抽象基类)
├── PlayerCharacter (玩家角色)
└── MapObject (地图对象)

Level : MonoBehaviour (partial class)
├── Level.cs (事件订阅和地图切换逻辑)
└── Level.Monobehaviour.cs (Unity生命周期方法)

MapBehaviour : MonoBehaviour
├── MapObjects[] (地图对象数组)
├── MapTeleported (传送事件)
└── Initialize() (初始化方法)

MapTeleportContext (传送上下文数据类)
├── TargetMapId (目标地图ID, String)
└── TargetTeleportPointIndex (目标进入点索引, int)
```

### 1.2 现有功能

- **LevelObject**: 提供基础的`Initialize()`虚方法
- **MapBehaviour**:
  - 管理MapObject数组
  - 提供`MapTeleported`事件
  - 实现`Initialize()`方法获取子对象
  - 实现`EnterMap()`和`ExitMap()`方法
- **Level**:
  - 在`Awake()`中获取MapBehaviour和PlayerCharacter
  - 订阅MapTeleported事件
  - 提供`OnMapTeleported()`方法框架（未实现）

---

## 2. 需求分析

### 2.1 核心需求

#### 需求1: Level初始化流程

- Level在`Awake()`时获取PlayerCharacter和MapBehaviour
- 触发MapBehaviour的初始化，让其获取并初始化所有子对象
- 初始化PlayerCharacter

#### 需求2: 地图切换系统

- Map通过`MapTeleported`事件（Action）通知Level
- Level订阅Map的MapTeleported事件
- 基于MapTeleportContext数据实现地图切换：
  - 实例化新地图Prefab
  - 初始化新地图的所有子对象
  - 取消对旧地图的订阅，订阅新地图
  - 销毁旧地图（定义方法，暂不实现）
  - 定位玩家到新地图的传送点

#### 需求3: 玩家与地图的关系

- PlayerCharacter不属于任何地图（独立于地图层级）
- PlayerCharacter参与地图GameObject的物理和逻辑运算
- 支持碰撞限制和MapObject交互

#### 需求4: 2D物理与侧视卷轴适配

- 使用Unity 2D物理系统进行碰撞和触发检测
- 游戏采用侧视卷轴视角
- 角色实际移动在X-Z平面
- 在Update方法中将Y轴物理数据映射到Z轴位置
- 所有LevelObject提供统一的位置和运动控制接口：
  - `SetPosition(Vector2 position, float height = 0.0f)`
  - `SetVelocity(Vector2 velocity)`

#### 需求5: 地图对象分类系统

定义三种地图对象类型：

##### 5.1 视觉物体 (VisualObject)

- 用途：装饰性物体，如地面、天花板、背景装饰
- 特点：一般无逻辑，仅用于视觉表现
- 继承：支持继承扩展

##### 5.2 碰撞物体 (CollisionObject)

- 用途：静态障碍物，如墙壁、桌椅、箱子
- 特点：一般不会移动，提供碰撞体
- 继承：支持继承后编写额外逻辑脚本

##### 5.3 NPC物体 (NPCObject)

- 用途：可交互的NPC角色
- 特点：
  - 继承自CollisionObject（具有碰撞功能）
  - NPC自行实现Trigger触发器检测
  - 具体触发逻辑和交互事件由NPC子类实现

---

## 3. 功能实现规划

### 3.1 需要新增的类

#### 3.1.1 MapContainer（地图容器）

```csharp
// 暂不实现，预留接口
// 用于管理和缓存多个地图Prefab
public class MapContainer
{
    public MapBehaviour GetMapByIndex(int mapIndex);
    public void UnloadMap(MapBehaviour map);
}
```

#### 3.1.2 VisualObject（视觉物体）

```csharp
public class VisualObject : MapObject
{
    // 装饰性物体，无特殊逻辑
    // 支持子类扩展
}
```

#### 3.1.3 CollisionObject（碰撞物体）

```csharp
public class CollisionObject : MapObject
{
    // 静态碰撞物体
    // 支持子类扩展以添加特殊逻辑
}
```

#### 3.1.4 MapTeleportPoint（传送点）

```csharp
public class MapTeleportPoint : MapObject
{
    [Header("Teleport Target")]
    [SerializeField] private string targetMapId;       // 目标地图ID
    [SerializeField] private int targetTeleportIndex; // 目标地图的进入点索引
    
    [Header("Trigger Settings")]
    private Collider2D triggerCollider;
    
    // 触发交互事件，传递MapTeleportContext
    public event Action<MapTeleportContext> TeleportTriggered;
    
    // 检测玩家进入触发区
    private void OnTriggerEnter2D(Collider2D other);
}
```

**说明**：

- MapTeleportPoint继承自MapObject，是地图对象的一部分
- 无碰撞体（Collider），仅有触发器（IsTrigger = true）
- 玩家进入触发区后触发交互，向MapBehaviour发送MapTeleportContext
- MapTeleportContext包含目标地图ID（String）和目标进入点索引（int）
- MapBehaviour订阅此事件并转发给Level

#### 3.1.5 MapTeleportContext（传送上下文数据类）

```csharp
public class MapTeleportContext
{
    /// <summary>
    /// 目标地图的ID（字符串标识符）
    /// </summary>
    public string TargetMapId { get; set; }
    
    /// <summary>
    /// 目标地图中进入点的索引
    /// </summary>
    public int TargetTeleportPointIndex { get; set; }
    
    /// <summary>
    /// 无参构造函数
    /// </summary>
    public MapTeleportContext()
    {
    }
    
    /// <summary>
    /// 使用目标地图ID和进入点索引初始化上下文
    /// </summary>
    /// <param name="targetMapId">目标地图ID</param>
    /// <param name="targetTeleportPointIndex">目标地图进入点索引</param>
    public MapTeleportContext(string targetMapId, int targetTeleportPointIndex)
    {
        TargetMapId = targetMapId;
        TargetTeleportPointIndex = targetTeleportPointIndex;
    }
}
```

**说明**：

- MapTeleportContext是传送上下文数据类，用于传递传送目标信息
- TargetMapId为地图的字符串标识符（如"Town_01", "Dungeon_02"等）
- TargetTeleportPointIndex为目标地图中的进入点索引，用于定位玩家传送后的位置

### 3.2 需要修改的类

#### 3.2.1 LevelObject（基类扩展）

**新增功能**:

- 添加2D物理到3D位置映射支持
- 添加统一的位置和运动控制接口
- 添加Update()中的Y轴到Z轴映射逻辑

**新增方法**:

```csharp
// 设置物体位置（X-Z平面 + 高度）
public void SetPosition(Vector2 position, float height = 0.0f)

// 设置物体速度（仅X轴）
public void SetVelocity(Vector2 velocity)

// Unity Update中调用，映射Y轴到Z轴
protected virtual void Update()
```

**新增字段**:

```csharp
protected Rigidbody2D rb2D;           // 2D刚体组件
protected Transform cachedTransform;   // 缓存Transform
protected float currentHeight;         // 当前高度（Z轴）
```

#### 3.2.2 MapBehaviour（地图管理扩展）

**新增功能**:

- 管理MapTeleportPoint数组
- 提供根据索引获取传送点的方法
- 提供Destroy()方法框架（暂不实现）

**新增方法**:

```csharp
// 获取传送点
public MapTeleportPoint GetTeleportPoint(int index)

// 销毁地图（定义，暂不实现）
public void DestroyMap()
```

**新增字段**:

```csharp
private MapTeleportPoint[] teleportPoints; // 传送点数组
```

#### 3.2.3 Level（核心逻辑扩展）

**新增功能**:

- 完善Awake()初始化流程
- 实现完整的OnMapTeleported()地图切换逻辑
- 实现Level的Input系统，处理玩家输入
- 直接订阅MapBehaviour的MapTeleported事件（不在Update中检查）

**新增方法**:

```csharp
// 初始化当前地图
private void InitializeMap(MapBehaviour map)

// 加载新地图（暂时直接实例化Prefab，后续用MapContainer替代）
private MapBehaviour LoadMap(string mapId)

// 切换到新地图
private void SwitchToMap(MapBehaviour newMap, int teleportPointIndex)

// Input系统相关方法
private void Update() // 处理玩家输入
private void HandleMovementInput() // 处理WASD移动输入
private void HandleInteractionInput() // 处理交互按键输入
```

**新增字段**:

```csharp
private bool isPlayerControlEnabled; // 玩家控制是否启用

// 临时：地图Prefab字典（后续用MapContainer替代）
[SerializeField] private GameObject[] mapPrefabs;
private Dictionary<string, GameObject> mapPrefabDict;
```

**Level Input系统**:

Level拥有独立的输入系统，负责处理以下输入：

1. **角色控制输入**：
   - WASD移动（实现）
   - 鼠标点击地图位置移动（暂不实现）
   - 交互按键（E键，实现）
   - 鼠标点击可交互目标后移动并交互（暂不实现）
   - 其他输入如背包、地图等（暂不实现）

2. **输入处理流程**：
   - Update()中检测输入
   - 根据isPlayerControlEnabled状态决定是否响应
   - 将输入转换为PlayerCharacter的控制命令

#### 3.2.4 PlayerCharacter（玩家角色扩展）

**新增功能**:

- 实现位置和速度控制
- 实现碰撞检测逻辑
- 添加移动控制接口

**新增方法**:

```csharp
// 启用/禁用玩家控制
public void EnableControl(bool enable)

// 传送到指定位置
public void TeleportTo(Vector2 position, float height)
```

**新增字段**:

```csharp
private bool isControlEnabled; // 控制是否启用
```

---

## 4. 详细实现描述

### 4.1 Level初始化流程（需求1）

#### 实现步骤

**Level.Monobehaviour.cs - Awake()**:

```csharp
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
    
    // 3. 初始化地图
    InitializeMap(currentMap);
    
    // 4. 初始化玩家
    playerCharacter.Initialize();
    
    // 5. 订阅事件
    SubscribeToMapTeleportEvent(currentMap);
}

private void InitializeMap(MapBehaviour map)
{
    // 1. 调用地图初始化
    map.Initialize();
    
    // 2. 触发地图进入逻辑
    map.EnterMap();
}
```

**约束**:

- currentMap和playerCharacter必须在Awake时存在或可获取
- 如果获取失败，应抛出异常而非继续执行
- 初始化顺序：地图 -> 玩家 -> 事件订阅

---

### 4.2 地图切换系统（需求2）

#### 实现步骤 - OnMapTeleported方法实现

**Level.cs - OnMapTeleported()**:

```csharp
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
    
    // 7. 获取传送点并传送玩家
    MapTeleportPoint teleportPoint = targetMap.GetTeleportPoint(
        context.TargetTeleportPointIndex);
    
    if (teleportPoint != null)
    {
        playerCharacter.TeleportTo(
            teleportPoint.Position, 
            teleportPoint.Height);
    }
    
    // 8. 销毁旧地图（定义方法，暂不实现内部逻辑）
    DestroyOldMap(currentMap);
    
    // 9. 更新当前地图引用
    currentMap = targetMap;
    
    // 10. 恢复玩家控制
    isPlayerControlEnabled = true;
    playerCharacter.EnableControl(true);
}

private MapBehaviour LoadMap(string mapId)
{
    // 临时实现：从mapPrefabDict字典实例化
    // 后续将使用MapContainer替代
    
    if (!mapPrefabDict.ContainsKey(mapId))
    {
        throw new System.Exception(
            $"Map with ID '{mapId}' not found in map prefab dictionary");
    }
    
    GameObject mapPrefab = mapPrefabDict[mapId];
    GameObject mapInstance = Instantiate(mapPrefab, transform);
    
    MapBehaviour mapBehaviour = mapInstance.GetComponent<MapBehaviour>();
    
    if (mapBehaviour == null)
    {
        throw new System.Exception(
            $"Map prefab '{mapId}' does not have MapBehaviour component");
    }
    
    return mapBehaviour;
}

private void DestroyOldMap(MapBehaviour map)
{
    // 定义方法框架，暂不实现
    // TODO: 实现地图销毁逻辑
    // - 清理地图对象引用
    // - 销毁GameObject
    // - 释放资源
}

private void SwitchToMap(MapBehaviour newMap, int teleportPointIndex)
{
    // 辅助方法：简化地图切换流程
    // 可在非传送场景下使用（如关卡加载）
}
```

**约束**:

- 地图切换期间必须禁用玩家控制
- 必须先取消旧地图订阅，再订阅新地图
- 传送点索引必须有效，否则应记录错误
- DestroyOldMap方法必须定义但暂不实现内部逻辑
- mapPrefabs是临时解决方案，后续用MapContainer替代

---

### 4.3 玩家与地图关系（需求3）

#### 设计原则

**层级结构**:

```text
Level (GameObject)
├── PlayerCharacter (GameObject) <-- 独立于地图
└── CurrentMap (MapBehaviour)
    ├── MapObject1
    ├── MapObject2
    └── ... (其他地图对象)
```

**物理交互**:

- PlayerCharacter必须有Rigidbody2D和Collider2D
- MapObject中的碰撞物体（CollisionObject）必须有Collider2D
- 使用Unity物理引擎自动处理碰撞

**交互系统**:

- NPCObject使用Trigger模式（IsTrigger = true）
- PlayerCharacter进入触发区时，NPC触发事件
- 事件传递链：NPC -> MapBehaviour -> Level -> 输入系统

**约束**:

- PlayerCharacter不应作为MapBehaviour的子对象
- PlayerCharacter与地图物体通过物理层和碰撞矩阵隔离不必要的碰撞
- 交互判断必须通过Level统一管理，不应在PlayerCharacter中直接处理

---

### 4.4 2D物理与侧视卷轴适配（需求4）

#### 技术实现方案

**LevelObject基类实现**:

```csharp
public abstract class LevelObject : MonoBehaviour
{
    protected Rigidbody2D rb2D;
    protected Transform cachedTransform;
    protected float currentHeight; // Z轴高度
    
    public virtual void Initialize()
    {
        // 缓存组件
        rb2D = GetComponent<Rigidbody2D>();
        cachedTransform = transform;
        
        // 初始化高度为当前Z位置
        currentHeight = cachedTransform.position.z;
    }
    
    /// <summary>
    /// 设置物体位置
    /// </summary>
    /// <param name="position">X-Z平面位置（X保持，Z映射到Y）</param>
    /// <param name="height">物体高度（最终Z轴位置），默认0.0f</param>
    public void SetPosition(Vector2 position, float height = 0.0f)
    {
        currentHeight = height;
        
        // X保持不变，Y用于2D物理计算，Z用于显示高度
        cachedTransform.position = new Vector3(
            position.x,      // X: 水平位置
            position.y,      // Y: 2D物理位置（临时）
            height           // Z: 实际显示高度
        );
        
        // 同步Rigidbody2D位置
        if (rb2D != null)
        {
            rb2D.position = new Vector2(position.x, position.y);
        }
    }
    
    /// <summary>
    /// 设置物体速度
    /// </summary>
    /// <param name="velocity">X-Y平面速度</param>
    public void SetVelocity(Vector2 velocity)
    {
        if (rb2D != null)
        {
            rb2D.velocity = velocity;
        }
    }
    
    protected virtual void Update()
    {
        // 核心：将2D物理的Y轴位置映射到3D的Z轴
        if (rb2D != null && cachedTransform != null)
        {
            Vector2 physicsPosition = rb2D.position;
            
            // 更新Transform位置：
            // - X从物理位置同步
            // - Y从物理位置读取（用于Z轴映射）
            // - Z设置为currentHeight + Y偏移
            cachedTransform.position = new Vector3(
                physicsPosition.x,                    // X: 水平位置
                0f,                                   // Y: 固定为0（不使用）
                currentHeight + physicsPosition.y     // Z: 高度 + 物理Y偏移
            );
        }
    }
}
```

**工作原理**:

1. 使用2D物理系统在X-Y平面计算碰撞和运动
2. 在Update()中读取Rigidbody2D的Y轴位置
3. 将Y轴数据映射到Transform的Z轴
4. Transform的Y轴固定为0（或其他视觉高度）
5. currentHeight作为基准高度，物理Y轴作为偏移量

**约束**:

- 所有LevelObject子类必须在场景中配置Rigidbody2D
- Rigidbody2D.bodyType根据对象类型设置：
  - PlayerCharacter: Dynamic
  - CollisionObject: Static
  - MapTeleportPoint: Static (仅Trigger，无碰撞体)
- 碰撞检测使用2D Colliders（BoxCollider2D、CircleCollider2D等）
- 物理层设置必须正确配置以避免不必要的碰撞

**示例场景配置**:

```text
PlayerCharacter:
- Rigidbody2D (Dynamic, Gravity Scale = 1)
- CapsuleCollider2D
- Layer: Player

CollisionObject (墙壁):
- Rigidbody2D (Static)
- BoxCollider2D
- Layer: Environment

MapTeleportPoint:
- 无Rigidbody2D (静态触发器)
- BoxCollider2D (IsTrigger = true)
- Layer: Teleport
```

---

### 4.5 地图对象分类系统（需求5）

#### 类定义

**VisualObject.cs**:

```csharp
using UnityEngine;

/// <summary>
/// 视觉装饰物体
/// 用于地面、天花板、背景等无逻辑的装饰性对象
/// </summary>
public class VisualObject : MapObject
{
    // 无特殊逻辑，支持子类扩展
    
    public override void Initialize()
    {
        base.Initialize();
        // 可在此添加视觉效果初始化
    }
}
```

**CollisionObject.cs**:

```csharp
using UnityEngine;

/// <summary>
/// 碰撞物体
/// 用于墙壁、桌椅、箱子等静态障碍物
/// </summary>
public class CollisionObject : MapObject
{
    // 基础碰撞物体，无特殊逻辑
    // 子类可扩展额外功能（如可破坏物体）
    
    public override void Initialize()
    {
        base.Initialize();
        
        // 获取Colliders子物体中的所有Collider2D组件
        Transform collidersParent = transform.Find("Colliders");
        
        if (collidersParent != null)
        {
            Collider2D[] colliders = collidersParent.GetComponentsInChildren<Collider2D>();
            if (colliders.Length == 0)
            {
                Debug.LogWarning($"{name} (CollisionObject) has Colliders child but no Collider2D components");
            }
        }
        else
        {
            // 如果没有Colliders子物体，尝试在自身查找
            Collider2D collider = GetComponent<Collider2D>();
            if (collider == null)
            {
                Debug.LogWarning($"{name} (CollisionObject) is missing Colliders child and Collider2D component");
            }
        }
    }
}
```

**重要说明**：

- CollisionObject可能有多个Collider2D组件
- 所有的Collider应当放置在GameObject的"Colliders"子物体中
- 如果有Colliders子物体，则从子物体中获取所有Collider2D
- 如果没有Colliders子物体，则尝试从自身获取（向后兼容

**MapTeleportPoint.cs**:

```csharp
using System;
using UnityEngine;

/// <summary>
/// 地图传送点
/// 继承自MapObject，无碰撞体，仅有触发器
/// 主要负责在玩家进入触发区后发送MapTeleportContext信息
/// </summary>
public class MapTeleportPoint : MapObject
{
    [Header("Teleport Target")]
    [SerializeField] private string targetMapId;       // 目标地图ID
    [SerializeField] private int targetTeleportIndex; // 目标地图的进入点索引
    
    [Header("Trigger Settings")]
    private Collider2D triggerCollider;
    
    /// <summary>
    /// 传送触发事件，传递MapTeleportContext
    /// 由MapBehaviour订阅
    /// </summary>
    public event Action<MapTeleportContext> TeleportTriggered;
    
    public string TargetMapId => targetMapId;
    public int TargetTeleportIndex => targetTeleportIndex;
    
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
            // 玩家进入触发区，触发传送事件
            var context = new MapTeleportContext(targetMapId, targetTeleportIndex);
            TeleportTriggered?.Invoke(context);
        }
    }
}
```

**MapBehaviour处理传送点事件**:

```csharp
public class MapBehaviour : MonoBehaviour
{
    public event Action<MapTeleportContext> MapTeleported;
    
    public override void Initialize()
    {
        // 获取所有MapObject
        MapObjects = GetComponentsInChildren<MapObject>();
        
        // 遍历MapObject，根据类型做特殊处理
        foreach (var mapObject in MapObjects)
        {
            // 初始化每个MapObject
            // 注意：部分MapObject在Initialize()时会获取全局信息
            // 例如：某些物体根据存档Flag决定是否出现
            // TODO: 实现全局信息获取机制（暂不实现）
            mapObject.Initialize();
            
            // 如果是MapTeleportPoint，订阅其传送事件
            if (mapObject is MapTeleportPoint teleportPoint)
            {
                teleportPoint.TeleportTriggered += OnTeleportPointTriggered;
            }
            
            // 可以在这里添加其他类型的特殊处理
            // 例如：NPC、可破坏物体等
            // 根据类型进行检测并执行相应的订阅或配置
        }
    }
    
    private void OnTeleportPointTriggered(MapTeleportContext context)
    {
        // 转发传送事件给Level
        MapTeleported?.Invoke(context);
    }
    
    public void ExitMap()
    {
        // 取消订阅所有传送点事件
        foreach (var mapObject in MapObjects)
        {
            if (mapObject is MapTeleportPoint teleportPoint)
            {
                teleportPoint.TeleportTriggered -= OnTeleportPointTriggered;
            }
        }
        
        // ... 其他退出逻辑
    }
}
```

**Level处理传送事件**:

```csharp
public partial class Level : MonoBehaviour
{
    private void Awake()
    {
        // ... 获取组件引用
        
        // 订阅MapBehaviour的MapTeleported事件
        // 注意：Level不在Update中检查传送点，而是直接订阅事件
        SubscribeToMapTeleportEvent(currentMap);
    }
    
    private void SubscribeToMapTeleportEvent(MapBehaviour map)
    {
        map.MapTeleported += OnMapTeleported;
    }
    
    private void OnMapTeleported(MapTeleportContext context)
    {
        // 玩家进入传送点触发区时自动调用
        // 执行地图切换逻辑
        // ... (地图切换代码)
    }
    
    // Level的Input系统
    private void Update()
    {
        if (!isPlayerControlEnabled) return;
        
        // 处理玩家移动输入
        HandleMovementInput();
        
        // 处理交互输入（如果需要确认传送等）
        HandleInteractionInput();
    }
    
    private void HandleMovementInput()
    {
        // WASD移动
        float horizontal = Input.GetAxis("Horizontal"); // A/D 或 左/右箭头
        float vertical = Input.GetAxis("Vertical");     // W/S 或 上/下箭头
        
        Vector2 moveDirection = new Vector2(horizontal, vertical);
        
        // 将输入传递给PlayerCharacter
        // playerCharacter.Move(moveDirection);
    }
    
    private void HandleInteractionInput()
    {
        // 交互按键（例如E键）
        if (Input.GetKeyDown(KeyCode.E))
        {
            // 处理其他交互（NPC对话等）
            // 传送点不需要在这里处理，因为是自动触发的
        }
    }
}
```

**约束**:

- VisualObject不应有Collider2D（除非需要视觉反馈）
- CollisionObject必须有非Trigger的Collider2D
- MapTeleportPoint必须有IsTrigger的Collider2D（无碰撞体）
- PlayerCharacter必须有"Player" Tag以便触发器识别
- Level统一管理所有交互逻辑，包括传送点和NPC（NPC自行实现）

---

## 5. 类图与关系

### 5.1 继承关系图

```text
MonoBehaviour
│
├── LevelObject (抽象基类)
│   ├── PlayerCharacter
│   └── MapObject
│       ├── VisualObject
│       ├── CollisionObject
│       │   └── NPCObject (自行实现Trigger)
│       └── MapTeleportPoint (触发器，由Level控制)
│
├── Level (主控制器)
│
└── MapBehaviour (地图管理)
```

### 5.2 事件流图

```text
传送点触发流程:
PlayerCharacter (进入Trigger)
    → MapTeleportPoint.OnTriggerEnter2D()
        → MapTeleportPoint.TeleportTriggered (event)
            → MapBehaviour.OnTeleportPointTriggered()
                → MapBehaviour.MapTeleported (event)
                    → Level.OnMapTeleported()
                        → 执行地图切换逻辑

地图切换流程:
Level.OnMapTeleported()
    → DisablePlayerControl()
    → UnsubscribeOldMap()
    → ExitOldMap()
    → LoadNewMap(mapId)
    → InitializeNewMap()
    → SubscribeNewMap()
    → TeleportPlayer(teleportPointIndex)
    → DestroyOldMap()
    → EnablePlayerControl()

Level Input系统流程:
Level.Update()
    → if (!isPlayerControlEnabled) return
    → HandleMovementInput()
        → 读取WASD输入
        → 转换为PlayerCharacter移动命令
    → HandleInteractionInput()
        → 读取交互按键（E键等）
        → 处理NPC对话、物品拾取等

MapBehaviour初始化流程:
MapBehaviour.Initialize()
    → 获取所有MapObject
    → 遍历MapObject:
        → mapObject.Initialize() (可能获取全局信息)
        → if (MapTeleportPoint) 订阅TeleportTriggered事件
        → if (其他特殊类型) 执行相应处理
    → 完成初始化

NPC交互流程 (由NPC自行实现):
PlayerCharacter (进入NPC Trigger)
    → NPCObject.OnTriggerEnter2D()
        → NPC自定义交互逻辑
        → NPC自定义交互逻辑
```

---

## 6. 实现约束与注意事项

### 6.1 硬性约束

1. **初始化顺序**:
   - Level.Awake() → MapBehaviour.Initialize() → MapObject.Initialize() → PlayerCharacter.Initialize()

2. **事件订阅顺序**:
   - 订阅：Map.MapTeleported
   - 取消订阅：按相反顺序

3. **地图切换流程**:
   - 必须先禁用玩家控制
   - 必须先取消订阅再销毁旧地图
   - 必须先初始化新地图再传送玩家

4. **物理配置**:
   - PlayerCharacter: Rigidbody2D (Dynamic), Tag = "Player"
   - CollisionObject: Rigidbody2D (Static), 非Trigger Collider2D
     - 多个Collider应放置在"Colliders"子物体中
   - MapTeleportPoint: 无Rigidbody2D, IsTrigger Collider2D

5. **Y轴到Z轴映射**:
   - 必须在LevelObject.Update()中执行
   - 顺序：读取rb2D.position.y → 映射到transform.position.z

6. **MapBehaviour初始化流程**:
   - 必须遍历所有MapObject并调用Initialize()
   - 必须对MapObject进行类型检测并执行特殊处理
   - MapTeleportPoint必须订阅TeleportTriggered事件

### 6.2 软性约束

1. **性能优化**:
   - 缓存组件引用（Transform, Rigidbody2D）
   - 避免在Update()中使用GetComponent()
   - MapObject数组应在Initialize时一次性获取

2. **扩展性**:
   - 所有类使用virtual方法以支持子类扩展
   - 事件使用Action<>以简化订阅
   - 预留MapContainer接口以便后续实现

3. **可维护性**:
   - 使用partial class分离Level逻辑
   - 明确注释"TODO"和"暂不实现"部分
   - 使用命名空间或Assembly Definition隔离代码

### 6.3 注意事项

1. **DestroyOldMap方法**:
   - 当前仅定义方法框架
   - 不实现内部逻辑
   - 添加TODO注释说明后续实现计划

2. **MapContainer**:
   - 当前使用mapPrefabs数组和mapPrefabDict字典临时替代
   - 在代码中添加注释说明这是临时方案
   - 后续实现时替换LoadMap()方法

3. **Level Input系统**:
   - 当前硬编码按键（WASD, E等）
   - 后续应使用Unity InputSystem或配置化输入

4. **传送点触发机制**:
   - 玩家进入触发区时自动触发MapTeleportPoint.TeleportTriggered事件
   - MapBehaviour订阅该事件并转发给Level
   - Level不应在Update中检查，而是直接订阅MapBehaviour.MapTeleported事件

5. **MapObject全局信息获取**:
   - 部分MapObject在Initialize()时需要获取全局信息（如存档Flag）
   - 例如：某些物体根据玩家进度决定是否出现
   - 当前暂不实现，预留接口

6. **错误处理**:
   - 组件缺失时应抛出异常或记录Error日志
   - 地图ID不存在时应抛出Exception
   - 避免使用null propagation忽略潜在错误

---

## 7. 测试检查清单

### 7.1 Level初始化测试

- [ ] Level能正确获取MapBehaviour和PlayerCharacter
- [ ] MapBehaviour.Initialize()被正确调用
- [ ] 所有MapObject的Initialize()被调用
- [ ] 事件订阅成功

### 7.2 地图切换测试

- [ ] 地图切换时玩家控制被禁用
- [ ] 旧地图事件被取消订阅
- [ ] 新地图被正确实例化和初始化
- [ ] 玩家被传送到正确位置
- [ ] 旧地图的DestroyOldMap被调用（即使暂不实现）
- [ ] 地图切换后玩家控制恢复

### 7.3 物理系统测试

- [ ] 玩家在X-Z平面正确移动
- [ ] Y轴数据正确映射到Z轴
- [ ] 玩家与墙壁碰撞正常
- [ ] SetPosition和SetVelocity方法工作正常

### 7.4 传送点交互测试

- [ ] 玩家进入传送点触发区时正确检测
- [ ] Level正确识别当前可交互传送点
- [ ] 按E键时正确触发传送逻辑
- [ ] 传送点触发MapTeleported事件
- [ ] 玩家离开触发区时清除交互状态

### 7.5 对象分类测试

- [ ] VisualObject正常渲染无碰撞
- [ ] CollisionObject正确阻挡玩家
- [ ] MapTeleportPoint仅有触发器无碰撞
- [ ] 各类对象的Initialize()方法正常工作

---

## 8. 后续扩展计划

### 8.1 短期扩展（当前版本后）

1. 实现DestroyOldMap()方法
   - 清理MapObjects引用
   - 销毁GameObject
   - 释放资源和事件订阅

2. 实现MapContainer类
   - 地图Prefab管理和缓存
   - 异步加载支持
   - 地图池管理

3. 配置化输入系统
   - 使用Unity InputSystem
   - 支持多设备输入
   - 输入重映射功能

### 8.2 中期扩展

1. 地图过渡效果
   - 淡入淡出
   - 加载界面
   - 过渡动画

2. NPC对话系统
   - 对话树结构
   - UI集成
   - 本地化支持

3. 可交互对象扩展
   - 可拾取物品
   - 可破坏物体
   - 机关和门

### 8.3 长期扩展

1. 保存/加载系统
   - 地图状态持久化
   - 玩家进度保存
   - 关卡选择

2. 动态地图生成
   - 程序化生成
   - 随机地图元素
   - 动态障碍物

---

## 9. 文件清单

### 9.1 需要创建的新文件

1. `Assets/Scripts/Level/LevelObject/VisualObject.cs`
2. `Assets/Scripts/Level/LevelObject/CollisionObject.cs`
3. `Assets/Scripts/Level/Map/MapObjects/MapTeleportPoint.cs`
4. `Assets/Scripts/Level/MapContainer.cs` (预留，暂不实现)

### 9.2 需要修改的现有文件

1. `Assets/Scripts/Level/Level.cs`
2. `Assets/Scripts/Level/Level.Monobehaviour.cs`
3. `Assets/Scripts/Level/LevelObject/LevelObject.cs`
4. `Assets/Scripts/Level/Map/MapBehaviour.cs`
5. `Assets/Scripts/Level/PlayerCharacter/PlayerCharacter.cs`
6. `Assets/Scripts/Level/Map/MapObjects/MapObject.cs`
7. `Assets/Scripts/Level/Map/MapTeleportContext.cs` (修改为包含地图ID和进入点索引)

---

## 10. 实现优先级

### P0 - 核心功能（必须实现）

1. LevelObject基类扩展（SetPosition, SetVelocity, Update映射）
2. Level初始化流程（Awake, InitializeMap）
3. 地图切换核心逻辑（OnMapTeleported, LoadMap, SwitchToMap）
4. MapTeleportContext类修改（包含地图ID和进入点索引）
5. MapTeleportPoint类实现（自动触发传送事件）
6. MapBehaviour的Initialize逻辑（类型检测和事件订阅）
7. 基础对象类型定义（VisualObject, CollisionObject）

### P1 - 重要功能（应实现）

1. Level的Input系统（WASD移动，交互按键）
2. PlayerCharacter扩展（EnableControl, TeleportTo, Move）
3. MapBehaviour的传送点管理（GetTeleportPoint）
4. CollisionObject的Colliders子物体支持

### P2 - 辅助功能（可延后）

1. DestroyOldMap方法框架（仅定义）
2. MapObject全局信息获取机制（暂不实现）
3. 错误处理和日志
4. Inspector属性配置
5. 调试辅助工具

---

## 11. 代码示例总结

### 完整的关键方法实现模板

参见第4节"详细实现描述"中的代码示例。

---

## 审阅检查点

在审阅本文档时，请重点关注以下问题：

1. **架构设计**:
   - 类继承关系是否合理？
   - 事件流设计是否清晰？
   - 模块职责是否明确？

2. **需求覆盖**:
   - 是否覆盖了所有5个核心需求？
   - 实现方案是否满足需求描述？
   - 是否有遗漏的功能点？

3. **技术方案**:
   - Y轴到Z轴映射方案是否可行？
   - 2D物理配置是否合理？
   - 传送点触发器处理是否符合预期？

4. **约束条件**:
   - 硬性约束是否过于严格？
   - 是否有遗漏的重要约束？
   - 临时方案（mapPrefabs）是否可接受？

5. **可实现性**:
   - 文档描述是否足够详细？
   - 另一个Agent能否基于此文档实现？
   - 是否需要补充更多细节？

---

## 变更记录

| 版本 | 日期 | 变更内容 | 审阅状态 |
| ---- | ---- | -------- | -------- |
| v1.0 | 2026-02-15 | 初始版本创建 | 已审阅 |
| v1.1 | 2026-02-15 | 1. MapTeleportContext改为包含地图ID(String)和进入点索引(int)<br>2. 添加Level的Input系统详细说明<br>3. 添加CollisionObject的Colliders子物体说明<br>4. 修改事件订阅逻辑：Level直接订阅MapBehaviour的MapTeleported事件，MapBehaviour在Initialize时订阅MapTeleportPoint事件<br>5. 添加Map初始化时全局信息获取说明（暂不实现） | 当前版本 |

---

## **文档结束**

请审阅以上内容，并反馈修改意见。审阅通过后，此文档将交由执行Agent实现。
