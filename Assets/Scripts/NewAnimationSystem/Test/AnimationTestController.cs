using UnityEngine;
using System.Collections.Generic;

public class AnimationTestController : MonoBehaviour
{
    [Header("动画控制器")]
    [SerializeField] private AnimationController animationController;
    
    [Header("测试配置")]
    [SerializeField] private string testComposition = "Idle";
    [SerializeField] private KeyCode testKey = KeyCode.Space;
    [SerializeField] private bool autoInitialize = true;
    
    [Header("动画切换测试")]
    [SerializeField] private string[] testClips = { "idle", "walk", "run" };
    [SerializeField] private int currentClipIndex = 0;
    
    [Header("组合动画测试")]
    [SerializeField] private string[] testCompositions = { "Idle", "Walk", "Run" };
    [SerializeField] private int currentCompositionIndex = 0;
    private List<CompositionName> availableCompositions = new List<CompositionName>();
    
    [Header("调试选项")]
    [SerializeField] private string configAssetPath = "Assets/Configs/Animations/玩家占位符_AnimationConfig.asset";
    
    [Header("层级权重测试")]
    [SerializeField] private string weightTestLayer = "UpperBody";
    [SerializeField] private float[] weightTestValues = { 0f, 0.25f, 0.5f, 0.75f, 1f };
    [SerializeField] private int currentWeightIndex = 0;
    
    private void Start()
    {
        if (animationController == null)
            animationController = GetComponent<AnimationController>();
            
        if (animationController == null)
        {
            Debug.LogError("AnimationTestController: 未找到 AnimationController 组件");
            return;
        }
            
        if (autoInitialize && !animationController.IsInitialized)
        {
            animationController.Initialize();
            if (!animationController.IsInitialized)
            {
                Debug.LogWarning("AnimationTestController: AnimationController 初始化失败，尝试加载配置...");
                
                // 尝试加载配置并重新初始化
                LoadAndAssignConfig();
                
                if (animationController.Config != null)
                {
                    animationController.Initialize();
                    if (animationController.IsInitialized)
                    {
                        Debug.Log("AnimationController 重新初始化成功");
                    }
                    else
                    {
                        Debug.LogError("AnimationTestController: 重新初始化失败");
                        return;
                    }
                }
                else
                {
                    Debug.LogError("AnimationTestController: 无法加载动画配置");
                    return;
                }
            }
        }
        
        // 动态获取可用的组合名称
        UpdateAvailableCompositions();
        
        // 播放初始组合
        if (!string.IsNullOrEmpty(testComposition) && animationController.IsInitialized)
        {
            if (System.Enum.TryParse<CompositionName>(testComposition, true, out var enumName) && availableCompositions.Contains(enumName))
            {
                animationController.PlayComposition(enumName);
                Debug.Log($"播放初始组合: {testComposition}");
                currentCompositionIndex = availableCompositions.IndexOf(enumName);
            }
            else if (availableCompositions.Count > 0)
            {
                testComposition = availableCompositions[0].ToString();
                animationController.PlayComposition(availableCompositions[0]);
                Debug.Log($"播放第一个可用组合: {testComposition}");
                currentCompositionIndex = 0;
            }
            else
            {
                Debug.LogWarning("AnimationTestController: 未找到任何可用的动画组合");
            }
        }
    }
    
    private void Update()
    {
        if (animationController == null || !animationController.IsInitialized)
            return;
            
        HandleKeyboardInput();
    }
    
