# AGENTS.md - 代理编码指南

本文档为在本项目中工作的 AI 代理提供编码规范和开发指南。

## 项目概述

基于 Unity 引擎的 C# 游戏项目，采用 MVVM 架构模式。

**核心模块**: Core（GameManager、UIManager、AudioManager、EventCenter）、MVVM（Aesthete.MVVM）、Launcher、Level、UI

## 构建、测试和检查命令

### Unity 编辑器

```bash
# 打开项目：Unity Hub 打开 Hippo-s-Epic.sln
# 构建：File > Build Settings > Build
# 测试：Window > General > Test Runner
```

### 单个测试执行

测试位于 `Assets/Scripts/Aesthete.MVVM/Test/Editor/`，使用 NUnit 框架：

```bash
# Unity Test Runner 中运行：
# 1. Window > General > Test Runner
# 2. 选择测试文件并点击 Run
```

### 代码检查

```bash
# ReSharper/Rider: Analyze > Inspect Code
# Unity: Edit > Preferences > Analysis
```

## 代码风格指南

### 命名约定

| 类型 | 风格 | 示例 |
|------|------|------|
| 类/接口/方法/属性 | PascalCase | `GameManager`, `IViewModel` |
| 私有字段 | camelCase | `isInitialized` |
| 序列化字段 | camelCase + `[SerializeField]` | `[SerializeField] private KeyCode resetKey;` |
| 常量 | SCREAMING_SNAKE_CASE | `MAX_LEVELS` |
| 布尔变量 | is/has/should/can 前缀 | `isEnabled`, `hasPermission` |

### 导入和命名空间

- 使用完整命名空间路径
- Unity 相关 using 放在顶部
- 按字母顺序排列（可选）

```csharp
using System;
using System.Collections.Generic;
using UnityEngine;

namespace GGJ2026
{
    // 类定义
}
```

### 代码格式化

- 4 空格缩进（Unity 默认）
- K&R 大括号风格
- 属性/方法间留空行
- 行长度软限制：120 字符

### 注释和文档

- 公共 API 使用 XML 文档注释（`/// <summary>`）
- 使用中文注释
- 解释"为什么"而非"做什么"

### 错误处理

- 空引用验证：抛出 `ArgumentNullException`
- 重复初始化：抛出 `Exception`
- 使用中文错误消息

```csharp
public static void Initialize(Transform parent)
{
    if (parent == null)
        throw new ArgumentNullException(nameof(parent), "parent 不能为空");
    if (IsInitialized)
        throw new Exception("已初始化，不允许重复调用");
}
```

## Unity 特定规范

```csharp
// MonoBehaviour 序列化字段
[SerializeField] private KeyCode resetKey = KeyCode.R;

// ScriptableObject 菜单创建
[CreateAssetMenu(fileName = "LaunchConfig", menuName = "Launcher/LaunchConfig")]
public class LaunchConfig : ScriptableObject { }

// 单例模式
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }
}
```

## MVVM 架构规范

- **View**: UI 渲染和用户交互
- **ViewModel**: 业务逻辑和数据绑定
- 使用 `Bind()` / `Unbind()` 管理生命周期
- 使用 `Dispose()` 释放资源

```csharp
public abstract class View : MonoBehaviour, IView
{
    protected IViewModel ViewModel;
    public virtual void Bind() => ViewModel?.Subscribe(OnViewModelChanged);
    public virtual void Unbind() => ViewModel?.Unsubscribe(OnViewModelChanged);
    public virtual void Dispose() { Unbind(); ViewModel = null; }
}
```

## 文件组织

```
Assets/Scripts/
├── Core/           # 核心系统
├── MVVM/           # MVVM 架构
├── Launcher/       # 启动系统
├── Level/          # 关卡系统
├── UI/             # 用户界面
└── Aesthete.MVVM/  # MVVM 框架库
    ├── Runtime/    # 运行时代码
    └── Test/Editor/# 编辑器测试
```

## Git 提交规范

使用简体中文，格式：`type: 描述`

```bash
git commit -m "feat: 添加新的关卡加载系统"
git commit -m "fix: 修复 GameManager 状态切换问题"
git commit -m "refactor: 重构 MVVM 绑定逻辑"
```

## 最佳实践

1. 单个类 ≤ 500 行，单个方法 ≤ 50 行
2. 避免魔法字符串/数字，使用常量或配置类
3. 优先组合而非继承
4. 及时释放资源，使用 `using` 或 `Dispose()`
5. 外部输入必须空值验证
6. 避免在 `Update` 中内存分配

## Copilot 规则

- 代码审查回复使用简体中文
- 理解任务意图后再执行，避免不必要的修改
- 执行前确认文件路径和目标

## Agent 自动化框架

项目集成 OpenCode Agent 框架，用于复杂任务拆分执行。

### 快速使用

```
# 规划任务
使用 task-planner skill 分析任务：重构用户认证模块

# 执行子任务
使用 task-executor skill 执行当前子任务

# 汇总结果
使用 result-aggregator skill 汇总所有子任务结果
```

### 状态文件

- `.agent/state.json` - 当前执行状态
- `.agent/plan.md` - 执行计划
- `.agent/report.md` - 最终报告

## 相关文档

- [LaunchTask 使用手册](./Docs/Scripting/LaunchTask.md)
- [LaunchConfig 使用手册](./Docs/Scripting/LaunchConfig.md)
- [UIManager 说明](./Docs/Scripting/UIManager.md)
