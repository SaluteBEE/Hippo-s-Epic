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

    private void OnOptions(List<OptionInfo> options)
    {
        var opBubble = Instantiate(optionBubblePrefab, chatContent);

        var texts = new string[options.Count];
        var extends = new string[options.Count];
        for (int i = 0; i < options.Count; i++)
        {
            texts[i] = options[i].Text;
            extends[i] = "";
        }

        opBubble.Bind(texts, extends, index =>
        {
            opBubble.gameObject.SetActive(false);
            RemoveFromTints(opBubble.gameObject);
            Destroy(opBubble.gameObject);
            DialogManager.Instance.ChooseOption(index);
        });

        LayoutRebuilder.ForceRebuildLayoutImmediate(chatContent);
        ScrollToBottom();
    }

    private void OnContent(SpeakerSide side, string text, string speakerName)
    {
        GameObject bubbleRoot;
        switch (side)
        {
            case SpeakerSide.Left:
                var leftBubble = Instantiate(leftBubblePrefab, chatContent);
                leftBubble.SetText(text);
                bubbleRoot = leftBubble.gameObject;
                break;
            case SpeakerSide.Right:
                var rightBubble = Instantiate(rightBubblePrefab, chatContent);
                rightBubble.SetText(text);
                bubbleRoot = rightBubble.gameObject;
                break;
            default:
                var middleBubble = Instantiate(middleBubblePrefab, chatContent);
                middleBubble.SetText(text);
                bubbleRoot = middleBubble.gameObject;
                break;
        }

        MarkAsLatest(bubbleRoot);
        LayoutRebuilder.ForceRebuildLayoutImmediate(bubbleRoot.GetComponent<RectTransform>());
        LayoutRebuilder.ForceRebuildLayoutImmediate(chatContent);
        ScrollToBottom();
    }

    private void OnDialogEnded()
    {
        ClearAllBubbles();
        DialogCharacterManager.Instance?.CleanupDynamicCharacters();
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
