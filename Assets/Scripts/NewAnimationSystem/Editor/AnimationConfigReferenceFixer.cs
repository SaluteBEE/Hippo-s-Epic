using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Spine.Unity;

public class AnimationConfigReferenceFixer : EditorWindow
{
    private static string defaultSkeletonDataPath = "Assets/Resources/spine/河马/河马_SkeletonData.asset";
    
    [MenuItem("Tools/Animation/修复 AnimationConfig 引用")]
    public static void ShowWindow()
    {
        GetWindow<AnimationConfigReferenceFixer>("AnimationConfig 引用修复工具");
    }
    
    [MenuItem("Tools/Animation/立即修复所有引用")]
    public static void FixAllConfigsImmediate()
    {
        FixAllAnimationConfigs();
    }
    
    private void OnGUI()
    {
        GUILayout.Label("AnimationConfig 引用修复工具", EditorStyles.boldLabel);
        GUILayout.Space(10);
        
        GUILayout.Label("自动修复所有 AnimationConfig 资产中的缺失引用。", EditorStyles.wordWrappedLabel);
        GUILayout.Space(20);
        
        if (GUILayout.Button("扫描并修复所有 AnimationConfig", GUILayout.Height(40)))
        {
            FixAllAnimationConfigs();
        }
        
        GUILayout.Space(20);
        
        GUILayout.Label("选项:", EditorStyles.boldLabel);
        GUILayout.Space(5);
        
        defaultSkeletonDataPath = EditorGUILayout.TextField("默认骨骼数据路径:", defaultSkeletonDataPath);
        
        if (GUILayout.Button("测试单个配置文件修复"))
        {
            TestSingleConfigFix();
        }
        
        GUILayout.Space(20);
        GUILayout.Label("说明:", EditorStyles.boldLabel);
        GUILayout.Label("1. 修复 skeletonData 引用缺失问题", EditorStyles.wordWrappedLabel);
        GUILayout.Label("2. 修复 animation 引用缺失问题（如果可能）", EditorStyles.wordWrappedLabel);
        GUILayout.Label("3. 验证配置完整性", EditorStyles.wordWrappedLabel);
    }
    
    private static void FixAllAnimationConfigs()
    {
        string[] guids = AssetDatabase.FindAssets("t:AnimationConfig");
        
        if (guids.Length == 0)
        {
            Debug.Log("未找到任何 AnimationConfig 资产");
            return;
        }
        
        Debug.Log($"找到 {guids.Length} 个 AnimationConfig 资产");
        
        int fixedCount = 0;
        int totalFixedIssues = 0;
        
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            AnimationConfig config = AssetDatabase.LoadAssetAtPath<AnimationConfig>(path);
            
            if (config == null)
            {
                Debug.LogWarning($"无法加载 AnimationConfig: {path}");
                continue;
            }
            
            Debug.Log($"检查: {path}");
            
            int issuesFixed = FixAnimationConfig(config, path);
            
            if (issuesFixed > 0)
            {
                fixedCount++;
                totalFixedIssues += issuesFixed;
                EditorUtility.SetDirty(config);
            }
        }
        
