using NUnit.Framework;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public static class TryAutoInitTestRunner
{
    [MenuItem("Tools/运行 TryAutoInit 测试")]
    public static void RunAll()
    {
        int pass = 0, fail = 0;
        var sb = new System.Text.StringBuilder();

        sb.AppendLine("===== TryAutoInitTests =====");
        RunTestFixture(new TryAutoInitTests(), sb, ref pass, ref fail);

        sb.AppendLine($"\n结果: {pass} 通过, {fail} 失败");

        string reportPath = Path.Combine(Application.dataPath, "..", "TestReport_AutoInit.txt");
        File.WriteAllText(reportPath, sb.ToString());

        Debug.Log($"[TryAutoInitTestRunner] {pass} 通过, {fail} 失败 (报告: {reportPath})");
    }

    private static void RunTestFixture(object fixture, System.Text.StringBuilder sb, ref int pass, ref int fail)
    {
        var setupMethods = fixture.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Where(m => m.GetCustomAttributes(typeof(SetUpAttribute), false).Length > 0)
            .ToArray();

        var tearDownMethods = fixture.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Where(m => m.GetCustomAttributes(typeof(TearDownAttribute), false).Length > 0)
            .ToArray();

        var testMethods = fixture.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Where(m => m.GetCustomAttributes(typeof(TestAttribute), false).Length > 0)
            .ToList();

        foreach (var m in testMethods)
        {
            foreach (var setup in setupMethods) setup.Invoke(fixture, null);
            try
            {
                m.Invoke(fixture, null);
                sb.AppendLine($"  PASS: {m.Name}");
                pass++;
            }
            catch (TargetInvocationException ex)
            {
                if (ex.InnerException is SuccessException)
                {
                    sb.AppendLine($"  PASS: {m.Name} (Assert.Inconclusive)");
                    pass++;
                }
                else
                {
                    sb.AppendLine($"  FAIL: {m.Name} → {ex.InnerException?.Message ?? ex.Message}");
                    fail++;
                }
            }
            finally
            {
                foreach (var td in tearDownMethods) td.Invoke(fixture, null);
            }
        }
    }
}
