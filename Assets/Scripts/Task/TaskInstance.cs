using System;
using System.Collections.Generic;

namespace QuestSystem
{
    [Serializable]
    public class TaskConditionConfig
    {
        public ConditionType Type;
        public int P1;
        public int P2;
        public int P3;
    }

    [Serializable]
    public class TaskDefinition
    {
        public int TaskId;
        public string Title;
        public string Description;
        public List<TaskConditionConfig> Conditions = new();
    }

    /// <summary>
    /// 运行时条件状态（用于存档与显示进度）
    /// </summary>
    [Serializable]
    public class ConditionRuntimeState
    {
        public int Current;
        public int Target;
        public bool Completed => Current >= Target;
    }

    public class TaskInstance
    {
        public TaskDefinition Def { get; }
        public bool IsCompleted { get; private set; }

        // 每个条件一个状态
        public readonly List<ConditionRuntimeState> ConditionStates = new();

        // 条件对象（订阅事件）
        private readonly List<ITaskCondition> _conditions = new();

        public event Action<TaskInstance> OnUpdated;
        public event Action<TaskInstance> OnCompleted;

        public TaskInstance(TaskDefinition def, ConditionFactory factory)
        {
            Def = def;

            for (int i = 0; i < def.Conditions.Count; i++)
            {
                var cfg = def.Conditions[i];
                var condition = factory.Create(cfg, i, this);
                _conditions.Add(condition);
                ConditionStates.Add(condition.State);
            }
        }

        public void Activate()
        {
            foreach (var c in _conditions) c.Activate();
            RecalculateCompleted();
        }

        public void Deactivate()
        {
            foreach (var c in _conditions) c.Deactivate();
        }

        internal void NotifyUpdated()
        {
            RecalculateCompleted();
            OnUpdated?.Invoke(this);
        }

        private void RecalculateCompleted()
        {
            if (IsCompleted) return;

            for (int i = 0; i < ConditionStates.Count; i++)
            {
                if (!ConditionStates[i].Completed)
                    return;
            }

            IsCompleted = true;
            foreach (var c in _conditions) c.Deactivate();
            OnCompleted?.Invoke(this);
        }
    }
}