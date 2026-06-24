// 本文件由 Tools/Map/收集所有场景数据 自动生成，请勿手动修改
using System.Collections.Generic;

public enum SceneMapId
{
    // --- Scene_Outer_City ---
    OuterCityGround, // Outer_City_Ground

    // --- Scene_Punk_City ---
    PunkCityGround, // Punk_City_Ground

    // --- Scene_Sewer ---
    SewerSurface, // Sewer_Surface

    // --- Scene_Staff_Lounge ---
    StaffLounge, // Staff Lounge
    UndergroundBoxingGym, // Underground Boxing Gym

    // --- Temple ---
    Temple, // temple
}

public static class SceneMapIdExtensions
{
    private static readonly Dictionary<SceneMapId, string> MapNames = new Dictionary<SceneMapId, string>
    {
        { SceneMapId.OuterCityGround, "Outer_City_Ground" },
        { SceneMapId.PunkCityGround, "Punk_City_Ground" },
        { SceneMapId.SewerSurface, "Sewer_Surface" },
        { SceneMapId.StaffLounge, "Staff Lounge" },
        { SceneMapId.UndergroundBoxingGym, "Underground Boxing Gym" },
        { SceneMapId.Temple, "temple" },
    };

    private static readonly Dictionary<SceneMapId, string> SceneNames = new Dictionary<SceneMapId, string>
    {
        { SceneMapId.OuterCityGround, "Scene_Outer_City" },
        { SceneMapId.PunkCityGround, "Scene_Punk_City" },
        { SceneMapId.SewerSurface, "Scene_Sewer" },
        { SceneMapId.StaffLounge, "Scene_Staff_Lounge" },
        { SceneMapId.UndergroundBoxingGym, "Scene_Staff_Lounge" },
        { SceneMapId.Temple, "Temple" },
    };

    private static readonly Dictionary<string, SceneMapId> MapNameToId = new Dictionary<string, SceneMapId>
    {
        { "Outer_City_Ground", SceneMapId.OuterCityGround },
        { "Punk_City_Ground", SceneMapId.PunkCityGround },
        { "Sewer_Surface", SceneMapId.SewerSurface },
        { "Staff Lounge", SceneMapId.StaffLounge },
        { "Underground Boxing Gym", SceneMapId.UndergroundBoxingGym },
        { "temple", SceneMapId.Temple },
    };

    public static string GetMapName(this SceneMapId id) => MapNames[id];
    public static string GetSceneName(this SceneMapId id) => SceneNames[id];
    public static SceneMapId GetSceneMapIdByMapName(string mapName) => MapNameToId.TryGetValue(mapName, out var id) ? id : SceneMapId.StaffLounge;
    public static IEnumerable<string> GetAllMapNames() => MapNameToId.Keys;
}
