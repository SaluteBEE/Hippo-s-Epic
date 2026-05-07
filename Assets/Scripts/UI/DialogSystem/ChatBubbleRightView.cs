using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class ChatBubbleRightView : MonoBehaviour
{
    [SerializeField] private TMP_Text mainText;
    [SerializeField] private RectTransform bubbleBgRect;

    [Header("Gain Item Box")]
    [SerializeField] private GameObject itemBoxRoot;
    [SerializeField] private TMP_Text itemText;

    public TMP_Text MainText => mainText;

    private const float MinBgWidth = 300f;
    private const float ItemExtraHeight = 50f;
    private float _maxTextWidth;
    private float _maxBgWidth;
    private VerticalLayoutGroup _bgLayout;
    private int _bgOriginalBottomPadding;

    public void SetText(string text, string gainItemText = null)
    {
        CacheOriginalWidths();

        bool hasMain = !string.IsNullOrWhiteSpace(text);
        mainText.gameObject.SetActive(hasMain);
        if (hasMain) mainText.text = text;

        bool hasItem = !string.IsNullOrWhiteSpace(gainItemText);
        itemBoxRoot.SetActive(hasItem);
        if (hasItem) itemText.text = gainItemText;

        _bgLayout.padding.bottom = _bgOriginalBottomPadding + (hasItem ? (int)ItemExtraHeight : 0);

        if (hasMain)
        {
            mainText.ForceMeshUpdate();
            float pw = mainText.preferredWidth;
            float bgMargin = _maxBgWidth - _maxTextWidth;
            float desiredBg = Mathf.Clamp(pw + bgMargin, MinBgWidth, _maxBgWidth);
            float desiredText = desiredBg - bgMargin;

            mainText.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, desiredText);
            bubbleBgRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, desiredBg);
        }
        else
        {
            mainText.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, _maxTextWidth);
            bubbleBgRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, _maxBgWidth);
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)transform);
    }

    private void CacheOriginalWidths()
    {
        if (_maxTextWidth > 0) return;
        _maxTextWidth = mainText.rectTransform.sizeDelta.x;
        _maxBgWidth = bubbleBgRect.sizeDelta.x;
        _bgLayout = bubbleBgRect.GetComponent<VerticalLayoutGroup>();
        _bgOriginalBottomPadding = _bgLayout.padding.bottom;
    }
}
