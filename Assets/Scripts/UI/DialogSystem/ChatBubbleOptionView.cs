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

    /// <summary>
    /// 设置三条文本，并绑定点击回调（回调参数为 0/1/2）
    /// </summary>
    public void Bind(string text1, string text2, string text3, Action<int> onChoose)
    {
        op1Text.text = text1;
        op2Text.text = text2;
        op3Text.text = text3;

        // 避免重复绑定（非常关键：否则多次生成会叠加监听）
        op1Button.onClick.RemoveAllListeners();
        op2Button.onClick.RemoveAllListeners();
        op3Button.onClick.RemoveAllListeners();

        op1Button.onClick.AddListener(() => onChoose?.Invoke(0));
        op2Button.onClick.AddListener(() => onChoose?.Invoke(1));
        op3Button.onClick.AddListener(() => onChoose?.Invoke(2));
    }
}