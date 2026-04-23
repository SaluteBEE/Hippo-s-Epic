using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

public class DialogFlowVerifier
{
    [MenuItem("Tools/验证对话流程")]
    public static void VerifyAll()
    {
        string binDir = Path.Combine(Application.streamingAssetsPath, "Gen", "bin");
        var tables = new cfg.Tables(tableName =>
        {
            string path = Path.Combine(binDir, tableName + ".bytes");
            byte[] bytes = File.ReadAllBytes(path);
            return new Luban.ByteBuf(bytes);
        });

        var sb = new StringBuilder();

        sb.AppendLine("===== 对话A: 线性对话 ID=100 =====");
        Walk(sb, tables, 100);

        sb.AppendLine("\n===== 对话D: 纯旁白 ID=400 =====");
        Walk(sb, tables, 400);

        sb.AppendLine("\n===== 对话B: 循环选项 ID=200 — 先选1(再来一遍)再选2(受够了) =====");
        WalkWithChoices(sb, tables, 200, new[] { 0, 1 });

        sb.AppendLine("\n===== 对话B: 循环选项 ID=200 — 选2(受够了)直接结束 =====");
        WalkWithChoices(sb, tables, 200, new[] { 1 });

        sb.AppendLine("\n===== 对话C: 嵌套 ID=300 — 选1(打听消息)→选1(关于拳王) =====");
        WalkWithChoices(sb, tables, 300, new[] { 0, 0 });

        sb.AppendLine("\n===== 对话C: 嵌套 ID=300 — 选1(打听消息)→选2(关于小镇) =====");
        WalkWithChoices(sb, tables, 300, new[] { 0, 1 });

        sb.AppendLine("\n===== 对话C: 嵌套 ID=300 — 选2(转身离开) =====");
        WalkWithChoices(sb, tables, 300, new[] { 1 });

        sb.AppendLine("\n===== 原始 ID=1 — 选1(跳舞→回选项) =====");
        WalkWithChoices(sb, tables, 1, new[] { 0 });

        sb.AppendLine("\n===== 原始 ID=1 — 选2(一拳打脸→结束) =====");
        WalkWithChoices(sb, tables, 1, new[] { 1 });

        sb.AppendLine("\n===== 原始 ID=1 — 选3(9之后是10→回选项) =====");
        WalkWithChoices(sb, tables, 1, new[] { 2 });

        sb.AppendLine("\n===== 原始 ID=1 — 选1→选2(跳舞后一拳) =====");
        WalkWithChoices(sb, tables, 1, new[] { 0, 1 });

        foreach (var line in sb.ToString().Split('\n'))
            Debug.Log(line.TrimEnd('\r'));
    }

    private static void Walk(StringBuilder sb, cfg.Tables tables, int startId)
    {
        var dialog = tables.TbDialog.GetOrDefault(startId);
        if (dialog == null) { sb.AppendLine($"  !! Dialog {startId} 不存在"); return; }

        int currentId = startId;
        int safety = 50;

        while (currentId > 0 && safety-- > 0)
        {
            dialog = tables.TbDialog.GetOrDefault(currentId);
            if (dialog == null) { sb.AppendLine($"  !! Dialog {currentId} 不存在"); return; }

            sb.AppendLine($"  [Dialog {currentId} Type={dialog.Type}]");

            var contents = tables.TbDialogcontent.DataList
                .Where(c => c.Dialogid == currentId)
                .OrderBy(c => c.Sortid).ToList();

            foreach (var dc in contents)
            {
                string pos = dc.Type == 0 ? "旁白" : dc.Type == 1 ? "左" : "右";
                string speaker = "";
                if (dc.Type == 1 && dialog.Speakerid1_Ref != null) speaker = $"({dialog.Speakerid1_Ref.Name})";
                if (dc.Type == 2 && dialog.Speakerid2_Ref != null) speaker = $"({dialog.Speakerid2_Ref.Name})";
                sb.AppendLine($"    #{dc.Sortid} [{pos}{speaker}] {Clean(dc.Content, 65)}");
            }

            if (dialog.Type == 2)
            {
                sb.AppendLine("    ◆ 出现选项:");
                for (int i = 0; i < dialog.Param1.Count; i++)
                {
                    var child = tables.TbDialog.GetOrDefault(dialog.Param1[i]);
                    string name = child != null ? child.SelectionName : "?";
                    sb.AppendLine($"      选项{i + 1}: {Clean(name, 40)} → Dialog {dialog.Param1[i]}");
                }
                sb.AppendLine("    -- 等待玩家选择，流程暂停 --");
                return;
            }

            if (dialog.Param1 == null || dialog.Param1.Count == 0)
            {
                sb.AppendLine("    -- 终端节点，对话结束 --");
                return;
            }

            currentId = dialog.Param1[0];
        }

        if (safety <= 0) sb.AppendLine("  !! 超过50步安全限制，可能存在无限循环");
    }

    private static void WalkWithChoices(StringBuilder sb, cfg.Tables tables, int startId, int[] choices)
    {
        var dialog = tables.TbDialog.GetOrDefault(startId);
        if (dialog == null) { sb.AppendLine($"  !! Dialog {startId} 不存在"); return; }

        int currentId = startId;
        int choiceIdx = 0;
        int safety = 50;

        while (currentId > 0 && safety-- > 0)
        {
            dialog = tables.TbDialog.GetOrDefault(currentId);
            if (dialog == null) { sb.AppendLine($"  !! Dialog {currentId} 不存在"); return; }

            sb.AppendLine($"  [Dialog {currentId} Type={dialog.Type}]");

            var contents = tables.TbDialogcontent.DataList
                .Where(c => c.Dialogid == currentId)
                .OrderBy(c => c.Sortid).ToList();

            foreach (var dc in contents)
            {
                string pos = dc.Type == 0 ? "旁白" : dc.Type == 1 ? "左" : "右";
                string speaker = "";
                if (dc.Type == 1 && dialog.Speakerid1_Ref != null) speaker = $"({dialog.Speakerid1_Ref.Name})";
                if (dc.Type == 2 && dialog.Speakerid2_Ref != null) speaker = $"({dialog.Speakerid2_Ref.Name})";
                sb.AppendLine($"    #{dc.Sortid} [{pos}{speaker}] {Clean(dc.Content, 65)}");
            }

            if (dialog.Type == 2)
            {
                sb.AppendLine("    ◆ 选项:");
                for (int i = 0; i < dialog.Param1.Count; i++)
                {
                    var child = tables.TbDialog.GetOrDefault(dialog.Param1[i]);
                    string name = child != null ? child.SelectionName : "?";
                    sb.AppendLine($"      {i + 1}. {Clean(name, 40)} → Dialog {dialog.Param1[i]}");
                }

                if (choiceIdx < choices.Length)
                {
                    int pick = choices[choiceIdx++];
                    sb.AppendLine($"    ▶ 玩家选择: {pick + 1}");
                    currentId = dialog.Param1[pick];
                    continue;
                }

                sb.AppendLine("    -- 选项已用完，流程暂停 --");
                return;
            }

            if (dialog.Param1 == null || dialog.Param1.Count == 0)
            {
                sb.AppendLine("    -- 对话结束 --");
                return;
            }

            currentId = dialog.Param1[0];
        }

        if (safety <= 0) sb.AppendLine("  !! 超过50步限制");
    }

    private static string Clean(string s, int max)
    {
        if (s == null) return "";
        string c = s.Replace("<color=red>", "").Replace("</color>", "").Replace("<br>", " ");
        return c.Length <= max ? c : c.Substring(0, max) + "...";
    }
}
