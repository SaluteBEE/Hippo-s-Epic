// ============================================================
// 自动生成文件 - 勿手动修改
// 生成时间: 2026-08-30 21:34:29
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
            sceneName = "Scene_Staff_Lounge",
            mapNames = new string[] { "Out_Side_Gym", "Staff Lounge", "Underground Boxing Gym" },
            interactables = new InteractableEntry[]
            {
                new InteractableEntry { entityId = "coach_fight_coach", mapName = "Underground Boxing Gym" },
                new InteractableEntry { entityId = "computer Mirror_Mirror", mapName = "Staff Lounge" },
                new InteractableEntry { entityId = "Toliet_toliet", mapName = "Underground Boxing Gym" },
                new InteractableEntry { entityId = "Toilet_toliet", mapName = "Staff Lounge" },
                new InteractableEntry { entityId = "trans_Transporter", mapName = "Out_Side_Gym" },
                new InteractableEntry { entityId = "Trash_trash", mapName = "Underground Boxing Gym" },
                new InteractableEntry { entityId = "Challenger 2_Challenger", mapName = "Underground Boxing Gym" },
                new InteractableEntry { entityId = "Cup_Cup", mapName = "Staff Lounge" },
                new InteractableEntry { entityId = "trans_Transporter", mapName = "Underground Boxing Gym" },
                new InteractableEntry { entityId = "quanwang_bk_champion", mapName = "Underground Boxing Gym" },
                new InteractableEntry { entityId = "trans_Teleporter_To_Gym", mapName = "Staff Lounge" }
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
