# Agent Orchestrator Framework

基于 OpenCode 的 Agent 自动化运行框架，支持任务拆分、上下文管理和顺序执行。

## 架构概览

```
┌────────────────────────────────────────────────────────────┐
│                    Agent Orchestrator                       │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐        │
│  │   Planner   │  │  Executor   │  │  Aggregator │        │
│  │ (任务规划)   │→│ (执行调度)   │→│ (结果汇总)   │        │
│  └─────────────┘  └─────────────┘  └─────────────┘        │
│                          ↓                                  │
│  ┌─────────────────────────────────────────────────────┐   │
│  │              OpenCode Integration Layer              │   │
│  │  Skills │ MCP Tools │ Agents │ Commands │ Context   │   │
│  └─────────────────────────────────────────────────────┘   │
└────────────────────────────────────────────────────────────┘
```

## 核心组件

### 1. Skills（技能）

| Skill | 用途 |
|-------|------|
| `task-planner` | 分析复杂任务并拆分为子任务 |
| `task-executor` | 执行单个子任务 |
| `context-manager` | 管理上下文加载和压缩 |
| `result-aggregator` | 汇总所有子任务结果 |

### 2. 状态管理（.agent/）

```
.agent/
├── state.json          # 当前任务状态
├── plan.md             # 执行计划
├── report.md           # 最终报告（执行完成后）
├── subtasks/           # 子任务结果
│   ├── 001_xxx.md
│   └── ...
├── context/            # 压缩的上下文
│   └── summary.md
└── memory/             # 长期记忆
    └── knowledge.json
```

### 3. 工作流程

```
1. 用户提交大任务
       ↓
2. task-planner 分析并拆分
       ↓
3. 保存计划到 .agent/plan.md
       ↓
4. task-executor 顺序执行子任务
   - 加载最小必要上下文
   - 执行并保存结果
   - 生成摘要供后续使用
       ↓
5. result-aggregator 汇总结果
       ↓
6. 生成最终报告
```

## 使用方法

### 1. 启动任务规划

```
使用 task-planner skill 分析以下任务：
[你的复杂任务描述]
```

### 2. 执行任务

```
使用 task-executor skill 执行子任务 N
```

### 3. 汇总结果

```
使用 result-aggregator skill 汇总所有结果
```

## 上下文管理策略

### Token 预算分配

```
总预算: ~100K tokens

- 系统提示: ~5K
- Skills 加载: ~3K
- 子任务描述: ~2K
- 前置摘要: ~5K
- 代码文件: ~50K (按需)
- 工作空间: ~35K
```

### 压缩原则

1. 每个子任务完成后生成摘要（< 500字）
2. 后续任务只加载必要的前置摘要
3. 文件按需读取，不预先全部加载
4. 长期知识提取到 memory/knowledge.json

## 状态文件格式

### state.json

```json
{
  "taskId": "uuid",
  "taskName": "任务名称",
  "phase": "idle|planned|executing|completed|blocked",
  "totalSubtasks": 5,
  "currentSubtask": 2,
  "subtasks": [
    {
      "id": 1,
      "name": "子任务名称",
      "status": "completed|in_progress|pending|blocked",
      "summaryFile": ".agent/subtasks/001_name.md"
    }
  ]
}
```

## 与 OpenCode 集成

- **Skills**: 安装在全局目录 `~/.config/opencode/skills/`，所有项目共享
- **MCP**: 可集成外部工具（记忆存储、代码执行等）
- **Commands**: 可创建自定义命令（/plan, /execute, /resume）
- **Agents**: 可配置不同角色的执行者

## 新项目初始化

```bash
# 方法1: 运行初始化脚本
./.agent/init.sh

# 方法2: 手动创建
mkdir -p .agent/{subtasks,context,memory}
# 然后复制 state.json 和 knowledge.json 模板
```

## 最佳实践

1. 任务拆分粒度适中（每个子任务 < 20K tokens）
2. 子任务之间尽量独立
3. 及时保存状态，支持断点续传
4. 每个子任务有明确的输入输出
5. 使用结构化格式提高压缩效率
