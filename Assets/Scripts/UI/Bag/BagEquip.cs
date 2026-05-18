using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class BagEquip : MonoBehaviour, IDropHandler
{
    [SerializeField] private GameObject selectedBg;
    [SerializeField] private TextMeshProUGUI title;
    [SerializeField] private TextMeshProUGUI tip;
    [SerializeField] private Image icon;
    [SerializeField] private Button btnEquipSlot;
    [SerializeField] private Button btnEquipIcon;
    [SerializeField] private Button _button;

    public EquipSlot Slot { get; private set; }
    public System.Action<BagEquip> onClick;
    public System.Action<BagEquip, BagItem> onDropItem;
    public System.Action<BagEquip> onBtnEquipClicked;
    public System.Action<BagEquip> onBtnUnequipClicked;

    

    private void Awake()
    {
        if (_button != null)
            _button.onClick.AddListener(OnClick);
        if (btnEquipSlot != null)
            btnEquipSlot.onClick.AddListener(OnBtnEquipClicked);
        if (btnEquipIcon != null)
            btnEquipIcon.onClick.AddListener(OnBtnUnequipClicked);
    }

    public void Setup(EquipSlot slot, int? equippedItemId)
    {
        Slot = slot;

        if (equippedItemId.HasValue && equippedItemId.Value > 0)
        {
            var itemCfg = GetItemConfig(equippedItemId.Value);
            if (itemCfg != null)
            {
                if (title != null)
                {
                    title.gameObject.SetActive(true);
                    title.text = itemCfg.Name;
                }
                if (tip != null)
                {
                    tip.gameObject.SetActive(true);
                    tip.text = itemCfg.Tip;
                }
                if (icon != null)
                {
                    icon.gameObject.SetActive(true);
                    icon.sprite = LoadIcon(itemCfg.Icon);
                }
            }
        }
        else
        {
            Clear();
            SetEmptyDesc(slot);
        }

        SetSelected(false);
        SetHighlight(false);
    }

    public void Clear()
    {
        if (title != null)
        {
            title.gameObject.SetActive(false);
            title.text = "";
        }
        if (tip != null)
        {
            tip.gameObject.SetActive(false);
            tip.text = "";
        }
        if (icon != null)
        {
            icon.gameObject.SetActive(false);
            icon.sprite = null;
        }
    }

    private void SetEmptyDesc(EquipSlot slot)
    {
        string titleText = "";
        string tipText = "";

        switch (slot)
        {
            case EquipSlot.Head:
                titleText = UIStrings.Bag.EmptyDesc.HeadTitle;
                tipText = UIStrings.Bag.EmptyDesc.HeadTip;
                break;
            case EquipSlot.Body:
                titleText = UIStrings.Bag.EmptyDesc.BodyTitle;
                tipText = UIStrings.Bag.EmptyDesc.BodyTip;
                break;
            case EquipSlot.Hand:
                titleText = UIStrings.Bag.EmptyDesc.HandTitle;
                tipText = UIStrings.Bag.EmptyDesc.HandTip;
                break;
            case EquipSlot.Foot:
                titleText = UIStrings.Bag.EmptyDesc.FootTitle;
                tipText = UIStrings.Bag.EmptyDesc.FootTip;
                break;
        }

        if (title != null && !string.IsNullOrEmpty(titleText))
        {
            title.gameObject.SetActive(true);
            title.text = titleText;
        }
        if (tip != null && !string.IsNullOrEmpty(tipText))
        {
            tip.gameObject.SetActive(true);
            tip.text = tipText;
        }
    }

    public void SetSelected(bool selected)
    {
        if (selectedBg != null)
            selectedBg.SetActive(selected);
    }

    public void SetHighlight(bool active)
    {
        if (selectedBg != null)
            selectedBg.SetActive(active);
    }

    public void OnDrop(PointerEventData eventData)
    {
        var dragItem = eventData.pointerDrag;
        if (dragItem == null) return;

        var bagItem = dragItem.GetComponent<BagItem>();
        if (bagItem != null)
            onDropItem?.Invoke(this, bagItem);
    }

    private void OnClick()
    {
        onClick?.Invoke(this);
    }

    private void OnBtnEquipClicked()
    {
        onBtnEquipClicked?.Invoke(this);
    }

    private void OnBtnUnequipClicked()
    {
        onBtnUnequipClicked?.Invoke(this);
    }

    private cfg.cfg.item.Item GetItemConfig(int itemId)
    {
        if (ManagerRegistry.TryGet<DataTableManager>(out var dtm) && dtm.Tables != null)
            return dtm.Tables.TbItem.GetOrDefault(itemId);
        return null;
    }

    private Sprite LoadIcon(int iconId)
    {
        return IconLoader.LoadItemIcon(iconId);
    }
}
