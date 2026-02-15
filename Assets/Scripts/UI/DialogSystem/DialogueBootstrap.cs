using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class DialogueBootstrap : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private TextAsset dialogueCsv;

    [Header("UI")]
    [SerializeField] private RectTransform chatContent;
    [SerializeField] private ChatBubbleLeftView leftBubbleLeftPrefab;
    [SerializeField] private ChatBubbleRightView rightBubbleLeftPrefab;
    [SerializeField] private ChatBubbleOptionView chatBubbleOptionPrefab;

    private DialogueView _view;

    private readonly List<RaycastResult> _raycastResults = new List<RaycastResult>();
    private PointerEventData _pointerEventData;

    private void Start()
    {
        _view = new DialogueView(chatContent, leftBubbleLeftPrefab, rightBubbleLeftPrefab, chatBubbleOptionPrefab);
        _view.Bind();

        _view.VM.Initialize(dialogueCsv, startId: 0);
        _view.VM.Advance();
    }

    private void Update()
    {
        if (!IsClickOrTapBegan()) return;

        // 只在“点到按钮/可交互控件”时不推进，避免点选项时同时推进
        if (IsPointerOverButtonOrSelectable()) return;

        _view.VM.Advance();
    }

    private bool IsClickOrTapBegan()
    {
        if (Input.GetMouseButtonDown(0)) return true;

        if (Input.touchCount > 0)
        {
            var t = Input.GetTouch(0);
            return t.phase == TouchPhase.Began;
        }
        return false;
    }

    private bool IsPointerOverButtonOrSelectable()
    {
        if (EventSystem.current == null) return false;

        // 构造 PointerEventData（鼠标位置）
        if (_pointerEventData == null)
            _pointerEventData = new PointerEventData(EventSystem.current);

        _pointerEventData.position = Input.mousePosition;

        _raycastResults.Clear();
        EventSystem.current.RaycastAll(_pointerEventData, _raycastResults);

        // 只要射线命中任意 Button/Selectable，就认为是“点在交互控件上”
        for (int i = 0; i < _raycastResults.Count; i++)
        {
            var go = _raycastResults[i].gameObject;
            if (go == null) continue;

            if (go.GetComponentInParent<Button>() != null) return true;
            if (go.GetComponentInParent<Selectable>() != null) return true; // 兼容 TMP Button、Toggle 等
        }

        return false;
    }

    private void OnDestroy()
    {
        _view?.Dispose();
    }
}
