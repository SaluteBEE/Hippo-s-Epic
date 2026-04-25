using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using NUnit.Framework;
using UnityEngine;

public class ExportPipelineTests
{
    private static readonly string ProjectRoot =
        Directory.GetParent(Application.dataPath).FullName;

    private static readonly string PythonScript =
        Path.Combine(ProjectRoot, "DataTables", "update_excel.py");

    private string _tempDir;

    [SetUp]
    public void SetUp()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"hippo_export_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }

    #region Emotion Mapping Tests

    [Test]
    public void GuessEmotion_Idle_Returns1()
    {
        Assert.AreEqual(1, GuessEmotion("idle"));
    }

    [Test]
    public void GuessEmotion_Talk_Returns2()
    {
        Assert.AreEqual(2, GuessEmotion("talk"));
    }

    [Test]
    public void GuessEmotion_Angry_Returns3()
    {
        Assert.AreEqual(3, GuessEmotion("angry"));
    }

    [Test]
    public void GuessEmotion_Think1_Returns4()
    {
        Assert.AreEqual(4, GuessEmotion("think1"));
    }

    [Test]
    public void GuessEmotion_Think2_Returns5()
    {
        Assert.AreEqual(5, GuessEmotion("think2"));
    }

    [Test]
    public void GuessEmotion_Think3_Returns6()
    {
        Assert.AreEqual(6, GuessEmotion("think3"));
    }

    [Test]
    public void GuessEmotion_Throw_Returns7()
    {
        Assert.AreEqual(7, GuessEmotion("throw"));
    }

    [Test]
    public void GuessEmotion_Unknown_Returns0()
    {
        Assert.AreEqual(0, GuessEmotion("dance"));
    }

    [Test]
    public void GuessEmotion_CaseInsensitive()
    {
        Assert.AreEqual(1, GuessEmotion("IDLE"));
        Assert.AreEqual(2, GuessEmotion("Talk"));
        Assert.AreEqual(3, GuessEmotion("ANGRY"));
    }

    [Test]
    public void GuessEmotion_SubstringMatch()
    {
        Assert.AreEqual(1, GuessEmotion("idle1"));
        Assert.AreEqual(1, GuessEmotion("idle_v2"));
        Assert.AreEqual(2, GuessEmotion("talking"));
        Assert.AreEqual(4, GuessEmotion("think1_special"));
    }

    [Test]
    public void GuessEmotion_EmptyString_Returns0()
    {
        Assert.AreEqual(0, GuessEmotion(""));
    }

    [Test]
    public void GuessEmotion_Idle1_Idle2_Idle3_Return1()
    {
        Assert.AreEqual(1, GuessEmotion("idle1"));
        Assert.AreEqual(1, GuessEmotion("idle2"));
        Assert.AreEqual(1, GuessEmotion("idle3"));
    }

    [Test]
    public void GuessEmotion_ThrowNotCaughtByOthers()
    {
        Assert.AreEqual(7, GuessEmotion("throw"));
        Assert.AreEqual(7, GuessEmotion("throw_ball"));
    }

    #endregion

    #region JSON Generation Tests

    [Test]
    public void BuildAnimationStateJson_SingleComposition_CorrectFormat()
    {
        string json = BuildAnimationStateJson(1, new[]
        {
            "idle",
        });

        Assert.IsTrue(json.StartsWith("["));
        Assert.IsTrue(json.EndsWith("]"));
        StringAssert.Contains("\"personid\":1", json);
        StringAssert.Contains("\"statename\":\"idle\"", json);
        StringAssert.Contains("\"slotstateid\":0", json);
    }

    [Test]
    public void BuildAnimationStateJson_MultipleCompositions()
    {
        string json = BuildAnimationStateJson(2, new[]
        {
            "idle",
            "talk",
            "angry",
        });

        StringAssert.Contains("\"personid\":2", json);
        StringAssert.Contains("\"statename\":\"idle\"", json);
        StringAssert.Contains("\"statename\":\"talk\"", json);
        StringAssert.Contains("\"statename\":\"angry\"", json);
    }

    [Test]
    public void BuildSlotStateJson_CorrectFormat()
    {
        string json = BuildSlotStateJson(1, 1, new Dictionary<string, int>
        {
            {"face", 1},
            {"body", 2}
        });

        Assert.IsTrue(json.StartsWith("["));
        Assert.IsTrue(json.EndsWith("]"));
        StringAssert.Contains("\"personid\":1", json);
        StringAssert.Contains("\"prefabtype\":1", json);
        StringAssert.Contains("\"face\":1", json);
        StringAssert.Contains("\"body\":2", json);
    }

    [Test]
    public void BuildSlotStateJson_MultipleSlots_AllPresent()
    {
        string json = BuildSlotStateJson(1, 1, new Dictionary<string, int>
        {
            {"face", 1},
            {"body", 2},
            {"hair", 3}
        });

        StringAssert.Contains("\"face\":1", json);
        StringAssert.Contains("\"body\":2", json);
        StringAssert.Contains("\"hair\":3", json);
    }

