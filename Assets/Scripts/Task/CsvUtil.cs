using System.Collections.Generic;
using System.Text;

namespace QuestSystem
{
    public static class CsvUtil
    {
        /// <summary>
        /// 简单 CSV 解析：支持逗号分隔与双引号包裹字段（字段内可含逗号）。
        /// </summary>
        public static List<string[]> Parse(string csvText)
        {
            var rows = new List<string[]>();
            var current = new List<string>();
            var sb = new StringBuilder();
            bool inQuotes = false;

            for (int i = 0; i < csvText.Length; i++)
            {
                char c = csvText[i];

                if (c == '\"')
                {
                    // 双引号转义："" -> "
                    if (inQuotes && i + 1 < csvText.Length && csvText[i + 1] == '\"')
                    {
                        sb.Append('\"');
                        i++;
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                }
                else if (c == ',' && !inQuotes)
                {
                    current.Add(sb.ToString().Trim());
                    sb.Clear();
                }
                else if ((c == '\n' || c == '\r') && !inQuotes)
                {
                    // 处理 \r\n
                    if (c == '\r' && i + 1 < csvText.Length && csvText[i + 1] == '\n') i++;

                    current.Add(sb.ToString().Trim());
                    sb.Clear();

                    // 跳过空行
                    bool allEmpty = true;
                    for (int k = 0; k < current.Count; k++)
                        if (!string.IsNullOrEmpty(current[k])) { allEmpty = false; break; }

                    if (!allEmpty)
                        rows.Add(current.ToArray());

                    current.Clear();
                }
                else
                {
                    sb.Append(c);
                }
            }

            // 最后一行
            if (sb.Length > 0 || current.Count > 0)
            {
                current.Add(sb.ToString().Trim());
                bool allEmpty = true;
                for (int k = 0; k < current.Count; k++)
                    if (!string.IsNullOrEmpty(current[k])) { allEmpty = false; break; }

                if (!allEmpty)
                    rows.Add(current.ToArray());
            }

            return rows;
        }
    }
}