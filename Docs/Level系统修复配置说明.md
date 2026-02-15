# Level系统修复说明

## 修复内容

### 问题原因

控制台错误显示：`Exception: Map with ID '' not found in map prefab dictionary`

这是因为传送点的 `Target Map Id` 为空字符串，导致无法找到目标地图。

### 解决方案

修改了传送系统逻辑，将进入点位置从MapTeleportPoint GameObject分离到MapBehaviour配置中。

---

## 代码修改

### 1. MapBehaviour.cs

**新增**：

- `teleportEntryPoints` 数组（Vector2[]）- 存储玩家传送到本地图时的进入位置
- `GetTeleportEntryPoint(int index, out Vector2 position)` 方法 - 获取进入点位置

### 2. Level.cs

**修改**：

- `OnMapTeleported()` 方法现在使用 `MapBehaviour.GetTeleportEntryPoint()` 而不是 `MapTeleportPoint.transform.position`

---

## Unity Editor配置步骤

### 第一步：配置Map_Town地图

1. 在Project窗口找到 `Map_Town` Prefab
2. 打开Prefab编辑模式（双击Prefab）
3. 选中根对象（Map_Town）
4. 在Inspector中找到 **MapBehaviour** 组件
5. 展开 **Teleport Entry Points** 数组
6. 设置 **Size = 1**
7. 设置 **Element 0 = (0, -4)**
   - 这是玩家从其他地图传送到Map_Town时的出现位置

### 第二步：配置Map_Town的传送点

1. 在Map_Town中找到传送点GameObject（如 `TeleportPoint_ToForest`）
2. 选中该传送点
3. 在Inspector中检查 **TestTeleportPoint** 或 **MapTeleportPoint** 组件
4. **重要配置**：
   - **Target Map Id**: 输入 `Map_Forest` （必须与目标地图Prefab名称完全一致）
   - **Target Teleport Index**: 设为 `0` （对应目标地图的teleportEntryPoints[0]）
   - **Trigger Size**: (2, 2) 或根据需要调整
5. 检查 **BoxCollider2D** 组件：
   - **Is Trigger**: 必须勾选 ✓

### 第三步：配置Map_Forest地图（如果有）

1. 在Project窗口找到 `Map_Forest` Prefab
2. 打开Prefab编辑模式
3. 选中根对象（Map_Forest）
4. 在Inspector中找到 **MapBehaviour** 组件
5. 展开 **Teleport Entry Points** 数组
6. 设置 **Size = 1**
7. 设置 **Element 0 = (0, 4)**
   - 这是玩家从Map_Town传送到Map_Forest时的出现位置

### 第四步：配置Map_Forest的传送点

1. 在Map_Forest中找到传送点GameObject（如 `TeleportPoint_ToTown`）
2. 选中该传送点
3. 配置组件：
   - **Target Map Id**: 输入 `Map_Town` （必须与Prefab名称完全一致）
   - **Target Teleport Index**: 设为 `0`
   - 检查 **Is Trigger** 必须勾选

### 第五步：配置Level的Map Prefabs

1. 在Hierarchy中选中 **Level** GameObject
2. 在Inspector中找到 **Level** 组件
3. 展开 **Map Prefabs** 数组
4. 设置 **Size = 2** （如果有两个地图）
5. 配置元素：
   - **Element 0**: 拖入 `Map_Town` Prefab
   - **Element 1**: 拖入 `Map_Forest` Prefab

### 第六步：配置PlayerCharacter

1. 在Hierarchy中选中 **PlayerCharacter**
2. 在Inspector的顶部，将 **Tag** 设为 `Player`（如果没有Player标签，需要先创建）
3. 检查组件：
   - **Rigidbody2D**: Body Type = Dynamic, Gravity Scale = 0
   - **CapsuleCollider2D**: 存在即可

---

## 配置检查清单

完成配置后，请检查以下项目：

### Map_Town Prefab