    [Test]
    public void BuildSlotStateJson_EmptySlots_EmptyArray()
    {
        string json = BuildSlotStateJson(1, 1, new Dictionary<string, int>());

        Assert.AreEqual("[]", json);
    }

    #endregion

    #region Pipeline Integration Tests

    [Test]
    public void Pipeline_AnimationState_CreatesXlsx()
    {
        string json = "[{\"personid\":1,\"statename\":\"idle\",\"prefabtype\":1,\"slotstateid\":0}]";
        string xlsxPath = Path.Combine(_tempDir, "animationstate.xlsx");
        string jsonPath = WriteTempJson(json);

        string result = RunPython("animationstate", xlsxPath, jsonPath);

        Assert.IsTrue(File.Exists(xlsxPath));
        StringAssert.Contains("OK", result);
    }

    [Test]
    public void Pipeline_SlotState_CreatesXlsx()
    {
        string json = "[{\"personid\":1,\"prefabtype\":1,\"slots\":{\"face\":1}}]";
        string xlsxPath = Path.Combine(_tempDir, "slotstate.xlsx");
        string jsonPath = WriteTempJson(json);

        string result = RunPython("slotstate", xlsxPath, jsonPath);

        Assert.IsTrue(File.Exists(xlsxPath));
        StringAssert.Contains("OK", result);
    }

    [Test]
    public void Pipeline_AnimationState_UpdateExisting()
    {
        string xlsxPath = Path.Combine(_tempDir, "animationstate.xlsx");

        string json1 = "[{\"personid\":1,\"statename\":\"idle\",\"prefabtype\":1,\"slotstateid\":0}]";
        RunPython("animationstate", xlsxPath, WriteTempJson(json1));

        string json2 = "[{\"personid\":1,\"statename\":\"idle\",\"prefabtype\":1,\"slotstateid\":0}]";
        string result = RunPython("animationstate", xlsxPath, WriteTempJson(json2));

        StringAssert.Contains("updated=1", result);
    }

    [Test]
    public void Pipeline_AnimationState_AddNew()
    {
        string xlsxPath = Path.Combine(_tempDir, "animationstate.xlsx");

        string json1 = "[{\"personid\":1,\"statename\":\"idle\",\"prefabtype\":1,\"slotstateid\":0}]";
        RunPython("animationstate", xlsxPath, WriteTempJson(json1));

        string json2 = "[{\"personid\":2,\"statename\":\"idle\",\"prefabtype\":1,\"slotstateid\":0}]";
        string result = RunPython("animationstate", xlsxPath, WriteTempJson(json2));

        StringAssert.Contains("added=1", result);
    }

    [Test]
    public void Pipeline_SlotState_UpdateExisting()
    {
        string xlsxPath = Path.Combine(_tempDir, "slotstate.xlsx");

        string json1 = "[{\"personid\":1,\"prefabtype\":1,\"slots\":{\"face\":1}}]";
        RunPython("slotstate", xlsxPath, WriteTempJson(json1));

        string json2 = "[{\"personid\":1,\"prefabtype\":1,\"slots\":{\"face\":0}}]";
        string result = RunPython("slotstate", xlsxPath, WriteTempJson(json2));

        StringAssert.Contains("updated=1", result);
    }

    [Test]
    public void Pipeline_SlotState_AddNew()
    {
        string xlsxPath = Path.Combine(_tempDir, "slotstate.xlsx");

        string json1 = "[{\"personid\":1,\"prefabtype\":1,\"slots\":{\"face\":1}}]";
        RunPython("slotstate", xlsxPath, WriteTempJson(json1));

        string json2 = "[{\"personid\":2,\"prefabtype\":1,\"slots\":{\"face\":1}}]";
        string result = RunPython("slotstate", xlsxPath, WriteTempJson(json2));

        StringAssert.Contains("added=1", result);
    }

    [Test]
    public void Pipeline_Person1FullExport()
    {
        string json = BuildAnimationStateJson(1, new[]
        {
            "idle",
            "talk",
            "think1",
            "think2",
            "think3",
        });
        string xlsxPath = Path.Combine(_tempDir, "animationstate.xlsx");

        string result = RunPython("animationstate", xlsxPath, WriteTempJson(json));

        StringAssert.Contains("OK", result);
        Assert.IsTrue(File.Exists(xlsxPath));
    }

    [Test]
    public void Pipeline_SlotState_MultipleSlots()
    {
        string json = BuildSlotStateJson(1, 1, new Dictionary<string, int>
        {
            {"face", 1},
            {"body", 1},
            {"hair", 1}
        });
        string xlsxPath = Path.Combine(_tempDir, "slotstate.xlsx");

        string result = RunPython("slotstate", xlsxPath, WriteTempJson(json));

        StringAssert.Contains("OK", result);
        StringAssert.Contains("added=1", result);
    }

