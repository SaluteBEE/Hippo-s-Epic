using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class ChatBubbleOptionView : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Button op1Button;
    [SerializeField] private Button op2Button;
    [SerializeField] private Button op3Button;

    [SerializeField] private TMP_Text op1Text;
    [SerializeField] private TMP_Text op2Text;
    [SerializeField] private TMP_Text op3Text;
    
    [SerializeField] private Button op1eButton;
    [SerializeField] private Button op2eButton;
    [SerializeField] private Button op3eButton;

    [SerializeField] private TMP_Text op1eText;
    [SerializeField] private TMP_Text op2eText;
    [SerializeField] private TMP_Text op3eText;

    [SerializeField] private GameObject op1;
    [SerializeField] private GameObject op2;
    [SerializeField] private GameObject op3;
    
    [SerializeField] private GameObject op1e;
    [SerializeField] private GameObject op2e;
    [SerializeField] private GameObject op3e;

    public void Bind(string[] texts, string[] extends, Action<int> onChoose)
    {
        SetupSlot(0, op1, op1e, op1Button, op1eButton, op1Text, op1eText, texts, extends);
        SetupSlot(1, op2, op2e, op2Button, op2eButton, op2Text, op2eText, texts, extends);
        SetupSlot(2, op3, op3e, op3Button, op3eButton, op3Text, op3eText, texts, extends);

        LayoutRebuilder.ForceRebuildLayoutImmediate(op1.GetComponent<RectTransform>());
        LayoutRebuilder.ForceRebuildLayoutImmediate(op2.GetComponent<RectTransform>());
        LayoutRebuilder.ForceRebuildLayoutImmediate(op3.GetComponent<RectTransform>());
        LayoutRebuilder.ForceRebuildLayoutImmediate(op1e.GetComponent<RectTransform>());
        LayoutRebuilder.ForceRebuildLayoutImmediate(op2e.GetComponent<RectTransform>());
        LayoutRebuilder.ForceRebuildLayoutImmediate(op3e.GetComponent<RectTransform>());

        op1Button.onClick.RemoveAllListeners();
        op2Button.onClick.RemoveAllListeners();
        op3Button.onClick.RemoveAllListeners();
        op1eButton.onClick.RemoveAllListeners();
        op2eButton.onClick.RemoveAllListeners();
        op3eButton.onClick.RemoveAllListeners();

        if (texts.Length > 0 && !string.IsNullOrEmpty(texts[0]))
        {
            op1Button.onClick.AddListener(() => onChoose?.Invoke(0));
            op1eButton.onClick.AddListener(() => onChoose?.Invoke(0));
        }

        if (texts.Length > 1 && !string.IsNullOrEmpty(texts[1]))
        {
            op2Button.onClick.AddListener(() => onChoose?.Invoke(1));
            op2eButton.onClick.AddListener(() => onChoose?.Invoke(1));
        }

        if (texts.Length > 2 && !string.IsNullOrEmpty(texts[2]))
        {
            op3Button.onClick.AddListener(() => onChoose?.Invoke(2));
            op3eButton.onClick.AddListener(() => onChoose?.Invoke(2));
        }
    }

    private void SetupSlot(int index,
        GameObject normalObj, GameObject extendObj,
        Button normalBtn, Button extendBtn,
        TMP_Text normalTxt, TMP_Text extendTxt,
        string[] texts, string[] extends)
    {
        bool hasOption = index < texts.Length && !string.IsNullOrEmpty(texts[index]);
        string text = hasOption ? texts[index] : "";
        string ext = hasOption && index < extends.Length ? extends[index] : "";

        normalObj.SetActive(hasOption && string.IsNullOrEmpty(ext));
        extendObj.SetActive(hasOption && !string.IsNullOrEmpty(ext));

        if (hasOption)
        {
            normalTxt.text = text;
            extendTxt.text = text;
        }
        else
        {
            normalObj.SetActive(false);
            extendObj.SetActive(false);
        }
    }
}
