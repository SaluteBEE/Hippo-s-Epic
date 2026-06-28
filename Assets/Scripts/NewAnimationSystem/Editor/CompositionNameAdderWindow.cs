using UnityEditor;
using UnityEngine;

public class CompositionNameAdderWindow : EditorWindow
{
    private string _enumName = "";

    public static void ShowWindow()
    {
        var window = GetWindow<CompositionNameAdderWindow>(true, "新增组合动画枚举名", true);
        window.minSize = new Vector2(300, 100);
        window.maxSize = new Vector2(300, 100);
    }

    private void OnGUI()
    {
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("请输入枚举名（PascalCase，如 Jump）：");
        _enumName = EditorGUILayout.TextField(_enumName);

        EditorGUILayout.Space(10);
        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("确定"))
        {
            if (string.IsNullOrEmpty(_enumName))
            {
                EditorUtility.DisplayDialog("错误", "枚举名不能为空", "确定");
                return;
            }

            string enumName = ToPascalCase(_enumName);

            var existingNames = System.Enum.GetNames(typeof(CompositionName));
            foreach (var existing in existingNames)
            {
                if (existing == enumName)
                {
                    EditorUtility.DisplayDialog("重复", $"枚举名 {enumName} 已存在", "确定");
                    return;
                }
            }

            string enumFilePath = FindCompositionNameFile();
            if (string.IsNullOrEmpty(enumFilePath))
            {
                EditorUtility.DisplayDialog("错误", "未找到 CompositionName.cs 文件", "确定");
                return;
            }

            string content = System.IO.File.ReadAllText(enumFilePath);
            int lastBrace = content.LastIndexOf('}');
            if (lastBrace < 0)
            {
                EditorUtility.DisplayDialog("错误", "CompositionName.cs 格式异常", "确定");
                return;
            }

            string newContent = content.Substring(0, lastBrace) + $"    {enumName},\n" + content.Substring(lastBrace);
            System.IO.File.WriteAllText(enumFilePath, newContent, new System.Text.UTF8Encoding(true));
            AssetDatabase.Refresh();

            Debug.Log($"[CompositionNameAdder] 已新增枚举 {enumName} 到 CompositionName");
            EditorUtility.DisplayDialog("完成", $"已新增枚举名: {enumName}\n请重新编译后使用", "确定");
            Close();
        }

        if (GUILayout.Button("取消"))
        {
            Close();
        }

        EditorGUILayout.EndHorizontal();
    }

    private static string FindCompositionNameFile()
    {
        string[] guids = AssetDatabase.FindAssets("CompositionName t:MonoScript", new[] { "Assets" });
        if (guids.Length == 0) return "";
        return AssetDatabase.GUIDToAssetPath(guids[0]);
    }

    private static string ToPascalCase(string raw)
    {
        var sb = new System.Text.StringBuilder();
        bool nextUpper = true;
        foreach (char c in raw)
        {
            if (char.IsLetterOrDigit(c))
            {
                sb.Append(nextUpper ? char.ToUpper(c) : c);
                nextUpper = false;
            }
            else
            {
                nextUpper = true;
            }
        }
        return sb.ToString();
    }
}
