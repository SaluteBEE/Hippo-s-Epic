using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UseItemTip : UIWindow
{
    public override UILayer Layer => UILayer.Popup;

    [Header("物品信息")]
    [SerializeField] private Image itemIcon;
    [SerializeField] private TextMeshProUGUI itemName;
    [SerializeField] private TextMeshProUGUI itemTip;
    [SerializeField] private TextMeshProUGUI itemCount;

    [Header("操作按钮")]
    [SerializeField] private Button useBtn;
    [SerializeField] private Button dropBtn;
    [SerializeField] private Button equipBtn;
    [SerializeField] private Button unequipBtn;
    [SerializeField] private Button cancelBtn;

    [Header("背景遮罩")]
    [SerializeField] private Image bgMask;

    private int _itemId;
    private bool _isEquipped;
    private EquipSlot? _equippedSlot;

    public override void OnCreate(object args)
    {
        if (useBtn != null)
            useBtn.onClick.AddListener(OnUse);
        if (dropBtn != null)
            dropBtn.onClick.AddListener(OnDrop);
        if (equipBtn != null)
            equipBtn.onClick.AddListener(OnEquip);
        if (unequipBtn != null)
            unequipBtn.onClick.AddListener(OnUnequip);
        if (cancelBtn != null)
            cancelBtn.onClick.AddListener(OnCancel);
        if (bgMask != null)
        {
            var maskBtn = bgMask.GetComponent<Button>();
            if (maskBtn != null)
                maskBtn.onClick.AddListener(OnCancel);
        }
    }

    public override void OnOpen(object args)
    {
        if (args is int itemId)
        {
            _itemId = itemId;
            Refresh();
        }
        else
        {
            Clear();
        }
    }

    public override void OnClose()
    {
        _itemId = 0;
    }

    private void Refresh()
    {
        var tables = GetTables();
        if (tables == null) return;

        var itemCfg = tables.TbItem.GetOrDefault(_itemId);
        if (itemCfg == null) return;

        if (itemName != null)
            itemName.text = itemCfg.Name;

        if (itemTip != null)
            itemTip.text = itemCfg.Tip;

        if (itemIcon != null)
        {
            itemIcon.gameObject.SetActive(true);
            itemIcon.sprite = LoadIcon(itemCfg.Icon);
        }

        if (itemCount != null)
        {
            int count = BagManager.Instance.GetItemCount(_itemId);
            itemCount.text = count > 1 ? $"x{count}" : "";
        }

        _isEquipped = false;
        _equippedSlot = null;

        EquipSlot equipSlot = EquipManager.Instance.GetEquipSlotByInstance(0);
        if (equipSlot != (EquipSlot)(-1))
        {
            _isEquipped = true;
            _equippedSlot = equipSlot;
        }

        UpdateButtons(itemCfg);
    }

    private void UpdateButtons(cfg.cfg.item.Item itemCfg)
    {
        if (useBtn != null)
        {
            bool showUse = itemCfg.Type == 3;
            useBtn.gameObject.SetActive(showUse);
        }

        if (equipBtn != null)
        {
            bool showEquip = itemCfg.Type == 4 && !_isEquipped;
            equipBtn.gameObject.SetActive(showEquip);
        }

        if (unequipBtn != null)
        {
            unequipBtn.gameObject.SetActive(_isEquipped);
        }

        if (dropBtn != null)
        {
            bool showDrop = itemCfg.Type != 2 && !_isEquipped;
            dropBtn.gameObject.SetActive(showDrop);
        }
    }

    private void OnUse()
    {
        if (_itemId <= 0) return;
        Debug.Log($"[UseItemTip] 使用物品: {_itemId}");
        BagManager.Instance.RemoveItem(_itemId, 1);

        int remaining = BagManager.Instance.GetItemCount(_itemId);
        if (remaining <= 0)
        {
            CloseSelf();
        }
        else
        {
            Refresh();
        }
    }

    private void OnDrop()
    {
        if (_itemId <= 0) return;
        Debug.Log($"[UseItemTip] 丢弃物品: {_itemId}");
        int count = BagManager.Instance.GetItemCount(_itemId);
        BagManager.Instance.RemoveItem(_itemId, count);
        CloseSelf();
    }

    private void OnEquip()
    {
        if (_itemId <= 0) return;
        bool ok = EquipManager.Instance.EquipByInstance(0);
        if (ok)
        {
            Debug.Log($"[UseItemTip] 装备物品: {_itemId}");
            CloseSelf();
        }
    }

    private void OnUnequip()
    {
        if (!_equippedSlot.HasValue) return;
        bool ok = EquipManager.Instance.Unequip(_equippedSlot.Value);
        if (ok)
        {
            Debug.Log($"[UseItemTip] 卸下装备: {_itemId}");
            CloseSelf();
        }
    }

    private void OnCancel()
    {
        CloseSelf();
    }

    private void CloseSelf()
    {
        var uiMgr = ManagerRegistry.Get<UIManager>();
        if (uiMgr != null)
            uiMgr.Close<UseItemTip>();
    }

    private void Clear()
    {
        if (itemIcon != null)
        {
            itemIcon.sprite = null;
            itemIcon.gameObject.SetActive(false);
        }
        if (itemName != null) itemName.text = "";
        if (itemTip != null) itemTip.text = "";
        if (itemCount != null) itemCount.text = "";

        SetAllButtonsActive(false);
    }

    private void SetAllButtonsActive(bool active)
    {
        if (useBtn != null) useBtn.gameObject.SetActive(active);
        if (dropBtn != null) dropBtn.gameObject.SetActive(active);
        if (equipBtn != null) equipBtn.gameObject.SetActive(active);
        if (unequipBtn != null) unequipBtn.gameObject.SetActive(active);
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
