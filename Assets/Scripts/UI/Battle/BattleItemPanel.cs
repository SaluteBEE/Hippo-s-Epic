using System;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;

/// <summary>
/// 物品子面板（翻找工具）
/// 显示背包中可使用的物品列表
/// </summary>
public class BattleItemPanel : BattleSubPanel
{
    [Header("物品列表")]
    [SerializeField] private Transform itemContainer;
    [SerializeField] private GameObject itemButtonPrefab;

    [Header("物品图标")]
    [Tooltip("物品图标加载地址格式，{0} 为物品表 Icon 字段；找不到地址时隐藏图标")]
    [SerializeField] private string itemIconPathFormat = "Assets/Art/Sprites/UI/Item/{0}";

    private Action<int> _onItemSelected;

    /// <summary>
    /// 初始化兜底：itemButtonPrefab 未拖引用时，取容器内第一个子对象作为模板；
    /// 若模板是场景实例则隐藏，避免其残留在列表里（Project 资产不隐藏）
    /// </summary>
    protected override void Awake()
    {
        base.Awake();
        if (itemButtonPrefab == null && itemContainer != null && itemContainer.childCount > 0)
            itemButtonPrefab = itemContainer.GetChild(0).gameObject;

        if (itemButtonPrefab != null && itemButtonPrefab.scene.IsValid())
            itemButtonPrefab.SetActive(false);
    }

    public void Open(BattleManager battleManager, Action<int> onItemSelected)
    {
        base.Open(battleManager);
        _onItemSelected = onItemSelected;
        RefreshItemList();
    }

    private void RefreshItemList()
    {
        if (itemContainer == null || itemButtonPrefab == null) return;

        // 清空旧列表（保留模板本身，模板可能是容器内被隐藏的场景对象）
        foreach (Transform child in itemContainer)
        {
            if (child == itemButtonPrefab.transform) continue;
            child.gameObject.SetActive(false);  // 先隐藏，避免销毁延迟期间遮挡新项
            Destroy(child.gameObject);
        }

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
            string detail = !string.IsNullOrEmpty(itemCfg.Battletip) ? itemCfg.Battletip : itemCfg.Tip;
            AddItem(entry.itemId, $"{name} x{entry.count}", detail, itemCfg.Icon);
            hasItem = true;
        }

        if (!hasItem)
            AddPlaceholder("没有可用物品");

        // 重置滚动位置到顶部，避免关闭再打开时 Content 残留上次滚动位置导致物品项错位/点不到
        var scrollRect = itemContainer.GetComponentInParent<ScrollRect>();
        if (scrollRect != null)
        {
            Canvas.ForceUpdateCanvases();
            scrollRect.verticalNormalizedPosition = 1f;
            scrollRect.horizontalNormalizedPosition = 0f;
        }
    }

    private void AddItem(int itemId, string displayName, string detail, int iconId)
    {
        var go = Instantiate(itemButtonPrefab, itemContainer);
        go.SetActive(true);

        // 模板组件可能是 disabled，克隆后需显式启用，否则 SetActive 不会激活 disable 的组件
        var bg = go.transform.Find("bg");
        var icon = go.transform.Find("icon");
        var nameGo = go.transform.Find("item_name");
        var detailGo = go.transform.Find("item_detail");

        foreach (var g in new[] { bg, icon, nameGo, detailGo })
        {
            if (g == null) continue;
            foreach (var c in g.GetComponents<Behaviour>())
                c.enabled = true;
        }

        var bgImg = bg != null ? bg.GetComponent<Image>() : null;
        var btn = bg != null ? bg.GetComponent<Button>() : null;
        var nameTmp = nameGo != null ? nameGo.GetComponent<TextMeshProUGUI>() : null;
        var detailTmp = detailGo != null ? detailGo.GetComponent<TextMeshProUGUI>() : null;
        var iconImg = icon != null ? icon.GetComponent<Image>() : null;

        if (nameTmp != null) nameTmp.text = displayName;
        if (detailTmp != null) detailTmp.text = detail;
        if (iconImg != null)
        {
            if (iconId > 0)
                LoadIcon(iconImg, iconId);  // 有图标配置时动态加载
            else
                iconImg.gameObject.SetActive(false);  // 无图标配置时隐藏
        }

        // 文本/图标设为不拦截点击，确保点击穿透到 bg 的 Button（否则命中文字区域时 onClick 不触发）
        if (iconImg != null) iconImg.raycastTarget = false;
        if (nameTmp != null) nameTmp.raycastTarget = false;
        if (detailTmp != null) detailTmp.raycastTarget = false;

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

    /// <summary> 检查 Addressable 是否存在该地址（不存在则静默跳过，避免 LoadAsset 报错） </summary>
    private static bool KeyExists(object key)
    {
        foreach (IResourceLocator locator in Addressables.ResourceLocators)
        {
            if (locator.Locate(key, typeof(UnityEngine.Object), out _))
                return true;
        }
        return false;
    }

    /// <summary>
    /// 异步加载物品图标到 icon；配置的图标资源不存在时隐藏 icon
    /// </summary>
    private void LoadIcon(Image target, int iconId)
    {
        string address = string.Format(itemIconPathFormat, iconId);
        if (!KeyExists(address))
        {
            target.gameObject.SetActive(false);  // 图标地址不存在 → 隐藏
            return;
        }

        var handle = Addressables.LoadAssetAsync<Sprite>(address);
        handle.Completed += op =>
        {
            if (op.Status == AsyncOperationStatus.Succeeded && target != null)
            {
                target.sprite = op.Result;
            }
            else
            {
                if (target != null)
                    target.gameObject.SetActive(false);  // 加载失败 → 隐藏 icon
                if (op.IsValid())
                    Addressables.Release(op);
            }
        };
    }
}
