using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class TeleportTargetCollector
{
    private const string OutputPath = "Assets/Scripts/Level2D/Interactable/TeleportTargetDef.cs";
    private const string MenuPath = "Tools/Map/收集所有场景的传送目标";

    [MenuItem(MenuPath)]
    public static void CollectAndGenerate()
    {
        var sceneFiles = Directory.GetFiles("Assets/Scenes", "*.unity")
            .OrderBy(f => f)
            .ToList();

        var entries = new List<SceneData>();

        string activeScenePath = EditorSceneManager.GetActiveScene().path;

        for (int i = 0; i < sceneFiles.Count; i++)
        {
            string scenePath = sceneFiles[i].Replace('\\', '/');
            float progress = (float)i / sceneFiles.Count;

            if (EditorUtility.DisplayCancelableProgressBar(
                "收集传送目标", $"扫描 {Path.GetFileNameWithoutExtension(scenePath)} ...", progress))
                break;

            var data = CollectFromScene(scenePath);
            if (data != null)
                entries.Add(data.Value);
        }

        EditorUtility.ClearProgressBar();

        if (!string.IsNullOrEmpty(activeScenePath) && EditorSceneManager.GetActiveScene().path != activeScenePath)
            EditorSceneManager.OpenScene(activeScenePath);

        GenerateCode(entries);
        AssetDatabase.Refresh();
        Debug.Log($"[TeleportTargetCollector] 生成完成，共 {entries.Count} 个场景，" +
                  $"{entries.Sum(e => e.interactables.Length)} 个交互点");
    }

    private struct SceneData
    {
        public string sceneName;
        public string[] mapNames;
        public InteractableData[] interactables;
    }

    private struct InteractableData
    {
        public string entityId;
        public string mapName;
        public string[] types;
    }

    private static SceneData? CollectFromScene(string scenePath)
    {
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

        var maps = scene.GetRootGameObjects()
            .SelectMany(go => go.GetComponentsInChildren<Map>(true))
            .ToList();

        var interactables = scene.GetRootGameObjects()
            .SelectMany(go => go.GetComponentsInChildren<Interactable>(true))
            .ToList();

        if (maps.Count == 0 && interactables.Count == 0)
            return null;

        var mapNames = maps.Select(m => m.name).OrderBy(n => n).ToArray();

        var iDataList = new List<InteractableData>();
        foreach (var ia in interactables)
        {
            string entityId = ia.EntityId;
            string mapName = FindParentMapName(ia.transform, maps);
            var types = CollectInteractionTypes(ia);

            iDataList.Add(new InteractableData
            {
                entityId = entityId,
                mapName = mapName ?? "",
                types = types
            });
        }

        return new SceneData
        {
            sceneName = Path.GetFileNameWithoutExtension(scenePath),
            mapNames = mapNames,
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

    private static string[] CollectInteractionTypes(Interactable ia)
    {
        var types = new HashSet<string>();
        ia.GetPhase(0)?.MigrateIfNeeded();

        var phases = new System.Collections.Generic.List<InteractionPhase>();
        var serialized = new SerializedObject(ia);
        var phasesProp = serialized.FindProperty("phases");
        for (int i = 0; i < phasesProp.arraySize; i++)
        {
            var phaseProp = phasesProp.GetArrayElementAtIndex(i);
            var buttonsProp = phaseProp.FindPropertyRelative("buttons");
            for (int j = 0; j < buttonsProp.arraySize; j++)
            {
                var btnProp = buttonsProp.GetArrayElementAtIndex(j);
                var typeProp = btnProp.FindPropertyRelative("type");
                types.Add(typeProp.enumNames[typeProp.enumValueIndex]);
            }
        }
        return types.OrderBy(t => t).ToArray();
    }

    private static void GenerateCode(List<SceneData> entries)
    {
        var sb = new StringBuilder();
        sb.AppendLine("// ============================================================");
        sb.AppendLine("// 自动生成文件 - 勿手动修改");
        sb.AppendLine($"// 生成时间: {System.DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine("// 通过 Tools/Map/收集所有场景的传送目标 重新生成");
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

        string dir = Path.GetDirectoryName(OutputPath);
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        File.WriteAllText(OutputPath, sb.ToString(), new UTF8Encoding(true));
    }
}
