using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class ChatBubbleLeftView : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private TMP_Text text;
    [SerializeField] private RectTransform bubbleBgRect;

    [Header("Gain Item Box")]
    [SerializeField] private GameObject itemBoxRoot;
    [SerializeField] private RectTransform itemBoxRect;
    [SerializeField] private TMP_Text itemText;

    private const float MinBgWidth = 300f;
    private const float ItemExtraHeight = 50f;
    private float _maxTextWidth;
    private float _maxBgWidth;
    private VerticalLayoutGroup _bgLayout;
    private int _bgOriginalBottomPadding;

    public void SetText(string message, string gainItemText = null)
    {
        CacheOriginalWidths();

        bool hasMain = !string.IsNullOrWhiteSpace(message);
        text.gameObject.SetActive(hasMain);
        if (hasMain) text.text = message;

        bool hasItem = !string.IsNullOrWhiteSpace(gainItemText);
        itemBoxRoot.SetActive(hasItem);
        if (hasItem) itemText.text = gainItemText;

        _bgLayout.padding.bottom = _bgOriginalBottomPadding + (hasItem ? (int)ItemExtraHeight : 0);

        if (hasMain)
        {
            text.ForceMeshUpdate();
            float pw = text.preferredWidth;
            float bgMargin = _maxBgWidth - _maxTextWidth;
            float desiredBg = Mathf.Clamp(pw + bgMargin, MinBgWidth, _maxBgWidth);
            float desiredText = desiredBg - bgMargin;

            text.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, desiredText);
            bubbleBgRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, desiredBg);
        }
        else
        {
            text.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, _maxTextWidth);
            bubbleBgRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, _maxBgWidth);
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)transform);
        PositionItemBox();
    }

    private void PositionItemBox()
    {
        if (!itemBoxRoot.activeSelf) return;
        float bgRight = bubbleBgRect.anchoredPosition.x + bubbleBgRect.sizeDelta.x / 2f - 50;
        float bgBottom = bubbleBgRect.sizeDelta.y - itemBoxRect.sizeDelta.y;
        itemBoxRect.anchoredPosition = new Vector2(bgRight, bgBottom);
    }

    private void CacheOriginalWidths()
    {
        if (_maxTextWidth > 0) return;
        _maxTextWidth = text.rectTransform.sizeDelta.x;
        _maxBgWidth = bubbleBgRect.sizeDelta.x;
        _bgLayout = bubbleBgRect.GetComponent<VerticalLayoutGroup>();
        _bgOriginalBottomPadding = _bgLayout.padding.bottom;
    }
}