        AssetDatabase.SaveAssets();
        Debug.Log($"修复完成！共修复 {fixedCount} 个配置文件，解决 {totalFixedIssues} 个问题");
    }
    
    private static int FixAnimationConfig(AnimationConfig config, string configPath)
    {
        int issuesFixed = 0;
        
        // 修复 skeletonData 引用
        if (config.skeletonData == null)
        {
            SkeletonDataAsset skeletonData = FindSuitableSkeletonData(configPath);
            if (skeletonData != null)
            {
                config.skeletonData = skeletonData;
                Debug.Log($"  ✓ 修复 skeletonData 引用: {skeletonData.name}");
                issuesFixed++;
            }
            else
            {
                Debug.LogWarning($"  ✗ 无法找到合适的骨骼数据资产");
            }
        }
        
        // 修复 animation 引用
        foreach (var clip in config.clips)
        {
            if (clip.animation == null && !string.IsNullOrEmpty(clip.animationName))
            {
                AnimationReferenceAsset animRef = FindAnimationReferenceAsset(clip.animationName);
                if (animRef != null)
                {
                    clip.animation = animRef;
                    Debug.Log($"  ✓ 修复动画引用: {clip.animationName}");
                    issuesFixed++;
                }
            }
        }
        
        // 验证配置
        config.ValidateConfig();
        
        return issuesFixed;
    }
    
    private static SkeletonDataAsset FindSuitableSkeletonData(string configPath)
    {
        // 首先尝试加载默认的骨骼数据
        SkeletonDataAsset defaultSkeletonData = AssetDatabase.LoadAssetAtPath<SkeletonDataAsset>(defaultSkeletonDataPath);
        if (defaultSkeletonData != null)
        {
            return defaultSkeletonData;
        }
        
        // 如果在 Hippo 目录下，查找河马骨骼数据
        if (configPath.Contains("Hippo") || configPath.Contains("河马"))
        {
            string hippoSkeletonPath = "Assets/Resources/spine/河马/河马_SkeletonData.asset";
            SkeletonDataAsset hippoSkeletonData = AssetDatabase.LoadAssetAtPath<SkeletonDataAsset>(hippoSkeletonPath);
            if (hippoSkeletonData != null)
            {
                return hippoSkeletonData;
            }
        }
        
        // 搜索所有 SkeletonDataAsset
        string[] skeletonGuids = AssetDatabase.FindAssets("t:SkeletonDataAsset");
        if (skeletonGuids.Length > 0)
        {
            string firstSkeletonPath = AssetDatabase.GUIDToAssetPath(skeletonGuids[0]);
            return AssetDatabase.LoadAssetAtPath<SkeletonDataAsset>(firstSkeletonPath);
        }
        
        return null;
    }
    
    private static AnimationReferenceAsset FindAnimationReferenceAsset(string animationName)
    {
        // 首先在河马 ReferenceAssets 目录中查找
        string hippoPath = $"Assets/Resources/spine/河马/ReferenceAssets/{animationName}.asset";
        AnimationReferenceAsset hippoAnim = AssetDatabase.LoadAssetAtPath<AnimationReferenceAsset>(hippoPath);
        if (hippoAnim != null)
        {
            return hippoAnim;
        }
        
        // 搜索所有 AnimationReferenceAsset
        string[] animGuids = AssetDatabase.FindAssets("t:AnimationReferenceAsset");
        foreach (string guid in animGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            AnimationReferenceAsset anim = AssetDatabase.LoadAssetAtPath<AnimationReferenceAsset>(path);
            if (anim != null && GetAnimationNameFromReference(anim) == animationName)
            {
                return anim;
            }
        }
        
        return null;
    }
    
    private static string GetAnimationNameFromReference(AnimationReferenceAsset animRef)
    {
        if (animRef == null)
            return string.Empty;
            
        try
        {
            // 尝试使用反射获取私有字段 animationName
            var field = typeof(AnimationReferenceAsset).GetField("animationName", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
            {
                string animationName = field.GetValue(animRef) as string;
                if (!string.IsNullOrEmpty(animationName))
                    return animationName;
            }
            
            // 尝试通过 Animation 属性获取
            if (animRef.Animation != null && !string.IsNullOrEmpty(animRef.Animation.Name))
                return animRef.Animation.Name;
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"获取动画名称失败: {ex.Message}");
        }
        
        return string.Empty;
    }
    
    private static void TestSingleConfigFix()
    {
        string testPath = "Assets/Configs/Animations/玩家占位符_AnimationConfig.asset";
        AnimationConfig config = AssetDatabase.LoadAssetAtPath<AnimationConfig>(testPath);
        
        if (config == null)
        {
            Debug.LogError($"无法加载测试配置文件: {testPath}");
            return;
        }
        
        Debug.Log($"测试修复配置文件: {testPath}");
        Debug.Log($"当前 skeletonData: {(config.skeletonData == null ? "null" : config.skeletonData.name)}");
        
        int issuesFixed = FixAnimationConfig(config, testPath);
        
        if (issuesFixed > 0)
        {
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            Debug.Log($"测试修复完成，修复了 {issuesFixed} 个问题");
        }
        else
        {
            Debug.Log("未发现需要修复的问题");
        }
    }
}