    private void HandleKeyboardInput()
    {
        // 空格键：切换单个动画片段
        if (Input.GetKeyDown(testKey))
        {
            TestNextClip();
        }
        
        // 数字键1-9：切换组合动画（动态支持最多9个组合）
        for (int i = 0; i < Mathf.Min(testCompositions.Length, 9); i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i))
            {
                PlayTestComposition(i);
                break;
            }
        }
        
        // 数字键4-6：测试层级权重
        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            TestNextWeight();
        }
        else if (Input.GetKeyDown(KeyCode.Alpha5))
        {
            SetLayerWeight(0.5f);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha6))
        {
            SetLayerWeight(1.0f);
        }
        
        // 功能键测试
        if (Input.GetKeyDown(KeyCode.S))
        {
            StopCurrentLayer();
        }
        else if (Input.GetKeyDown(KeyCode.A))
        {
            StopAllAnimations();
        }
        else if (Input.GetKeyDown(KeyCode.I))
        {
            InitializeController();
        }
        
        // 速度控制
        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            IncreaseAnimationSpeed();
        }
        else if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            DecreaseAnimationSpeed();
        }
    }
    
    private void TestNextClip()
    {
        if (testClips.Length == 0) return;
        
        currentClipIndex = (currentClipIndex + 1) % testClips.Length;
        string clipName = testClips[currentClipIndex];
        
        // 播放到基础层
        animationController.PlayClip("Base", clipName, 0.2f);
        Debug.Log($"Playing clip: {clipName} on Base layer");
    }
    
    private void PlayTestComposition(int index)
    {
        List<CompositionName> compositionList = availableCompositions.Count > 0 ? availableCompositions : 
                                               new List<CompositionName>();

        if (index >= 0 && index < compositionList.Count)
        {
            CompositionName compositionName = compositionList[index];
            
            if (animationController.IsInitialized)
            {
                animationController.PlayComposition(compositionName);
                Debug.Log($"播放组合动画: {compositionName}");
                currentCompositionIndex = index;
                
                if (testCompositions != null && index < testCompositions.Length)
                {
                    testComposition = compositionName.ToString();
                }
            }
            else
            {
                Debug.LogWarning($"无法播放组合 {compositionName}: AnimationController 未初始化");
            }
        }
        else
        {
            Debug.LogWarning($"组合索引 {index} 超出范围 (0-{compositionList.Count - 1})");
        }
    }
    
    private void TestNextWeight()
    {
        if (weightTestValues.Length == 0) return;
        
        currentWeightIndex = (currentWeightIndex + 1) % weightTestValues.Length;
        float weight = weightTestValues[currentWeightIndex];
        
        SetLayerWeight(weight);
    }
    
    private void SetLayerWeight(float weight)
    {
        animationController.SetLayerWeight(weightTestLayer, weight);
        Debug.Log($"Set {weightTestLayer} weight to {weight:F2}");
    }
    
    private void StopCurrentLayer()
    {
        if (testClips.Length > 0 && currentClipIndex < testClips.Length)
        {
            string layerName = "Base";
            animationController.StopLayer(layerName);
            Debug.Log($"Stopped layer: {layerName}");
        }
    }
    
    private void StopAllAnimations()
    {
        animationController.StopAll();
        Debug.Log("Stopped all animations");
    }
    
    private void UpdateAvailableCompositions()
    {
        availableCompositions.Clear();
        
        if (animationController == null || !animationController.IsInitialized)
        {
            Debug.LogWarning("无法获取组合列表: AnimationController 未初始化");
            return;
        }
        
        var compositionNames = animationController.GetCompositionNames();
        if (compositionNames != null && compositionNames.Count > 0)
        {
            availableCompositions.AddRange(compositionNames);
            Debug.Log($"找到 {availableCompositions.Count} 个可用组合: {string.Join(", ", availableCompositions)}");
            
            if (testCompositions == null || testCompositions.Length == 0 || 
                !ArraysMatch(testCompositions, availableCompositions))
            {
                testCompositions = new string[availableCompositions.Count];
                for (int i = 0; i < availableCompositions.Count; i++)
                    testCompositions[i] = availableCompositions[i].ToString();
                Debug.Log("已更新测试组合列表以匹配可用组合");
            }
        }
        else
        {
            Debug.LogWarning("AnimationConfig 中未定义任何动画组合");
        }
    }
    
    private bool ArraysMatch(string[] array1, List<CompositionName> list2)
    {
        if (array1 == null || list2 == null) return false;
        if (array1.Length != list2.Count) return false;
        
        for (int i = 0; i < array1.Length; i++)
        {
            if (array1[i] != list2[i].ToString()) return false;
        }
        return true;
    }
    
    private string GetConfigurationStatus()
    {
        if (animationController == null)
            return "动画控制器: 未分配";
            
        if (!animationController.IsInitialized)
            return "动画控制器: 未初始化";
            
        var config = animationController.Config;
        if (config == null)
            return "动画配置: 未分配";
            
        return $"动画配置: {config.name}\n" +
               $"剪辑数: {config.clips?.Count ?? 0}, " +
               $"图层数: {config.layers?.Count ?? 0}, " +
               $"组合数: {config.compositions?.Count ?? 0}";
    }
    
    private void LoadAndAssignConfig()
    {
        if (animationController == null)
        {
            Debug.LogError("无法加载配置: AnimationController 未分配");
            return;
        }
        
        #if UNITY_EDITOR
        var config = UnityEditor.AssetDatabase.LoadAssetAtPath<AnimationConfig>(configAssetPath);
        if (config != null)
        {
            animationController.SetConfig(config);
            Debug.Log($"已分配动画配置: {config.name}");
            
            // 重新初始化
            if (animationController.IsInitialized)
            {
                animationController.Initialize(); // 重新初始化以应用新配置
                UpdateAvailableCompositions();
            }
        }
        else
        {
            Debug.LogError($"无法加载动画配置: {configAssetPath}");
        }
        #else
        Debug.LogWarning("LoadAndAssignConfig 仅在编辑器模式下可用");
        #endif
    }
    
    private void InitializeController()
    {
        if (!animationController.IsInitialized)
        {
            animationController.Initialize();
            if (animationController.IsInitialized)
            {
                Debug.Log("AnimationController 初始化成功");
                UpdateAvailableCompositions();
            }
            else
            {
                Debug.LogError("AnimationController 初始化失败");
            }
        }
        else
        {
            Debug.Log("AnimationController 已经初始化");
        }
    }
    
    private void IncreaseAnimationSpeed()
    {
        string layerName = "Base";
        float currentSpeed = 1.0f; // 这里应该从控制器获取当前速度
        float newSpeed = Mathf.Min(currentSpeed + 0.2f, 3.0f);
        animationController.SetAnimationSpeed(layerName, newSpeed);
        Debug.Log($"Set {layerName} speed to {newSpeed:F1}");
    }
    
    private void DecreaseAnimationSpeed()
    {
        string layerName = "Base";
        float currentSpeed = 1.0f; // 这里应该从控制器获取当前速度
        float newSpeed = Mathf.Max(currentSpeed - 0.2f, 0.1f);
        animationController.SetAnimationSpeed(layerName, newSpeed);
        Debug.Log($"Set {layerName} speed to {newSpeed:F1}");
    }
    
    // 显示当前状态
    private void OnGUI()
    {
        if (animationController == null || !animationController.IsInitialized)
            return;
            
        GUILayout.BeginArea(new Rect(10, 10, 300, 400));
        GUILayout.Label("=== 动画系统测试 ===", GUI.skin.box);
        
        GUILayout.Space(10);
        GUILayout.Label("控制说明:");
        GUILayout.Label("空格键: 切换动画片段");
        GUILayout.Label($"1-{Mathf.Min(testCompositions.Length, 9)}: 播放组合动画");
        GUILayout.Label("4: 切换层级权重");
        GUILayout.Label("5: 设置权重0.5");
        GUILayout.Label("6: 设置权重1.0");
        GUILayout.Label("S: 停止当前层级");
        GUILayout.Label("A: 停止所有动画");
        GUILayout.Label("I: 初始化控制器");
        GUILayout.Label("↑/↓: 调整动画速度");
        
        GUILayout.Space(10);
        GUILayout.Label("当前状态:");
        GUILayout.Label($"当前动画片段: {testClips[currentClipIndex]}");
        
        // 显示当前组合
        string currentCompName = "无";
        if (availableCompositions.Count > 0 && currentCompositionIndex < availableCompositions.Count)
        {
            currentCompName = availableCompositions[currentCompositionIndex].ToString();
        }
        else if (testCompositions.Length > 0 && currentCompositionIndex < testCompositions.Length)
        {
            currentCompName = testCompositions[currentCompositionIndex];
        }
        GUILayout.Label($"当前组合: {currentCompName}");
        
        GUILayout.Label($"当前权重: {weightTestValues[currentWeightIndex]:F2}");
        
        // 显示配置状态
        GUILayout.Space(10);
        GUILayout.Label("配置状态:");
        GUILayout.Label(GetConfigurationStatus());
        
        // 配置管理按钮
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("加载配置"))
        {
            LoadAndAssignConfig();
        }
        if (GUILayout.Button("重新初始化"))
        {
            InitializeController();
        }
        GUILayout.EndHorizontal();
        
        // 显示可用组合列表
        GUILayout.Space(10);
        GUILayout.Label($"可用组合 ({availableCompositions.Count}个):");
        for (int i = 0; i < availableCompositions.Count; i++)
        {
            string compName = availableCompositions[i].ToString();
            string indicator = (i == currentCompositionIndex) ? "▶ " : "  ";
            GUILayout.Label($"{indicator}{i+1}. {compName}");
        }
        
        if (availableCompositions.Count == 0)
        {
            GUILayout.Label("  (无可用组合)");
        }
        
        GUILayout.Space(10);
        GUILayout.Label("可用层级:");
        foreach (var layerName in animationController.GetLayerNames())
        {
            bool isPlaying = animationController.IsPlaying(layerName);
            float weight = animationController.GetLayerWeight(layerName);
            GUILayout.Label($"  {layerName}: {(isPlaying ? "播放中" : "停止")}, 权重: {weight:F2}");
        }
        
        GUILayout.EndArea();
    }
    
    // 编辑器按钮
    #if UNITY_EDITOR
    [ContextMenu("测试下一个动画片段")]
    private void TestNextClipEditor()
    {
        TestNextClip();
    }
    
    [ContextMenu("播放第一个组合")]
    private void PlayFirstComposition()
    {
        PlayTestComposition(0);
    }
    
    [ContextMenu("播放下一个组合")]
    private void PlayNextComposition()
    {
        int nextIndex = (currentCompositionIndex + 1) % Mathf.Max(availableCompositions.Count, testCompositions.Length);
        PlayTestComposition(nextIndex);
    }
    
    [ContextMenu("更新组合列表")]
    private void UpdateCompositionsList()
    {
        UpdateAvailableCompositions();
    }
    
    [ContextMenu("加载并分配配置")]
    private void LoadAndAssignConfigEditor()
    {
        LoadAndAssignConfig();
    }
    
    [ContextMenu("测试下一个权重")]
    private void TestNextWeightEditor()
    {
        TestNextWeight();
    }
    
    [ContextMenu("停止所有动画")]
    private void StopAllAnimationsEditor()
    {
        StopAllAnimations();
    }
    
    [ContextMenu("初始化控制器")]
    private void InitializeControllerEditor()
    {
        InitializeController();
    }
    #endif
}