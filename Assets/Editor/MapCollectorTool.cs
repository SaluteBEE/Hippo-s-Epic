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
    private const string SceneMapIdOutputPath = "Assets/Scripts/Level2D/Map/SceneMapId.cs";
    private const string TeleportTargetOutputPath = "Assets/Scripts/Level2D/Interactable/TeleportTargetDef.cs";
    private const string MapNodeIdOutputPath = "Assets/Scripts/UI/map/MapNodeId.cs";

    [MenuItem("Tools/Map/收集所有场景数据")]
    public static void CollectAll()
    {
        string activeScenePath = SceneManager.GetActiveScene().path;

        string[] sceneGuids = AssetDatabase.FindAssets("t:Scene", new[] { "Assets" });
        var scenePaths = sceneGuids
            .Select(AssetDatabase.GUIDToAssetPath)
            .Where(p => p.EndsWith(".unity"))
            .OrderBy(p => p)
            .ToList();

        Debug.Log($"[MapCollector] 扫描到 {scenePaths.Count} 个场景文件");

        var mapEntries = new List<MapEntry>();
        var teleportEntries = new List<TeleportSceneData>();
        int totalInteractables = 0;

        for (int i = 0; i < scenePaths.Count; i++)
        {
            string scenePath = scenePaths[i];
            string sceneName = Path.GetFileNameWithoutExtension(scenePath);
            float progress = (float)i / scenePaths.Count;

            if (EditorUtility.DisplayCancelableProgressBar(
                "收集场景数据", $"扫描 {sceneName} ...", progress))
                break;

            if (scenePath == activeScenePath)
            {
                var (maps, interactableCount) = CollectFromActiveScene();
                foreach (var mapName in maps)
                    mapEntries.Add(new MapEntry { SceneName = sceneName, MapName = mapName });
                totalInteractables += interactableCount;

                var teleportData = CollectTeleportFromActiveScene(maps);
                if (teleportData != null)
                    teleportEntries.Add(teleportData.Value);

                Debug.Log($"[MapCollector] 场景 {sceneName}(活跃): {maps.Count} 个 Map, {interactableCount} 个 Interactable");
                continue;
            }

            var (foundMaps, foundInteractables) = CollectFromScene(scenePath);
            foreach (var mapName in foundMaps)
                mapEntries.Add(new MapEntry { SceneName = sceneName, MapName = mapName });
            totalInteractables += foundInteractables;

            var tData = CollectTeleportFromScene(scenePath, foundMaps);
            if (tData != null)
                teleportEntries.Add(tData.Value);

            Debug.Log($"[MapCollector] 场景 {sceneName}: {foundMaps.Count} 个 Map, {foundInteractables} 个 Interactable");
        }

        EditorUtility.ClearProgressBar();

        GenerateSceneMapIdFile(mapEntries);
        GenerateMapNodeIdFile(mapEntries);
        GenerateTeleportTargetFile(teleportEntries);

        Debug.Log($"[MapCollector] 完成，共 {mapEntries.Count} 个 Map，{totalInteractables} 个 Interactable，{teleportEntries.Count} 个场景有传送数据");
    }

    #region Map + Interactable 收集

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

    private static void GenerateSceneMapIdFile(List<MapEntry> entries)
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
        sb.AppendLine("// 本文件由 Tools/Map/收集所有场景数据 自动生成，请勿手动修改");
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

        sb.AppendLine("    private static readonly Dictionary<string, SceneMapId> MapNameToId = new Dictionary<string, SceneMapId>");
        sb.AppendLine("    {");
        foreach (var item in enumEntries)
        {
            sb.AppendLine($"        {{ \"{item.mapName}\", SceneMapId.{item.enumName} }},");
        }
        sb.AppendLine("    };");
        sb.AppendLine();

        sb.AppendLine("    public static string GetMapName(this SceneMapId id) => MapNames[id];");
        sb.AppendLine("    public static string GetSceneName(this SceneMapId id) => SceneNames[id];");
        sb.AppendLine("    public static SceneMapId GetSceneMapIdByMapName(string mapName) => MapNameToId.TryGetValue(mapName, out var id) ? id : SceneMapId.StaffLounge;");
        sb.AppendLine("    public static IEnumerable<string> GetAllMapNames() => MapNameToId.Keys;");
        sb.AppendLine("}");

        WriteFile(SceneMapIdOutputPath, sb.ToString());
        Debug.Log($"[MapCollector] 已生成 {SceneMapIdOutputPath}，共 {entries.Count} 个 Map 枚举项");
    }

    #endregion

    #region MapNodeId 自动生成

    private static void GenerateMapNodeIdFile(List<MapEntry> entries)
    {
        var usedNames = new HashSet<string>();
        var allEnumNames = new List<string>();

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
            allEnumNames.Add(enumName);
        }

        var existingMap = ParseExistingMapNodeId();
        int maxValue = 0;
        foreach (var v in existingMap.Values)
            if (v > maxValue) maxValue = v;

        var finalEntries = new List<(string name, int value)>();

        foreach (var kvp in existingMap)
            finalEntries.Add((kvp.Key, kvp.Value));

        foreach (var name in allEnumNames)
        {
            if (existingMap.ContainsKey(name)) continue;

            maxValue++;
            finalEntries.Add((name, maxValue));
        }

        var sb = new StringBuilder();
        sb.AppendLine("// 本文件由 Tools/Map/收集所有场景数据 自动生成，请勿手动修改");
        sb.AppendLine();
        sb.AppendLine("public enum MapNodeId");
        sb.AppendLine("{");
        sb.AppendLine("    None = 0,");

        foreach (var entry in finalEntries)
            sb.AppendLine($"    {entry.name} = {entry.value},");

        sb.AppendLine("}");

        WriteFile(MapNodeIdOutputPath, sb.ToString());
        Debug.Log($"[MapCollector] 已生成 {MapNodeIdOutputPath}，共 {finalEntries.Count} 个节点枚举项");
    }

    private static Dictionary<string, int> ParseExistingMapNodeId()
    {
        var result = new Dictionary<string, int>();

        if (!File.Exists(MapNodeIdOutputPath))
            return result;

        string[] lines = File.ReadAllLines(MapNodeIdOutputPath);
        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith("//") || string.IsNullOrEmpty(trimmed))
                continue;

            if (trimmed.StartsWith("None"))
                continue;

            int eqIdx = trimmed.IndexOf('=');
            int commaIdx = trimmed.IndexOf(',');
            if (eqIdx < 0 || commaIdx < 0) continue;

            string name = trimmed.Substring(0, eqIdx).Trim();
            string valueStr = trimmed.Substring(eqIdx + 1, commaIdx - eqIdx - 1).Trim();

            if (int.TryParse(valueStr, out int value))
                result[name] = value;
        }

        return result;
    }

    #endregion

    #region 传送目标收集

    private struct TeleportSceneData
    {
        public string sceneName;
        public string[] mapNames;
        public TeleportInteractableData[] interactables;
    }

    private struct TeleportInteractableData
    {
        public string entityId;
        public string mapName;
    }

    private static TeleportSceneData? CollectTeleportFromActiveScene(List<string> mapNames)
    {
        var scene = SceneManager.GetActiveScene();
        var maps = GameObject.FindObjectsOfType<Map>(true).ToList();
        var interactables = GameObject.FindObjectsOfType<Interactable>(true);

        if (maps.Count == 0 && interactables.Length == 0)
            return null;

        var iDataList = new List<TeleportInteractableData>();
        foreach (var ia in interactables)
        {
            string mapName = FindParentMapName(ia.transform, maps);
            iDataList.Add(new TeleportInteractableData
            {
                entityId = ia.EntityId,
                mapName = mapName ?? ""
            });
        }

        return new TeleportSceneData
        {
            sceneName = scene.name,
            mapNames = mapNames.ToArray(),
            interactables = iDataList.ToArray()
        };
    }

    private static TeleportSceneData? CollectTeleportFromScene(string scenePath, List<string> mapNames)
    {
        Scene scene = EditorSceneManager.GetSceneByPath(scenePath);
        if (!scene.isLoaded) return null;

        var rootObjects = scene.GetRootGameObjects();
        var maps = new List<Map>();
        foreach (var root in rootObjects)
            maps.AddRange(root.GetComponentsInChildren<Map>(true));

        var interactables = new List<Interactable>();
        foreach (var root in rootObjects)
            interactables.AddRange(root.GetComponentsInChildren<Interactable>(true));

        if (maps.Count == 0 && interactables.Count == 0)
            return null;

        var iDataList = new List<TeleportInteractableData>();
        foreach (var ia in interactables)
        {
            string mapName = FindParentMapName(ia.transform, maps);
            iDataList.Add(new TeleportInteractableData
            {
                entityId = ia.EntityId,
                mapName = mapName ?? ""
            });
        }

        return new TeleportSceneData
        {
            sceneName = Path.GetFileNameWithoutExtension(scenePath),
            mapNames = mapNames.ToArray(),
            interactables = iDataList.ToArray()
        };
    }

    private static string FindParentMapName(Transform t, List<Map> maps)
    {
        Transform current = t.parent;
        while (current != null)
        {
            var map = maps.FirstOrDefault(m => m.transform == current);
            if (map != null) return map.name;
            current = current.parent;
        }
        return null;
    }

    private static void GenerateTeleportTargetFile(List<TeleportSceneData> entries)
    {
        var sb = new StringBuilder();
        sb.AppendLine("// ============================================================");
        sb.AppendLine("// 自动生成文件 - 勿手动修改");
        sb.AppendLine($"// 生成时间: {System.DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine("// 通过 Tools/Map/收集所有场景数据 重新生成");
        sb.AppendLine("// ============================================================");
        sb.AppendLine();
        sb.AppendLine("public static class TeleportTargetDef");
        sb.AppendLine("{");
        sb.AppendLine("    public struct SceneEntry");
        sb.AppendLine("    {");
        sb.AppendLine("        public string sceneName;");
        sb.AppendLine("        public string[] mapNames;");
        sb.AppendLine("        public InteractableEntry[] interactables;");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    public struct InteractableEntry");
        sb.AppendLine("    {");
        sb.AppendLine("        public string entityId;");
        sb.AppendLine("        public string mapName;");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    public static readonly SceneEntry[] Scenes = new SceneEntry[]");
        sb.AppendLine("    {");

        for (int i = 0; i < entries.Count; i++)
        {
            var e = entries[i];
            sb.AppendLine($"        new SceneEntry");
            sb.AppendLine($"        {{");
            sb.AppendLine($"            sceneName = \"{e.sceneName}\",");

            sb.Append("            mapNames = new string[] { ");
            sb.Append(string.Join(", ", e.mapNames.Select(m => $"\"{m}\"")));
            sb.AppendLine(" },");

            if (e.interactables.Length > 0)
            {
                sb.AppendLine("            interactables = new InteractableEntry[]");
                sb.AppendLine("            {");
                for (int j = 0; j < e.interactables.Length; j++)
                {
                    var ia = e.interactables[j];
                    sb.AppendLine($"                new InteractableEntry {{ entityId = \"{ia.entityId}\", mapName = \"{ia.mapName}\" }}{(j < e.interactables.Length - 1 ? "," : "")}");
                }
                sb.AppendLine("            }");
            }
            else
            {
                sb.AppendLine("            interactables = System.Array.Empty<InteractableEntry>()");
            }

            sb.Append("        }");
            if (i < entries.Count - 1) sb.Append(",");
            sb.AppendLine();
        }

        sb.AppendLine("    };");
        sb.AppendLine();
        sb.AppendLine("    public static string[] AllSceneNames");
        sb.AppendLine("    {");
        sb.AppendLine("        get");
        sb.AppendLine("        {");
        sb.AppendLine("            var names = new string[Scenes.Length];");
        sb.AppendLine("            for (int i = 0; i < Scenes.Length; i++)");
        sb.AppendLine("                names[i] = Scenes[i].sceneName;");
        sb.AppendLine("            return names;");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    public static string[] GetMapNames(string sceneName)");
        sb.AppendLine("    {");
        sb.AppendLine("        foreach (var s in Scenes)");
        sb.AppendLine("            if (s.sceneName == sceneName) return s.mapNames;");
        sb.AppendLine("        return System.Array.Empty<string>();");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    public static InteractableEntry[] GetInteractables(string sceneName)");
        sb.AppendLine("    {");
        sb.AppendLine("        foreach (var s in Scenes)");
        sb.AppendLine("            if (s.sceneName == sceneName) return s.interactables;");
        sb.AppendLine("        return System.Array.Empty<InteractableEntry>();");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    public static InteractableEntry[] GetInteractablesInCurrentScene()");
        sb.AppendLine("    {");
        sb.AppendLine("        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();");
        sb.AppendLine("        return GetInteractables(scene.name);");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        WriteFile(TeleportTargetOutputPath, sb.ToString());
        Debug.Log($"[MapCollector] 已生成 {TeleportTargetOutputPath}，共 {entries.Count} 个场景，{entries.Sum(e => e.interactables.Length)} 个交互点");
    }

    #endregion

    #region 工具方法

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

    private static void WriteFile(string path, string content)
    {
        string dir = Path.GetDirectoryName(path);
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        File.WriteAllText(path, content, new UTF8Encoding(true));
        AssetDatabase.Refresh();
    }

    private struct MapEntry
    {
        public string SceneName;
        public string MapName;
    }

    #endregion
}
