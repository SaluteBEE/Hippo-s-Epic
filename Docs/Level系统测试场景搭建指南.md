# Level系统测试场景搭建指南

## 1. 创建基础测试场景

### 1.1 场景结构

在Unity中创建或使用现有的TestScene，场景结构如下：

```text
TestScene
├── Main Camera         (俯视角摄像机)
├── Level               (主Level控制器)
│   ├── PlayerCharacter (玩家)
│   └── Map_Town        (初始地图)
│       ├── Ground      (地面装饰)
│       ├── Walls       (墙壁碰撞)
│       └── TeleportPoint_ToForest (传送点)
└── Canvas              (UI，可选)
```

---

## 2. 创建Level GameObject

### 2.1 创建Level根对象

1. 在Hierarchy中右键 → Create Empty
2. 重命名为 `Level`
3. 添加组件 `Level` (Level.cs)
4. 添加组件 `LevelTest` (测试脚本)

### 2.2 Level组件配置

- **Current Map**: 留空（会自动获取子对象中的MapBehaviour）
- **Player Character**: 留空（会自动获取子对象）
- **Map Prefabs**: 创建2个元素
  - Element 0: Map_Town (Prefab)
  - Element 1: Map_Forest (Prefab)

---

## 3. 创建PlayerCharacter

### 3.1 创建玩家GameObject

1. 在Level下右键 → Create Empty
2. 重命名为 `PlayerCharacter`
3. 添加组件 `PlayerCharacter` (PlayerCharacter.cs)
4. 添加组件 `Rigidbody2D`
   - Body Type: Dynamic
   - Gravity Scale: 0 (俯视角不需要重力)
   - Linear Drag: 5 (移动阻力)
   - Constraints: Freeze Rotation Z ✓
5. 添加组件 `CapsuleCollider2D`
   - Size: (0.8, 1.2)
   - Offset: (0, 0)
6. 设置Tag为 `Player` (重要！传送点需要检测Player标签)

### 3.2 添加玩家视觉表现（可选）

1. 在PlayerCharacter下创建子对象 `Visual`
2. 添加 `Sprite Renderer` 组件
3. 选择一个临时Sprite (如Unity默认的Knob)
4. 调整颜色为青色以便识别

> **注意**: PlayerCharacter的Transform.position使用(X, Y, Z)坐标系，其中：
>
> - **X**: 左右位置
> - **Y**: 俯视角下的前后位置（地图纵向）
> - **Z**: 深度/层级（通常为0，用于视觉分层）

### 3.3 坐标轴手性处理（重要）

**问题描述**：由于Unity Scene坐标系与用户输入期望之间的手性（Handedness）差异，直接使用W/S键控制Y轴会导致移动方向与玩家预期相反：

- 用户按 **W键** 期望角色向"屏幕上方"移动（远离玩家视角）→ Unity中应为 **+Y**
- 用户按 **S键** 期望角色向"屏幕下方"移动（靠近玩家视角）→ Unity中应为 **-Y**

但在某些坐标系配置下，Unity的+Y方向可能与用户期望相反。

**解决方案**：在LevelObject基类中对Y轴做反向处理，统一协调输入和显示：

```csharp
// 在 LevelObject.cs 中修改

public void SetPosition(Vector2 position)
{
    // Y轴反向：用户空间 → Unity空间
    transform.position = new Vector3(position.x, -position.y, transform.position.z);
}

public void SetVelocity(Vector2 velocity)
{
    if (TryGetComponent<Rigidbody2D>(out var rb))
    {
        // Y轴反向：用户空间 → Unity空间
        rb.linearVelocity = new Vector2(velocity.x, -velocity.y);
    }
}
```

**影响范围**：

- PlayerCharacter的移动控制
- 所有地图对象的位置设置
- 传送点的Entry Points配置（需要按用户空间坐标配置）

> **建议**: 在所有使用SetPosition/SetVelocity的地方，都使用用户空间坐标（+Y=向上），由LevelObject统一转换为Unity空间坐标。

---

