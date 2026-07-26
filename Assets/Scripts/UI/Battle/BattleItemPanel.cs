using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 物品子面板（翻找工具）
/// 显示背包中可使用的物品列表
/// </summary>
public class BattleItemPanel : BattleSubPanel
{
    [Header("物品列表")]
    [SerializeField] private Transform itemContainer;
    [SerializeField] private Button itemButtonPrefab;

    private Action<int> _onItemSelected;

    public void Open(BattleManager battleManager, Action<int> onItemSelected)
    {
        base.Open(battleManager);
        _onItemSelected = onItemSelected;
        RefreshItemList();
    }

    private void RefreshItemList()
    {
        if (itemContainer == null || itemButtonPrefab == null) return;

        // 清空旧列表
        foreach (Transform child in itemContainer)
            Destroy(child.gameObject);

        // 从背包获取可使用物品
        var bagManager = BagManager.Instance;
        if (bagManager == null)
        {
            AddPlaceholder("背包为空");
            return;
        }

        var tables = _battleManager?.GetTables();
        var items = bagManager.AllItems;
        bool hasItem = false;

        foreach (var entry in items)
        {
            if (entry.count <= 0) continue;

            var itemCfg = tables?.TbItem.GetOrDefault(entry.itemId);
            if (itemCfg == null) continue;

            // 只显示消耗品（Type=1 或其他可使用类型）
            // 目前显示所有物品，后续按类型过滤
            string name = itemCfg.Name;
            AddItem(entry.itemId, $"{name} x{entry.count}");
            hasItem = true;
        }

        if (!hasItem)
            AddPlaceholder("没有可用物品");
    }

    private void AddItem(int itemId, string displayName)
    {
        var go = Instantiate(itemButtonPrefab.gameObject, itemContainer);
        go.SetActive(true);

        var tmp = go.GetComponentInChildren<TextMeshProUGUI>();
        if (tmp != null) tmp.text = displayName;

        var btn = go.GetComponent<Button>();
        if (btn != null)
        {
            int capturedId = itemId;
            btn.onClick.AddListener(() => _onItemSelected?.Invoke(capturedId));
        }
    }

    private void AddPlaceholder(string text)
    {
        var go = new GameObject("Placeholder");
        go.transform.SetParent(itemContainer, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = 16;
        tmp.color = new Color(0.5f, 0.5f, 0.5f);
        tmp.alignment = TextAlignmentOptions.Center;
    }
}
