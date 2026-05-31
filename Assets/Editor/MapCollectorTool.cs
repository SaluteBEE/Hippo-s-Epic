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

    [MenuItem("Tools/Map/收集所有场景的 Map 和 Interactable")]
    public static void CollectAllMapsAndInteractables()
    {
        var allEntries = new List<MapEntry>();
        string activeScenePath = SceneManager.GetActiveScene().path;
        int totalInteractables = 0;

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
                var (maps, interactableCount) = CollectFromActiveScene();
                foreach (var mapName in maps)
                    allEntries.Add(new MapEntry { SceneName = sceneName, MapName = mapName });
                totalInteractables += interactableCount;
                Debug.Log($"[MapCollector] 场景 {sceneName}(活跃): 找到 {maps.Count} 个 Map, {interactableCount} 个 Interactable");
                continue;
            }

            var (foundMaps, foundInteractables) = CollectFromScene(scenePath);
            foreach (var mapName in foundMaps)
                allEntries.Add(new MapEntry { SceneName = sceneName, MapName = mapName });
            totalInteractables += foundInteractables;
            Debug.Log($"[MapCollector] 场景 {sceneName}: 找到 {foundMaps.Count} 个 Map, {foundInteractables} 个 Interactable");
        }

        GenerateEnumFile(allEntries);
        Debug.Log($"[MapCollector] 完成，共 {allEntries.Count} 个 Map，{totalInteractables} 个 Interactable");
    }

    private static (List<string> maps, int interactableCount) CollectFromActiveScene()
    {
        var maps = new List<string>();
        int interactableCount = 0;

        var mapComponents = GameObject.FindObjectsOfType<Map>(true);
        foreach (var map in mapComponents)
        {
            if (map != null && !string.IsNullOrEmpty(map.name))
            {
                maps.Add(map.name);
                Undo.RecordObject(map, "Collect Interactables");
                map.CollectInteractables();
                EditorUtility.SetDirty(map);
                interactableCount += map.InteractableList.Count;
            }
        }

        maps.Sort();
        return (maps, interactableCount);
    }

    private static (List<string> maps, int interactableCount) CollectFromScene(string scenePath)
    {
        var maps = new List<string>();
        int interactableCount = 0;

        Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);

        try
        {
            var rootObjects = scene.GetRootGameObjects();
            var mapComponents = new List<Map>();
            foreach (var root in rootObjects)
                mapComponents.AddRange(root.GetComponentsInChildren<Map>(true));

            foreach (var map in mapComponents)
            {
                if (map != null && !string.IsNullOrEmpty(map.name))
                {
                    maps.Add(map.name);
                    Undo.RecordObject(map, "Collect Interactables");
                    map.CollectInteractables();
                    EditorUtility.SetDirty(map);
                    interactableCount += map.InteractableList.Count;
                }
            }

            EditorSceneManager.SaveScene(scene);
            maps.Sort();
        }
        finally
        {
            EditorSceneManager.CloseScene(scene, true);
        }

        return (maps, interactableCount);
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
        sb.AppendLine("// 本文件由 Tools/Map/收集所有场景的 Map 和 Interactable 自动生成，请勿手动修改");
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
        var sb = new StringBuilder();
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

    private struct MapEntry
    {
        public string SceneName;
        public string MapName;
    }
}