## 4. 创建地图Prefab - Map_Town

### 4.1 创建地图GameObject

1. 在Project窗口创建文件夹 `Assets/Prefabs/Maps`
2. 在Hierarchy中右键 → Create Empty
3. 重命名为 `Map_Town`
4. 添加组件 `MapBehaviour` (MapBehaviour.cs)
5. **配置MapBehaviour组件**:
   - **Teleport Entry Points**: 设置Size=1
     - Element 0: (0, -4, 0) - 这是玩家从其他地图传送到这里时的出现位置（X, Y, Z）

> **重要**: Teleport Entry Points数组存储的是玩家传送到本地图时的**进入位置**，与传送点GameObject的位置无关。传送点GameObject是交互区域（玩家需要按E键触发传送）。

### 4.2 添加地图内容

#### 4.2.1 添加地面装饰 (VisualObject)

1. 在Map_Town下创建 `Ground`
2. 添加组件 `VisualObject`
3. 添加子对象，添加Sprite Renderer显示地面
4. 颜色设为灰色

#### 4.2.2 添加墙壁 (CollisionObject)

1. 在Map_Town下创建 `Walls`
2. 创建子对象 `Colliders`
3. 在Colliders下创建4个墙壁：
   - `Wall_Top`: Position (0, 5, 0), BoxCollider2D Size (20, 1)
   - `Wall_Bottom`: Position (0, -5, 0), BoxCollider2D Size (20, 1)
   - `Wall_Left`: Position (-10, 0, 0), BoxCollider2D Size (1, 10)
   - `Wall_Right`: Position (10, 0, 0), BoxCollider2D Size (1, 10)
4. 给Walls添加 `CollisionObject` 组件
5. 给Walls添加 `Rigidbody2D` 组件
   - Body Type: Static

#### 4.2.3 添加传送点 (TestTeleportPoint)

1. 在Map_Town下创建 `TeleportPoint_ToForest`
2. Position设为 (8, 3, 0) - Z轴保持为0，Y轴用于地图纵向定位
3. 添加组件 `TestTeleportPoint`
4. 配置Inspector:
   - Target Map Id: `Map_Forest` (必须与Prefab名称一致)
   - Target Teleport Index: `0`
   - Trigger Size: (2, 2)
5. 组件会自动添加 `BoxCollider2D` (已设为IsTrigger)

> **交互机制**: 玩家进入传送点区域后，需要按 **E键** 才会触发传送。传送点会检测玩家是否在区域内，由Level系统监听玩家的交互输入并执行传送。

### 4.3 保存为Prefab

将Map_Town拖到 `Assets/Prefabs/Maps` 文件夹，创建Prefab

---

## 5. 创建第二个地图 - Map_Forest

### 5.1 复制并修改

1. 复制Map_Town Prefab，重命名为 `Map_Forest`
2. 打开Prefab编辑模式
3. 修改地面颜色为绿色
4. **配置MapBehaviour的Teleport Entry Points**:
   - Size: 1
   - Element 0: (0, 4, 0) - 从Town传送过来时的进入位置（X, Y, Z）
5. 修改传送点：
   - 重命名为 `TeleportPoint_ToTown`
   - Position: (-8, -3, 0) - 这是交互区位置（X, Y, Z），Z轴保持为0
   - Target Map Id: `Map_Town` (**重要：必须与Prefab名称完全一致**)
   - Target Teleport Index: `0` - 对应Map_Town的Teleport Entry Points[0]

> **说明**：传送点GameObject的位置是**交互区**位置（玩家在这里按E键触发传送），而Teleport Entry Points中配置的是**进入点**位置（传送到本地图时的出现位置），两者可以不同。所有位置使用(X, Y, Z)坐标，其中Z轴用于深度分层，Y轴用于俯视角的前后位置。

---

## 6. 场景最终配置

### 6.1 将Map_Town实例化到场景中

1. 将Map_Town Prefab拖到Hierarchy的Level下
2. 这将作为初始地图

### 6.2 配置Level的Map Prefabs

