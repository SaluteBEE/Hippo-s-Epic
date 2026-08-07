using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 战斗日志缓冲器。所有战斗系统日志写入此处而非 Console。
/// 通过 GUI 的 ScrollView 展示。
/// </summary>
public static class BattleLogger
{
    public static List<string> Entries = new List<string>();
    public const int MaxEntries = 500;

    /// <summary>
    /// 记录普通日志（带时间戳，保留最近 MaxEntries 条）
    /// </summary>
    public static void Log(string msg)
    {
        if (Entries.Count >= MaxEntries)
            Entries.RemoveAt(0);
        Entries.Add(msg);
    }

    /// <summary>
    /// 记录警告日志
    /// </summary>
    public static void LogWarning(string msg)
    {
        Log($"[W] {msg}");
    }

    /// <summary>
    /// 记录错误日志
    /// </summary>
    public static void LogError(string msg)
    {
        Log($"[E] {msg}");
    }

    /// <summary>
    /// 清空全部日志
    /// </summary>
    public static void Clear()
    {
        Entries.Clear();
    }
}
