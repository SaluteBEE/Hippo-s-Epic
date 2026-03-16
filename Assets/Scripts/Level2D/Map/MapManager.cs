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
            Debug.LogError("[MapManager] sceneMaps is empty.");
            return;
        }

        // 启动时先全部隐藏
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

        LoadMap(startMap, player, true);
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
            teleportPlayer
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
            teleportPlayer
        );
    }

    private void LoadMap(Map targetMap, PlayerCharacter player, bool teleportPlayer)
    {
        if (targetMap == null)
            return;

        // 切换前取消旧地图的相机事件
        if (currentMap != null && cameraController != null)
        {
            cameraController.Moved -= currentMap.OnFocusMoved;
        }

        // 如果切到的是另一张地图，隐藏旧地图
        if (currentMap != null && currentMap != targetMap)
        {
            currentMap.gameObject.SetActive(false);
        }

        currentMap = targetMap;
        currentMap.gameObject.SetActive(true);

        // 先传送玩家，再更新相机；这样相机基准点才是正确的
        if (teleportPlayer && player != null)
        {
            MovePlayerToMainEntrance(player, currentMap);
        }

        ApplyCamera(currentMap, player);

        Debug.Log($"[MapManager] Switched to map: {currentMap.name}");
    }

    private void MovePlayerToMainEntrance(PlayerCharacter player, Map map)
    {
        Vector3 entranceWorld = map.transform.TransformPoint(map.MainEntrance);
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

        // 1. 先更新摄像机边界
        cameraController.SetCameraClamp(map.CameraClampXWorld, map.CameraClampYWorld);

        // 2. 如果有玩家，先让相机对准玩家
        if (player != null)
        {
            cameraController.SetFocusTarget(player);
            cameraController.SnapToFocusTarget();
        }

        // 3. 读取当前相机位置，作为这张地图的视差基准点
        Vector3 camPos3 = cameraController.transform.position;
        Vector2 camPos2 = new Vector2(camPos3.x, camPos3.y);

        // 4. 用当前相机位置初始化地图的视差系统
        map.Initialize(camPos2);

        // 5. 重新绑定地图对相机移动的监听
        cameraController.Moved -= map.OnFocusMoved;
        cameraController.Moved += map.OnFocusMoved;

        // 6. 初始化后立刻同步一次，确保视差层位置正确
        map.OnFocusMoved(camPos2);
    }
}