1. 选中Level GameObject
2. 在Level组件的Map Prefabs中：
   - Size: 2
   - Element 0: 拖入Map_Town Prefab
   - Element 1: 拖入Map_Forest Prefab

### 6.3 配置摄像机（俯视角）

1. 选中Main Camera
2. Transform:
   - Position: (0, 0, -10)
   - Rotation: (0, 0, 0) 或稍微倾斜 (30, 0, 0) 以模拟侧视
3. Camera组件:
   - Projection: Orthographic
   - Size: 8

### 6.4 配置Physics 2D设置

1. Edit → Project Settings → Physics 2D
2. 确保Gravity Y = 0 (俯视角不需要重力)
3. 配置Layer Collision Matrix:
   - Player 层与 Environment 层碰撞 ✓
   - Player 层与 Teleport 层触发 ✓

---

## 6.5 传送系统工作原理（重要）

### 关键概念：交互区 vs 进入点

- **传送点GameObject (MapTeleportPoint)**：定义**交互区**位置
  - 玩家进入这个区域后，区域会记录玩家在内
  - 玩家按 **E键** 触发传送（由Level监听输入并调用传送点的Teleport方法）
  - 配置Target Map Id（目标地图）和Target Teleport Index（目标索引）
  
- **Teleport Entry Points (MapBehaviour中配置)**：定义**进入点**位置
  - 玩家从其他地图传送到本地图时的出现位置（Vector3: X, Y, Z）
  - Y轴用于俯视角的前后位置，Z轴用于深度分层（通常为0）
  - 索引对应MapTeleportContext.TargetTeleportPointIndex

### 示例流程

```text
玩家在Map_Town → 进入TeleportPoint_ToForest交互区
  → 区域检测到玩家（OnTriggerEnter2D）
  → 玩家按E键
    → Level监听到E键输入
    → Level调用当前激活传送点的Teleport()
      → Target Map Id: "Map_Forest" 
      → Target Teleport Index: 0
        → 加载Map_Forest
        → 读取Map_Forest.teleportEntryPoints[0] = (0, 4, 0)
          → 玩家出现在(0, 4, 0)位置
```

### 坐标系统说明

- **X轴**: 地图横向（左-右）
- **Y轴**: 俯视角纵向（下-上 / 后-前）
- **Z轴**: 深度/层级（负值= 远离相机）
- 传送点位置和进入点位置都使用完整的(X, Y, Z)坐标
- 游戏对象通常Z=0，摄像机Z=-10

### 配置检查清单

- ✓ 每个地图的Teleport Entry Points数组必须配置（使用Vector3格式：X, Y, Z）
- ✓ Target Map Id必须与目标地图Prefab名称**完全一致**（区分大小写）
- ✓ Target Teleport Index必须小于目标地图的teleportEntryPoints.Length
- ✓ 传送点GameObject必须有BoxCollider2D且IsTrigger=true
- ✓ PlayerCharacter必须有Tag="Player"
- ✓ Level必须监听玩家的E键输入来触发传送
- ✓ 传送点需要实现玩家进入/离开检测（OnTriggerEnter2D/OnTriggerExit2D）

---

## 7. 测试流程

### 7.1 测试清单

1. **初始化测试**
   - [ ] 按Play，Level正确初始化
   - [ ] 控制台显示"Level System Test Started"
   - [ ] PlayerCharacter在场景中可见
   - [ ] Map_Town的所有对象初始化

2. **移动测试**
   - [ ] WASD键控制玩家移动
   - [ ] 玩家与墙壁正常碰撞
   - [ ] 玩家在俯视角下正常显示

3. **传送点交互测试**
   - [ ] 玩家移动到TeleportPoint_ToForest交互区
   - [ ] 屏幕显示交互提示（如"按E键传送"）
   - [ ] 按E键触发传送
   - [ ] 场景切换到Map_Forest
   - [ ] 玩家出现在Map_Forest配置的进入点位置（如(0, 4, 0)）
   - [ ] 玩家的Y轴位置正确（俯视角前后位置），Z轴保持为0
   - [ ] 旧地图Map_Town消失（或标记待销毁）

