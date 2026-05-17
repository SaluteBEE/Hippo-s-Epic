using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class MapCollectorTool
{
    private const string OutputPath = "Assets/Scripts/Level2D/Map/SceneMapId.cs";

    [MenuItem("Tools/Map/收集所有场景的 Map 并生成枚举")]
    public static void CollectAllMapsAndGenerateEnum()
    {
        var allEntries = new List<MapEntry>();
        string activeScenePath = SceneManager.GetActiveScene().path;

        string[] sceneGuids = AssetDatabase.FindAssets("t:Scene", new[] { "Assets" });
        var scenePaths = sceneGuids
            .Select(AssetDatabase.GUIDToAssetPath)
            .Where(p => p.EndsWith(".unity"))
            .OrderBy(p => p)
            .ToList();

        Debug.Log($"[MapCollector] 扫描到 {scenePaths.Count} 个场景文件");

        foreach (string scenePath in scenePaths)
        {
            string sceneName = Path.GetFileNameWithoutExtension(scenePath);

            if (scenePath == activeScenePath)
            {
                var maps = CollectFromActiveScene();
                foreach (var mapName in maps)
                    allEntries.Add(new MapEntry { SceneName = sceneName, MapName = mapName });
                Debug.Log($"[MapCollector] 场景 {sceneName}(活跃): 找到 {maps.Count} 个 Map");
                continue;
            }

            var found = FindMapsInScene(scenePath);
            foreach (var mapName in found)
                allEntries.Add(new MapEntry { SceneName = sceneName, MapName = mapName });

            Debug.Log($"[MapCollector] 场景 {sceneName}: 找到 {found.Count} 个 Map");
        }

        GenerateEnumFile(allEntries);
    }

    private static List<string> CollectFromActiveScene()
    {
        var result = new List<string>();
        var maps = GameObject.FindObjectsOfType<Map>(true);
        foreach (var map in maps)
        {
            if (map != null && !string.IsNullOrEmpty(map.name))
                result.Add(map.name);
        }
        result.Sort();
        return result;
    }

    private static List<string> FindMapsInScene(string scenePath)
    {
        var result = new List<string>();

        Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);

        try
        {
            var mapComponents = GameObject.FindObjectsOfType<Map>(true);
            foreach (var map in mapComponents)
            {
                if (map != null && !string.IsNullOrEmpty(map.name))
                {
                    result.Add(map.name);
                }
            }

            result.Sort();
        }
        finally
        {
            EditorSceneManager.CloseScene(scene, true);
        }

        return result;
    }

    private static void GenerateEnumFile(List<MapEntry> entries)
    {
        var usedNames = new HashSet<string>();
        var enumEntries = new List<(string enumName, string mapName, string sceneName)>();

        foreach (var entry in entries)
        {
            if (string.IsNullOrWhiteSpace(entry.MapName)) continue;

            string baseName = ToEnumName(entry.MapName);
            string enumName = baseName;
            int suffix = 2;

            while (usedNames.Contains(enumName))
            {
                enumName = baseName + "_" + ToEnumName(entry.SceneName) + (suffix > 2 ? suffix.ToString() : "");
                suffix++;
            }

            usedNames.Add(enumName);
            enumEntries.Add((enumName, entry.MapName, entry.SceneName));
        }

        var sb = new StringBuilder();
        sb.AppendLine("// 本文件由 Tools/Map/收集所有场景的 Map 并生成枚举 自动生成，请勿手动修改");
        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine();
        sb.AppendLine("public enum SceneMapId");
        sb.AppendLine("{");

        var grouped = enumEntries.GroupBy(e => e.sceneName).ToList();

        for (int g = 0; g < grouped.Count; g++)
        {
            var group = grouped[g];
            sb.AppendLine($"    // --- {group.Key} ---");

            foreach (var item in group)
            {
                sb.AppendLine($"    {item.enumName}, // {item.mapName}");
            }

            if (g < grouped.Count - 1)
                sb.AppendLine();
        }

        sb.AppendLine("}");
        sb.AppendLine();
        sb.AppendLine("public static class SceneMapIdExtensions");
        sb.AppendLine("{");

        sb.AppendLine("    private static readonly Dictionary<SceneMapId, string> MapNames = new Dictionary<SceneMapId, string>");
        sb.AppendLine("    {");
        foreach (var item in enumEntries)
        {
            sb.AppendLine($"        {{ SceneMapId.{item.enumName}, \"{item.mapName}\" }},");
        }
        sb.AppendLine("    };");
        sb.AppendLine();

        sb.AppendLine("    private static readonly Dictionary<SceneMapId, string> SceneNames = new Dictionary<SceneMapId, string>");
        sb.AppendLine("    {");
        foreach (var item in enumEntries)
        {
            sb.AppendLine($"        {{ SceneMapId.{item.enumName}, \"{item.sceneName}\" }},");
        }
        sb.AppendLine("    };");
        sb.AppendLine();

        sb.AppendLine("    public static string GetMapName(this SceneMapId id) => MapNames[id];");
        sb.AppendLine("    public static string GetSceneName(this SceneMapId id) => SceneNames[id];");
        sb.AppendLine("}");

        string dir = Path.GetDirectoryName(OutputPath);
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        File.WriteAllText(OutputPath, sb.ToString());
        AssetDatabase.Refresh();

        Debug.Log($"[MapCollector] 已生成 {OutputPath}，共 {entries.Count} 个 Map 枚举项");
    }

    private static string ToEnumName(string raw)
    {
        var parts = raw.Split(new[] { '_', ' ', '-' });
        var sb = new StringBuilder();
        foreach (var part in parts)
        {
            if (string.IsNullOrEmpty(part)) continue;
            sb.Append(char.ToUpper(part[0]));
            if (part.Length > 1)
                sb.Append(part.Substring(1));
        }
        return sb.ToString();
    }

    private struct MapEntry
    {
        public string SceneName;
        public string MapName;
    }
}
