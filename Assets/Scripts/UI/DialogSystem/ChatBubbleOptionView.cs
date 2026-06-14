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

    [SerializeField] private Color disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
    private Color _normalColor = Color.white;

    public void Bind(string[] texts, string[] extends, bool[] interactables, Action<int> onChoose)
    {
        if (op1Text != null) _normalColor = op1Text.color;

        SetupSlot(0, op1, op1e, op1Button, op1eButton, op1Text, op1eText, texts, extends, interactables);
        SetupSlot(1, op2, op2e, op2Button, op2eButton, op2Text, op2eText, texts, extends, interactables);
        SetupSlot(2, op3, op3e, op3Button, op3eButton, op3Text, op3eText, texts, extends, interactables);

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
            if (op1.activeInHierarchy)
                op1Button.onClick.AddListener(() => onChoose?.Invoke(0));
            else if (op1e.activeInHierarchy)
                op1eButton.onClick.AddListener(() => onChoose?.Invoke(0));
        }

        if (texts.Length > 1 && !string.IsNullOrEmpty(texts[1]))
        {
            if (op2.activeInHierarchy)
                op2Button.onClick.AddListener(() => onChoose?.Invoke(1));
            else if (op2e.activeInHierarchy)
                op2eButton.onClick.AddListener(() => onChoose?.Invoke(1));
        }

        if (texts.Length > 2 && !string.IsNullOrEmpty(texts[2]))
        {
            if (op3.activeInHierarchy)
                op3Button.onClick.AddListener(() => onChoose?.Invoke(2));
            else if (op3e.activeInHierarchy)
                op3eButton.onClick.AddListener(() => onChoose?.Invoke(2));
        }

        ApplyHalfHeight();
    }

    private void ApplyHalfHeight()
    {
        var rectTransform = (RectTransform)transform;
        LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);

        var fitter = GetComponent<ContentSizeFitter>();
        if (fitter != null && fitter.enabled)
        {
            float fullHeight = rectTransform.rect.height;
            fitter.enabled = false;

            float halfHeight = fullHeight / 2f;
            rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, halfHeight);

            var layoutElement = GetComponent<LayoutElement>();
            if (layoutElement == null)
                layoutElement = gameObject.AddComponent<LayoutElement>();
            layoutElement.preferredHeight = halfHeight;
        }
    }

    private void SetupSlot(int index,
        GameObject normalObj, GameObject extendObj,
        Button normalBtn, Button extendBtn,
        TMP_Text normalTxt, TMP_Text extendTxt,
        string[] texts, string[] extends, bool[] interactables)
    {
        bool hasOption = index < texts.Length && !string.IsNullOrEmpty(texts[index]);
        string text = hasOption ? texts[index] : "";
        string ext = hasOption && index < extends.Length ? extends[index] : "";
        bool interactable = hasOption && index < interactables.Length && interactables[index];

        normalObj.SetActive(hasOption && string.IsNullOrEmpty(ext));
        extendObj.SetActive(hasOption && !string.IsNullOrEmpty(ext));

        if (hasOption)
        {
            normalTxt.text = text;
            extendTxt.text = text;

            normalBtn.interactable = interactable;
            extendBtn.interactable = interactable;

            Color c = interactable ? _normalColor : disabledColor;
            normalTxt.color = c;
            extendTxt.color = c;
        }
        else
        {
            normalObj.SetActive(false);
            extendObj.SetActive(false);
        }
    }
}