- [ ] MapBehaviour组件存在
- [ ] Teleport Entry Points数组: Size ≥ 1
- [ ] Teleport Entry Points[0] 已配置向量值（如(0, -4)）
- [ ] 传送点GameObject存在
- [ ] 传送点的Target Map Id = "Map_Forest"（不为空）
- [ ] 传送点的Target Teleport Index = 0
- [ ] 传送点的BoxCollider2D的IsTrigger = true

### Map_Forest Prefab（如果有）

- [ ] MapBehaviour组件存在
- [ ] Teleport Entry Points数组: Size ≥ 1
- [ ] Teleport Entry Points[0] 已配置向量值（如(0, 4)）
- [ ] 传送点的Target Map Id = "Map_Town"（不为空）
- [ ] 传送点的Target Teleport Index = 0
- [ ] 传送点的BoxCollider2D的IsTrigger = true

### Level GameObject

- [ ] Level组件的Map Prefabs数组包含所有地图Prefab
- [ ] PlayerCharacter的Tag = "Player"
- [ ] PlayerCharacter是Level的直接子对象（不是Map的子对象）

---

## 测试流程

1. 按Play进入游戏
2. 检查控制台是否有错误：
   - ✓ "Level System Test Started" - 正常
   - ✗ "Map with ID '' not found" - Target Map Id未配置
   - ✗ "Teleport entry point index X is out of range" - 目标地图的teleportEntryPoints数组未配置或索引越界
3. 使用WASD移动玩家到传送点触发区
4. 观察是否正确切换地图并传送到配置的进入点位置

---

## 常见错误与解决

### 错误1: "Map with ID '' not found in map prefab dictionary"

**原因**: 传送点的Target Map Id为空或未配置
**解决**:

1. 打开地图Prefab
2. 选中传送点GameObject
3. 在Inspector中输入正确的Target Map Id（必须与目标Prefab名称完全一致）

### 错误2: "Teleport entry point index 0 is out of range"

**原因**: 目标地图的Teleport Entry Points数组未配置或为空
**解决**:

1. 打开目标地图Prefab
2. 选中根对象
3. 在MapBehaviour组件中配置Teleport Entry Points数组
4. 至少添加一个进入点位置

### 错误3: "Collider2D is not a trigger. Setting it now."

**原因**: 传送点的BoxCollider2D的IsTrigger未勾选
**解决**:

1. 选中传送点GameObject
2. 在BoxCollider2D组件中勾选 **Is Trigger**
3. 或让代码自动设置（会显示一次警告）

### 错误4: 玩家碰到传送点没有反应

**原因**: PlayerCharacter的Tag不是"Player"
**解决**:

1. 选中PlayerCharacter GameObject
2. 在Inspector顶部将Tag设为"Player"

---

## 工作原理图示

```text
玩家移动到传送点触发区
  ↓
MapTeleportPoint.OnTriggerEnter2D() 检测到Player Tag
  ↓
触发 TeleportTriggered 事件，传递 MapTeleportContext
  {
    TargetMapId: "Map_Forest",          ← 从Target Map Id字段读取
    TargetTeleportPointIndex: 0          ← 从Target Teleport Index字段读取
  }
  ↓
MapBehaviour 转发 MapTeleported 事件给 Level
  ↓
Level.OnMapTeleported() 执行地图切换：
  1. 禁用玩家控制
  2. 退出当前地图
  3. 加载目标地图（通过mapPrefabDict["Map_Forest"]）
  4. 初始化新地图
  5. 获取进入点位置：
     newMap.GetTeleportEntryPoint(0, out Vector2 pos)  ← 从新地图的teleportEntryPoints[0]读取
  6. 传送玩家到进入点位置：
     playerCharacter.TeleportTo(pos, 0f)
  7. 恢复玩家控制
```

---

## 快速修复命令行（如果你想快速验证）

如果你想快速测试，可以暂时跳过Forest地图，只测试Town地图内部循环传送：

### Map_Town配置

- Teleport Entry Points[0] = (0, -4)
- TeleportPoint配置：
  - Target Map Id: `Map_Town`（传送到自己）
  - Target Teleport Index: 0
  - 触发区位置: (8, 3, 0)

这样玩家碰到传送点会被传送到(0, -4)位置，可以快速验证系统工作正常。

---

保存所有Prefab后按Play测试！
