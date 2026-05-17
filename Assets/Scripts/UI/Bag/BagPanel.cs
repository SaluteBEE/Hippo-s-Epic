using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BagPanel : UIWindow
{
    [Header("穿戴展示")]
    [SerializeField] private Image wearLeftHand;
    [SerializeField] private Image wearRightHand;
    [SerializeField] private Image wearHead;
    [SerializeField] private Image wearFace;
    [SerializeField] private Image wearLeftFoot;
    [SerializeField] private Image wearRightFoot;
    [SerializeField] private Image wearBody;

    [Header("装备槽")]
    [SerializeField] private BagEquip equipHand;
    [SerializeField] private BagEquip equipBody;
    [SerializeField] private BagEquip equipFoot;
    [SerializeField] private BagEquip equipHead;

    [Header("物品详情")]
    [SerializeField] private Image itemIcon;
    [SerializeField] private TextMeshProUGUI itemName;
    [SerializeField] private TextMeshProUGUI itemTip;
    [SerializeField] private TextMeshProUGUI typeName;
    [SerializeField] private Button btnUseItem;

    [Header("列表")]
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private Transform content;

    [Header("分类筛选")]
    [SerializeField] private ToggleGroup toggleGroup;
    [SerializeField] private Toggle btnEquip;
    [SerializeField] private Toggle btnTrash;
    [SerializeField] private Toggle btnUse;
    [SerializeField] private Toggle btnKey;

    [Header("预制体")]
    [SerializeField] private BagItem bagItemPrefab;

    private readonly List<BagItem> _bagItems = new List<BagItem>();
    private readonly Dictionary<EquipSlot, BagEquip> _equipSlots = new Dictionary<EquipSlot, BagEquip>();

    private int _selectedItemId;
    private int _selectedInstanceId;
    private EquipSlot? _selectedEquipSlot;
    private int _currentFilterType = -1;

    public override void OnCreate(object args)
    {
        _equipSlots[EquipSlot.Hand] = equipHand;
        _equipSlots[EquipSlot.Body] = equipBody;
        _equipSlots[EquipSlot.Foot] = equipFoot;
        _equipSlots[EquipSlot.Head] = equipHead;

        foreach (var kvp in _equipSlots)
        {
            if (kvp.Value != null)
            {
                EquipSlot slot = kvp.Key;
                kvp.Value.onClick = OnEquipSlotClicked;
            }
        }

        BagManager.Instance.OnItemChanged += OnItemChanged;
        EquipManager.Instance.OnEquipChanged += OnEquipChanged;

        if (toggleGroup != null)
            toggleGroup.allowSwitchOff = true;

        btnEquip.onValueChanged.AddListener(isOn => { if (isOn) SetFilter(4); else if (_currentFilterType == 4) SetFilter(-1); });
        btnTrash.onValueChanged.AddListener(isOn => { if (isOn) SetFilter(5); else if (_currentFilterType == 5) SetFilter(-1); });
        btnUse.onValueChanged.AddListener(isOn => { if (isOn) SetFilter(3); else if (_currentFilterType == 3) SetFilter(-1); });
        btnKey.onValueChanged.AddListener(isOn => { if (isOn) SetFilter(2); else if (_currentFilterType == 2) SetFilter(-1); });

        if (btnUseItem != null)
            btnUseItem.onClick.AddListener(OnBtnUseItemClicked);

        _currentFilterType = -1;
        btnEquip.isOn = false;
        btnTrash.isOn = false;
        btnUse.isOn = false;
        btnKey.isOn = false;
    }

    public override void OnOpen(object args)
    {
        RefreshList();
        RefreshEquips();
        ClearSelection();
    }

    public override void OnClose()
    {
    }

    private void SetFilter(int type)
    {
        _currentFilterType = type;
        RefreshList();
        ClearSelection();
    }

    private void RefreshList()
    {
        ClearList();

        var items = BagManager.Instance.AllItems;
        var tables = GetTables();
        if (tables == null) return;

        foreach (var entry in items)
        {
            int itemId = entry.itemId;

            var itemCfg = tables.TbItem.GetOrDefault(itemId);
            if (itemCfg == null) continue;
            if (itemCfg.Type == 1) continue;
            if (_currentFilterType > 0 && itemCfg.Type != _currentFilterType) continue;

            EquipSlot equipSlot = EquipManager.Instance.GetEquipSlotByInstance(entry.instanceId);
            if (equipSlot != (EquipSlot)(-1)) continue;

            BagItem bagItem = Instantiate(bagItemPrefab, content, false);
            bagItem.Setup(entry.instanceId, itemId, entry.count, itemCfg);
            bagItem.onClick = OnBagItemLeftClick;
            bagItem.onRightClick = OnBagItemRightClick;
            _bagItems.Add(bagItem);
        }
    }

    private void ClearList()
    {
        foreach (var item in _bagItems)
        {
            if (item != null)
                Destroy(item.gameObject);
        }
        _bagItems.Clear();
    }

    private void RefreshEquips()
    {
        foreach (var kvp in _equipSlots)
        {
            if (kvp.Value == null) continue;
            int equippedId = EquipManager.Instance.GetEquippedItemId(kvp.Key);
            kvp.Value.Setup(kvp.Key, equippedId > 0 ? equippedId : (int?)null);
        }
    }

    private void OnBagItemLeftClick(BagItem item)
    {
        _selectedEquipSlot = null;
        _selectedItemId = item.ItemId;
        _selectedInstanceId = item.InstanceId;
        HighlightBagItem(item);
        ShowItemDetail(item.ItemId);
    }

    private void OnBagItemRightClick(BagItem item)
    {
        _selectedEquipSlot = null;
        _selectedItemId = item.ItemId;
        _selectedInstanceId = item.InstanceId;
        HighlightBagItem(item);
        ShowItemDetail(item.ItemId);

        UseSelectedItem();
    }

    private void OnBtnUseItemClicked()
    {
        if (_selectedItemId <= 0) return;
        UseSelectedItem();
    }

    private void UseSelectedItem()
    {
        if (_selectedItemId <= 0) return;

        var tables = GetTables();
        if (tables == null) return;

        var itemCfg = tables.TbItem.GetOrDefault(_selectedItemId);
        if (itemCfg == null) return;

        if (itemCfg.Type == 4)
        {
            EquipManager.Instance.EquipByInstance(_selectedInstanceId);
        }
        else if (itemCfg.Type == 3)
        {
            BagManager.Instance.RemoveItem(_selectedItemId, 1);
            Debug.Log($"[BagPanel] 使用消耗品: {_selectedItemId}");
        }
    }

    private void OnEquipSlotClicked(BagEquip equip)
    {
        _selectedItemId = 0;
        _selectedEquipSlot = equip.Slot;
        HighlightEquipSlot(equip);

        int equippedId = EquipManager.Instance.GetEquippedItemId(equip.Slot);
        if (equippedId > 0)
            ShowItemDetail(equippedId);
        else
            ClearDetail();
    }

    private void ShowItemDetail(int itemId)
    {
        var tables = GetTables();
        if (tables == null) return;

        var itemCfg = tables.TbItem.GetOrDefault(itemId);
        if (itemCfg == null) return;

        if (itemIcon != null)
        {
            itemIcon.gameObject.SetActive(true);
            itemIcon.sprite = LoadIcon(itemCfg.Icon);
        }

        if (itemName != null)
            itemName.text = itemCfg.Name;

        if (itemTip != null)
            itemTip.text = itemCfg.Tip;

        bool showBtn = false;
        string actionLabel = "";

        if (itemCfg.Type == 4)
        {
            bool isThisEquipped = EquipManager.Instance.IsInstanceEquipped(_selectedInstanceId);
            if (isThisEquipped)
            {
                actionLabel = "已装备";
                showBtn = false;
            }
            else
            {
                actionLabel = "装备";
                showBtn = true;
            }
        }
        else if (itemCfg.Type == 3)
        {
            bool hasFunc = itemCfg.Func != null && itemCfg.Func.Count > 0;
            if (hasFunc)
            {
                actionLabel = "使用";
                showBtn = true;
            }
            else
            {
                actionLabel = "";
                showBtn = false;
            }
        }

        if (typeName != null)
        {
            typeName.gameObject.SetActive(!string.IsNullOrEmpty(actionLabel));
            typeName.text = actionLabel;
        }

        if (btnUseItem != null)
            btnUseItem.gameObject.SetActive(showBtn);
    }

    private void ClearDetail()
    {
        if (itemIcon != null)
        {
            itemIcon.sprite = null;
            itemIcon.gameObject.SetActive(false);
        }
        if (itemName != null) itemName.text = "";
        if (itemTip != null) itemTip.text = "";
        if (typeName != null)
        {
            typeName.text = "";
            typeName.gameObject.SetActive(false);
        }
        if (btnUseItem != null)
            btnUseItem.gameObject.SetActive(false);
    }

    private void ClearSelection()
    {
        _selectedItemId = 0;
        _selectedInstanceId = 0;
        _selectedEquipSlot = null;

        foreach (var item in _bagItems)
            item.SetSelected(false);
        foreach (var kvp in _equipSlots)
        {
            if (kvp.Value != null)
                kvp.Value.SetSelected(false);
        }
        ClearDetail();
    }

    private void HighlightBagItem(BagItem selected)
    {
        foreach (var item in _bagItems)
            item.SetSelected(item == selected);
        foreach (var kvp in _equipSlots)
        {
            if (kvp.Value != null)
                kvp.Value.SetSelected(false);
        }
    }

    private void HighlightEquipSlot(BagEquip selected)
    {
        foreach (var item in _bagItems)
            item.SetSelected(false);
        foreach (var kvp in _equipSlots)
        {
            if (kvp.Value != null)
                kvp.Value.SetSelected(kvp.Value == selected);
        }
    }

    private void OnItemChanged(BagManager.BagChangeType changeType, int itemId)
    {
        if (!gameObject.activeInHierarchy) return;
        RefreshList();
        if (_selectedItemId == itemId)
        {
            int remaining = BagManager.Instance.GetItemCount(itemId);
            if (remaining <= 0)
                ClearSelection();
            else
                ShowItemDetail(itemId);
        }
    }

    private void OnEquipChanged(EquipSlot slot, int itemId)
    {
        if (!gameObject.activeInHierarchy) return;
        RefreshEquips();
        RefreshList();
        if (_selectedEquipSlot.HasValue && _selectedEquipSlot.Value == slot)
        {
            if (itemId > 0)
                ShowItemDetail(itemId);
            else
                ClearSelection();
        }
    }

    private cfg.Tables GetTables()
    {
        if (ManagerRegistry.TryGet<DataTableManager>(out var dtm) && dtm.Tables != null)
            return dtm.Tables;
        return null;
    }

    private Sprite LoadIcon(int iconId)
    {
        return IconLoader.LoadItemIcon(iconId);
    }
}
