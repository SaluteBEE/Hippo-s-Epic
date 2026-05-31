using System;
using UnityEngine;
using TMPro;

public class InteractHint : MonoBehaviour
{
    [SerializeField] private GameObject root;
    [SerializeField] private GameObject btnRoot;
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

        bool anyActive = false;
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] == null) continue;

            bool active = i < buttonTexts.Length && !string.IsNullOrEmpty(buttonTexts[i]);
            buttons[i].gameObject.SetActive(active);
            if (active)
            {
                buttons[i].Setup(buttonTexts[i], i);
                anyActive = true;
            }
        }

        if (!anyActive)
        {
            if (btnRoot != null)
                btnRoot.SetActive(false);

            if (root != null)
                root.SetActive(true);
            else
                gameObject.SetActive(true);
            return;
        }

        if (btnRoot != null)
            btnRoot.SetActive(true);

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
        if (btnRoot != null)
            btnRoot.SetActive(false);

        if (root != null)
            root.SetActive(false);
        else
            gameObject.SetActive(false);
    }
}