    [Test]
    public void Pipeline_UTF8BOM_HandledCorrectly()
    {
        string json = "[{\"personid\":1,\"statename\":\"idle\",\"prefabtype\":1,\"slotstateid\":0}]";
        string xlsxPath = Path.Combine(_tempDir, "animationstate.xlsx");
        string jsonPath = Path.Combine(_tempDir, "bom_test.json");

        byte[] jsonBytes = Encoding.UTF8.GetBytes(json);
        byte[] bom = Encoding.UTF8.GetPreamble();
        byte[] bomJson = new byte[bom.Length + jsonBytes.Length];
        Buffer.BlockCopy(bom, 0, bomJson, 0, bom.Length);
        Buffer.BlockCopy(jsonBytes, 0, bomJson, bom.Length, jsonBytes.Length);
        File.WriteAllBytes(jsonPath, bomJson);

        string result = RunPython("animationstate", xlsxPath, jsonPath);

        StringAssert.Contains("OK", result);
    }

    [Test]
    public void Pipeline_SecondExportPreservesFirst()
    {
        string xlsxPath = Path.Combine(_tempDir, "animationstate.xlsx");

        string json1 = "[{\"personid\":1,\"statename\":\"idle\",\"prefabtype\":1,\"slotstateid\":0}]";
        RunPython("animationstate", xlsxPath, WriteTempJson(json1));

        string json2 = "[{\"personid\":2,\"statename\":\"idle\",\"prefabtype\":1,\"slotstateid\":0}]";
        string result = RunPython("animationstate", xlsxPath, WriteTempJson(json2));

        StringAssert.Contains("added=1", result);

        string json3 = "[{\"personid\":1,\"statename\":\"idle\",\"prefabtype\":1,\"slotstateid\":0}]";
        result = RunPython("animationstate", xlsxPath, WriteTempJson(json3));

        StringAssert.Contains("updated=1", result);
        StringAssert.Contains("added=0", result);
    }

    [Test]
    public void Pipeline_UnknownMode_Fails()
    {
        string json = "[{}]";
        string xlsxPath = Path.Combine(_tempDir, "unknown.xlsx");
        string jsonPath = WriteTempJson(json);

        bool failed = false;
        try
        {
            RunPython("unknown_mode", xlsxPath, jsonPath);
        }
        catch (AssertionException)
        {
            failed = true;
        }

        Assert.IsTrue(failed, "Python should fail with unknown mode");
    }

    #endregion

    #region Helpers

    private static int GuessEmotion(string name)
    {
        string lower = name.ToLower();
        if (lower.Contains("idle")) return 1;
        if (lower.Contains("talk")) return 2;
        if (lower.Contains("angry")) return 3;
        if (lower.Contains("think1")) return 4;
        if (lower.Contains("think2")) return 5;
        if (lower.Contains("think3")) return 6;
        if (lower.Contains("throw")) return 7;
        return 0;
    }

    private static string BuildAnimationStateJson(int personId,
        IEnumerable<string> statenames)
    {
        var sb = new StringBuilder();
        sb.Append("[");
        bool first = true;
        foreach (var name in statenames)
        {
            if (!first) sb.Append(",");
            first = false;
            sb.Append($"{{\"personid\":{personId},\"statename\":\"{name}\",\"prefabtype\":1,\"slotstateid\":0}}");
        }
        sb.Append("]");
        return sb.ToString();
    }

    private static string BuildSlotStateJson(int personId, int prefabType,
        Dictionary<string, int> slots)
    {
        if (slots == null || slots.Count == 0)
            return "[]";

        var sb = new StringBuilder();
        sb.Append("[{\"personid\":");
        sb.Append(personId);
        sb.Append(",\"prefabtype\":");
        sb.Append(prefabType);
        sb.Append(",\"slots\":{");
        bool first = true;
        foreach (var kvp in slots)
        {
            if (!first) sb.Append(",");
            first = false;
            sb.Append($"\"{kvp.Key}\":{kvp.Value}");
        }
        sb.Append("}}]");
        return sb.ToString();
    }

    private string WriteTempJson(string json)
    {
        string path = Path.Combine(_tempDir, $"test_{Guid.NewGuid():N}.json");
        File.WriteAllText(path, json, Encoding.UTF8);
        return path;
    }

    private static int CountOccurrences(string source, string substring)
    {
        int count = 0;
        int index = 0;
        while ((index = source.IndexOf(substring, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += substring.Length;
        }
        return count;
    }

    private string RunPython(string mode, string xlsxPath, string jsonPath)
    {
        Assert.IsTrue(File.Exists(PythonScript),
            $"Python script not found: {PythonScript}");

        var psi = new ProcessStartInfo
        {
            FileName = "python",
            Arguments = $"\"{PythonScript}\" {mode} \"{xlsxPath}\" \"{jsonPath}\"",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };

        using (var proc = Process.Start(psi))
        {
            string output = proc.StandardOutput.ReadToEnd();
            string error = proc.StandardError.ReadToEnd();
            proc.WaitForExit(10000);

            if (proc.ExitCode != 0)
                Assert.Fail($"Python exited with code {proc.ExitCode}: {error}");

            return output.Trim();
        }
    }

    #endregion
}
