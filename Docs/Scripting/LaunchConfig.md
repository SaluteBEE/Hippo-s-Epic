# LaunchConfig 使用手册

## 概述

`LaunchConfig` 是一个 ScriptableObject 配置文件，用于管理游戏启动时需要执行的所有 `LaunchTask`。通过将多个启动任务组织在一个配置文件中，可以方便地管理游戏的启动流程。

## 类定义

```csharp
[CreateAssetMenu(fileName = "LaunchConfig", menuName = "Launcher/LaunchConfig")]
public class LaunchConfig : ScriptableObject
{
    // TODO: 后续有空修改为私有字段，并提供只读属性访问
    public LaunchTask[] LaunchTasks;
}
```

**脚本路径：** [Assets/Scripts/Launcher/LaunchConfig/LaunchConfig.cs](../../Assets/Scripts/Launcher/LaunchConfig/LaunchConfig.cs)

## 属性说明

### LaunchTasks

```csharp
public LaunchTask[] LaunchTasks;
```

启动任务数组，包含所有需要在游戏启动时执行的任务。任务会按照数组中的顺序依次执行。

> **注意：** 此属性当前为 public，后续会改为私有字段并提供只读属性访问。

## 使用方法

### 1. 创建 LaunchConfig 资产

1. 在 Unity 编辑器的 Project 窗口中右键点击
2. 选择 **Create > Launcher > LaunchConfig**
3. 命名配置文件（例如：`GameLaunchConfig`）

### 2. 配置启动任务

1. 在 Inspector 窗口中选择创建的 LaunchConfig 资产
2. 设置 `Launch Tasks` 数组的大小
3. 将创建好的 LaunchTask 资产拖拽到数组元素中
4. 调整任务的执行顺序（数组顺序即执行顺序）

**示例配置：**

```text
Launch Tasks
├─ Element 0: InitializeManagers
├─ Element 1: LoadGlobalSettings
├─ Element 2: SetupAudioSystem
└─ Element 3: ShowMainMenu
```

### 3. 关联到 Launcher

1. 在场景中找到或创建包含 `Launcher` 组件的 GameObject
2. 将配置好的 LaunchConfig 资产拖拽到 `Launcher` 组件的 `Launch Config` 字段

### 4. 执行流程

当游戏启动（或场景加载）时：

1. `Launcher.Awake()` 方法被调用
2. 检查 `launchConfig` 是否已赋值
3. 如果已赋值，调用 `Launch(launchConfig)` 方法
4. 遍历 `LaunchTasks` 数组，依次执行每个任务的 `Execute()` 方法

```csharp
private void Launch(LaunchConfig config)
{
    foreach (var task in config.LaunchTasks)
    {
        task.Execute();
    }
}
```

## 最佳实践

### 任务组织原则

1. **按职责分类**：将相似功能的任务归类
   - 初始化类任务（Managers、Systems）
   - 加载类任务（Settings、Resources）
   - UI类任务（Splash Screen、Main Menu）

2. **最小化任务数量**：避免创建过多细粒度的任务，合理归并相关功能

3. **考虑执行顺序**：
   - 先执行基础系统初始化
   - 再执行依赖于基础系统的任务
   - 最后执行UI相关任务

### 配置管理

- **多环境配置**：可以为不同的场景或测试场景创建不同的 LaunchConfig
  - `GameLaunchConfig`：正式游戏启动配置
  - `TestLaunchConfig`：测试场景配置
  - `DemoLaunchConfig`：演示版本配置

- **版本控制**：LaunchConfig 是资产文件，确保添加到版本控制系统中

## 调试技巧

### 检查配置

在编辑器模式下，`Launcher` 会在未分配 LaunchConfig 时输出错误日志：

```csharp
#if UNITY_EDITOR
else
{
    Debug.LogError("LaunchConfig is not assigned in the Launcher.");
}
#endif
```

### 验证任务

在 LaunchConfig 的 Inspector 中，可以看到所有已配置的任务。确保：

- 没有空引用（None）元素
- 任务顺序符合逻辑
- 所有任务资产都已正确配置

### 运行时监控

可以在各个 LaunchTask 的 `Execute()` 方法中添加日志，监控启动流程：

```csharp
public override void Execute()
{
    Debug.Log($"[LaunchTask] 开始执行: {name}");
    // 任务逻辑
    Debug.Log($"[LaunchTask] 完成执行: {name}");
}
```

## 已知问题与改进计划

- **TODO**: 对 `launchConfig` 进行校验，确保其中的 `LaunchTask` 不为 null
- **TODO**: 将 `LaunchTasks` 修改为私有字段，并提供只读属性访问

## 相关文档

- [LaunchTask 使用手册](./LaunchTask.md)
- [Launcher 组件说明](./Launcher.md)

## API参考

### 使用示例

以下是一个完整的启动配置使用示例：

```csharp
// 1. 创建自定义任务
[CreateAssetMenu(fileName = "InitGameManager", menuName = "Launcher/Tasks/InitGameManager")]
public class InitGameManagerTask : LaunchTask
{
    public override void Execute()
    {
        GameManager.Instance.Initialize();
    }
}

// 2. 在编辑器中创建任务资产
// 3. 创建 LaunchConfig 并添加任务
// 4. 将 LaunchConfig 分配给场景中的 Launcher
```

当游戏运行时，Launcher 会自动按顺序执行所有配置的任务。
