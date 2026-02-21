using System;
using Aesthete.MVVM.Views;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public sealed class DialogueView : View
{
    public DialogueViewModel VM { get; }

    private readonly RectTransform _content;
    private readonly ChatBubbleLeftView leftBubblePrefab;
    private readonly ChatBubbleMiddleView middleBubblePrefab;
    private readonly ChatBubbleRightView rightBubblePrefab;
    private readonly ChatBubbleOptionView bubbleOptionPrefab;
    
    private readonly List<BubbleTintController> _bubbleTints = new List<BubbleTintController>();

    private Action<ChatMessage> _onMsgHandler;
    private Action<OptionMessage> _onOpMsgHandler;
    
    private readonly BackgroundView _backgroundView;
    private System.Action<BackgroundChange> _onBgHandler;

    public DialogueView(RectTransform content,
        ChatBubbleLeftView left,
        ChatBubbleMiddleView middle,
        ChatBubbleRightView right,
        ChatBubbleOptionView option,
        BackgroundView backgroundView)
    {
        _content = content;
        leftBubblePrefab = left;
        middleBubblePrefab = middle;
        rightBubblePrefab = right;
        bubbleOptionPrefab = option;
        _backgroundView = backgroundView;

        VM = new DialogueViewModel();
    }

    public override void Bind()
    {
        _onMsgHandler = OnMessagePushed;
        VM.OnMessagePushed.Subscribe(_onMsgHandler);

        _onOpMsgHandler = OnOpMessagePushed;
        VM.OnOpMessagePushed.Subscribe(_onOpMsgHandler);
        
        _onBgHandler = bg => _backgroundView?.Apply(bg.Name);
        VM.OnBackgroundChanged.Subscribe(_onBgHandler);

        VM.Bind();
    }

    public override void Unbind()
    {
        if (_onMsgHandler != null)
            VM.OnMessagePushed.Unsubscribe(_onMsgHandler);

        if (_onOpMsgHandler != null)
            VM.OnOpMessagePushed.Unsubscribe(_onOpMsgHandler);
        
        if (_onBgHandler != null)
            VM.OnBackgroundChanged.Unsubscribe(_onBgHandler);

        VM.Unbind();
    }



    public override void Dispose()
    {
        Unbind();
        VM.Dispose();
    }

    private void OnMessagePushed(ChatMessage msg)
    {
        if (msg.Side == SpeakerSide.Middle)
        {
            var bubble = UnityEngine.Object.Instantiate(middleBubblePrefab, _content);
            bubble.SetText(msg.Text);
            MarkAsLatest(bubble.gameObject);
            LayoutRebuilder.ForceRebuildLayoutImmediate(bubble.GetComponent<RectTransform>());
            
        }
        else if (msg.Side == SpeakerSide.Left)
        {
            var bubble = UnityEngine.Object.Instantiate(leftBubblePrefab, _content);
            bubble.SetText(msg.Text);
            MarkAsLatest(bubble.gameObject);
            LayoutRebuilder.ForceRebuildLayoutImmediate(bubble.GetComponent<RectTransform>());
            
        }
        else if (msg.Side == SpeakerSide.Right)
        {
            var bubble = UnityEngine.Object.Instantiate(rightBubblePrefab, _content);
            bubble.SetText(msg.Text, msg.GainItemText);
            MarkAsLatest(bubble.gameObject);
            LayoutRebuilder.ForceRebuildLayoutImmediate(bubble.GetComponent<RectTransform>());
        }
        LayoutRebuilder.ForceRebuildLayoutImmediate(_content);
    }

    private void OnOpMessagePushed(OptionMessage msg)
    {
        var opBubble = UnityEngine.Object.Instantiate(bubbleOptionPrefab, _content);

        opBubble.Bind(msg.Text1, msg.Extend1,msg.Text2,msg.Extend2, msg.Text3,msg.Extend3, index =>
        {
            VM.ChooseOption(index);
            UnityEngine.Object.Destroy(opBubble.gameObject);
        });
        MarkAsLatest(opBubble.gameObject);
        LayoutRebuilder.ForceRebuildLayoutImmediate(_content);
    }
    
    private void MarkAsLatest(GameObject bubbleRoot)
    {
        // 清理已经被 Destroy 的引用，避免列表膨胀
        for (int i = _bubbleTints.Count - 1; i >= 0; i--)
        {
            if (_bubbleTints[i] == null) _bubbleTints.RemoveAt(i);
        }

        var tint = bubbleRoot.GetComponent<BubbleTintController>();
        if (tint == null)
        {
            // 强烈建议你在 prefab 上挂好；这里留一个兜底，避免忘挂导致功能失效
            tint = bubbleRoot.AddComponent<BubbleTintController>();
        }

        // 先把全部置灰
        for (int i = 0; i < _bubbleTints.Count; i++)
            _bubbleTints[i].SetDimmed(true);

        // 再把最新恢复原色
        tint.SetDimmed(false);

        // 记录
        if (!_bubbleTints.Contains(tint))
            _bubbleTints.Add(tint);
    }
}
