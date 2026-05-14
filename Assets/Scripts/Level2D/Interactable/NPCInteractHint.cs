using System;
using UnityEngine;
using TMPro;

public class InteractHint : MonoBehaviour
{
    [SerializeField] private GameObject root;
    [SerializeField] private TMP_Text textLabel;
    [SerializeField] private InteractHintButton[] buttons;

    private Action<int> onButtonClicked;

    private void Awake()
    {
        Hide();
    }

    public void Show(string text, string[] buttonTexts, Action<int> callback)
    {
        if (textLabel != null)
            textLabel.text = text;

        onButtonClicked = callback;

        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] == null) continue;

            bool active = i < buttonTexts.Length && !string.IsNullOrEmpty(buttonTexts[i]);
            buttons[i].gameObject.SetActive(active);
            if (active)
                buttons[i].Setup(buttonTexts[i], i);
        }

        if (root != null)
            root.SetActive(true);
        else
            gameObject.SetActive(true);
    }

    public void OnButtonClicked(int index)
    {
        onButtonClicked?.Invoke(index);
    }

    public void Hide()
    {
        if (root != null)
            root.SetActive(false);
        else
            gameObject.SetActive(false);
    }
}
