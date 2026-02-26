using System;
using System.Text;
using System.Collections.Generic;
using UnityEngine;

namespace QuestSystem
{
    public class TaskManager
    {
        public TaskDatabase Database { get; } = new();
        private readonly ConditionFactory _factory = new();

        private readonly Dictionary<int, TaskInstance> _active = new();
        private readonly HashSet<int> _completed = new();

        public event Action<TaskInstance> OnTaskAdded;
        public event Action<TaskInstance> OnTaskUpdated;
        public event Action<TaskInstance> OnTaskCompleted;

        public void InitializeFromCsv()
        {
            Database.LoadFromStreamingAssets("tasks.csv");
        }

        public bool IsActive(int taskId) => _active.ContainsKey(taskId);
        public bool IsCompleted(int taskId) => _completed.Contains(taskId);

        public TaskInstance GetActive(int taskId)
            => _active.TryGetValue(taskId, out var t) ? t : null;

        /// <summary>
        /// 对外接口：创建/激活任务
        /// </summary>
        public TaskInstance CreateTask(int taskId)
        {
            Debug.Log("TaskId: " + taskId);
            if (_completed.Contains(taskId)) return null;
            if (_active.ContainsKey(taskId)) return _active[taskId];

            if (!Database.Tasks.TryGetValue(taskId, out var def))
            {
                Debug.LogError($"TaskId not found in DB: {taskId}");
                return null;
            }

            var inst = new TaskInstance(def, _factory);
            inst.OnUpdated += HandleTaskUpdated;
            inst.OnCompleted += HandleTaskCompleted;

            _active.Add(taskId, inst);
            inst.Activate();

            // 新增：打印任务信息
            LogTaskInstance(inst, prefix: "Task Created");

            OnTaskAdded?.Invoke(inst);
            return inst;
        }
        private void LogTaskInstance(TaskInstance inst, string prefix = "Task")
        {
            if (inst == null || inst.Def == null)
            {
                Debug.LogWarning($"{prefix}: null");
                return;
            }

            var sb = new StringBuilder(256);

            sb.AppendLine($"[{prefix}] ID={inst.Def.TaskId}  Completed={inst.IsCompleted}");
            sb.AppendLine($"Title: {inst.Def.Title}");
            sb.AppendLine($"Desc : {inst.Def.Description}");
            sb.AppendLine($"Conditions: {inst.Def.Conditions.Count}");

            for (int i = 0; i < inst.Def.Conditions.Count; i++)
            {
                var cfg = inst.Def.Conditions[i];
                var st = (i < inst.ConditionStates.Count) ? inst.ConditionStates[i] : null;

                // cfg：配置数据；st：运行时进度
                if (st != null)
                {
                    sb.AppendLine(
                        $"- [{i}] Type={cfg.Type}  P1={cfg.P1} P2={cfg.P2} P3={cfg.P3}  " +
                        $"Progress={st.Current}/{st.Target}  Done={st.Completed}"
                    );
                }
                else
                {
                    sb.AppendLine(
                        $"- [{i}] Type={cfg.Type}  P1={cfg.P1} P2={cfg.P2} P3={cfg.P3}"
                    );
                }
            }

            Debug.Log(sb.ToString());
        }

        /// <summary>
        /// 对外接口：手动完成（少数剧情任务可能需要）
        /// </summary>
        public void ForceComplete(int taskId)
        {
            var inst = GetActive(taskId);
            if (inst == null) return;

            // 直接把所有条件置满
            foreach (var s in inst.ConditionStates)
                s.Current = s.Target;

            // 触发一次更新以结算
            inst.NotifyUpdated();
        }

        /// <summary>
        /// 对外接口：放弃任务
        /// </summary>
        public void Abandon(int taskId)
        {
            if (!_active.TryGetValue(taskId, out var inst)) return;

            inst.Deactivate();
            inst.OnUpdated -= HandleTaskUpdated;
            inst.OnCompleted -= HandleTaskCompleted;

            _active.Remove(taskId);
        }

        private void HandleTaskUpdated(TaskInstance inst)
        {
            OnTaskUpdated?.Invoke(inst);
        }

        private void HandleTaskCompleted(TaskInstance inst)
        {
            int id = inst.Def.TaskId;

            _completed.Add(id);
            _active.Remove(id);

            OnTaskCompleted?.Invoke(inst);
        }

        // ===== 存档支持 =====

        public TaskSaveData BuildSaveData()
        {
            var data = new TaskSaveData();
            data.CompletedTaskIds.AddRange(_completed);

            foreach (var kv in _active)
            {
                var t = kv.Value;
                var rt = new TaskRuntimeSave
                {
                    TaskId = t.Def.TaskId
                };
                for (int i = 0; i < t.ConditionStates.Count; i++)
                {
                    rt.ConditionCurrents.Add(t.ConditionStates[i].Current);
                }
                data.ActiveTasks.Add(rt);
            }
            return data;
        }

        public void RestoreFromSaveData(TaskSaveData data)
        {
            _active.Clear();
            _completed.Clear();

            foreach (var id in data.CompletedTaskIds)
                _completed.Add(id);

            // 先创建，再回填进度
            foreach (var rt in data.ActiveTasks)
            {
                var inst = CreateTask(rt.TaskId);
                if (inst == null) continue;

                for (int i = 0; i < inst.ConditionStates.Count && i < rt.ConditionCurrents.Count; i++)
                    inst.ConditionStates[i].Current = rt.ConditionCurrents[i];

                inst.NotifyUpdated();
            }
        }
    }

    [Serializable]
    public class TaskRuntimeSave
    {
        public int TaskId;
        public List<int> ConditionCurrents = new();
    }

    [Serializable]
    public class TaskSaveData
    {
        public List<int> CompletedTaskIds = new();
        public List<TaskRuntimeSave> ActiveTasks = new();
    }
}