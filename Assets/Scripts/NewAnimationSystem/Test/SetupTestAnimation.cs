using UnityEngine;
using Spine.Unity;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class SetupTestAnimation : MonoBehaviour
{
    [ContextMenu("设置测试动画配置")]
    public void SetupTestConfig()
    {
#if UNITY_EDITOR
        var animController = GetComponent<AnimationController>();
        if (animController == null)
        {
            Debug.LogError("没有找到AnimationController组件");
            return;
        }
        
        // 获取或创建配置
        AnimationConfig config = animController.Config;
        if (config == null)
        {
            string configPath = "Assets/Configs/Animations/TestHippoConfig.asset";
            config = AssetDatabase.LoadAssetAtPath<AnimationConfig>(configPath);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<AnimationConfig>();
                AssetDatabase.CreateAsset(config, configPath);
                Debug.Log("创建新的动画配置: " + configPath);
            }
            
            animController.SetConfig(config);
        }
        
        // 设置骨骼数据
        string skeletonDataPath = "Assets/Resources/spine/河马_SkeletonData.asset";
        var skeletonData = AssetDatabase.LoadAssetAtPath<SkeletonDataAsset>(skeletonDataPath);
        if (skeletonData != null)
        {
            config.skeletonData = skeletonData;
        }
        
        // 清空现有配置
        config.clips.Clear();
        config.layers.Clear();
        config.compositions.Clear();
        
        // 添加动画片段
        AddAnimationClip(config, "body_idle", "body-idle", 1.0f, true);
        AddAnimationClip(config, "body_walk", "body-walk", 1.0f, true);
        AddAnimationClip(config, "leg_idle", "leg-idle", 1.0f, true);
        AddAnimationClip(config, "leg_walk", "leg-walk", 1.0f, true);
        
        // 添加动画层级
        AddAnimationLayer(config, "Base", 0, 1.0f);
        AddAnimationLayer(config, "UpperBody", 1, 1.0f);
        AddAnimationLayer(config, "LowerBody", 2, 1.0f);
        
        // 添加动画组合
        AddAnimationComposition(config, "Idle", "空闲状态",
            new string[] { "Base", "UpperBody", "LowerBody" },
            new string[] { "body_idle", "body_idle", "leg_idle" });
            
        AddAnimationComposition(config, "Walk", "行走状态",
            new string[] { "Base", "UpperBody", "LowerBody" },
            new string[] { "body_walk", "body_walk", "leg_walk" });
        
        // 保存配置
        EditorUtility.SetDirty(config);
        AssetDatabase.SaveAssets();
        
        Debug.Log("测试动画配置设置完成！");
        Debug.Log("动画片段: " + config.clips.Count);
        Debug.Log("动画层级: " + config.layers.Count);
        Debug.Log("动画组合: " + config.compositions.Count);
        
        // 初始化控制器
        if (!animController.IsInitialized)
        {
            animController.Initialize();
        }
#endif
    }
    
    private void AddAnimationClip(AnimationConfig config, string name, string animationName, float speed, bool loop)
    {
        var clip = new AnimationConfig.AnimationClip();
        clip.name = name;
        clip.animationName = animationName;
        clip.speed = speed;
        clip.loop = loop;
        config.clips.Add(clip);
    }
    
    private void AddAnimationLayer(AnimationConfig config, string name, int trackIndex, float defaultWeight)
    {
        var layer = new AnimationConfig.AnimationLayer();
        layer.name = name;
        layer.trackIndex = trackIndex;
        layer.defaultWeight = defaultWeight;
        config.layers.Add(layer);
    }
    
    private void AddAnimationComposition(AnimationConfig config, string name, string description, 
                                        string[] layerNames, string[] clipNames)
    {
        var composition = new AnimationConfig.AnimationComposition();
        composition.name = name;
        composition.description = description;
        
        for (int i = 0; i < layerNames.Length && i < clipNames.Length; i++)
        {
            var layer = new AnimationConfig.AnimationComposition.CompositionLayer();
            layer.layerName = layerNames[i];
            layer.clipName = clipNames[i];
            layer.weight = 1.0f;
            layer.transitionTime = 0.1f;
            composition.layers.Add(layer);
        }
        
        config.compositions.Add(composition);
    }
    
    [ContextMenu("测试播放空闲动画")]
    public void TestPlayIdle()
    {
        var animController = GetComponent<AnimationController>();
        if (animController != null && animController.IsInitialized)
        {
            animController.PlayComposition("Idle");
            Debug.Log("播放空闲动画组合");
        }
    }
    
    [ContextMenu("测试播放行走动画")]
    public void TestPlayWalk()
    {
        var animController = GetComponent<AnimationController>();
        if (animController != null && animController.IsInitialized)
        {
            animController.PlayComposition("Walk");
            Debug.Log("播放行走动画组合");
        }
    }
}