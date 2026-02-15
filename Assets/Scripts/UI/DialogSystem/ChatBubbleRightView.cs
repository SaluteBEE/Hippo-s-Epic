using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class ChatBubbleRightView : MonoBehaviour
{
    [SerializeField] private TMP_Text mainText;

    [Header("Gain Item Box")]
    [SerializeField] private GameObject itemBoxRoot;
    [SerializeField] private TMP_Text itemText;

    public void SetText(string text, string gainItemText = null)
    {
        bool hasMain = !string.IsNullOrWhiteSpace(text);
        mainText.gameObject.SetActive(hasMain);
        if (hasMain) mainText.text = text;

        bool hasItem = !string.IsNullOrWhiteSpace(gainItemText);
        itemBoxRoot.SetActive(hasItem);
        if (hasItem) itemText.text = gainItemText;

        LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)transform);
    }
}