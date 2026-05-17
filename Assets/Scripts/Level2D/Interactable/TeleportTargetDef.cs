// ============================================================
// 自动生成文件 - 勿手动修改
// 生成时间: 2026-05-17 18:22:33
// 通过 Tools/Map/收集所有场景的传送目标 重新生成
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
            sceneName = "Scene_Staff_Lounge",
            mapNames = new string[] { "Staff Lounge", "Underground Boxing Gym" },
            interactables = new InteractableEntry[]
            {
                new InteractableEntry { entityId = "NPC_NPC_Coach", mapName = "Staff Lounge" },
                new InteractableEntry { entityId = "trans_Teleporter_To_Gym", mapName = "Staff Lounge" },
                new InteractableEntry { entityId = "trans_Transporter", mapName = "Underground Boxing Gym" }
            }
        },
        new SceneEntry
        {
            sceneName = "Temple",
            mapNames = new string[] { "temple" },
            interactables = System.Array.Empty<InteractableEntry>()
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
