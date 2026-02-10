# Launcher TDD

## Objectives

作为游戏唯一启动入口，将挂在的配置文件作为参数，调用带有 `RuntimeInitializeOnLoadMethod` 属性的方法，实现游戏的启动

## Background

游戏中的模块需要一个单一入口控制启动时机和顺序

## Non-Goals

## Future Goals

- 实现异步启动，保证在启动期间的加载画面流畅性
- 实现基于启动项的启动情况，判断在遇到异常时的操作

## Assumptions

## Solution

- Launcher
  - 继承 `Monobehaviour` 类，用于挂载启动所用的配置文件
- LaunchConfig
  - 继承 `ScriptableObject` 类
  - 提供对应启动阶段的启动项数组
- LaunchAction
  - 启动项抽象类
  - 继承 `ScriptableObject` 类
  - 记录每个启动项的参数，实现启动逻辑

## Further Considerations

## Success Evaluation

- `Launcher` 组件中能自行选择 `LaunchConfig`
- `LaunchConfig` 文件中能配置 `LaunchAction`
- 其他模块可以继承 `LaunchAction` 类，并创建对应的文件
- `Launcher` 在启动时在正确的时机执行对应的启动方法
- `Launcher` 在执行启动方法时严格按照 `LaunchConfig` 中配置的启动顺序启动

## Deliberation

## End Matter
