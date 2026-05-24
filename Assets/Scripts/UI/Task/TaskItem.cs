using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TaskItem : MonoBehaviour
{
    [SerializeField] private GameObject content;
    [SerializeField] private TextMeshProUGUI nameTxt;

    private readonly List<GameObject> _contentInstances = new List<GameObject>();

    public void Setup(QuestInstance quest, int index, GameObject contentTemplate)
    {
        nameTxt.text = $"{index}. {quest.Def.Name}";
        ClearContent();

        if (quest.State == QuestState.Completed || quest.State == QuestState.Failed)
        {
            AddContentLine(contentTemplate, $"{(quest.State == QuestState.Completed ? "v" : "x")} {(quest.State == QuestState.Completed ? "已完成" : "已失败")}");
            return;
        }

        var tables = GetTables();
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
            AddContentLine(contentTemplate, $"v {text}", true);
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

    private void AddContentLine(GameObject template, string text, bool strikethrough = false)
    {
        var go = Instantiate(template, content.transform, false);
        go.SetActive(true);
        var tmp = go.GetComponent<TextMeshProUGUI>();
        if (tmp != null) tmp.text = strikethrough ? $"<#888888>{text}</color>" : text;

        if (strikethrough && tmp != null)
        {
            var line = new GameObject("Strikethrough");
            line.transform.SetParent(go.transform, false);
            var img = line.AddComponent<Image>();
            img.color = new Color(0.53f, 0.53f, 0.53f, 1f);
            var rt = line.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 0.5f);
            rt.anchorMax = new Vector2(0, 0.5f);
            rt.pivot = new Vector2(0, 0.5f);
            float textWidth = tmp.preferredWidth;
            rt.sizeDelta = new Vector2(textWidth, 2);
            rt.anchoredPosition = Vector2.zero;
        }

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
