using UnityEngine;
using UnityEditor;
using System.IO;
using Spine.Unity;

[CustomEditor(typeof(AnimationController))]
public class AnimationControllerEditor : Editor
{
    private AnimationController animationController;
    private SerializedProperty configProperty;
    private SerializedProperty debugLogProperty;
    private SerializedProperty autoInitializeProperty;
    
    private void OnEnable()
    {
        animationController = (AnimationController)target;
        configProperty = serializedObject.FindProperty("config");
        debugLogProperty = serializedObject.FindProperty("debugLog");
        autoInitializeProperty = serializedObject.FindProperty("autoInitialize");
        
        // 检查是否已有配置，如果没有则尝试自动创建
        if (configProperty.objectReferenceValue == null)
        {
            TryAutoCreateConfigAsset();
        }
    }
    
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("动画控制器配置", EditorStyles.boldLabel);
        EditorGUILayout.Space();
        
        // 配置字段
        EditorGUILayout.PropertyField(configProperty, new GUIContent("动画配置", "AnimationConfig ScriptableObject 资产"));
        
        // 配置创建按钮
        EditorGUILayout.Space();
        EditorGUILayout.BeginHorizontal();
        
        if (GUILayout.Button("创建配置资产", GUILayout.Width(120)))
        {
            CreateConfigAsset();
        }
        
        if (GUILayout.Button("重新生成配置", GUILayout.Width(120)))
        {
            RegenerateConfigAsset();
        }
        
        EditorGUILayout.EndHorizontal();
        
        // 配置状态信息
        if (configProperty.objectReferenceValue != null)
        {
            var config = (AnimationConfig)configProperty.objectReferenceValue;
            EditorGUILayout.HelpBox($"配置状态: {config.name}\n动画片段: {config.clips.Count}\n层级: {config.layers.Count}\n组合: {config.compositions.Count}", 
                                   MessageType.Info);
        }
        else
        {
            EditorGUILayout.HelpBox("未分配动画配置。请点击上方按钮创建或手动分配一个 AnimationConfig 资产。", 
                                   MessageType.Warning);
        }
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("调试设置", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(debugLogProperty, new GUIContent("启用调试日志"));
        EditorGUILayout.PropertyField(autoInitializeProperty, new GUIContent("自动初始化"));
        
        EditorGUILayout.Space();
        
        // 操作按钮
        if (GUILayout.Button("初始化控制器"))
        {
            animationController.Initialize();
            EditorUtility.SetDirty(animationController);
        }
        
        if (GUILayout.Button("停止所有动画"))
        {
            animationController.StopAll();
        }
        
        serializedObject.ApplyModifiedProperties();
    }
    
    private void TryAutoCreateConfigAsset()
    {
        // 尝试从默认路径查找配置
        string defaultConfigPath = GetDefaultConfigPath();
        if (File.Exists(defaultConfigPath))
        {
            var existingConfig = AssetDatabase.LoadAssetAtPath<AnimationConfig>(defaultConfigPath);
            if (existingConfig != null)
            {
                configProperty.objectReferenceValue = existingConfig;
                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(animationController);
                Debug.Log($"找到现有配置并自动分配: {defaultConfigPath}");
                return;
            }
        }
        
        // 询问用户是否创建配置
        if (EditorUtility.DisplayDialog("创建动画配置", 
            $"没有为 {animationController.gameObject.name} 找到动画配置。是否创建新的配置资产？", 
            "创建", "稍后"))
        {
            CreateConfigAsset();
        }
    }
    
    private void CreateConfigAsset()
    {
        string configPath = GetDefaultConfigPath();
        string folderPath = Path.GetDirectoryName(configPath);
        
        // 确保目录存在
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
            AssetDatabase.Refresh();
        }
        
        // 避免覆盖现有文件
        if (File.Exists(configPath))
        {
            if (!EditorUtility.DisplayDialog("配置已存在", 
                $"配置资产已存在于路径: {configPath}\n是否覆盖？", 
                "覆盖", "取消"))
            {
                return;
            }
        }
        
        // 创建新的配置资产
        AnimationConfig newConfig = ScriptableObject.CreateInstance<AnimationConfig>();
        
