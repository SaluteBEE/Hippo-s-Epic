using System;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class InteractHint : MonoBehaviour
{
    [SerializeField] private GameObject root;
    [SerializeField] private GameObject btnRoot;
    [SerializeField] private TMP_Text textLabel;
    [SerializeField] private InteractHintButton[] buttons;

    private Action<int> onButtonClicked;
    private ContentSizeFitter _borderFitter;
    private VerticalLayoutGroup _borderLayout;

    private void Awake()
    {
        Hide();

        if (textLabel != null)
        {
            var textFitter = textLabel.GetComponent<ContentSizeFitter>();
            if (textFitter != null)
            {
                textFitter.SetLayoutHorizontal();
                textFitter.SetLayoutVertical();
            }
        }

        var border = root != null ? root.transform : transform;
        _borderFitter = border.GetComponent<ContentSizeFitter>();
        _borderLayout = border.GetComponent<VerticalLayoutGroup>();
    }

    public void Show(string text, string[] buttonTexts, bool[] buttonInteractables, Action<int> callback)
    {
        if (textLabel != null)
            textLabel.text = text;

        onButtonClicked = callback;

        bool anyVisible = false;
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] == null) continue;

            bool visible = i < buttonTexts.Length && !string.IsNullOrEmpty(buttonTexts[i]);
            buttons[i].gameObject.SetActive(visible);
            if (visible)
            {
                bool interactable = i < buttonInteractables.Length && buttonInteractables[i];
                buttons[i].Setup(buttonTexts[i], i, interactable);
                anyVisible = true;
            }
        }

        if (!anyVisible)
        {
            if (btnRoot != null)
                btnRoot.SetActive(false);

            if (root != null)
                root.SetActive(true);
            else
                gameObject.SetActive(true);

            ForceRebuildLayout();
            return;
        }

        if (btnRoot != null)
            btnRoot.SetActive(true);

        if (root != null)
            root.SetActive(true);
        else
            gameObject.SetActive(true);

        ForceRebuildLayout();
    }

    private void ForceRebuildLayout()
    {
        if (textLabel != null)
        {
            var textFitter = textLabel.GetComponent<ContentSizeFitter>();
            if (textFitter != null)
            {
                textFitter.SetLayoutHorizontal();
                textFitter.SetLayoutVertical();
            }
        }

        if (_borderLayout != null)
        {
            _borderLayout.CalculateLayoutInputHorizontal();
            _borderLayout.CalculateLayoutInputVertical();
            _borderLayout.SetLayoutHorizontal();
            _borderLayout.SetLayoutVertical();
        }

        if (_borderFitter != null)
        {
            _borderFitter.SetLayoutHorizontal();
            _borderFitter.SetLayoutVertical();
        }

        var rect = root != null ? root.GetComponent<RectTransform>() : GetComponent<RectTransform>();
        if (rect != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
    }

    public void OnButtonClicked(int index)
    {
        onButtonClicked?.Invoke(index);
    }

    public void Hide()
    {
        if (btnRoot != null)
            btnRoot.SetActive(false);

        if (root != null)
            root.SetActive(false);
        else
            gameObject.SetActive(false);
    }
}
