using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace QuestSystem
{
    public class TaskDatabase
    {
        public readonly Dictionary<int, TaskDefinition> Tasks = new();

        /// <summary>
        /// 从 StreamingAssets 读取 tasks.csv
        /// </summary>
        public void LoadFromStreamingAssets(string fileName = "tasks.csv")
        {
            string path = Path.Combine(Application.streamingAssetsPath, fileName);

            string csvText;
#if UNITY_ANDROID && !UNITY_EDITOR
            // Android StreamingAssets 需要用 UnityWebRequest，简单起见给出同步替代方案可自行改为协程
            var www = new UnityEngine.Networking.UnityWebRequest(path);
            www.downloadHandler = new UnityEngine.Networking.DownloadHandlerBuffer();
            www.SendWebRequest();
            while (!www.isDone) { }
            if (www.result != UnityEngine.Networking.UnityWebRequest.Result.Success)
                throw new Exception($"Failed to load CSV: {www.error}");
            csvText = www.downloadHandler.text;
#else
            csvText = File.ReadAllText(path);
#endif
            LoadFromText(csvText);
        }

        public void LoadFromText(string csvText)
        {
            Tasks.Clear();

            var rows = CsvUtil.Parse(csvText);
            if (rows.Count <= 1) return;

            // header
            var header = rows[0];
            int Col(string name)
            {
                for (int i = 0; i < header.Length; i++)
                    if (string.Equals(header[i], name, StringComparison.OrdinalIgnoreCase))
                        return i;
                return -1;
            }

            int cId = Col("任务ID");
            int cType = Col("条件类型");
            int cP1 = Col("参数1");
            int cP2 = Col("参数2");
            int cP3 = Col("参数3");
            int cTitle = Col("任务标题");
            int cDesc = Col("任务文本描述");

            for (int r = 1; r < rows.Count; r++)
            {
                var row = rows[r];
                int GetInt(int idx)
                {
                    if (idx < 0 || idx >= row.Length) return 0;
                    return int.TryParse(row[idx], out var v) ? v : 0;
                }
                string GetStr(int idx)
                {
                    if (idx < 0 || idx >= row.Length) return string.Empty;
                    return row[idx] ?? string.Empty;
                }

                int id = GetInt(cId);
                if (id <= 0) continue;

                if (!Tasks.TryGetValue(id, out var def))
                {
                    def = new TaskDefinition
                    {
                        TaskId = id,
                        Title = GetStr(cTitle),
                        Description = GetStr(cDesc),
                    };
                    Tasks.Add(id, def);
                }
                else
                {
                    // 同 ID 多行：标题描述以第一行为准，也可在此决定覆盖策略
                }

                var cfg = new TaskConditionConfig
                {
                    Type = (ConditionType)GetInt(cType),
                    P1 = GetInt(cP1),
                    P2 = GetInt(cP2),
                    P3 = GetInt(cP3),
                };
                def.Conditions.Add(cfg);
            }
        }
    }
}