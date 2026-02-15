using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public static class DialogueCsvLoader
{
    public static Dictionary<int, DialogueRow> Load(TextAsset csv)
    {
        if (csv == null) throw new ArgumentNullException(nameof(csv));

        var dict = new Dictionary<int, DialogueRow>();
        var lines = csv.text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

        // 第 0 行是表头：标志,ID,人物,位置,内容,跳转,效果,目标
        for (int i = 1; i < lines.Length; i++)
        {
            var line = lines[i];

            // 处理 UTF-8 BOM（只可能出现在文件开头，但稳妥起见）
            if (i == 1) line = line.TrimStart('\uFEFF');

            var cols = SplitCsvLine(line);
            if (cols.Length < 2) continue;

            string flagStr = GetCol(cols, 0);
            if (string.IsNullOrWhiteSpace(flagStr)) continue;

            if (!int.TryParse(GetCol(cols, 1), out int id)) continue;

            var row = new DialogueRow
            {
                Flag = flagStr.Trim()[0],
                Id = id,
                Character = GetCol(cols, 2),
                Position  = GetCol(cols, 3),
                Content   = GetCol(cols, 4),
                Jump      = TryParseInt(GetCol(cols, 5)),
                Effect    = GetCol(cols, 6),
                Target    = GetCol(cols, 7),
            };

            dict[id] = row;
        }

        return dict;
    }

    private static int? TryParseInt(string s)
        => int.TryParse(s, out var v) ? v : (int?)null;

    private static string GetCol(string[] cols, int index)
        => index >= 0 && index < cols.Length ? cols[index].Trim() : "";

    // 简单但可靠的 CSV 行解析（支持引号与逗号内容）
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
                // 处理 "" 转义为 "
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    sb.Append('"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (c == ',' && !inQuotes)
            {
                result.Add(sb.ToString());
                sb.Clear();
            }
            else
            {
                sb.Append(c);
            }
        }

        result.Add(sb.ToString());
        return result.ToArray();
    }
}
