# Launcher TDD

## Objectives

作为游戏唯一启动入口，通过场景中的 `Launcher` 组件在 `Awake()` 时读取挂载的配置文件并以此为参数启动游戏

## Background

游戏中的模块需要一个单一入口控制启动时机和顺序

## Non-Goals

## Future Goals

- 实现异步启动，保证在启动期间的加载画面流畅性
- 实现基于启动项的启动情况，判断在遇到异常时的操作

## Assumptions

## Solution

- Launcher
  - 继承 `MonoBehaviour` 类，用于挂载启动所用的配置文件
- LaunchConfig
  - 继承 `ScriptableObject` 类
  - 提供对应启动阶段的启动项数组
- LaunchTask
  - 启动项抽象类
  - 继承 `ScriptableObject` 类
  - 记录每个启动项的参数
  - 通过实现 `LaunchTask.Execute()` 执行该启动项的具体启动逻辑

## Further Considerations

## Success Evaluation

- `Launcher` 组件中能自行选择 `LaunchConfig`
- `LaunchConfig` 文件中能配置 `LaunchTask`
- 其他模块可以继承 `LaunchTask` 类，并创建对应的文件
- `Launcher` 在启动时在正确的时机执行对应的启动方法
- `Launcher` 在执行启动方法时严格按照 `LaunchConfig` 中配置的启动顺序启动

## Deliberation

## End Matter