4. **往返传送测试**
   - [ ] 在Map_Forest中移动到TeleportPoint_ToTown
   - [ ] 按E键触发传送
   - [ ] 场景切换回Map_Town
   - [ ] 玩家出现在Map_Town配置的进入点位置（如(0, -4, 0)）
   - [ ] 验证玩家坐标的X、Y、Z值都正确

5. **测试快捷键**
   - [ ] 按T键切换调试信息显示/隐藏
   - [ ] 按P键在控制台输出玩家位置
   - [ ] 按M键在控制台输出当前地图信息
   - [ ] 按1/2/3键传送玩家到测试位置
   - [ ] 按0键切换玩家控制开/关

---

## 8. 常见问题与解决

### 问题1: 按E键后传送不触发

**检查项**:

- PlayerCharacter的Tag是否设为"Player"
- 玩家是否真的在传送点的交互区内（检查OnTriggerEnter2D是否被调用）
- Level是否正确监听了E键输入
- MapTeleportPoint的Collider2D的IsTrigger是否勾选
- Target Map Id是否与Prefab名称完全一致（区分大小写）
- **控制台是否显示"Map with ID '' not found"** - 说明Target Map Id未配置
- 是否有UI提示显示（确认传送点检测到玩家）

### 问题2: 地图切换后玩家出现在错误位置或消失

**检查项**:

- PlayerCharacter是Level的直接子对象（不是Map的子对象）
- **目标地图的Teleport Entry Points是否已配置为Vector3 (X, Y, Z)**
- Target Teleport Index是否小于目标地图的teleportEntryPoints.Length
- **控制台是否显示"Teleport entry point index X is out of range"**
- 检查Y轴（俯视角前后）和Z轴（深度）是否分离配置
- 玩家Z轴是否被错误设置（应保持为0或接近0）

### 问题3: 异常"Map with ID 'XXX' not found in map prefab dictionary"

**原因**: Level的Map Prefabs数组中没有添加对应的Prefab
**解决**:

- 检查Level组件的Map Prefabs数组
- 确保目标地图Prefab已添加到数组中
- 确保Target Map Id与Prefab的name完全一致

### 问题4: 墙壁碰撞不生效

**检查项**:

- CollisionObject的Colliders子物体是否正确创建
- BoxCollider2D的IsTrigger是否未勾选（墙壁应为碰撞体，非触发器）
- Rigidbody2D的Body Type是否为Static

### 问题5: 玩家无法移动

**检查项**:

- PlayerCharacter的Rigidbody2D存在且为Dynamic
- Level的输入系统正常（按P键检查IsControlEnabled）
- Rigidbody2D的Constraints是否冻结了错误的轴

### 问题6: 玩家移动方向相反（W/S键反向）

**原因**: Unity Scene坐标系与用户输入预期的手性不匹配

**症状**:

- 按W键时玩家向下移动（屏幕下方）
- 按S键时玩家向上移动（屏幕上方）
- 传送后玩家出现在预期位置的镜像位置

**解决方案**:

1. **在LevelObject中添加Y轴反向处理**（推荐）
   - 修改`LevelObject.SetPosition()`：使用`new Vector3(x, -y, z)`
   - 修改`LevelObject.SetVelocity()`：使用`new Vector2(vx, -vy)`
   - 这样所有继承LevelObject的对象都会自动处理Y轴反向

2. **调整输入映射**（不推荐）
   - 在PlayerCharacter的输入处理中反向W/S
   - 但这会导致配置文件中的坐标难以理解

3. **验证修改**:

   ```csharp
   // 测试代码
   Debug.Log($"Input: W → Velocity: {rb.linearVelocity}"); // 应显示 (0, -某正数)
   Debug.Log($"Position user space: {userPos} → Unity: {transform.position}");
   ```

**检查项**:

