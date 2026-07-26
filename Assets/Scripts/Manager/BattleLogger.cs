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

    public static void Log(string msg)
    {
        if (Entries.Count >= MaxEntries)
            Entries.RemoveAt(0);
        Entries.Add(msg);
    }

    public static void LogWarning(string msg)
    {
        Log($"[W] {msg}");
    }

    public static void LogError(string msg)
    {
        Log($"[E] {msg}");
    }

    public static void Clear()
    {
        Entries.Clear();
    }
}
