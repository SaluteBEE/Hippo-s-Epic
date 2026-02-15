using UnityEngine;

/// <summary>
/// Level系统测试脚本，用于验证地图切换、传送点等功能。
/// 挂载到Level GameObject上，提供测试用的输入和调试信息。
/// </summary>
public class LevelTest : MonoBehaviour
{
    [Header("Test Settings")]
    [SerializeField] private bool enableDebugLogs = true;
    [SerializeField] private bool showGizmos = true;

    [Header("Test References")]
    [SerializeField] private Level level;
    [SerializeField] private PlayerCharacter player;

    private void OnValidate()
    {
        // 自动获取Level和PlayerCharacter引用
        if (level == null)
            level = GetComponent<Level>();
        
        if (player == null)
            player = GetComponentInChildren<PlayerCharacter>();
    }

    private void Start()
    {
        if (enableDebugLogs)
        {
            Debug.Log("=== Level System Test Started ===");
            Debug.Log($"Level: {(level != null ? "Found" : "NOT FOUND")}");
            Debug.Log($"Player: {(player != null ? "Found" : "NOT FOUND")}");
        }
    }

    private void Update()
    {
        HandleTestInputs();
    }

    /// <summary>
    /// 处理测试用的输入（数字键1-9用于测试功能）。
    /// </summary>
    private void HandleTestInputs()
    {
        // 按T键切换调试日志
        if (Input.GetKeyDown(KeyCode.T))
        {
            enableDebugLogs = !enableDebugLogs;
            Debug.Log($"Debug Logs: {(enableDebugLogs ? "Enabled" : "Disabled")}");
        }

        // 按P键输出玩家位置信息
        if (Input.GetKeyDown(KeyCode.P) && player != null)
        {
            Debug.Log($"Player Position: {player.transform.position}");
            Debug.Log($"Player Control Enabled: {player.IsControlEnabled}");
        }

        // 按M键输出当前地图信息
        if (Input.GetKeyDown(KeyCode.M) && level != null)
        {
            // 通过反射获取私有字段（仅用于测试）
            var currentMapField = typeof(Level).GetField("currentMap", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (currentMapField != null)
            {
                var currentMap = currentMapField.GetValue(level) as MapBehaviour;
                if (currentMap != null)
                {
                    Debug.Log($"Current Map: {currentMap.name}");
                    Debug.Log($"Map Objects Count: {(currentMap.MapObjects != null ? currentMap.MapObjects.Length : 0)}");
                }
            }
        }

        // 按数字键1传送玩家到测试位置1（中心）
        if (Input.GetKeyDown(KeyCode.Alpha1) && player != null)
        {
            player.TeleportTo(new Vector2(0, 0), 0);
            if (enableDebugLogs) Debug.Log("Teleported player to (0, 0, 0)");
        }

        // 按数字键2传送玩家到测试位置2（右上）
        if (Input.GetKeyDown(KeyCode.Alpha2) && player != null)
        {
            player.TeleportTo(new Vector2(5, 5), 0);
            if (enableDebugLogs) Debug.Log("Teleported player to (5, 5, 0)");
        }

        // 按数字键3传送玩家到测试位置3（左下）
        if (Input.GetKeyDown(KeyCode.Alpha3) && player != null)
        {
            player.TeleportTo(new Vector2(-5, -5), 0);
            if (enableDebugLogs) Debug.Log("Teleported player to (-5, -5, 0)");
        }

        // 按数字键0切换玩家控制
        if (Input.GetKeyDown(KeyCode.Alpha0) && player != null)
        {
            bool newState = !player.IsControlEnabled;
            player.EnableControl(newState);
            if (enableDebugLogs) Debug.Log($"Player Control: {(newState ? "Enabled" : "Disabled")}");
        }
    }

    private void OnGUI()
    {
        if (!enableDebugLogs) return;

        GUILayout.BeginArea(new Rect(10, 10, 300, 400));
        GUILayout.BeginVertical("box");

        GUILayout.Label("=== Level System Test ===");
        GUILayout.Space(10);

        GUILayout.Label("Test Controls:");
        GUILayout.Label("T - Toggle Debug Logs");
        GUILayout.Label("P - Print Player Info");
        GUILayout.Label("M - Print Map Info");
        GUILayout.Label("1/2/3 - Teleport Player");
        GUILayout.Label("0 - Toggle Player Control");
        GUILayout.Space(10);

        GUILayout.Label("Game Controls:");
        GUILayout.Label("WASD - Move Player");
        GUILayout.Label("E - Interact");
        GUILayout.Space(10);

        if (player != null)
        {
            GUILayout.Label($"Player Pos: {player.transform.position}");
            GUILayout.Label($"Control: {(player.IsControlEnabled ? "ON" : "OFF")}");
        }

        GUILayout.EndVertical();
        GUILayout.EndArea();
    }

    private void OnDrawGizmos()
    {
        if (!showGizmos) return;

        // 绘制原点
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(Vector3.zero, 0.5f);

        // 绘制测试位置
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(new Vector3(5, 5, 0), 0.3f);
        Gizmos.DrawWireSphere(new Vector3(-5, -5, 0), 0.3f);

        // 绘制玩家位置
        if (player != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(player.transform.position, Vector3.one * 0.5f);
        }
    }
}