- LevelObject.SetPosition是否包含Y轴反向逻辑
- LevelObject.SetVelocity是否包含Y轴反向逻辑
- MapBehaviour的teleportEntryPoints配置是否使用用户空间坐标（+Y=向上）
- 测试按W键后rb.linearVelocity.y的符号（应为负数表示向Unity的-Y方向）

---

## 9. 调试技巧

### 9.1 使用Scene视图Gizmos

- TestTeleportPoint会在Scene视图显示粉色wireframe
- LevelTest会显示测试位置的黄色球体
- 选中传送点时会显示目标信息

### 9.2 使用控制台日志

```csharp
// 在Level.cs的关键位置添加日志
Debug.Log($"OnMapTeleported: {context.TargetMapId}");
Debug.Log($"Player position after teleport: {playerCharacter.transform.position}");
Debug.Log($"E key pressed, teleport point active: {currentTeleportPoint != null}");

// 调试Y轴反向问题
Debug.Log($"Input velocity (user space): {inputVelocity}");
Debug.Log($"Rigidbody velocity (Unity space): {rb.linearVelocity}");
Debug.Log($"Expected: Y signs should be opposite");
```

### 9.3 使用断点调试

- 在Level.OnMapTeleported()设置断点
- 在Level的Update()监听E键输入处设置断点
- 在MapTeleportPoint.OnTriggerEnter2D()和OnTriggerExit2D()设置断点
- 检查MapTeleportContext数据是否正确
- 检查传送后的玩家position.y（俯视角前后）和position.z（深度）值

---

## 10. 下一步扩展

### 10.1 添加NPC测试

1. 创建NPCObject子类
2. 实现OnTriggerEnter2D检测玩家
3. 测试E键交互

### 10.2 添加可破坏物体

1. 创建CollisionObject子类
2. 添加生命值系统
3. 测试碰撞伤害

### 10.3 优化地图切换

1. 实现DestroyOldMap()逻辑
2. 添加淡入淡出效果
3. 添加加载界面

---

## 附录：快速启动模板

### 最小场景配置（5分钟搭建）

```text
Level (Level组件: Map Prefabs = [Map_Test], 监听E键输入)
├── PlayerCharacter (Player tag, Rigidbody2D Dynamic, CapsuleCollider2D)
│   - Position.z = 0 (深度层级)
└── Map_Test (MapBehaviour: Teleport Entry Points[0] = (0, 0, 0))
    ├── Ground (VisualObject, Sprite)
    ├── Wall (CollisionObject, Rigidbody2D Static, BoxCollider2D)
    └── TestPoint (TestTeleportPoint)
        - Position: (x, y, 0) - Z轴保持为0
        - Target Map Id: "Map_Test"
        - Target Teleport Index: 0
        - BoxCollider2D (IsTrigger = true)
        - 玩家进入后按E键传送

Camera: Position (0, 0, -10), Orthographic Size 8
注意：Y轴用于俯视角前后位置，Z轴用于深度分层
```

### 双地图配置示例

```text
Map_Town (MapBehaviour)
├── Teleport Entry Points[0] = (0, -4, 0)  ← 从Forest来时的进入点 (X, Y, Z)
│                                            Y=-4表示靠近地图下方
└── TeleportPoint_ToForest
    - Position: (8, 3, 0)  ← 交互区位置 (X=8右侧, Y=3上方, Z=0)
    - Target Map Id: "Map_Forest"
    - Target Teleport Index: 0
    - 玩家在此区域内按E键传送

Map_Forest (MapBehaviour)
├── Teleport Entry Points[0] = (0, 4, 0)  ← 从Town来时的进入点 (X, Y, Z)
│                                           Y=4表示靠近地图上方
└── TeleportPoint_ToTown
    - Position: (-8, -3, 0)  ← 交互区位置 (X=-8左侧, Y=-3下方, Z=0)
    - Target Map Id: "Map_Town"
    - Target Teleport Index: 0
    - 玩家在此区域内按E键传送

交互流程：进入区域 → 显示提示 → 按E键 → 传送
```

保存后按Play即可开始测试！
