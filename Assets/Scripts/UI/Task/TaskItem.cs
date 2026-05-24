using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class TaskItem : MonoBehaviour
{
    [SerializeField] private GameObject content;
    [SerializeField] private TextMeshProUGUI nameTxt;

    private readonly List<GameObject> _contentInstances = new List<GameObject>();

    public void Setup(QuestInstance quest, int index, GameObject contentTemplate)
    {
        ClearContent();

        bool done = quest.State == QuestState.Completed || quest.State == QuestState.Failed;
        nameTxt.text = done
            ? $"<s><#888888>{index}. {quest.Def.Name}</color></s>"
            : $"{index}. {quest.Def.Name}";

        var tables = GetTables();

        if (done)
        {
            if (tables != null)
            {
                foreach (var nodeId in quest.CompletedNodeIds)
                {
                    var completedNode = tables.TbQuestcontext.GetOrDefault(nodeId);
                    if (completedNode == null) continue;

                    int branchIdx = quest.GetCompletedBranchIndex(nodeId);
                    string text = branchIdx >= 0 && completedNode.Fintext != null && branchIdx < completedNode.Fintext.Count
                        ? completedNode.Fintext[branchIdx]
                        : completedNode.Des;
                    AddContentLine(contentTemplate, $"<s><#888888>v {text}</color></s>");
                }
            }
            return;
        }

        if (tables == null)
        {
            if (!string.IsNullOrEmpty(quest.Def.Shorttext))
                AddContentLine(contentTemplate, $"* {quest.Def.Shorttext}");
            return;
        }

        foreach (var nodeId in quest.CompletedNodeIds)
        {
            var completedNode = tables.TbQuestcontext.GetOrDefault(nodeId);
            if (completedNode == null) continue;

            int branchIdx = quest.GetCompletedBranchIndex(nodeId);
            string text = branchIdx >= 0 && completedNode.Fintext != null && branchIdx < completedNode.Fintext.Count
                ? completedNode.Fintext[branchIdx]
                : completedNode.Des;
            AddContentLine(contentTemplate, $"<s><#888888>v {text}</color></s>");
        }

        var node = quest.CurrentNodeDef;
        if (node == null)
        {
            if (!string.IsNullOrEmpty(quest.Def.Shorttext))
                AddContentLine(contentTemplate, $"* {quest.Def.Shorttext}");
            return;
        }

        AddContentLine(contentTemplate, $"* {node.Des}");
    }

    private void AddContentLine(GameObject template, string text)
    {
        var go = Instantiate(template, content.transform, false);
        go.SetActive(true);
        var tmp = go.GetComponent<TextMeshProUGUI>();
        if (tmp != null) tmp.text = text;
        _contentInstances.Add(go);
    }

    private void ClearContent()
    {
        foreach (var go in _contentInstances)
        {
            if (go != null) Destroy(go);
        }
        _contentInstances.Clear();
    }

    private cfg.Tables GetTables()
    {
        if (ManagerRegistry.TryGet<DataTableManager>(out var dtm) && dtm.Tables != null)
            return dtm.Tables;
        return null;
    }
}
