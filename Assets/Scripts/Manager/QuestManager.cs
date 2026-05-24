using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum QuestState
{
    NotAccepted = 1,
    InProgress = 2,
    Completed = 3,
    Failed = 4
}

public class QuestManager
{
    private static QuestManager _instance;
    public static QuestManager Instance => _instance ??= new QuestManager();

    private readonly Dictionary<int, QuestInstance> _quests = new Dictionary<int, QuestInstance>();

    public IReadOnlyDictionary<int, QuestInstance> AllQuests => _quests;

    public event Action<QuestInstance> OnQuestAccepted;
    public event Action<QuestInstance> OnQuestNodeChanged;
    public event Action<QuestInstance> OnQuestCompleted;
    public event Action<QuestInstance> OnQuestFailed;

    public void Initialize()
    {
        _quests.Clear();

        var tables = ManagerRegistry.GetTables<cfg.Tables>();
        if (tables == null) return;

        foreach (var quest in tables.TbQuest.DataList)
        {
            if (quest.State == (int)QuestState.InProgress)
            {
                var inst = new QuestInstance(quest);
                var startNode = quest.Nodeseq != null && quest.Nodeseq.Count > 0
                    ? quest.Nodeseq[0]
                    : 0;
                inst.EnterNode(startNode);
                _quests[quest.Id] = inst;
                Debug.Log($"[QuestManager] 初始化 InProgress 任务: {quest.Name} (id={quest.Id})");
            }
            else if (quest.State == (int)QuestState.NotAccepted)
            {
                var inst = new QuestInstance(quest);
                inst.SetNotAccepted();
                _quests[quest.Id] = inst;
            }
        }
    }

    public void StartConditionListening()
    {
        ConditionSystem.Instance.OnConditionChanged += OnConditionChanged;

        var tables = ManagerRegistry.GetTables<cfg.Tables>();
        if (tables == null) return;

        foreach (var quest in tables.TbQuest.DataList)
        {
            if (quest.State != (int)QuestState.NotAccepted) continue;
            if (quest.Conditionid == 0) continue;

            int questId = quest.Id;
            ConditionSystem.Instance.OnConditionChanged += (condId, met) =>
            {
                if (!met) return;
                if (_quests.ContainsKey(questId)) return;
                if (!CanAccept(questId)) return;

                AcceptQuest(questId);
            };
        }
    }

    private void OnConditionChanged(int condId, bool met)
    {
        if (!met) return;
        CheckAllActiveQuests();
    }

    public bool CanAccept(int questId)
    {
        if (_quests.ContainsKey(questId)) return false;

        var tables = ManagerRegistry.GetTables<cfg.Tables>();
        if (tables == null) return false;

        var quest = tables.TbQuest.GetOrDefault(questId);
        if (quest == null) return false;

        if (quest.Conditionid != 0)
            return ConditionSystem.Instance.IsConditionMet(quest.Conditionid);

        return true;
    }

    public QuestInstance AcceptQuest(int questId)
    {
        if (_quests.ContainsKey(questId)) return _quests[questId];
        if (!CanAccept(questId)) return null;

        var tables = ManagerRegistry.GetTables<cfg.Tables>();
        var quest = tables.TbQuest.GetOrDefault(questId);

        var instance = new QuestInstance(quest);

        var startNode = quest.Nodeseq != null && quest.Nodeseq.Count > 0
            ? quest.Nodeseq[0]
            : 0;

        instance.EnterNode(startNode);
        _quests[questId] = instance;

        OnQuestAccepted?.Invoke(instance);
        ConditionSystem.Instance.Notify(ConditionChangeType.Task);
        Debug.Log($"[QuestManager] 接取任务: {quest.Name} (id={questId}), 起始节点={startNode}");
        return instance;
    }

    public QuestInstance GetQuest(int questId)
    {
        _quests.TryGetValue(questId, out var q);
        return q;
    }

    public QuestState GetQuestState(int questId)
    {
        if (!_quests.TryGetValue(questId, out var inst))
            return QuestState.NotAccepted;
        return inst.State;
    }

    public bool IsQuestCompleted(int questId)
    {
        return _quests.TryGetValue(questId, out var inst) && inst.State == QuestState.Completed;
    }

    public bool IsQuestFailed(int questId)
    {
        return _quests.TryGetValue(questId, out var inst) && inst.State == QuestState.Failed;
    }

    public bool IsQuestActive(int questId)
    {
        return _quests.TryGetValue(questId, out var inst) && inst.State == QuestState.InProgress;
    }

    public void TryAdvanceNode(int questId, int chosenNextNodeId)
    {
        var inst = GetQuest(questId);
        if (inst == null || inst.State != QuestState.InProgress) return;

        inst.EnterNode(chosenNextNodeId);

        OnQuestNodeChanged?.Invoke(inst);
        ConditionSystem.Instance.Notify(ConditionChangeType.Task);

        if (inst.State == QuestState.Completed)
        {
            GrantRewards(inst);
            OnQuestCompleted?.Invoke(inst);
            Debug.Log($"[QuestManager] 任务完成: {inst.Def.Name} (id={questId})");
        }
        else if (inst.State == QuestState.Failed)
        {
            OnQuestFailed?.Invoke(inst);
            Debug.Log($"[QuestManager] 任务失败: {inst.Def.Name} (id={questId})");
        }
    }

