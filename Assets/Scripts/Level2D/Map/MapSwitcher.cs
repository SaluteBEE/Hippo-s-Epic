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

        // 4. 传送玩家（如果有引用）
        if (teleportPlayer && LevelManager.LevelController?.PlayerCharacter != null)
        {
            // 将地图的本地入口坐标转换为该地图物体在世界中的绝对坐标
            Vector3 absoluteEntrance = _currentMap.transform.TransformPoint(_currentMap.MainEntrance);
    
            // 设置玩家位置
            LevelManager.LevelController.PlayerCharacter.transform.position = absoluteEntrance;
            
            _cameraController.SetCameraClamp(_currentMap.CameraClampX, _currentMap.CameraClampY);
        
            // 关键点：强制触发一次位置更新，让摄像机立刻瞬移到玩家当前位置，而不是等待玩家下一次移动
            // 这里假设 PlayerCharacter 是当前的 FocusTarget
            var player = LevelManager.LevelController.PlayerCharacter;
            if (player != null)
            {
                _cameraController.SetPosition(player.transform.position);
            }
        }

        Debug.Log($"[MapRepository] 已切换至地图: {mapName}");
    }
}