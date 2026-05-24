using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TaskItem : MonoBehaviour
{
    [SerializeField] private GameObject content;
    [SerializeField] private TextMeshProUGUI nameTxt;

    private readonly List<GameObject> _contentInstances = new List<GameObject>();
    private GameObject _titleStrikethrough;

    public void Setup(QuestInstance quest, int index, GameObject contentTemplate)
    {
        nameTxt.text = $"{index}. {quest.Def.Name}";
        ClearContent();
        SetTitleStrikethrough(false);

        var tables = GetTables();

        if (quest.State == QuestState.Completed || quest.State == QuestState.Failed)
        {
            SetTitleStrikethrough(true, quest.State == QuestState.Failed);
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
                    AddContentLine(contentTemplate, $"v {text}", true);
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
        SetTitleStrikethrough(false);
    }

    private void SetTitleStrikethrough(bool show, bool isFailed = false)
    {
        if (_titleStrikethrough != null)
        {
            Destroy(_titleStrikethrough);
            _titleStrikethrough = null;
        }

        if (!show) return;

        nameTxt.text = $"<#888888>{nameTxt.text}</color>";

        _titleStrikethrough = new GameObject("TitleStrikethrough");
        _titleStrikethrough.transform.SetParent(nameTxt.transform, false);
        var img = _titleStrikethrough.AddComponent<Image>();
        img.color = isFailed ? new Color(0.8f, 0.3f, 0.3f) : new Color(0.53f, 0.53f, 0.53f);
        var rt = _titleStrikethrough.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 0.5f);
        rt.anchorMax = new Vector2(0, 0.5f);
        rt.pivot = new Vector2(0, 0.5f);
        rt.sizeDelta = new Vector2(nameTxt.preferredWidth, 2);
        rt.anchoredPosition = Vector2.zero;
    }

    private cfg.Tables GetTables()
    {
        if (ManagerRegistry.TryGet<DataTableManager>(out var dtm) && dtm.Tables != null)
            return dtm.Tables;
        return null;
    }
}
