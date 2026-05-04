using System.Linq;
using UnityEngine;

public class MapManager : MonoBehaviour
{
    [Header("Maps")]
    [Tooltip("将场景中所有已存在的地图物体拖入此列表")]
    [SerializeField] private Map[] sceneMaps;

    [Tooltip("启动时要初始化的地图名称；为空时默认使用列表中的第一个有效地图")]
    [SerializeField] private string initialMapName;

    [Header("References")]
    [SerializeField] private CameraController cameraController;

    private Map currentMap;

    public Map CurrentMap => currentMap;

    public void Initialize(PlayerCharacter player)
    {
        if (sceneMaps == null || sceneMaps.Length == 0)
        {
            Debug.LogWarning("[MapManager] sceneMaps is empty, skipping map initialization.");
            return;
        }

        foreach (var map in sceneMaps)
        {
            if (map != null)
                map.gameObject.SetActive(false);
        }

        Map startMap = null;

        if (!string.IsNullOrEmpty(initialMapName))
        {
            startMap = sceneMaps.FirstOrDefault(m => m != null && m.name == initialMapName);
            if (startMap == null)
            {
                Debug.LogWarning($"[MapManager] Initial map not found: {initialMapName}");
            }
        }

        if (startMap == null)
        {
            startMap = sceneMaps.FirstOrDefault(m => m != null);
        }

        if (startMap == null)
        {
            Debug.LogError("[MapManager] No valid map found.");
            return;
        }

        LoadMap(startMap, player, true, null);
    }

    public void SwitchMap(string mapName, bool teleportPlayer = true)
    {
        Map targetMap = sceneMaps.FirstOrDefault(m => m != null && m.name == mapName);

        if (targetMap == null)
        {
            Debug.LogError($"[MapManager] Map not found: {mapName}");
            return;
        }

        LoadMap(
            targetMap,
            LevelController.Instance != null ? LevelController.Instance.PlayerCharacter : null,
            teleportPlayer,
            null
        );
    }

    public void SwitchMap(string mapName, Vector2 targetEntrance, bool teleportPlayer = true)
    {
        Map targetMap = sceneMaps.FirstOrDefault(m => m != null && m.name == mapName);

        if (targetMap == null)
        {
            Debug.LogError($"[MapManager] Map not found: {mapName}");
            return;
        }

        LoadMap(
            targetMap,
            LevelController.Instance != null ? LevelController.Instance.PlayerCharacter : null,
            teleportPlayer,
            targetEntrance
        );
    }

    public void SwitchMap(Map targetMap, bool teleportPlayer = true)
    {
        if (targetMap == null)
        {
            Debug.LogError("[MapManager] Target map is null.");
            return;
        }

        LoadMap(
            targetMap,
            LevelController.Instance != null ? LevelController.Instance.PlayerCharacter : null,
            teleportPlayer,
            null
        );
    }

    public void SwitchMap(Map targetMap, Vector2 targetEntrance, bool teleportPlayer = true)
    {
        if (targetMap == null)
        {
            Debug.LogError("[MapManager] Target map is null.");
            return;
        }

        LoadMap(
            targetMap,
            LevelController.Instance != null ? LevelController.Instance.PlayerCharacter : null,
            teleportPlayer,
            targetEntrance
        );
    }

    private void LoadMap(Map targetMap, PlayerCharacter player, bool teleportPlayer, Vector2? customEntrance)
    {
        if (targetMap == null)
            return;

        if (currentMap != null && cameraController != null)
        {
            cameraController.Moved -= currentMap.OnFocusMoved;
        }

        if (currentMap != null && currentMap != targetMap)
        {
            currentMap.gameObject.SetActive(false);
        }

        currentMap = targetMap;
        currentMap.gameObject.SetActive(true);

        if (teleportPlayer && player != null)
        {
            if (customEntrance.HasValue)
                MovePlayerToEntrance(player, currentMap, customEntrance.Value);
            else
                MovePlayerToMainEntrance(player, currentMap);
        }

        ApplyCamera(currentMap, player);

        Debug.Log($"[MapManager] Switched to map: {currentMap.name}");
    }

    private void MovePlayerToMainEntrance(PlayerCharacter player, Map map)
    {
        MovePlayerToEntrance(player, map, map.MainEntrance);
    }

    private void MovePlayerToEntrance(PlayerCharacter player, Map map, Vector2 entranceLocal)
    {
        Vector3 entranceWorld = map.transform.TransformPoint(entranceLocal);
        player.transform.position = new Vector3(
            entranceWorld.x,
            entranceWorld.y,
            entranceWorld.y
        );
    }

    private void ApplyCamera(Map map, PlayerCharacter player)
    {
        if (cameraController == null || map == null)
            return;

        cameraController.SetCameraClamp(map.CameraClampXWorld, map.CameraClampYWorld);

        if (player != null)
        {
            cameraController.SetFocusTarget(player);
            cameraController.SnapToFocusTarget();
        }

        map.Initialize();

        cameraController.Moved -= map.OnFocusMoved;
        cameraController.Moved += map.OnFocusMoved;

        Vector3 camPos3 = cameraController.transform.position;
        Vector2 camPos2 = new Vector2(camPos3.x, camPos3.y);
        map.OnFocusMoved(camPos2);
    }
}