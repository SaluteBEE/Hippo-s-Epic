using UnityEngine;
using System.Collections.Generic;
using Spine.Unity;

[CreateAssetMenu(fileName = "AnimationConfig", menuName = "HippoEpic/Animation/Config", order = 100)]
public class AnimationConfig : ScriptableObject
{
    [Header("骨骼数据")]
    [Tooltip("Spine骨骼数据资产")]
    public SkeletonDataAsset skeletonData;
    
    [System.Serializable]
    public class AnimationClip
    {
        [Tooltip("动画片段名称（在代码中引用）")]
        public string name;
        
        [Tooltip("Spine动画引用资产（可选）")]
        public AnimationReferenceAsset animation;
        
        [Tooltip("动画名称（在Spine骨骼数据中的名称）")]
        public string animationName;
        
        [Tooltip("播放速度乘数")]
        [Range(0.1f, 3.0f)]
        public float speed = 1.0f;
        
        [Tooltip("是否循环播放")]
        public bool loop = true;
        
        [Tooltip("动画类型")]
        public AnimationClipType type = AnimationClipType.FullBody;
    }
    
    [System.Serializable]
    public class AnimationLayer
    {
        [Tooltip("层级名称")]
        public string name;
        
        [Tooltip("Spine轨道索引 (0-...)")]
        [Range(0, 10)]
        public int trackIndex = 0;
        
        [Tooltip("默认权重 (0-1)")]
        [Range(0f, 1f)]
        public float defaultWeight = 1.0f;
        
        [Tooltip("是否为叠加动画")]
        public bool additive = false;
        
        [Tooltip("控制的骨骼槽位 (为空表示控制所有槽位)")]
        public List<string> slotNames = new List<string>();
        
        [Tooltip("该层级可用的动画片段")]
        public List<string> availableClipNames = new List<string>();
    }
    
    [System.Serializable]
    public class AnimationComposition
    {
        [Tooltip("组合名称")]
        public string name;
        
        [Tooltip("组合描述")]
        [TextArea(1, 3)]
        public string description;
        
        [Tooltip("组合中的层级动画")]
        public List<CompositionLayer> layers = new List<CompositionLayer>();
        
        [System.Serializable]
        public class CompositionLayer
        {
            [Tooltip("层级名称")]
            public string layerName;
            
            [Tooltip("动画片段名称")]
            public string clipName;
            
            [Tooltip("权重 (0-1)")]
            [Range(0f, 1f)]
            public float weight = 1.0f;
            
            [Tooltip("过渡时间 (秒)")]
            [Min(0f)]
            public float transitionTime = 0.1f;
            
            [Tooltip("延迟播放时间 (秒)")]
            [Min(0f)]
            public float delay = 0f;
        }
    }
    
    public enum AnimationClipType
    {
        FullBody,      // 全身动画
        UpperBody,     // 上半身动画
        LowerBody,     // 下半身动画
        Face,          // 面部动画
        Special        // 特殊效果动画
    }
    
    [Header("动画片段")]
    [Tooltip("所有可用的动画片段")]
    public List<AnimationClip> clips = new List<AnimationClip>();
    
    [Header("动画层级")]
    [Tooltip("动画层级配置")]
    public List<AnimationLayer> layers = new List<AnimationLayer>();
    
    [Header("动画组合")]
    [Tooltip("预定义的动画组合")]
    public List<AnimationComposition> compositions = new List<AnimationComposition>();
    
    [Header("默认设置")]
    [Tooltip("默认动画组合")]
    public string defaultComposition = "Idle";
    
    [Tooltip("默认过渡时间")]
    [Min(0f)]
    public float defaultTransitionTime = 0.1f;
    
    // 辅助方法
    public AnimationClip GetClip(string clipName)
    {
        return clips.Find(c => c.name == clipName);
    }
    
    public AnimationLayer GetLayer(string layerName)
    {
        return layers.Find(l => l.name == layerName);
    }
    
    public AnimationComposition GetComposition(string compositionName)
    {
        return compositions.Find(c => c.name == compositionName);
    }
    
    public bool HasClip(string clipName)
    {
        return GetClip(clipName) != null;
    }
    
    public bool HasLayer(string layerName)
    {
        return GetLayer(layerName) != null;
    }
    
    public bool HasComposition(string compositionName)
    {
        return GetComposition(compositionName) != null;
    }
    
    public void ValidateConfig()
    {
        // 验证配置的完整性
        foreach (var composition in compositions)
        {
            foreach (var layer in composition.layers)
            {
                if (!HasLayer(layer.layerName))
                {
                    Debug.LogWarning($"Composition '{composition.name}' references non-existent layer: {layer.layerName}");
                }
                
                if (!HasClip(layer.clipName))
                {
                    Debug.LogWarning($"Composition '{composition.name}' references non-existent clip: {layer.clipName}");
                }
            }
        }
        
        foreach (var layer in layers)
        {
            foreach (var clipName in layer.availableClipNames)
            {
                if (!HasClip(clipName))
                {
                    Debug.LogWarning($"Layer '{layer.name}' references non-existent clip: {clipName}");
                }
            }
        }
    }
}