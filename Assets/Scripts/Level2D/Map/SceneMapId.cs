// 本文件由 Tools/Map/收集所有场景的 Map 并生成枚举 自动生成，请勿手动修改
using System.Collections.Generic;

public enum SceneMapId
{
    // --- Scene_Staff_Lounge ---
    StaffLounge, // Staff Lounge
    UndergroundBoxingGym, // Underground Boxing Gym
}

public static class SceneMapIdExtensions
{
    private static readonly Dictionary<SceneMapId, string> MapNames = new Dictionary<SceneMapId, string>
    {
        { SceneMapId.StaffLounge, "Staff Lounge" },
        { SceneMapId.UndergroundBoxingGym, "Underground Boxing Gym" },
    };

    private static readonly Dictionary<SceneMapId, string> SceneNames = new Dictionary<SceneMapId, string>
    {
        { SceneMapId.StaffLounge, "Scene_Staff_Lounge" },
        { SceneMapId.UndergroundBoxingGym, "Scene_Staff_Lounge" },
    };

    public static string GetMapName(this SceneMapId id) => MapNames[id];
    public static string GetSceneName(this SceneMapId id) => SceneNames[id];
}
