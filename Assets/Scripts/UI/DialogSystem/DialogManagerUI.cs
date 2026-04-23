using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DialogManagerUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private RectTransform chatContent;
    [SerializeField] private ChatBubbleLeftView leftBubblePrefab;
    [SerializeField] private ChatBubbleMiddleView middleBubblePrefab;
    [SerializeField] private ChatBubbleRightView rightBubblePrefab;
    [SerializeField] private ChatBubbleOptionView optionBubblePrefab;

    private readonly List<BubbleTintController> _bubbleTints = new List<BubbleTintController>();
    private readonly List<RaycastResult> _raycastResults = new List<RaycastResult>();
    private PointerEventData _pointerEventData;
    private ScrollRect _scrollRect;
    private RectTransform _scrollViewRect;

    public void Initialize(
        RectTransform contentRect,
        ChatBubbleLeftView leftPrefab,
        ChatBubbleMiddleView middlePrefab,
        ChatBubbleRightView rightPrefab,
        ChatBubbleOptionView optionPrefab)
    {
        chatContent = contentRect;
        leftBubblePrefab = leftPrefab;
        middleBubblePrefab = middlePrefab;
        rightBubblePrefab = rightPrefab;
        optionBubblePrefab = optionPrefab;

        _scrollRect = contentRect.GetComponentInParent<ScrollRect>();
        if (_scrollRect != null)
            _scrollViewRect = _scrollRect.GetComponent<RectTransform>();
    }

    private void OnEnable()
    {
        var mgr = DialogManager.Instance;
        if (mgr == null) return;

        mgr.OnContent += OnContent;
        mgr.OnOptions += OnOptions;
        mgr.OnDialogEnded += OnDialogEnded;
    }

    private void OnDisable()
    {
        var mgr = DialogManager.Instance;
        if (mgr == null) return;

        mgr.OnContent -= OnContent;
        mgr.OnOptions -= OnOptions;
        mgr.OnDialogEnded -= OnDialogEnded;
    }

    private void Update()
    {
        if (DialogManager.Instance == null) return;
        if (DialogManager.Instance.State != DialogState.Playing) return;
        if (!IsClickOrTapBegan()) return;
        if (IsPointerOverButtonOrSelectable()) return;
        if (!IsPointerOverScrollView()) return;

        DialogManager.Instance.Advance();
    }

    private void OnContent(SpeakerSide side, string text, string speakerName)
    {
        switch (side)
        {
            case SpeakerSide.Left:
            {
                var bubble = Instantiate(leftBubblePrefab, chatContent);
                bubble.SetText(text);
                MarkAsLatest(bubble.gameObject);
                LayoutRebuilder.ForceRebuildLayoutImmediate(bubble.GetComponent<RectTransform>());
                break;
            }
            case SpeakerSide.Middle:
            {
                var bubble = Instantiate(middleBubblePrefab, chatContent);
                bubble.SetText(text);
                MarkAsLatest(bubble.gameObject);
                LayoutRebuilder.ForceRebuildLayoutImmediate(bubble.GetComponent<RectTransform>());
                break;
            }
            case SpeakerSide.Right:
            {
                var bubble = Instantiate(rightBubblePrefab, chatContent);
                bubble.SetText(text);
                MarkAsLatest(bubble.gameObject);
                LayoutRebuilder.ForceRebuildLayoutImmediate(bubble.GetComponent<RectTransform>());
                break;
            }
        }
        LayoutRebuilder.ForceRebuildLayoutImmediate(chatContent);
        ScrollToBottom();
    }

    private void OnOptions(List<OptionInfo> options)
    {
        var opBubble = Instantiate(optionBubblePrefab, chatContent);

        string t1 = options.Count > 0 ? options[0].Text : "";
        string t2 = options.Count > 1 ? options[1].Text : "";
        string t3 = options.Count > 2 ? options[2].Text : "";

        opBubble.Bind(t1, "", t2, "", t3, "", index =>
        {
            DialogManager.Instance.ChooseOption(index);
            Destroy(opBubble.gameObject);
        });

        MarkAsLatest(opBubble.gameObject);
        LayoutRebuilder.ForceRebuildLayoutImmediate(chatContent);
        ScrollToBottom();
    }

    private void OnDialogEnded()
    {
        ClearAllBubbles();
    }

    private void MarkAsLatest(GameObject bubbleRoot)
    {
        for (int i = _bubbleTints.Count - 1; i >= 0; i--)
        {
            if (_bubbleTints[i] == null)
                _bubbleTints.RemoveAt(i);
        }

        var tint = bubbleRoot.GetComponent<BubbleTintController>();
        if (tint == null)
            tint = bubbleRoot.AddComponent<BubbleTintController>();

        for (int i = 0; i < _bubbleTints.Count; i++)
            _bubbleTints[i].SetDimmed(true);

        tint.SetDimmed(false);

        if (!_bubbleTints.Contains(tint))
            _bubbleTints.Add(tint);
    }

    private void ClearAllBubbles()
    {
        for (int i = chatContent.childCount - 1; i >= 0; i--)
            Destroy(chatContent.GetChild(i).gameObject);
        _bubbleTints.Clear();
    }

    private void ScrollToBottom()
    {
        if (_scrollRect != null)
            _scrollRect.verticalNormalizedPosition = 0f;
    }

    private bool IsClickOrTapBegan()
    {
        if (Input.GetMouseButtonDown(0)) return true;
        if (Input.touchCount > 0) return Input.GetTouch(0).phase == TouchPhase.Began;
        return false;
    }

    private bool IsPointerOverButtonOrSelectable()
    {
        if (EventSystem.current == null) return false;

        if (_pointerEventData == null)
            _pointerEventData = new PointerEventData(EventSystem.current);

        _pointerEventData.position = Input.mousePosition;

        _raycastResults.Clear();
        EventSystem.current.RaycastAll(_pointerEventData, _raycastResults);

        for (int i = 0; i < _raycastResults.Count; i++)
        {
            var go = _raycastResults[i].gameObject;
            if (go == null) continue;
            if (go.GetComponentInParent<Button>() != null) return true;
            if (go.GetComponentInParent<Selectable>() != null) return true;
        }
        return false;
    }

    private bool IsPointerOverScrollView()
    {
        if (_scrollViewRect == null) return false;
        return RectTransformUtility.RectangleContainsScreenPoint(
            _scrollViewRect, Input.mousePosition, null);
    }
}
