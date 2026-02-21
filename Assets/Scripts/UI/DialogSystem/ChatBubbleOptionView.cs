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
    

    /// <summary>
    /// 设置三条文本，并绑定点击回调（回调参数为 0/1/2）
    /// </summary>
    public void Bind(string text1,string extend1, string text2, string extend2, string text3, string extend3,Action<int> onChoose)
    {
        if (extend1 == "")
        {
            op1.SetActive(true);
            op1e.SetActive(false);
        }
        else
        {
            op1.SetActive(false);
            op1e.SetActive(true);
        }
        
        if (extend2 == "")
        {
            op2.SetActive(true);
            op2e.SetActive(false);
        }
        else
        {
            op2.SetActive(false);
            op2e.SetActive(true);
        }
        
        if (extend3 == "")
        {
            op3.SetActive(true);
            op3e.SetActive(false);
        }
        else
        {
            op3.SetActive(false);
            op3e.SetActive(true);
        }
        op1Text.text = text1;
        op2Text.text = text2;
        op3Text.text = text3;
        op1eText.text = text1;
        op2eText.text = text2;
        op3eText.text = text3;
        
        LayoutRebuilder.ForceRebuildLayoutImmediate(op1.GetComponent<RectTransform>());
        LayoutRebuilder.ForceRebuildLayoutImmediate(op2.GetComponent<RectTransform>());
        LayoutRebuilder.ForceRebuildLayoutImmediate(op3.GetComponent<RectTransform>());
        
        LayoutRebuilder.ForceRebuildLayoutImmediate(op1e.GetComponent<RectTransform>());
        LayoutRebuilder.ForceRebuildLayoutImmediate(op2e.GetComponent<RectTransform>());
        LayoutRebuilder.ForceRebuildLayoutImmediate(op3e.GetComponent<RectTransform>());
        

        // 避免重复绑定（非常关键：否则多次生成会叠加监听）
        op1Button.onClick.RemoveAllListeners();
        op2Button.onClick.RemoveAllListeners();
        op3Button.onClick.RemoveAllListeners();
        
        op1eButton.onClick.RemoveAllListeners();
        op2eButton.onClick.RemoveAllListeners();
        op3eButton.onClick.RemoveAllListeners();

        op1Button.onClick.AddListener(() => onChoose?.Invoke(0));
        op2Button.onClick.AddListener(() => onChoose?.Invoke(1));
        op3Button.onClick.AddListener(() => onChoose?.Invoke(2));
        
        op1eButton.onClick.AddListener(() => onChoose?.Invoke(0));
        op2eButton.onClick.AddListener(() => onChoose?.Invoke(1));
        op3eButton.onClick.AddListener(() => onChoose?.Invoke(2));
    }
}