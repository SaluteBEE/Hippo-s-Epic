using UnityEngine;
using System.Collections.Generic;
using Spine;
using Spine.Unity;
using AnimationState = Spine.AnimationState;

[RequireComponent(typeof(SkeletonAnimation))]
public class AnimationController : MonoBehaviour
{
    [Header("配置")]
    [SerializeField] private AnimationConfig config;
    
    [Header("调试")]
    [SerializeField] private bool debugLog = false;
    [SerializeField] private bool autoInitialize = true;
    
    [Header("插槽控制")]
    [SerializeField] private SlotManager slotManager;
    [SerializeField] private bool controlSlotsWithLayers = true;
    
    private SkeletonAnimation skeletonAnimation;
    private AnimationState animationState;
    private Dictionary<string, AnimationLayer> layers = new Dictionary<string, AnimationLayer>();
    private bool isInitialized = false;
    
    public bool IsInitialized => isInitialized;
    public AnimationConfig Config => config;
    public void SetConfig(AnimationConfig newConfig) => config = newConfig;
    
    private void Awake()
    {
        try
        {
            skeletonAnimation = GetComponent<SkeletonAnimation>();
            animationState = skeletonAnimation.AnimationState;
            
            // 自动获取 SlotManager
            if (slotManager == null)
            {
                slotManager = GetComponent<SlotManager>();
            }
            
            if (autoInitialize)
            {
                Initialize();
            }
            
            if (debugLog) Debug.Log($"AnimationController.Awake: autoInitialize={autoInitialize}, config={(config != null ? "assigned" : "null")}, skeletonAnimation={(skeletonAnimation != null ? "assigned" : "null")}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"AnimationController.Awake 异常: {e.GetType().Name}: {e.Message}\n堆栈: {e.StackTrace}");
            throw;
        }
    }
    
    private bool TryLoadDefaultConfig()
    {
        #if UNITY_EDITOR
        if (!UnityEditor.EditorApplication.isPlaying)
        {
            // 仅在编辑器模式下尝试加载默认配置
            string defaultConfigPath = $"Assets/Configs/Animations/{gameObject.name}_AnimationConfig.asset";
            var loadedConfig = UnityEditor.AssetDatabase.LoadAssetAtPath<AnimationConfig>(defaultConfigPath);
            if (loadedConfig != null)
            {
                config = loadedConfig;
                if (debugLog) Debug.Log($"自动加载默认配置: {defaultConfigPath}");
                return true;
            }
        }
        #endif
        
        return false;
    }
    
    public void Initialize()
    {
        try
        {
            if (debugLog) Debug.Log($"AnimationController.Initialize开始: config={(config != null ? config.name : "null")}, skeletonAnimation={(skeletonAnimation != null ? "assigned" : "null")}");
            if (isInitialized)
            {
                if (debugLog) Debug.LogWarning("AnimationController already initialized.");
                return;
            }
            
            // 编辑器模式下确保组件引用已获取
#if UNITY_EDITOR
            if (!UnityEngine.Application.isPlaying)
            {
                if (skeletonAnimation == null)
                    skeletonAnimation = GetComponent<SkeletonAnimation>();
                if (skeletonAnimation != null)
                    animationState = skeletonAnimation.AnimationState;
                if (slotManager == null)
                    slotManager = GetComponent<SlotManager>();
            }
#endif
            
            if (config == null)
            {
                // 尝试加载默认配置（仅在编辑器中）
                if (!TryLoadDefaultConfig())
                {
                    Debug.LogError($"AnimationController on {gameObject.name} has no AnimationConfig assigned. " +
                                  "Please assign an AnimationConfig asset in the inspector or create one using the editor tools.");
                    return;
                }
            }
            
            // 验证配置
            config.ValidateConfig();
            
            // 验证骨骼数据匹配
            if (config.skeletonData != null && skeletonAnimation != null && 
                config.skeletonData != skeletonAnimation.SkeletonDataAsset)
            {
                Debug.LogWarning($"AnimationConfig skeleton data doesn't match SkeletonAnimation on {gameObject.name}");
            }
            
            // 初始化层级
            InitializeLayers();
            
            isInitialized = true;
            
            // 播放默认组合
            if (config.HasComposition(config.defaultComposition))
            {
                PlayComposition(config.defaultComposition);
            }
            
            if (debugLog)
            {
                Debug.Log($"AnimationController initialized on {gameObject.name} with {layers.Count} layers.");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"AnimationController.Initialize 异常: {e.GetType().Name}: {e.Message}\n堆栈: {e.StackTrace}");
            throw;
        }
    }
    

    
    private void InitializeLayers()
    {
        layers.Clear();
        
        bool hasLayers = false;
        
        if (config != null && config.layers != null && config.layers.Count > 0)
        {
            foreach (var layerConfig in config.layers)
            {
                var layer = new AnimationLayer(layerConfig, skeletonAnimation, config, 
                                               controlSlotsWithLayers ? slotManager : null);
                layers[layerConfig.name] = layer;
                
                if (debugLog)
                {
                    Debug.Log($"Initialized animation layer: {layerConfig.name} (Track {layerConfig.trackIndex})");
                }
            }
            hasLayers = true;
        }
        
        if (!hasLayers)
        {
            // 使用默认层级
            CreateDefaultLayers();
        }
        
        if (debugLog)
        {
            Debug.Log($"初始化了 {layers.Count} 个动画层级");
        }
    }
    
    private void CreateDefaultLayers()
    {
        // 创建默认层级
        var defaultLayers = new List<AnimationConfig.AnimationLayer>
        {
            new AnimationConfig.AnimationLayer { name = "Base", trackIndex = 0, defaultWeight = 1.0f },
            new AnimationConfig.AnimationLayer { name = "UpperBody", trackIndex = 1, defaultWeight = 1.0f },
            new AnimationConfig.AnimationLayer { name = "LowerBody", trackIndex = 2, defaultWeight = 1.0f }
        };
        
        foreach (var layerConfig in defaultLayers)
        {
                var layer = new AnimationLayer(layerConfig, skeletonAnimation, config, 
                                               controlSlotsWithLayers ? slotManager : null);
            layers[layerConfig.name] = layer;
            
            if (debugLog)
            {
                Debug.Log($"创建默认动画层级: {layerConfig.name} (Track {layerConfig.trackIndex})");
            }
        }
    }
    
    public void PlayClip(string layerName, string clipName, float transitionTime = -1f)
    {
        PlayClipInternal(layerName, clipName, transitionTime, null);
    }

    private void PlayClipInternal(string layerName, string clipName, float transitionTime, bool? loopOverride)
    {
        if (!isInitialized)
        {
            Debug.LogWarning("AnimationController not initialized. Call Initialize() first.");
            return;
        }

        if (debugLog)
        {
            Debug.Log($"尝试播放动画: 层级={layerName}, 动画={clipName}, 过渡时间={transitionTime}");
            Debug.Log($"可用层级: {string.Join(", ", layers.Keys)}");
        }

        if (layers.TryGetValue(layerName, out var layer))
        {
            if (transitionTime < 0)
                transitionTime = config != null ? config.defaultTransitionTime : 0.1f;

            layer.PlayClip(clipName, transitionTime, loopOverride);

            if (debugLog)
            {
                Debug.Log($"Playing clip: {clipName} on layer: {layerName} (transition: {transitionTime}s)");
            }
        }
        else
        {
            Debug.LogWarning($"Animation layer not found: {layerName}");
            Debug.LogWarning($"可用层级: {string.Join(", ", layers.Keys)}");
        }
    }
    
    public void PlayComposition(CompositionName compositionName)
    {
        PlayCompositionInternal(compositionName, null);
    }

    /// <summary> 播放一次组合动画（强制非循环，播完停在末帧），用于攻击/受击等一次性动作 </summary>
    public void PlayCompositionOnce(CompositionName compositionName)
    {
        PlayCompositionInternal(compositionName, false);
    }

    private void PlayCompositionInternal(CompositionName compositionName, bool? loopOverride)
    {
        if (!isInitialized)
        {
            Debug.LogWarning("AnimationController not initialized. Call Initialize() first.");
            return;
        }
        
        var composition = config.GetComposition(compositionName);
        if (composition != null)
        {
            if (debugLog)
            {
                Debug.Log($"开始播放组合动画: {compositionName} (包含 {composition.layers.Count} 个层级)");
            }
            
            foreach (var compLayer in composition.layers)
            {
                if (debugLog)
                {
                    Debug.Log($"  组合层: {compLayer.layerName} -> {compLayer.clipName} (权重: {compLayer.weight}, 过渡: {compLayer.transitionTime}s)");
                }
                
                PlayClipInternal(compLayer.layerName, compLayer.clipName, compLayer.transitionTime, loopOverride);
                
                if (compLayer.delay > 0)
                {
                    // 延迟设置权重（仅在播放模式下使用协程）
                    if (Application.isPlaying)
                    {
                        StartCoroutine(DelayedSetWeight(compLayer.layerName, compLayer.weight, compLayer.delay));
                    }
                    else
                    {
                        // 编辑器模式下直接设置权重
                        SetLayerWeight(compLayer.layerName, compLayer.weight);
                    }
                }
                else
                {
                    SetLayerWeight(compLayer.layerName, compLayer.weight);
                }
            }
            
            if (debugLog)
            {
                Debug.Log($"组合动画播放完成: {compositionName}");
            }
        }
        else
        {
            Debug.LogWarning($"Animation composition not found: {compositionName}");
        }
    }

    public void PlayComposition(string compositionName)
    {
        if (System.Enum.TryParse<CompositionName>(compositionName, true, out var enumName))
        {
            PlayComposition(enumName);
            return;
        }

        Debug.LogWarning($"Animation composition name cannot parse to enum: {compositionName}");
    }
    
    private System.Collections.IEnumerator DelayedSetWeight(string layerName, float weight, float delay)
    {
        yield return new WaitForSeconds(delay);
        SetLayerWeight(layerName, weight);
    }
    
    public void SetLayerWeight(string layerName, float weight)
    {
        if (!isInitialized)
        {
            Debug.LogWarning("AnimationController not initialized. Call Initialize() first.");
            return;
        }
        
        if (layers.TryGetValue(layerName, out var layer))
        {
            layer.SetWeight(weight);
            
            if (debugLog)
            {
                Debug.Log($"Set layer weight: {layerName} = {weight:F2}");
            }
        }
        else
        {
            Debug.LogWarning($"Animation layer not found: {layerName}");
        }
    }
    
    public void SetAnimationSpeed(string layerName, float speed)
    {
        if (!isInitialized)
        {
            Debug.LogWarning("AnimationController not initialized. Call Initialize() first.");
            return;
        }
        
        if (layers.TryGetValue(layerName, out var layer))
        {
            layer.SetSpeed(speed);
            
            if (debugLog)
            {
                Debug.Log($"Set animation speed: {layerName} = {speed:F2}");
            }
        }
        else
        {
            Debug.LogWarning($"Animation layer not found: {layerName}");
        }
    }
    
    public void StopLayer(string layerName)
    {
        if (!isInitialized)
        {
            Debug.LogWarning("AnimationController not initialized. Call Initialize() first.");
            return;
        }
        
        if (layers.TryGetValue(layerName, out var layer))
        {
            layer.Stop();
            
            if (debugLog)
            {
                Debug.Log($"Stopped layer: {layerName}");
            }
        }
        else
        {
            Debug.LogWarning($"Animation layer not found: {layerName}");
        }
    }
    
    public void StopAll()
    {
        if (!isInitialized)
        {
            Debug.LogWarning("AnimationController not initialized. Call Initialize() first.");
            return;
        }
        
        foreach (var layer in layers.Values)
        {
            layer.Stop();
        }
        
        if (debugLog)
        {
            Debug.Log("Stopped all animation layers");
        }
    }
    
    public bool IsPlaying(string layerName)
    {
        if (layers.TryGetValue(layerName, out var layer))
        {
            return layer.IsPlaying;
        }
        return false;
    }
    
    public float GetAnimationTime(string layerName)
    {
        if (layers.TryGetValue(layerName, out var layer))
        {
            return layer.CurrentTime;
        }
        return 0f;
    }
    
    public float GetAnimationDuration(string layerName)
    {
        if (layers.TryGetValue(layerName, out var layer))
        {
            return layer.CurrentDuration;
        }
        return 0f;
    }
    
    public float GetLayerWeight(string layerName)
    {
        if (layers.TryGetValue(layerName, out var layer))
        {
            return layer.CurrentWeight;
        }
        return 0f;
    }
    
    public List<string> GetLayerNames()
    {
        return new List<string>(layers.Keys);
    }
    
    public List<CompositionName> GetCompositionNames()
    {
        var names = new List<CompositionName>();
        if (config == null || config.compositions == null)
        {
            if (debugLog) Debug.LogWarning("GetCompositionNames: config or compositions is null");
            return names;
        }
        
        foreach (var composition in config.compositions)
        {
            if (composition != null)
            {
                names.Add(composition.name);
            }
        }
        return names;
    }
    
    // 内部类：AnimationLayer
    private class AnimationLayer
    {
        private AnimationConfig.AnimationLayer config;
        private SkeletonAnimation skeletonAnimation;
        private AnimationState animationState;
        private AnimationConfig animationConfig;
        private SlotManager slotManager;
        private TrackEntry currentTrack;
        private float currentWeight = 1.0f;
        private float currentSpeed = 1.0f;
        private AnimationConfig.AnimationClip currentClip;
        
        public bool IsPlaying => currentTrack != null;
        public float CurrentTime => currentTrack?.AnimationTime ?? 0f;
        public float CurrentDuration => currentTrack?.AnimationEnd ?? 0f;
        public float CurrentWeight => currentWeight;
        
        public AnimationLayer(AnimationConfig.AnimationLayer config, 
                             SkeletonAnimation skeletonAnimation, 
                             AnimationConfig animationConfig,
                             SlotManager slotManager = null)
        {
            this.config = config;
            this.skeletonAnimation = skeletonAnimation;
            this.animationState = skeletonAnimation != null ? skeletonAnimation.AnimationState : null;
            this.animationConfig = animationConfig;
            this.slotManager = slotManager;
            this.currentWeight = config != null ? config.defaultWeight : 1.0f;
        }
        
        public void PlayClip(string clipName, float transitionTime, bool? loopOverride = null)
        {
            // 编辑器模式下提前返回
            if (animationState == null)
            {
                Debug.LogWarning($"Cannot play animation in editor mode: {clipName}");
                return;
            }
            
            // 尝试从配置获取动画
            var clip = animationConfig.GetClip(clipName);
            Spine.Animation animation = null;
            float speed = 1.0f;
            
            if (clip != null && clip.animation != null)
            {
                // 使用配置的动画
                animation = clip.animation.Animation;
                speed = clip.speed;
                
                // 检查动画是否可用于此层级
                if (config.availableClipNames.Count > 0 && 
                    !config.availableClipNames.Contains(clipName))
                {
                    Debug.LogWarning($"Clip {clipName} is not available for layer {config.name}");
                    return;
                }
                
                // Debug.Log($"播放动画: {clipName} (循环: {loop}, 速度: {speed}, 轨道: {config.trackIndex})");
            }
            else
            {
                // 尝试直接从骨骼数据获取动画
                if (skeletonAnimation == null)
                {
                    Debug.LogWarning($"No skeleton animation available");
                    return;
                }
                
                var skeleton = skeletonAnimation.Skeleton;
                if (skeleton != null && skeleton.Data != null)
                {
                    animation = skeleton.Data.FindAnimation(clipName);
                    if (animation == null)
                    {
                        Debug.LogWarning($"Animation not found in skeleton data: {clipName}");
                        return;
                    }
                }
                else
                {
                    Debug.LogWarning($"No skeleton data available");
                    return;
                }
                
                // Debug.Log($"播放动画(直接查找): {clipName} (循环: {loop}, 速度: {speed}, 轨道: {config.trackIndex})");
            }
            
            bool loop = loopOverride ?? (clip != null ? clip.loop : true);

            if (animation != null)
            {
                // 调试信息：动画时长
                float animationDuration = animation.Duration;
                // Debug.Log($"动画 '{clipName}' 时长: {animationDuration:F2}秒 ({animationDuration * 30:F0}帧)");
                
                currentTrack = animationState.SetAnimation(
                    config.trackIndex,
                    animation,
                    loop
                );
                
                currentTrack.Alpha = currentWeight;
                currentTrack.TimeScale = currentSpeed * speed;
                
                if (transitionTime > 0)
                {
                    currentTrack.MixDuration = transitionTime;
                }
                
                // 设置叠加模式
                if (config.additive)
                {
                    currentTrack.MixBlend = MixBlend.Add;
                }
                
                // 存储当前剪辑引用
                currentClip = clip;
                
                // 控制插槽
                ControlSlots(true, transitionTime);
                
                // 调试信息：轨道状态
                // Debug.Log($"轨道设置完成: 轨道{config.trackIndex}, Alpha={currentTrack.Alpha:F2}, TimeScale={currentTrack.TimeScale:F2}, MixDuration={currentTrack.MixDuration:F2}, Loop={loop}");
            }
        }
        
        private void ControlSlots(bool visible, float transitionTime = 0f)
        {
            if (slotManager == null || config == null || config.slotNames == null || config.slotNames.Count == 0)
                return;
            
            // 控制图层指定的所有插槽
            foreach (var slotName in config.slotNames)
            {
                slotManager.SetSlotVisible(slotName, visible, transitionTime);
            }
        }
        
        public void SetWeight(float weight)
        {
            currentWeight = Mathf.Clamp01(weight);
            if (currentTrack != null)
            {
                currentTrack.Alpha = currentWeight;
            }
        }
        
        public void SetSpeed(float speed)
        {
            currentSpeed = Mathf.Max(0, speed);
            if (currentTrack != null)
            {
                if (currentClip != null)
                {
                    currentTrack.TimeScale = currentSpeed * currentClip.speed;
                }
                else
                {
                    currentTrack.TimeScale = currentSpeed;
                }
            }
        }
        
        public void Stop()
        {
            if (currentTrack != null)
            {
                // 停止时隐藏插槽
                ControlSlots(false, 0.1f);
                
                if (animationState != null)
                {
                    animationState.ClearTrack(config.trackIndex);
                }
                currentTrack = null;
                currentClip = null;
            }
        }
        
        private AnimationConfig.AnimationClip FindClipByTrack(TrackEntry track)
        {
            if (track?.Animation == null) return null;
            
            var animationName = track.Animation.Name;
            return animationConfig.clips.Find(c => 
                c.animation != null && 
                c.animation.Animation != null && 
                c.animation.Animation.Name == animationName);
        }
    }
    
    // 编辑器辅助方法
    #if UNITY_EDITOR
    [ContextMenu("Initialize Animation Controller")]
    private void InitializeInEditor()
    {
        if (config != null)
        {
            Initialize();
            Debug.Log("AnimationController initialized in editor.");
        }
        else
        {
            Debug.LogError("Cannot initialize: No AnimationConfig assigned.");
        }
    }
    
    [ContextMenu("Stop All Animations")]
    private void StopAllInEditor()
    {
        StopAll();
        Debug.Log("All animations stopped.");
    }
    
    [ContextMenu("创建动画配置资产")]
    private void CreateConfigAssetInEditor()
    {
        if (config != null)
        {
            Debug.LogWarning($"配置已存在: {config.name}");
            return;
        }
        
        Debug.LogWarning("请使用 AnimationController 的 Inspector 中的按钮创建配置资产。");
        Debug.LogWarning("或者在 Assets 菜单中: Create > Animation > Config");
    }
    
    private void Reset()
    {
        // 当组件被添加或重置时调用
        // 注意：实际的配置创建现在由 AnimationControllerEditor 处理
        // 这里只提供日志提示
        Debug.Log($"AnimationController 已添加到 {gameObject.name}。请使用 Inspector 中的按钮创建动画配置。");
    }
    #endif
}