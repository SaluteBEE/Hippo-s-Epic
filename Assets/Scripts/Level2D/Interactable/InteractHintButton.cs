using UnityEngine;
using TMPro;

public class InteractHintButton : MonoBehaviour
{
    [SerializeField] private TMP_Text buttonText;

    public void Setup(string text, int index)
    {
        if (buttonText != null)
            buttonText.text = text;

        var btn = GetComponent<UnityEngine.UI.Button>();
        if (btn != null)
        {
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() =>
            {
                var hint = GetComponentInParent<InteractHint>();
                if (hint != null)
                    hint.OnButtonClicked(index);
            });
        }
    }
}
