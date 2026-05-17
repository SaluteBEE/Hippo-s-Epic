using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class BagItem : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private GameObject selectedBg;
    [SerializeField] private TextMeshProUGUI num;
    [SerializeField] private Image icon;

    public int InstanceId { get; private set; }
    public int ItemId { get; private set; }
    public System.Action<BagItem> onClick;
    public System.Action<BagItem> onRightClick;

    private RectTransform _rt;

    private void Awake()
    {
        _rt = GetComponent<RectTransform>();
    }

    public void Setup(int instanceId, int itemId, int count, cfg.cfg.item.Item itemCfg)
    {
        InstanceId = instanceId;
        ItemId = itemId;

        if (icon != null)
        {
            if (itemCfg != null && itemCfg.Icon > 0)
            {
                icon.gameObject.SetActive(true);
                icon.sprite = LoadIcon(itemCfg.Icon);
            }
            else
            {
                icon.gameObject.SetActive(true);
                icon.sprite = null;
            }
        }

        if (num != null)
        {
            if (count > 1)
            {
                num.gameObject.SetActive(true);
                num.text = count.ToString();
            }
            else
            {
                num.gameObject.SetActive(false);
            }
        }

        if (_rt != null)
        {
            _rt.anchorMin = Vector2.zero;
            _rt.anchorMax = Vector2.zero;
            _rt.pivot = new Vector2(0.5f, 0.5f);
        }

        SetSelected(false);
    }

    public void SetSelected(bool selected)
    {
        if (selectedBg != null)
            selectedBg.SetActive(selected);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right)
            onRightClick?.Invoke(this);
        else
            onClick?.Invoke(this);
    }

    private Sprite LoadIcon(int iconId)
    {
        return IconLoader.LoadItemIcon(iconId);
    }
}
