using System;
using Aesthete.MVVM.Views;
using UnityEngine;
using UnityEngine.UI;

public sealed class DialogueView : View
{
    public DialogueViewModel VM { get; }

    private readonly RectTransform _content;
    private readonly ChatBubbleLeftView leftBubblePrefab;
    private readonly ChatBubbleRightView rightBubblePrefab;
    private readonly ChatBubbleOptionView bubbleOptionPrefab;

    private Action<ChatMessage> _onMsgHandler;
    private Action<OptionMessage> _onOpMsgHandler;

    public DialogueView(
        RectTransform content,
        ChatBubbleLeftView leftBubblePrefab,
        ChatBubbleRightView rightBubblePrefab,
        ChatBubbleOptionView optionPrefab)
    {
        _content = content;
        this.leftBubblePrefab = leftBubblePrefab;
        this.rightBubblePrefab = rightBubblePrefab;
        this.bubbleOptionPrefab = optionPrefab;

        VM = new DialogueViewModel();
    }

    public override void Bind()
    {
        _onMsgHandler = OnMessagePushed;
        VM.OnMessagePushed.Subscribe(_onMsgHandler);

        _onOpMsgHandler = OnOpMessagePushed;
        VM.OnOpMessagePushed.Subscribe(_onOpMsgHandler);

        VM.Bind();
    }

    public override void Unbind()
    {
        if (_onMsgHandler != null)
            VM.OnMessagePushed.Unsubscribe(_onMsgHandler);

        if (_onOpMsgHandler != null)
            VM.OnOpMessagePushed.Unsubscribe(_onOpMsgHandler);

        VM.Unbind();
    }



    public override void Dispose()
    {
        Unbind();
        VM.Dispose();
    }

    private void OnMessagePushed(ChatMessage msg)
    {
        if (msg.Side == SpeakerSide.Left)
        {
            var bubble = UnityEngine.Object.Instantiate(leftBubblePrefab, _content);
            bubble.SetText(msg.Text);
            LayoutRebuilder.ForceRebuildLayoutImmediate(bubble.GetComponent<RectTransform>());
            
        }
        else if (msg.Side == SpeakerSide.Right)
        {
            var bubble = UnityEngine.Object.Instantiate(rightBubblePrefab, _content);
            bubble.SetText(msg.Text, msg.GainItemText);
            LayoutRebuilder.ForceRebuildLayoutImmediate(bubble.GetComponent<RectTransform>());
        }
        LayoutRebuilder.ForceRebuildLayoutImmediate(_content);
    }

    private void OnOpMessagePushed(OptionMessage msg)
    {
        var opBubble = UnityEngine.Object.Instantiate(bubbleOptionPrefab, _content);

        opBubble.Bind(msg.Text1, msg.Text2, msg.Text3, index =>
        {
            VM.ChooseOption(index);
            UnityEngine.Object.Destroy(opBubble.gameObject);
        });
        LayoutRebuilder.ForceRebuildLayoutImmediate(_content);
    }
}
