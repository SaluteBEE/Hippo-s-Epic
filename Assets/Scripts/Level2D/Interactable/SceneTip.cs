using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// 物品获得提示框（SceneTip）—— 按物品 ID 从物品表取数据填充：
///   HintText    &lt;- item.Gethint     （获取方式）
///   tip_content &lt;- item.Tip         （物品详情，已有字段）
///   ButtonText  &lt;- item.Getexclaim  （获取感叹，按钮上的文字）
///   icon        &lt;- item.Icon        （IconLoader.LoadItemIcon）
///   btn_1       =  确认按钮
/// 用法：实例化后调用 Show(itemId[, onConfirm])；确认按钮点击后自动隐藏并回调。
/// </summary>
public class SceneTip : MonoBehaviour
{
    [Header("物品信息")]
    [SerializeField] private Image itemIcon;
    [SerializeField] private TextMeshProUGUI hintText;
    [SerializeField] private TextMeshProUGUI tipText;
    [SerializeField] private TextMeshProUGUI exclaimText;

    [Header("操作按钮")]
    [SerializeField] private Button confirmButton;

    [Header("框体（需带 VerticalLayoutGroup + ContentSizeFitter）")]
    [SerializeField] private GameObject boxRoot;

    [Header("悬浮才显示的详情")]
    [SerializeField] private RectTransform hoverArea;   // icon 节点
    [SerializeField] private GameObject detailBox;      // left_detail 节点

    private Action _onConfirm;
    private ContentSizeFitter _boxFitter;
    private VerticalLayoutGroup _boxLayout;
    private bool _cached;
    private InputAction _confirmAction;
    private PointerEventData _pointerData;
    private int _pendingRebuildFrames;

    private void Awake()
    {
        CacheLayout();
        Hide();
    }

    private void CacheLayout()
    {
        Transform t = boxRoot != null ? boxRoot.transform : transform;
        _boxFitter = t.GetComponent<ContentSizeFitter>();
        _boxLayout = t.GetComponent<VerticalLayoutGroup>();
        _cached = true;
    }

    /// <summary>按物品 ID 显示提示。onConfirm 为确认按钮回调，可为 null。</summary>
    public bool Show(int itemId, Action onConfirm = null)
    {
        cfg.Tables tables = GetTables();
        if (tables == null)
        {
            Debug.LogWarning("[SceneTip] Tables 未就绪，无法显示");
            return false;
        }

        cfg.cfg.item.Item itemCfg = tables.TbItem.GetOrDefault(itemId);
        if (itemCfg == null)
        {
            Debug.LogWarning("[SceneTip] 物品表中不存在 id=" + itemId);
            return false;
        }

        if (!_cached) CacheLayout();

        _onConfirm = onConfirm;
        EnsureConfirmAction();

        if (hintText != null) hintText.text = itemCfg.Gethint ?? string.Empty;
        if (tipText != null) tipText.text = itemCfg.Tip ?? string.Empty;
        if (exclaimText != null) exclaimText.text = itemCfg.Getexclaim ?? string.Empty;

        if (itemIcon != null)
        {
            Sprite sp = IconLoader.LoadItemIcon(itemCfg.Icon);
            itemIcon.sprite = sp;
            itemIcon.gameObject.SetActive(sp != null);
        }

        if (confirmButton != null)
        {
            confirmButton.onClick.RemoveAllListeners();
            confirmButton.onClick.AddListener(OnConfirmClicked);
        }

        SetDetailVisible(false);
        if (boxRoot != null) boxRoot.SetActive(true);
        gameObject.SetActive(true);
        ForceRebuildLayout();
        _pendingRebuildFrames = 1;   // 下一帧（激活后）再重建一次
        return true;
    }

    public void Hide()
    {
        if (confirmButton != null) confirmButton.onClick.RemoveAllListeners();
        _onConfirm = null;
        if (boxRoot != null) boxRoot.SetActive(false);
        gameObject.SetActive(false);
    }

