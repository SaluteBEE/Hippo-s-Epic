// ============================================================
// 自动生成文件 - 勿手动修改
// 生成时间: 2026-08-02 22:53:13
// 通过 Tools/Map/收集所有场景数据 重新生成
// ============================================================

public static class TeleportTargetDef
{
    public struct SceneEntry
    {
        public string sceneName;
        public string[] mapNames;
        public InteractableEntry[] interactables;
    }

    public struct InteractableEntry
    {
        public string entityId;
        public string mapName;
    }

    public static readonly SceneEntry[] Scenes = new SceneEntry[]
    {
        new SceneEntry
        {
            sceneName = "Scene_Outer_City",
            mapNames = new string[] { "Behind_Moon", "Outer_City_Ground" },
            interactables = new InteractableEntry[]
            {
                new InteractableEntry { entityId = "alien_02_alienB", mapName = "Behind_Moon" },
                new InteractableEntry { entityId = "alien_01_alienA", mapName = "Behind_Moon" }
            }
        }
    };

    public static string[] AllSceneNames
    {
        get
        {
            var names = new string[Scenes.Length];
            for (int i = 0; i < Scenes.Length; i++)
                names[i] = Scenes[i].sceneName;
            return names;
        }
    }

    public static string[] GetMapNames(string sceneName)
    {
        foreach (var s in Scenes)
            if (s.sceneName == sceneName) return s.mapNames;
        return System.Array.Empty<string>();
    }

    public static InteractableEntry[] GetInteractables(string sceneName)
    {
        foreach (var s in Scenes)
            if (s.sceneName == sceneName) return s.interactables;
        return System.Array.Empty<InteractableEntry>();
    }

    public static InteractableEntry[] GetInteractablesInCurrentScene()
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        return GetInteractables(scene.name);
    }
}
