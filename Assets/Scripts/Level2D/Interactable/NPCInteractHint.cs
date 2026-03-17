using UnityEngine;
using TMPro;

public class NPCInteractHint : MonoBehaviour
{
    [SerializeField] private GameObject root;
    [SerializeField] private TMP_Text textLabel;
    [SerializeField] private GameObject ButtonSprite;
    [SerializeField] private TMP_Text ButtonTextLabel;

    private void Awake()
    {
        Hide();
    }

    public void Show(string text,string buttonText)
    {
        if (textLabel != null)
            textLabel.text = text;
        if (ButtonTextLabel != null)
        {
            if (buttonText == "")
            {
                ButtonSprite.SetActive(false);
                ButtonTextLabel.text = "";
            }
            else
            {
                ButtonSprite.SetActive(true);
                ButtonTextLabel.text = buttonText;
            }
        }
            

        if (root != null)
            root.SetActive(true);
        else
            gameObject.SetActive(true);
    }

    public void SetText(string text)
    {
        if (textLabel != null)
            textLabel.text = text;
    }

    public void Hide()
    {
        if (root != null)
            root.SetActive(false);
        else
            gameObject.SetActive(false);
    }
}