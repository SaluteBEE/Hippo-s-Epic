using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class InteractHintButton : MonoBehaviour
{
    [SerializeField] private TMP_Text buttonText;
    [SerializeField] private Color normalColor = new Color(0.1f, 0.1f, 0.1f, 1f);
    [SerializeField] private Color disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);

    public void Setup(string text, int index, bool interactable = true)
    {
        if (buttonText != null)
            buttonText.text = text;

        var btn = GetComponent<Button>();
        if (btn != null)
        {
            btn.interactable = interactable;
            btn.onClick.RemoveAllListeners();
            if (interactable)
            {
                btn.onClick.AddListener(() =>
                {
                    var hint = GetComponentInParent<InteractHint>();
                    if (hint != null)
                        hint.OnButtonClicked(index);
                });
            }
        }

        if (buttonText != null)
            buttonText.color = interactable ? normalColor : disabledColor;
    }
}
