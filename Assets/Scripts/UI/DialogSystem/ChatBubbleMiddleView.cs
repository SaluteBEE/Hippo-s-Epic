using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class ChatBubbleMiddleView : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private TMP_Text text;              // 气泡内文字

    public void SetText(string message)
    {
        text.text = message;
    }
}