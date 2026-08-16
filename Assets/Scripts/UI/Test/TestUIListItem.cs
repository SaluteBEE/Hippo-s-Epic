using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 测试 UI 的列表行 / 网格格子条目,含图标、名称、数量、选中态。
/// </summary>
public class TestUIListItem : MonoBehaviour
{
    [SerializeField] private Image icon;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI countText;
    [SerializeField] private Image selectedBg;
    [SerializeField] private Button button;

    public void SetData(int index, string itemName, int count)
    {
        if (nameText != null) nameText.text = itemName;
        if (countText != null) countText.text = count > 1 ? $"x{count}" : "";
        if (icon != null)
        {
            // 演示:按 index 给图标调色,区分条目(项目内可直接换 IconLoader)
            icon.color = Color.HSVToRGB((index * 0.13f) % 1f, 0.6f, 0.9f);
        }
    }

    public void SetSelected(bool selected)
    {
        if (selectedBg != null) selectedBg.gameObject.SetActive(selected);
    }

    public void SetClickHandler(System.Action onClick)
    {
        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => onClick?.Invoke());
        }
    }
}
