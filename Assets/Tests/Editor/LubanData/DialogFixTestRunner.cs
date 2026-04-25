using NUnit.Framework;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class DialogFixTestRunner
{
    [MenuItem("Tools/运行对话修复测试")]
    public static void RunAll()
    {
        int pass = 0, fail = 0;
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("===== DialogManagerFixTests =====");

        var test = new DialogManagerFixTests();

        var methods = typeof(DialogManagerFixTests).GetMethods(
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

        var testMethods = methods
            .Where(m => m.GetCustomAttributes(typeof(TestAttribute), false).Length > 0)
            .ToList();

        foreach (var m in testMethods)
        {
            test.Setup();
            try
            {
                m.Invoke(test, null);
                sb.AppendLine($"  PASS: {m.Name}");
                pass++;
            }
            catch (System.Exception ex)
            {
                if (ex.InnerException is NUnit.Framework.SuccessException)
                {
                    sb.AppendLine($"  SKIP: {m.Name} ({ex.InnerException.Message})");
                }
                else
                {
                    sb.AppendLine($"  FAIL: {m.Name} → {ex.InnerException?.Message ?? ex.Message}");
                    fail++;
                }
            }
            finally
            {
                test.TearDown();
            }
        }

        sb.AppendLine($"\n结果: {pass} 通过, {fail} 失败 / 共 {testMethods.Count} 个测试");

        string reportPath = Path.Combine(UnityEngine.Application.dataPath, "..", "TestReport.txt");
        File.WriteAllText(reportPath, sb.ToString());

        Debug.Log($"测试完成: {pass} 通过, {fail} 失败 / 共 {testMethods.Count} 个测试 (详细报告: {reportPath})");
    }
}
