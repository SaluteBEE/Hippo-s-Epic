using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BagEquip : MonoBehaviour
{
    [SerializeField] private GameObject selectedBg;
    [SerializeField] private TextMeshProUGUI title;
    [SerializeField] private TextMeshProUGUI tip;
    [SerializeField] private Image icon;

    public EquipSlot Slot { get; private set; }
    public System.Action<BagEquip> onClick;

    private Button _button;

    private void Awake()
    {
        _button = GetComponent<Button>();
        if (_button != null)
            _button.onClick.AddListener(OnClick);
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
        }

        SetSelected(false);
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

    public void SetSelected(bool selected)
    {
        if (selectedBg != null)
            selectedBg.SetActive(selected);
    }

    private void OnClick()
    {
        onClick?.Invoke(this);
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
