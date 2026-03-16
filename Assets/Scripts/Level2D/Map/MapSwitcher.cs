using UnityEngine;
using System.Linq;
public class MapSwitcher : MonoBehaviour
{
    public static MapSwitcher Instance { get; private set; }

    [Tooltip("将场景中所有已存在的地图物体拖入此列表")]
    [SerializeField] private Map[] sceneMaps;
    
    [SerializeField] CameraController _cameraController;

    private Map _currentMap;

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;

        // // 隐藏所有地图，初始化时只保留一个或全部隐藏
        // foreach (var map in sceneMaps)
        // {
        //     if (map != null) map.gameObject.SetActive(false);
        // }
    }

    /// <summary>
    /// 切换当前活动的地图
    /// </summary>
    /// <param name="mapName">地图物体的名称</param>
    /// <param name="teleportPlayer">是否自动传送玩家到入口</param>
    public void SwitchMap(string mapName, bool teleportPlayer = true)
    {
        Map targetMap = sceneMaps.FirstOrDefault(m => m.name == mapName);

        if (targetMap == null)
        {
            Debug.LogError($"[MapRepository] 未找到名为 {mapName} 的地图！");
            return;
        }

        // 1. 关闭当前地图
        if (_currentMap != null) _currentMap.gameObject.SetActive(false);

        // 2. 激活目标地图
        _currentMap = targetMap;
        _currentMap.gameObject.SetActive(true);
            
        // 3. 初始化地图（如果你的地图有初始化逻辑）
        _currentMap.Initialize();

        // 4. 传送玩家
        if (teleportPlayer && LevelManager.LevelController?.PlayerCharacter != null)
        {
            var player = LevelManager.LevelController.PlayerCharacter;

            // 入口世界坐标
            Vector3 absoluteEntrance = _currentMap.transform.TransformPoint(_currentMap.MainEntrance);

            // 玩家位置
            player.transform.position = new Vector3(
                absoluteEntrance.x,
                absoluteEntrance.y,
                absoluteEntrance.y
            );

            // 更新摄像机限制
            _cameraController.SetCameraClamp(_currentMap.CameraClampX, _currentMap.CameraClampY);

            // 重新绑定 FocusTarget（防止地图事件残留）
            _cameraController.SetFocusTarget(player);

            // 强制同步摄像机
            _cameraController.SetPosition(new Vector2(
                player.transform.position.x,
                player.transform.position.y
            ));
        }

        Debug.Log($"[MapRepository] 已切换至地图: {mapName}");
    }
}