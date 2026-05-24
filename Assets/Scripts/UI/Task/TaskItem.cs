using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class TaskItem : MonoBehaviour
{
    [SerializeField] private GameObject content;
    [SerializeField] private TextMeshProUGUI nameTxt;

    private readonly List<GameObject> _contentInstances = new List<GameObject>();

    public void Setup(QuestInstance quest, GameObject contentTemplate)
    {
        nameTxt.text = $"{GetStateTag(quest.State)}{quest.Def.Name}";
        ClearContent();

        if (quest.State == QuestState.Completed || quest.State == QuestState.Failed)
        {
            AddContentLine(contentTemplate, quest.State == QuestState.Completed ? "已完成" : "已失败");
            return;
        }

        var tables = GetTables();
        if (tables == null)
        {
            if (!string.IsNullOrEmpty(quest.Def.Shorttext))
                AddContentLine(contentTemplate, quest.Def.Shorttext);
            return;
        }

        var node = quest.CurrentNodeDef;
        if (node == null)
        {
            if (!string.IsNullOrEmpty(quest.Def.Shorttext))
                AddContentLine(contentTemplate, quest.Def.Shorttext);
            return;
        }

        AddContentLine(contentTemplate, node.Des);

        if (node.NextState == null || node.NextState.Count == 0)
            return;

        if (node.NextState.Count == 1)
        {
            int nextId = node.NextState[0];
            if (nextId > 0)
            {
                var nextNode = tables.TbQuestcontext.GetOrDefault(nextId);
                if (nextNode != null)
                    AddContentLine(contentTemplate, $"  -> {nextNode.Des}");
            }
        }
        else
        {
            for (int b = 0; b < node.NextState.Count; b++)
            {
                int nextId = node.NextState[b];
                if (nextId <= 0) continue;
                var branchNode = tables.TbQuestcontext.GetOrDefault(nextId);
                if (branchNode != null)
                    AddContentLine(contentTemplate, $"  * {branchNode.Des}");
            }
        }
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

    private static string GetStateTag(QuestState state)
    {
        switch (state)
        {
            case QuestState.InProgress: return "<color=#FFDD44>> </color>";
            case QuestState.Completed: return "<color=#44FF44>v </color>";
            case QuestState.Failed: return "<color=#FF4444>x </color>";
            default: return "";
        }
    }

    private cfg.Tables GetTables()
    {
        if (ManagerRegistry.TryGet<DataTableManager>(out var dtm) && dtm.Tables != null)
            return dtm.Tables;
        return null;
    }
}
