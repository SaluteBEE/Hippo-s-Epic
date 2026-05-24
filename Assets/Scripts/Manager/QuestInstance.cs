using System.Collections.Generic;
using UnityEngine;

public class QuestInstance
{
    public cfg.cfg.quest.Quest Def { get; }
    public QuestState State { get; private set; }
    public int CurrentNodeId { get; private set; }
    public HashSet<int> CompletedNodeIds { get; } = new HashSet<int>();

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

        foreach (var condId in node.Conditionid)
        {
            if (!ConditionSystem.Instance.IsConditionMet(condId))
                return;
        }

        CompleteCurrentNode();

        if (node.NextState == null || node.NextState.Count == 0)
        {
            Complete();
            return;
        }

        if (node.NextState.Count == 1)
        {
            EnterNode(node.NextState[0]);
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

        CompleteCurrentNode();
        EnterNode(nextNodeId);
    }

    private void CompleteCurrentNode()
    {
        if (CurrentNodeId > 0)
        {
            CompletedNodeIds.Add(CurrentNodeId);
            Debug.Log($"[QuestInstance] 节点完成: {CurrentNodeId}");
        }
    }

    public void ForceComplete()
    {
        if (CurrentNodeId > 0)
            CompletedNodeIds.Add(CurrentNodeId);

        State = QuestState.Completed;
    }

    public void ForceFail()
    {
        State = QuestState.Failed;
    }

    private void Complete()
    {
        State = QuestState.Completed;
    }

    private void Fail()
    {
        State = QuestState.Failed;
    }

    public void RestoreState(QuestState state, int currentNodeId, List<int> completedNodeIds)
    {
        State = state;
        CurrentNodeId = currentNodeId;
        CompletedNodeIds.Clear();
        if (completedNodeIds != null)
        {
            foreach (var id in completedNodeIds)
                CompletedNodeIds.Add(id);
        }
    }
}
