using System;
using System.Collections.Generic;
using TMPro;
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

    [Header("跳字")]
    [SerializeField] private bool enableTypewriter = true;
    [SerializeField] private float typewriterSpeed = 30f;

    private readonly List<BubbleTintController> _bubbleTints = new List<BubbleTintController>();
    private readonly List<RaycastResult> _raycastResults = new List<RaycastResult>();
    private PointerEventData _pointerEventData;
    private ScrollRect _scrollRect;
    private RectTransform _scrollViewRect;
    private bool _blockAdvance;
    private bool _optionChosen;
    private RectTransform _lastRightBubbleBeforeOption;
    private bool _pendingCollapse;
    private TypewriterEffect _currentTypewriter;

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

    private DialogManager _dialogManager;

    private void OnEnable()
    {
        _dialogManager = ManagerRegistry.Get<DialogManager>();
        if (_dialogManager == null) return;

        _dialogManager.OnContent += OnContent;
        _dialogManager.OnOptions += OnOptions;
        _dialogManager.OnDialogEnded += OnDialogEnded;
    }

    private void OnDisable()
    {
        if (_dialogManager == null) return;

        _dialogManager.OnContent -= OnContent;
        _dialogManager.OnOptions -= OnOptions;
        _dialogManager.OnDialogEnded -= OnDialogEnded;
    }

    private void Update()
    {
        if (_dialogManager == null) return;
        if (_dialogManager.State != DialogState.Playing) return;

        if (_blockAdvance)
        {
            _blockAdvance = false;
            return;
        }

        if (!IsClickOrTapBegan()) return;
        if (IsPointerOverButtonOrSelectable()) return;
        if (!IsPointerOverScrollView()) return;

        if (enableTypewriter && _currentTypewriter != null && _currentTypewriter.IsTyping)
        {
            _currentTypewriter.Skip();
            return;
        }

        _dialogManager.Advance();
    }

    private void OnOptions(List<OptionInfo> options)
    {
        if (_currentTypewriter != null && _currentTypewriter.IsTyping)
            _currentTypewriter.Skip();
        _currentTypewriter = null;

        if (_pendingCollapse)
        {
            _pendingCollapse = false;
            _lastRightBubbleBeforeOption = null;
        }

        _optionChosen = false;

        if (_bubbleTints.Count > 0)
        {
            var latest = _bubbleTints[_bubbleTints.Count - 1];
            if (latest != null && latest.GetComponent<ChatBubbleRightView>() != null)
                _lastRightBubbleBeforeOption = latest.GetComponent<RectTransform>();
        }

        var opBubble = Instantiate(optionBubblePrefab, chatContent);

        var texts = new string[options.Count];
        var extends = new string[options.Count];
        for (int i = 0; i < options.Count; i++)
        {
            texts[i] = options[i].Text;
            extends[i] = options[i].FirstContentType == 0 ? "narrator" : "";
        }

        opBubble.Bind(texts, extends, index =>
        {
            if (_optionChosen) return;
            _optionChosen = true;
            _blockAdvance = true;

            if (_lastRightBubbleBeforeOption != null && !IsOptionAction(index))
                _pendingCollapse = true;

            RemoveFromTints(opBubble.gameObject);
            DestroyImmediate(opBubble.gameObject);

            LayoutRebuilder.ForceRebuildLayoutImmediate(chatContent);
            ScrollToBottom();

            _dialogManager.ChooseOption(index);
        });

        LayoutRebuilder.ForceRebuildLayoutImmediate(chatContent);
        ScrollToBottom();
    }

    private void OnContent(SpeakerSide side, string text, string speakerName, string gainItemText)
    {
        if (_pendingCollapse && _lastRightBubbleBeforeOption != null)
        {
            _pendingCollapse = false;
            CollapseBubble(_lastRightBubbleBeforeOption);
            _lastRightBubbleBeforeOption = null;
        }

        TMP_Text mainText = null;
        GameObject bubbleRoot;
        switch (side)
        {
            case SpeakerSide.Left:
                var leftBubble = Instantiate(leftBubblePrefab, chatContent);
                leftBubble.SetText(text, gainItemText);
                mainText = leftBubble.MainText;
                bubbleRoot = leftBubble.gameObject;
                break;
            case SpeakerSide.Right:
                var rightBubble = Instantiate(rightBubblePrefab, chatContent);
                rightBubble.SetText(text, gainItemText);
                mainText = rightBubble.MainText;
                bubbleRoot = rightBubble.gameObject;
                break;
            default:
                var middleBubble = Instantiate(middleBubblePrefab, chatContent);
                middleBubble.SetText(text);
                mainText = middleBubble.MainText;
                bubbleRoot = middleBubble.gameObject;
                break;
        }

        if (enableTypewriter && mainText != null)
        {
            var tw = bubbleRoot.AddComponent<TypewriterEffect>();
            tw.Play(mainText, typewriterSpeed);
            _currentTypewriter = tw;
        }

        MarkAsLatest(bubbleRoot);
        LayoutRebuilder.ForceRebuildLayoutImmediate(bubbleRoot.GetComponent<RectTransform>());
        LayoutRebuilder.ForceRebuildLayoutImmediate(chatContent);
        ScrollToBottom();
    }

    private void OnDialogEnded()
    {
        ClearAllBubbles();
        var uiManager = ManagerRegistry.Get<UIManager>();
        if (uiManager != null && uiManager.IsOpen<DialogWindow>())
            uiManager.Close<DialogWindow>();
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
        _currentTypewriter = null;
        for (int i = chatContent.childCount - 1; i >= 0; i--)
            Destroy(chatContent.GetChild(i).gameObject);
        _bubbleTints.Clear();
        _lastRightBubbleBeforeOption = null;
        _pendingCollapse = false;
    }

    private bool IsOptionAction(int optionIndex)
    {
        var current = _dialogManager?.CurrentDialog;
        if (current == null || string.IsNullOrEmpty(current.Param2)) return false;
        var parts = current.Param2.Split('|');
        return optionIndex < parts.Length && parts[optionIndex].Trim() == "2";
    }

    private void CollapseBubble(RectTransform bubble)
    {
        if (bubble == null) return;

        var csf = bubble.GetComponent<ContentSizeFitter>();
        if (csf != null) csf.enabled = false;

        float currentHeight = bubble.rect.height;
        float newHeight = Mathf.Max(currentHeight - 70f, 0f);
        bubble.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, newHeight);

        var le = bubble.GetComponent<LayoutElement>();
        if (le == null) le = bubble.gameObject.AddComponent<LayoutElement>();
        le.preferredHeight = newHeight;

        LayoutRebuilder.ForceRebuildLayoutImmediate(chatContent);
    }

    private void RemoveFromTints(GameObject go)
    {
        for (int i = _bubbleTints.Count - 1; i >= 0; i--)
        {
            if (_bubbleTints[i] == null || _bubbleTints[i].gameObject == go)
                _bubbleTints.RemoveAt(i);
        }
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
