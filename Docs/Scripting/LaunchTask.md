# LaunchTask 使用手册

## 概述

`LaunchTask` 是一个抽象的 ScriptableObject 基类，用于定义游戏启动时需要执行的任务。通过继承此类并实现 `Execute()` 方法，可以创建自定义的启动任务。

## 类定义

```csharp
public abstract class LaunchTask : ScriptableObject
{
    public abstract void Execute();
}
```

**脚本路径：** [Assets/Scripts/Launcher/LaunchTask/LaunchTask.cs](../../Assets/Scripts/Launcher/LaunchTask/LaunchTask.cs)

## 核心方法

### Execute()

```csharp
public abstract void Execute();
```

抽象方法，需要在派生类中实现。此方法会在游戏启动时被 `Launcher` 组件按顺序调用。

## 使用方法

### 1. 创建自定义启动任务

要创建一个具体的启动任务，需要继承 `LaunchTask` 类并实现 `Execute()` 方法：

```csharp
using UnityEngine;

[CreateAssetMenu(fileName = "NewTask", menuName = "Launcher/Tasks/NewTask")]
public class CustomLaunchTask : LaunchTask
{
    public string taskName;
    
    public override void Execute()
    {
        Debug.Log($"执行任务: {taskName}");
        // 在这里实现具体的启动逻辑
    }
}
```

### 2. 创建任务资产

1. 在 Unity 编辑器中，右键点击 Project 窗口
2. 选择 **Create > Launcher > Tasks > [你的任务类型]**
3. 配置任务的属性
4. 将创建的任务资产添加到 `LaunchConfig` 中

### 3. 任务执行顺序

任务的执行顺序由它们在 `LaunchConfig.LaunchTasks` 数组中的顺序决定。`Launcher` 会按照数组顺序依次执行每个任务的 `Execute()` 方法。

## 设计原则

- **单一职责**：每个 LaunchTask 应该只负责一个特定的启动任务
- **无依赖**：任务之间应尽量避免相互依赖，通过执行顺序来控制流程
- **可配置**：通过 ScriptableObject 的序列化特性，可以在编辑器中配置任务参数

## 常见应用场景

- 加载全局配置
- 初始化游戏管理器
- 播放启动动画
- 加载默认场景
- 初始化第三方SDK
- 设置全局事件监听器

## 相关文档

- [LaunchConfig 使用手册](./LaunchConfig.md)
- [Launcher 组件说明](./Launcher.md)

## 注意事项

1. `Execute()` 方法应该是同步执行的，如果需要异步操作，请在方法内自行处理
2. 避免在 `Execute()` 中执行耗时操作，以免阻塞启动流程
3. 测试时注意任务执行顺序对游戏状态的影响
