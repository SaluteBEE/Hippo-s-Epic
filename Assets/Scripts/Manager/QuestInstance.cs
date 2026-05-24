using System.Collections.Generic;
using UnityEngine;

public class QuestInstance
{
    public cfg.cfg.quest.Quest Def { get; }
    public QuestState State { get; private set; }
    public int CurrentNodeId { get; private set; }
    public List<int> CompletedNodeIds { get; } = new List<int>();
    private readonly Dictionary<int, int> _completedBranchIndices = new Dictionary<int, int>();

    public cfg.cfg.questcontext.Questcontext CurrentNodeDef
    {
        get
        {
            if (CurrentNodeId <= 0) return null;
            var tables = ManagerRegistry.GetTables<cfg.Tables>();
            return tables?.TbQuestcontext.GetOrDefault(CurrentNodeId);
        }
    }

    public QuestInstance(cfg.cfg.quest.Quest quest)
    {
        Def = quest;
        State = QuestState.InProgress;
        CurrentNodeId = 0;
    }

    public void EnterNode(int nodeId)
    {
        CurrentNodeId = nodeId;

        if (nodeId == -1)
        {
            Complete();
            return;
        }

        if (nodeId == -2)
        {
            Fail();
            return;
        }

        if (nodeId == 0) return;

        var tables = ManagerRegistry.GetTables<cfg.Tables>();
        if (tables == null) return;

        var node = tables.TbQuestcontext.GetOrDefault(nodeId);
        if (node == null)
        {
            Debug.LogWarning($"[QuestInstance] 节点不存在: {nodeId}");
            Fail();
            return;
        }

        Debug.Log($"[QuestInstance] 进入节点: {nodeId} ({node.Des})");
    }

    public void CheckNodeCompletion()
    {
        if (State != QuestState.InProgress) return;
        if (CurrentNodeId <= 0) return;

        var node = CurrentNodeDef;
        if (node == null) return;

        if (node.Conditionid == null || node.Conditionid.Count == 0)
            return;

        for (int i = 0; i < node.Conditionid.Count; i++)
        {
            if (!ConditionSystem.Instance.IsConditionMet(node.Conditionid[i]))
                continue;

            int completedNodeId = CurrentNodeId;
            CompleteCurrentNode();
            _completedBranchIndices[completedNodeId] = i;

            if (node.NextState == null || i >= node.NextState.Count)
            {
                Complete();
                return;
            }

            EnterNode(node.NextState[i]);
            return;
        }
    }

    public void ChooseBranch(int nextNodeId)
    {
        if (State != QuestState.InProgress) return;

        var node = CurrentNodeDef;
        if (node == null) return;

        if (node.NextState == null || !node.NextState.Contains(nextNodeId))
        {
            Debug.LogWarning($"[QuestInstance] 无效分支: {nextNodeId}");
            return;
        }

        int completedNodeId = CurrentNodeId;
        int branchIdx = node.NextState.IndexOf(nextNodeId);
        CompleteCurrentNode();
        if (branchIdx >= 0)
            _completedBranchIndices[completedNodeId] = branchIdx;
        EnterNode(nextNodeId);
    }

    public int GetCompletedBranchIndex(int nodeId)
    {
        return _completedBranchIndices.TryGetValue(nodeId, out var idx) ? idx : -1;
    }

    private void CompleteCurrentNode()
    {
        if (CurrentNodeId > 0 && !CompletedNodeIds.Contains(CurrentNodeId))
        {
            CompletedNodeIds.Add(CurrentNodeId);
            Debug.Log($"[QuestInstance] 节点完成: {CurrentNodeId}");
        }
    }

    public void ForceComplete()
    {
        if (CurrentNodeId > 0 && !CompletedNodeIds.Contains(CurrentNodeId))
            CompletedNodeIds.Add(CurrentNodeId);

        State = QuestState.Completed;
    }

    public void ForceFail()
    {
        State = QuestState.Failed;
    }

    public void SetNotAccepted()
    {
        State = QuestState.NotAccepted;
        var startNode = Def.Nodeseq != null && Def.Nodeseq.Count > 0 ? Def.Nodeseq[0] : 0;
        if (startNode > 0)
            CurrentNodeId = startNode;
    }

    private void Complete()
    {
        State = QuestState.Completed;
    }

    private void Fail()
    {
        State = QuestState.Failed;
    }

    public void RestoreState(QuestState state, int currentNodeId, List<int> completedNodeIds, Dictionary<int, int> branchIndices)
    {
        State = state;
        CurrentNodeId = currentNodeId;
        CompletedNodeIds.Clear();
        _completedBranchIndices.Clear();
        if (completedNodeIds != null)
        {
            foreach (var id in completedNodeIds)
                CompletedNodeIds.Add(id);
        }
        if (branchIndices != null)
        {
            foreach (var kv in branchIndices)
                _completedBranchIndices[kv.Key] = kv.Value;
        }
    }
}
