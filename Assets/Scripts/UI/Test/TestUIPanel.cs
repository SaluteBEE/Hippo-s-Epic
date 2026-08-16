using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 测试 UI 面板 —— 覆盖框架常见场景的示例面板:
/// HUD 顶栏 / 滚动列表 / 物品网格 / 弹窗 / 状态条 / 安全区。
/// 继承 UIWindow,注册到 UIManager 后可用 Open&lt;TestUIPanel&gt;() 打开。
/// </summary>
public class TestUIPanel : UIWindow
{
    [Header("HUD 顶栏")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI goldText;
    [SerializeField] private Button popupBtn;
    [SerializeField] private Button closeBtn;

    [Header("滚动列表")]
    [SerializeField] private ScrollRect listScroll;
    [SerializeField] private RectTransform listContent;
    [SerializeField] private TestUIListItem listItemTemplate;
    [SerializeField] private int listItemCount = 20;

    [Header("物品网格")]
    [SerializeField] private GridLayoutGroup grid;
    [SerializeField] private TestUIListItem gridItemTemplate;
    [SerializeField] private int gridItemCount = 12;

    [Header("弹窗")]
    [SerializeField] private GameObject popupRoot;
    [SerializeField] private TextMeshProUGUI popupText;
    [SerializeField] private Button popupConfirmBtn;
    [SerializeField] private Button popupCancelBtn;

    [Header("状态条")]
    [SerializeField] private Image hpBar;
    [SerializeField] private Image mpBar;
    [SerializeField] private Button addGoldBtn;
    [SerializeField] private Button resetListBtn;

    private readonly List<TestUIListItem> listItems = new List<TestUIListItem>();
    private readonly List<TestUIListItem> gridItems = new List<TestUIListItem>();
    private int goldCount;
    private int hpValue = 70;

    private static readonly string[] DemoNames =
    {
        "生命药水", "法力药水", "长剑", "盾牌", "头盔", "护甲",
        "魔法书", "回城卷轴", "铁矿石", "金币袋", "宝石", "钥匙"
    };

    public override void OnCreate(object args)
    {
        // 顶栏
        if (titleText != null) titleText.text = "测试 UI 面板(框架示例)";
        goldCount = 0;
        RefreshGold();

        popupBtn?.onClick.AddListener(OpenPopup);
        closeBtn?.onClick.AddListener(CloseSelf);
        addGoldBtn?.onClick.AddListener(AddGold);
        resetListBtn?.onClick.AddListener(ResetList);
        popupConfirmBtn?.onClick.AddListener(() => SetPopupVisible(false));
        popupCancelBtn?.onClick.AddListener(() => SetPopupVisible(false));

        // 列表
        if (listItemTemplate != null)
        {
            listItemTemplate.gameObject.SetActive(false);
            for (int i = 0; i < listItemCount; i++)
            {
                var item = Instantiate(listItemTemplate, listContent);
                item.SetData(i, $"物品 {i + 1:D2}", Random.Range(1, 99));
                item.SetSelected(false);
                listItems.Add(item);
            }
        }

        // 网格
        if (gridItemTemplate != null)
        {
            gridItemTemplate.gameObject.SetActive(false);
            for (int i = 0; i < gridItemCount; i++)
            {
                var item = Instantiate(gridItemTemplate, grid.transform);
                item.SetData(i, DemoNames[i % DemoNames.Length], Random.Range(1, 5));
                item.SetSelected(false);
                gridItems.Add(item);
            }
        }

        // 状态条
        SetPopupVisible(false);
        RefreshBars();
    }

    public override void OnOpen(object args)
    {
        RefreshGold();
        RefreshBars();
    }

    public override void OnClose()
    {
        popupBtn?.onClick.RemoveAllListeners();
        closeBtn?.onClick.RemoveAllListeners();
        addGoldBtn?.onClick.RemoveAllListeners();
        resetListBtn?.onClick.RemoveAllListeners();
        popupConfirmBtn?.onClick.RemoveAllListeners();
        popupCancelBtn?.onClick.RemoveAllListeners();
    }

    private void CloseSelf() => Destroy(gameObject);

    private void OpenPopup()
    {
        if (popupText != null)
            popupText.text = $"这是一个弹窗示例。\n当前金币: {goldCount}\n确定要执行该操作吗?";
        SetPopupVisible(true);
    }

    private void SetPopupVisible(bool visible) => popupRoot?.SetActive(visible);

    private void AddGold()
    {
        goldCount += 10;
        RefreshGold();
        RefreshBars();
    }

    private void ResetList()
    {
        for (int i = 0; i < listItems.Count; i++)
            listItems[i].SetData(i, $"物品 {i + 1:D2}", Random.Range(1, 99));
        RefreshBars();
    }

    private void RefreshGold()
    {
        if (goldText != null) goldText.text = $"金币: {goldCount}";
    }

    private void RefreshBars()
    {
        hpValue = Mathf.Clamp(hpValue + (goldCount / 10) % 3 - 1, 0, 100);
        if (hpBar != null) hpBar.fillAmount = hpValue / 100f;
        if (mpBar != null) mpBar.fillAmount = (goldCount % 100) / 100f;
    }
}
