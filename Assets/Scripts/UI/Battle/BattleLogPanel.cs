using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 战斗记录面板：显示最近N条战斗事件，红色高亮伤害数值
/// </summary>
public class BattleLogPanel : MonoBehaviour
{
    [Header("配置")]
    [Tooltip("最大显示条数")]
    [SerializeField] private int maxLogs = 3;

    [Tooltip("日志文本模板")]
    [SerializeField] private TextMeshProUGUI logTemplate;

    [Tooltip("日志容器")]
    [SerializeField] private Transform logContainer;

    [Header("颜色")]
    [SerializeField] private Color damageColor = Color.red;
    [SerializeField] private Color healColor = Color.green;
    [SerializeField] private Color normalColor = Color.black;

    private readonly List<TextMeshProUGUI> _logEntries = new List<TextMeshProUGUI>();
    private readonly Queue<string> _pendingLogs = new Queue<string>();

    private void Awake()
    {
        if (logContainer == null) logContainer = transform;
        if (logTemplate != null) logTemplate.gameObject.SetActive(false);
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
            entry.color = color;
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

        // 超出最大条数时移除最旧的
        while (_logEntries.Count > maxLogs)
        {
            var old = _logEntries[0];
            _logEntries.RemoveAt(0);
            if (old != null) Destroy(old.gameObject);
        }

        // 重新排列（最新在底部）
        for (int i = 0; i < _logEntries.Count; i++)
        {
            if (_logEntries[i] == null) continue;
            var rect = _logEntries[i].GetComponent<RectTransform>();
            if (rect != null)
            {
                float y = -(_logEntries.Count - 1 - i) * 32f; // 从底部向上排列
                rect.anchoredPosition = new Vector2(0, y);
            }
        }
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