    private void Update()
    {
        if (boxRoot != null ? !boxRoot.activeSelf : !gameObject.activeSelf)
        {
            SetDetailVisible(false);
            return;
        }

        if (_pendingRebuildFrames > 0)
        {
            _pendingRebuildFrames--;
            if (_pendingRebuildFrames == 0) ForceRebuildLayout();
        }

        UpdateDetailHover();

        if (_confirmAction != null)
        {
            if (_confirmAction.WasPressedThisFrame())
                OnConfirmClicked();
            return;
        }

        // 兜底：取不到游戏交互动作时直接看键盘（E / 回车 / 空格）
        Keyboard kb = Keyboard.current;
        if (kb != null && (kb.eKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame))
            OnConfirmClicked();
    }

    private void UpdateDetailHover()
    {
        if (detailBox == null || hoverArea == null) return;
        SetDetailVisible(IsPointerOverIcon());
    }

    private void SetDetailVisible(bool visible)
    {
        if (detailBox == null) return;
        if (detailBox.activeSelf == visible) return;

        detailBox.SetActive(visible);

        // 强制刷新它自己 + 它后面（下面）节点的布局，别等下一帧
        var dRect = detailBox.transform as RectTransform;
        if (dRect != null)
        {
            LayoutRebuilder.MarkLayoutForRebuild(dRect);
            LayoutRebuilder.ForceRebuildLayoutImmediate(dRect);
        }

        ForceRebuildLayout();
        _pendingRebuildFrames = 1;   // 下一帧（激活后）再重建一次
    }

    private bool IsPointerOverIcon()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null || hoverArea == null) return false;

        Canvas cv = GetComponent<Canvas>();
        Camera cam = cv != null && cv.worldCamera != null ? cv.worldCamera : Camera.main;
        if (cam == null) return false;

        // 屏幕点 → 射线 → 与 icon 所在平面求交 → 换算到 icon 本地判断是否在矩形内
        // （正交/透视相机、有无缩放旋转都成立；不走 EventSystem，所以不受全屏 UI 遮挡影响）
        Vector2 screen = mouse.position.ReadValue();
        Ray ray = cam.ScreenPointToRay(new Vector3(screen.x, screen.y, 0f));
        Plane plane = new Plane(hoverArea.rotation * Vector3.forward, hoverArea.position);
        float dist;
        if (!plane.Raycast(ray, out dist)) return false;

        Vector3 local = hoverArea.InverseTransformPoint(ray.GetPoint(dist));
        return hoverArea.rect.Contains(new Vector2(local.x, local.y));
    }

    private void EnsureConfirmAction()
    {
        if (_confirmAction != null) return;

        InputManager inputManager = ManagerRegistry.Get<InputManager>();
        if (inputManager != null && inputManager.GameInput != null)
            _confirmAction = inputManager.GameInput.Player.Interact;
    }

    private void OnConfirmClicked()
    {
        Action cb = _onConfirm;
        Hide();
        if (cb != null) cb();
    }

    private void ForceRebuildLayout()
    {
        RefreshTextMetrics(hintText);
        RefreshTextMetrics(tipText);
        RefreshTextMetrics(exclaimText);
        Canvas.ForceUpdateCanvases();

        RebuildFitter(hintText);
        RebuildFitter(tipText);
        RebuildFitter(exclaimText);

        if (_boxLayout != null)
        {
            _boxLayout.CalculateLayoutInputHorizontal();
            _boxLayout.CalculateLayoutInputVertical();
            _boxLayout.SetLayoutHorizontal();
            _boxLayout.SetLayoutVertical();
        }

        if (_boxFitter != null)
        {
            _boxFitter.SetLayoutHorizontal();
            _boxFitter.SetLayoutVertical();
        }

        RectTransform rect = boxRoot != null ? boxRoot.GetComponent<RectTransform>() : GetComponent<RectTransform>();
        if (rect != null) LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
    }

    private static void RefreshTextMetrics(TextMeshProUGUI label)
    {
        if (label == null) return;
        label.ForceMeshUpdate();
        label.SetLayoutDirty();
    }

    private static void RebuildFitter(TextMeshProUGUI label)
    {
        if (label == null) return;
        ContentSizeFitter fitter = label.GetComponent<ContentSizeFitter>();
        if (fitter == null) return;
        fitter.SetLayoutHorizontal();
        fitter.SetLayoutVertical();
    }

    private static cfg.Tables GetTables()
    {
        if (ManagerRegistry.TryGet<DataTableManager>(out DataTableManager dtm) && dtm.Tables != null)
            return dtm.Tables;
        return null;
    }
}
