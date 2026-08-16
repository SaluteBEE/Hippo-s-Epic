using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 战斗记录面板：ScrollView 显示，保留全部历史，红色高亮伤害数值，最新日志在底部并自动滚动可见
/// </summary>
public class BattleLogPanel : MonoBehaviour
{
    [Header("配置")]
    [Tooltip("日志文本模板")]
    [SerializeField] private TextMeshProUGUI logTemplate;

    [Tooltip("日志容器（留空则自动创建 ScrollRect 结构）")]
    [SerializeField] private Transform logContainer;

    [Header("颜色")]
    [SerializeField] private Color damageColor = Color.red;
    [SerializeField] private Color healColor = Color.green;
    [SerializeField] private Color normalColor = Color.black;

    private readonly List<TextMeshProUGUI> _logEntries = new List<TextMeshProUGUI>();
    private ScrollRect _scrollRect;

    private void Awake()
    {
        SetupScrollView();
        if (logTemplate != null) logTemplate.gameObject.SetActive(false);
    }

    /// <summary>
    /// 动态创建 ScrollRect（Viewport + Content），让日志可滚动并保留历史
    /// </summary>
    private void SetupScrollView()
    {
        _scrollRect = GetComponent<ScrollRect>();
        if (_scrollRect == null) _scrollRect = gameObject.AddComponent<ScrollRect>();

        // Viewport：拉伸填满面板，带 RectMask2D 裁剪
        var viewportGo = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
        viewportGo.transform.SetParent(transform, false);
        var viewportRect = viewportGo.GetComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.offsetMin = Vector2.zero;
        viewportRect.offsetMax = Vector2.zero;

        // Content：顶部对齐，高度由 VerticalLayoutGroup + ContentSizeFitter 自适应
        var contentGo = new GameObject("Content", typeof(RectTransform));
        contentGo.transform.SetParent(viewportGo.transform, false);
        var contentRect = contentGo.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0, 1);
        contentRect.anchorMax = new Vector2(1, 1);
        contentRect.pivot = new Vector2(0.5f, 1);
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.sizeDelta = new Vector2(0, 0);

        var layout = contentGo.AddComponent<VerticalLayoutGroup>();
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.spacing = 2f;

        var fitter = contentGo.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        _scrollRect.viewport = viewportRect;
        _scrollRect.content = contentRect;
        _scrollRect.horizontal = false;
        _scrollRect.vertical = true;
        _scrollRect.movementType = ScrollRect.MovementType.Clamped;
        _scrollRect.scrollSensitivity = 20f;

        logContainer = contentRect;
    }

    /// <summary>
    /// 添加一条战斗记录
    /// </summary>
    public void AddLog(string message)
    {
        AddLogInternal(message, normalColor);
    }

    /// <summary>
    /// 添加伤害记录（红色高亮数值）
    /// </summary>
    public void AddDamageLog(string attackerName, string targetName, int damage, string skillName = null)
    {
        string msg;
        if (!string.IsNullOrEmpty(skillName))
            msg = $"{attackerName} 使用 {skillName} 对 {targetName} 造成 <color=#{ColorToHex(damageColor)}>{damage}点伤害</color>";
        else
            msg = $"{attackerName} 对 {targetName} 造成 <color=#{ColorToHex(damageColor)}>{damage}点伤害</color>";
        AddLogInternal(msg, normalColor);
    }

    /// <summary>
    /// 添加治疗记录
    /// </summary>
    public void AddHealLog(string healerName, string targetName, int amount)
    {
        string msg = $"{healerName} 治疗 {targetName} <color=#{ColorToHex(healColor)}>{amount}点生命</color>";
        AddLogInternal(msg, normalColor);
    }

    /// <summary>
    /// 添加阵亡记录
    /// </summary>
    public void AddDeathLog(string unitName)
    {
        AddLogInternal($"<color=#{ColorToHex(damageColor)}>{unitName} 倒下了！</color>", normalColor);
    }

    /// <summary>
    /// 添加回合开始记录
    /// </summary>
    public void AddRoundLog(int round)
    {
        AddLogInternal($"—— 第{round}轮 ——", new Color(0.5f, 0.5f, 0.5f));
    }

    private void AddLogInternal(string message, Color color)
    {
        if (logContainer == null) return;

        // 创建新条目
        TextMeshProUGUI entry;
        if (logTemplate != null)
        {
            var go = Instantiate(logTemplate.gameObject, logContainer);
            go.SetActive(true);
            entry = go.GetComponent<TextMeshProUGUI>();
        }
        else
        {
            var go = new GameObject($"Log_{_logEntries.Count}");
            go.transform.SetParent(logContainer, false);
            entry = go.AddComponent<TextMeshProUGUI>();
            entry.fontSize = 14;
            entry.alignment = TextAlignmentOptions.Left;
            entry.richText = true;
            entry.enableWordWrapping = true;

            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(1, 1);
            rect.pivot = new Vector2(0.5f, 1);
            rect.sizeDelta = new Vector2(0, 30);
        }

        entry.text = message;
        entry.color = color;
        _logEntries.Add(entry);

        // 滚到底部，让最新日志可见（VerticalLayoutGroup 从上往下排，最新在最下）
        ScrollToBottom();
    }

    private void ScrollToBottom()
    {
        StartCoroutine(ScrollToBottomRoutine());
    }

    private System.Collections.IEnumerator ScrollToBottomRoutine()
    {
        yield return null; // 等一帧让布局生效
        if (_scrollRect == null) yield break;
        Canvas.ForceUpdateCanvases();
        _scrollRect.verticalNormalizedPosition = 0f; // 0=底部
    }

    /// <summary>
    /// 清空所有记录
    /// </summary>
    public void ClearLogs()
    {
        foreach (var entry in _logEntries)
        {
            if (entry != null) Destroy(entry.gameObject);
        }
        _logEntries.Clear();
    }

    private string ColorToHex(Color color)
    {
        return $"{(int)(color.r * 255):X2}{(int)(color.g * 255):X2}{(int)(color.b * 255):X2}";
    }
}
