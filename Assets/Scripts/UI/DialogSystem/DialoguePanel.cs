// using System.Collections.Generic;
// using System.Collections.Generic;
// using UnityEngine;
// using UnityEngine.EventSystems;
// using UnityEngine.UI;
//
//
// public sealed class DialoguePanel : UIWindow
// {
//     public override UILayer Layer => UILayer.Popup;
//     // public override bool PushToStack => true;
//     // public override bool CacheOnClose => true; // 推荐缓存，避免频繁 Instantiate
//
//     // [Header("Default Data (optional)")]
//     // [SerializeField] private TextAsset dialogueCsv;
//
//     [Header("UI")]
//     [SerializeField] private RectTransform chatContent;
//     [SerializeField] private ChatBubbleLeftView leftBubbleLeftPrefab;
//     [SerializeField] private ChatBubbleRightView rightBubbleLeftPrefab;
//     [SerializeField] private ChatBubbleMiddleView middleBubblePrefab;
//     [SerializeField] private ChatBubbleOptionView chatBubbleOptionPrefab;
//     [SerializeField] private BackgroundView backgroundView;
//
//     private DialogueView _view;
//
//     private readonly List<RaycastResult> _raycastResults = new List<RaycastResult>();
//     private PointerEventData _pointerEventData;
//
//     private bool _inited;
//
//     public override void OnCreate(object args)
//     {
//         // 只创建一次 View 并绑定
//         if (_inited) return;
//         _inited = true;
//
//         _view = new DialogueView(chatContent, leftBubbleLeftPrefab, middleBubblePrefab,
//             rightBubbleLeftPrefab, chatBubbleOptionPrefab, backgroundView);
//         _view.Bind();
//     }
//
//
//     private void Update()
//     {
//         // 面板隐藏时不处理输入
//         if (!gameObject.activeInHierarchy) return;
//         if (_view == null) return;
//
//         // if (!IsClickOrTapBegan()) return;
//         // if (IsPointerOverButtonOrSelectable()) return;
//
//         _view.VM.Advance();
//     }
//     public override void OnOpen(object args)
//     {
//         var openArgs = args as DialogueOpenArgs;
//         if (openArgs == null || openArgs.Csv == null)
//         {
//             Debug.LogError("[DialoguePanel] DialogueOpenArgs/Csv is null.");
//             return;
//         }
//
//         _view.VM.Initialize(openArgs.Csv, openArgs.StartId);
//
//         if (openArgs.AutoAdvanceFirstLine)
//             _view.VM.Advance();
//     }
//     
//     public override void OnClose()
//     {
//         // 可选：关闭时停止输入推进；如果要保留内容则不清空
//     }
//     
//     // public override void OnDestroyWindow()
//     // {
//     //     _view?.Dispose();
//     //     _view = null;
//     //     _inited = false;
//     }
//
//     private bool IsClickOrTapBegan()
//     {
//         if (Input.GetMouseButtonDown(0)) return true;
//         if (Input.touchCount > 0) return Input.GetTouch(0).phase == TouchPhase.Began;
//         return false;
//     }
//
//     private bool IsPointerOverButtonOrSelectable()
//     {
//         if (EventSystem.current == null) return false;
//
//         if (_pointerEventData == null)
//             _pointerEventData = new PointerEventData(EventSystem.current);
//
//         _pointerEventData.position = Input.mousePosition;
//
//         _raycastResults.Clear();
//         EventSystem.current.RaycastAll(_pointerEventData, _raycastResults);
//
//         for (int i = 0; i < _raycastResults.Count; i++)
//         {
//             var go = _raycastResults[i].gameObject;
//             if (go == null) continue;
//
//             if (go.GetComponentInParent<Button>() != null) return true;
//             if (go.GetComponentInParent<Selectable>() != null) return true;
//         }
//         return false;
//     }
//     // 每次打开都清空历史气泡，可实现这个方法
//     private void ClearAllBubbles()
//     {
//         for (int i = chatContent.childCount - 1; i >= 0; i--)
//             Destroy(chatContent.GetChild(i).gameObject);
//     }
//
//     private void OnDestroy()
//     {
//         _view?.Dispose();
//     }
// }