        // 尝试获取骨骼数据
        var skeletonAnimation = animationController.GetComponent<SkeletonAnimation>();
        if (skeletonAnimation != null && skeletonAnimation.SkeletonDataAsset != null)
        {
            newConfig.skeletonData = skeletonAnimation.SkeletonDataAsset;
        }
        else
        {
            Debug.LogWarning("无法获取骨骼数据，请稍后手动设置。");
        }
        
        // 添加默认层级
        AddDefaultLayers(newConfig);
        
        // 保存资产
        AssetDatabase.CreateAsset(newConfig, configPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        // 分配配置
        configProperty.objectReferenceValue = newConfig;
        serializedObject.ApplyModifiedProperties();
        EditorUtility.SetDirty(animationController);
        
        Debug.Log($"动画配置已创建: {configPath}");
        
        // 提示用户可能需要手动配置动画片段
        EditorUtility.DisplayDialog("配置创建完成", 
            $"动画配置已创建: {Path.GetFileName(configPath)}\n\n请手动配置动画片段和组合，或使用 SetupTestAnimation 组件自动填充。", 
            "确定");
    }
    
    private void RegenerateConfigAsset()
    {
        if (configProperty.objectReferenceValue == null)
        {
            EditorUtility.DisplayDialog("错误", "没有可重新生成的配置。请先创建配置。", "确定");
            return;
        }
        
        var config = (AnimationConfig)configProperty.objectReferenceValue;
        
        // 清空现有层级（但保留动画片段）
        config.layers.Clear();
        
        // 重新添加默认层级
        AddDefaultLayers(config);
        
        // 确保有默认组合
        if (string.IsNullOrEmpty(config.defaultComposition.ToString()))
        {
            config.defaultComposition = CompositionName.Idle;
        }
        
        // 保存修改
        EditorUtility.SetDirty(config);
        AssetDatabase.SaveAssets();
        
        Debug.Log($"配置已重新生成: {AssetDatabase.GetAssetPath(config)}");
    }
    
    private void AddDefaultLayers(AnimationConfig config)
    {
        // 添加基础层级
        var baseLayer = new AnimationConfig.AnimationLayer();
        baseLayer.name = "Base";
        baseLayer.trackIndex = 0;
        baseLayer.defaultWeight = 1.0f;
        baseLayer.additive = false;
        config.layers.Add(baseLayer);
        
        // 添加上半身层级
        var upperBodyLayer = new AnimationConfig.AnimationLayer();
        upperBodyLayer.name = "UpperBody";
        upperBodyLayer.trackIndex = 1;
        upperBodyLayer.defaultWeight = 1.0f;
        upperBodyLayer.additive = false;
        config.layers.Add(upperBodyLayer);
        
        // 添加下半身层级
        var lowerBodyLayer = new AnimationConfig.AnimationLayer();
        lowerBodyLayer.name = "LowerBody";
        lowerBodyLayer.trackIndex = 2;
        lowerBodyLayer.defaultWeight = 1.0f;
        lowerBodyLayer.additive = false;
        config.layers.Add(lowerBodyLayer);
        
        Debug.Log($"添加了 {config.layers.Count} 个默认动画层级");
    }
    
    private string GetDefaultConfigPath()
    {
        // 清理 GameObject 名称（移除路径中的无效字符）
        string cleanName = animationController.gameObject.name;
        foreach (char c in Path.GetInvalidFileNameChars())
        {
            cleanName = cleanName.Replace(c, '_');
        }
        
        // 生成默认路径
        string fileName = $"{cleanName}_AnimationConfig.asset";
        string folderPath = "Assets/Configs/Animations";
        
        // 确保文件夹存在
        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            // 创建完整目录结构
            string[] folders = folderPath.Split('/');
            string currentPath = "";
            foreach (string folder in folders)
            {
                if (string.IsNullOrEmpty(folder)) continue;
                currentPath = currentPath.Length > 0 ? $"{currentPath}/{folder}" : folder;
                if (!AssetDatabase.IsValidFolder(currentPath))
                {
                    AssetDatabase.CreateFolder(Path.GetDirectoryName(currentPath), Path.GetFileName(currentPath));
                }
            }
            AssetDatabase.Refresh();
        }
        
        return Path.Combine(folderPath, fileName).Replace("\\", "/");
    }
}