    public void CheckCurrentNodeCompletion(int questId)
    {
        var inst = GetQuest(questId);
        if (inst == null || inst.State != QuestState.InProgress) return;
        if (inst.CurrentNodeId <= 0) return;

        inst.CheckNodeCompletion();

        if (inst.State == QuestState.Completed)
        {
            GrantRewards(inst);
            OnQuestCompleted?.Invoke(inst);
            ConditionSystem.Instance.Notify(ConditionChangeType.Task);
            Debug.Log($"[QuestManager] 任务完成: {inst.Def.Name} (id={questId})");
        }
    }

    public void CheckAllActiveQuests()
    {
        var activeQuestIds = _quests.Values
            .Where(q => q.State == QuestState.InProgress)
            .Select(q => q.Def.Id)
            .ToList();

        foreach (var questId in activeQuestIds)
            CheckCurrentNodeCompletion(questId);
    }

    private void GrantRewards(QuestInstance inst)
    {
        var tables = ManagerRegistry.GetTables<cfg.Tables>();
        if (tables == null) return;

        var allRewards = new List<List<int>>();

        if (inst.CurrentNodeDef != null && inst.CurrentNodeDef.Reward != null)
            allRewards.AddRange(inst.CurrentNodeDef.Reward);

        foreach (var reward in allRewards)
        {
            if (reward == null || reward.Count < 2) continue;

            int rewardType = reward[0];
            int rewardAmount = reward[1];

            if (rewardType == 1)
                BagManager.Instance.AddItem(rewardType, rewardAmount);
            else
                BagManager.Instance.AddItem(rewardType, rewardAmount);

            Debug.Log($"[QuestManager] 发放奖励: type={rewardType}, amount={rewardAmount}");
        }
    }

    public void ForceComplete(int questId)
    {
        var inst = GetQuest(questId);
        if (inst == null) return;

        inst.ForceComplete();
        GrantRewards(inst);
        OnQuestCompleted?.Invoke(inst);
        ConditionSystem.Instance.Notify(ConditionChangeType.Task);
    }

    public void ForceFail(int questId)
    {
        var inst = GetQuest(questId);
        if (inst == null) return;

        inst.ForceFail();
        OnQuestFailed?.Invoke(inst);
        ConditionSystem.Instance.Notify(ConditionChangeType.Task);
    }

    #region 存档

    public QuestSaveData BuildSaveData()
    {
        var data = new QuestSaveData();
        foreach (var kv in _quests)
        {
            var inst = kv.Value;
            var branchIndices = new List<int>();
            foreach (var nodeId in inst.CompletedNodeIds)
                branchIndices.Add(inst.GetCompletedBranchIndex(nodeId));

            data.entries.Add(new QuestSaveEntry
            {
                questId = inst.Def.Id,
                state = (int)inst.State,
                currentNodeId = inst.CurrentNodeId,
                completedNodeIds = new List<int>(inst.CompletedNodeIds),
                branchIndices = branchIndices
            });
        }
        return data;
    }

    public void RestoreFromSaveData(QuestSaveData data)
    {
        if (data?.entries == null || data.entries.Count == 0)
        {
            Debug.Log("[QuestManager] 无存档任务数据，跳过恢复");
            return;
        }

        _quests.Clear();

        var tables = ManagerRegistry.GetTables<cfg.Tables>();
        if (tables == null) return;

        foreach (var entry in data.entries)
        {
            var quest = tables.TbQuest.GetOrDefault(entry.questId);
            if (quest == null) continue;

            var branchDict = new Dictionary<int, int>();
            if (entry.completedNodeIds != null && entry.branchIndices != null)
            {
                for (int i = 0; i < entry.completedNodeIds.Count && i < entry.branchIndices.Count; i++)
                {
                    if (entry.branchIndices[i] >= 0)
                        branchDict[entry.completedNodeIds[i]] = entry.branchIndices[i];
                }
            }

            var inst = new QuestInstance(quest);
            inst.RestoreState(
                (QuestState)entry.state,
                entry.currentNodeId,
                entry.completedNodeIds ?? new List<int>(),
                branchDict
            );
            _quests[entry.questId] = inst;
        }

        Debug.Log($"[QuestManager] 从存档恢复: {_quests.Count} 个任务");
    }

    public void Clear()
    {
        _quests.Clear();
    }

    #endregion
}

[Serializable]
public class QuestSaveData
{
    public List<QuestSaveEntry> entries = new List<QuestSaveEntry>();
}

[Serializable]
public class QuestSaveEntry
{
    public int questId;
    public int state;
    public int currentNodeId;
    public List<int> completedNodeIds;
    public List<int> branchIndices;
}
