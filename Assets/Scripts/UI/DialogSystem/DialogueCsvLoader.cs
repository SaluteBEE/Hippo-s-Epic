using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public static class DialogueCsvLoader
{
    public static Dictionary<int, DialogueRow> Load(TextAsset csv)
    {
        if (csv == null) throw new ArgumentNullException(nameof(csv));

        var lines = csv.text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length <= 1) return new Dictionary<int, DialogueRow>();

        // 解析表头
        var header = SplitCsvLine(lines[0].TrimStart('\uFEFF'));
        var col = BuildHeaderIndex(header);

        int Idx(string name) => col.TryGetValue(name, out var i) ? i : -1;
        string Get(string[] cols, string name)
        {
            int i = Idx(name);
            return (i >= 0 && i < cols.Length) ? cols[i].Trim() : "";
        }

        var dict = new Dictionary<int, DialogueRow>();

        for (int i = 1; i < lines.Length; i++)
        {
            var cols = SplitCsvLine(lines[i]);
            if (!int.TryParse(Get(cols, "对话ID"), out var id)) continue;

            var typeStr = Get(cols, "对话类型");
            if (!Enum.TryParse(typeStr, out DialogueType type))
                continue;

            var row = new DialogueRow
            {
                Id = id,
                Type = type,

                // 新增：对话任务id（列名按你要求）
                DialogueTaskId = TryParseInt(Get(cols, "对话任务id")),

                Speaker = Get(cols, "说话人"),
                Text = Get(cols, "对话文本"),
                Text2 = Get(cols, "对话文本2"),
                Condition = Get(cols, "条件"),
                Jump = TryParseInt(Get(cols, "跳转")),
                Emotion = Get(cols, "表情"),
                Background = Get(cols, "背景变化"),
                GainItem = Get(cols, "获得道具"),
            };

            dict[id] = row;
        }

        return dict;
    }

    private static int? TryParseInt(string s) => int.TryParse(s, out var v) ? v : (int?)null;

    private static Dictionary<string, int> BuildHeaderIndex(string[] header)
    {
        var map = new Dictionary<string, int>();
        for (int i = 0; i < header.Length; i++)
        {
            var key = header[i].Trim();
            if (!string.IsNullOrEmpty(key) && !map.ContainsKey(key))
                map[key] = i;
        }
        return map;
    }

    // 支持引号与逗号内容
    private static string[] SplitCsvLine(string line)
    {
        var result = new List<string>();
        var sb = new StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];

            if (c == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    sb.Append('"');
                    i++;
                }
                else inQuotes = !inQuotes;
            }
            else if (c == ',' && !inQuotes)
            {
                result.Add(sb.ToString());
                sb.Clear();
            }
            else sb.Append(c);
        }

        result.Add(sb.ToString());
        return result.ToArray();
    }
}
