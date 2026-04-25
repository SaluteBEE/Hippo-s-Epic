using UnityEngine;
using System.Collections.Generic;
using Spine;
using Spine.Unity;

public class SlotManager : MonoBehaviour
{
    [Header("骨骼组件")]
    [SerializeField] private SkeletonAnimation skeletonAnimation;
    [SerializeField] private SkeletonGraphic skeletonGraphic;
    
    [Header("插槽管理")]
    [SerializeField] private bool autoInitialize = true;
    [SerializeField] private bool debugLog = false;
    
    [Header("插槽配置")]
    [SerializeField] private List<ManagedSlot> managedSlots = new List<ManagedSlot>();
    
    [Header("插槽分组")]
    [SerializeField] private List<SlotGroup> slotGroups = new List<SlotGroup>();
    
    // 内部状态
    private ISkeletonComponent skeletonComponent;
    private Skeleton skeleton;
    private Dictionary<string, Slot> slots = new Dictionary<string, Slot>();
    private Dictionary<string, SlotGroup> groupByName = new Dictionary<string, SlotGroup>();
    private Dictionary<string, ManagedSlot> managedSlotByName = new Dictionary<string, ManagedSlot>();
    private Dictionary<string, Coroutine> activeFadeCoroutines = new Dictionary<string, Coroutine>();
    private bool isInitialized = false;
    
    public bool IsInitialized => isInitialized;
    public int SlotCount => slots.Count;
    public List<string> SlotNames => new List<string>(slots.Keys);
    
    [System.Serializable]
    public class ManagedSlot
    {
        [Tooltip("插槽名称")]
        [SpineSlot]
        public string slotName;
        
        [Tooltip("默认附件名称")]
        [SpineAttachment(slotField: "slotName")]
        public string defaultAttachment;
        
        [Tooltip("是否在开始时显示")]
        public bool visibleOnStart = true;
        
        [Tooltip("默认透明度")]
        [Range(0f, 1f)]
        public float defaultAlpha = 1.0f;
        
        [Tooltip("淡入淡出时间")]
        [Min(0f)]
        public float fadeDuration = 0.2f;
        
        [Header("运行时状态")]
        [System.NonSerialized] public Slot slotReference;
        [System.NonSerialized] public bool isVisible = true;
        [System.NonSerialized] public float currentAlpha = 1.0f;
        
        public void SetVisible(bool visible) => isVisible = visible;
        public void SetAlpha(float alpha) => currentAlpha = Mathf.Clamp01(alpha);
    }
    
    [System.Serializable]
    public class SlotGroup
    {
        [Tooltip("分组名称")]
        public string name;
        
        [Tooltip("包含的插槽名称")]
        [SpineSlot]
        public List<string> slotNames = new List<string>();
        
        [Tooltip("默认透明度")]
        [Range(0f, 1f)]
        public float defaultAlpha = 1.0f;
        
        [Tooltip("淡入淡出时间")]
        [Min(0f)]
        public float fadeDuration = 0.2f;
        
        [Tooltip("是否在开始时显示")]
        public bool visibleOnStart = true;
        
        [Header("运行时状态")]
        [SerializeField] private bool isVisible = true;
        [SerializeField] private float currentAlpha = 1.0f;
        
        public bool IsVisible => isVisible;
        public float CurrentAlpha => currentAlpha;
        
        public void SetVisible(bool visible) => isVisible = visible;
        public void SetAlpha(float alpha) => currentAlpha = Mathf.Clamp01(alpha);
    }
    
    private void Awake()
    {
        if (skeletonAnimation == null)
            skeletonAnimation = GetComponent<SkeletonAnimation>();
        
        if (skeletonGraphic == null)
            skeletonGraphic = GetComponent<SkeletonGraphic>();
        
        if (skeletonAnimation != null)
            skeletonComponent = skeletonAnimation;
        else if (skeletonGraphic != null)
            skeletonComponent = skeletonGraphic;
        
        if (skeletonComponent == null)
        {
            Debug.LogError($"SlotManager on {gameObject.name}: Requires either SkeletonAnimation or SkeletonGraphic component.");
            return;
        }
        
        skeleton = skeletonComponent.Skeleton;
        if (skeleton == null)
        {
            Debug.LogError($"SlotManager on {gameObject.name}: Skeleton is null. Ensure the skeleton component is initialized.");
            return;
        }
        
        if (autoInitialize)
            Initialize();
    }
    
    public void Initialize()
    {
        if (isInitialized)
        {
            if (debugLog) Debug.LogWarning("SlotManager already initialized.");
            return;
        }
        
        // 收集所有插槽
        CollectAllSlots();
        
        // 初始化管理的插槽
        InitializeManagedSlots();
        
        // 初始化分组
        InitializeGroups();
        
        // 应用管理的插槽初始状态
        ApplyManagedSlotsInitialStates();
        
        // 应用分组初始状态
        ApplyGroupInitialStates();
        
        isInitialized = true;
        
        if (debugLog)
        {
            Debug.Log($"SlotManager initialized on {gameObject.name} with {slots.Count} slots, {managedSlots.Count} managed slots and {slotGroups.Count} groups.");
        }
    }
    
    private void CollectAllSlots()
    {
        slots.Clear();
        
        var skeletonData = skeleton.Data;
        if (skeletonData == null)
        {
            Debug.LogError($"SlotManager on {gameObject.name}: Skeleton data is null.");
            return;
        }
        
        // 获取所有插槽
        var slotList = skeleton.Slots.Items;
        for (int i = 0; i < slotList.Length; i++)
        {
            Slot slot = slotList[i];
            if (slot != null && slot.Data != null)
            {
                string slotName = slot.Data.Name;
                slots[slotName] = slot;
                
                if (debugLog)
                    Debug.Log($"Found slot: {slotName}");
            }
        }
    }
    
    private void InitializeManagedSlots()
    {
        managedSlotByName.Clear();
        
        foreach (var managedSlot in managedSlots)
        {
            if (string.IsNullOrEmpty(managedSlot.slotName))
            {
                Debug.LogWarning($"SlotManager: Found managed slot with empty name.");
                continue;
            }
            
            if (managedSlotByName.ContainsKey(managedSlot.slotName))
            {
                Debug.LogWarning($"SlotManager: Duplicate managed slot name '{managedSlot.slotName}'.");
                continue;
            }
            
            // 获取插槽引用
            if (slots.TryGetValue(managedSlot.slotName, out var slot))
            {
                managedSlot.slotReference = slot;
                managedSlot.SetAlpha(managedSlot.defaultAlpha);
                managedSlot.SetVisible(managedSlot.visibleOnStart);
                managedSlotByName[managedSlot.slotName] = managedSlot;
            }
            else
            {
                Debug.LogWarning($"SlotManager: Managed slot '{managedSlot.slotName}' not found in skeleton.");
            }
        }
    }
    
    private void InitializeGroups()
    {
        groupByName.Clear();
        
        foreach (var group in slotGroups)
        {
            if (string.IsNullOrEmpty(group.name))
            {
                Debug.LogWarning($"SlotManager: Found group with empty name.");
                continue;
            }
            
            if (groupByName.ContainsKey(group.name))
            {
                Debug.LogWarning($"SlotManager: Duplicate group name '{group.name}'.");
                continue;
            }
            
            groupByName[group.name] = group;
            group.SetAlpha(group.defaultAlpha);
            group.SetVisible(group.visibleOnStart);
        }
    }
    
    private void ApplyManagedSlotsInitialStates()
    {
        foreach (var managedSlot in managedSlots)
        {
            if (managedSlot.slotReference == null) continue;
            
            if (!managedSlot.visibleOnStart)
            {
                SetSlotVisible(managedSlot.slotName, false, 0f);
            }
            else if (Mathf.Abs(managedSlot.defaultAlpha - 1.0f) > 0.01f)
            {
                SetSlotAlpha(managedSlot.slotName, managedSlot.defaultAlpha, 0f);
            }
            
            // 设置默认附件
            if (!string.IsNullOrEmpty(managedSlot.defaultAttachment))
            {
                SetSlotAttachment(managedSlot.slotName, managedSlot.defaultAttachment, false);
            }
        }
    }
    
    private void ApplyGroupInitialStates()
    {
        foreach (var group in slotGroups)
        {
            if (!group.visibleOnStart)
            {
                SetGroupVisible(group.name, false, 0f);
            }
            else if (Mathf.Abs(group.defaultAlpha - 1.0f) > 0.01f)
            {
                SetGroupAlpha(group.name, group.defaultAlpha, 0f);
            }
        }
    }
    
    #region 插槽操作
    
    /// <summary>
    /// 获取管理的插槽配置
    /// </summary>
    public ManagedSlot GetManagedSlot(string slotName)
    {
        managedSlotByName.TryGetValue(slotName, out var managedSlot);
        return managedSlot;
    }
    
    /// <summary>
    /// 设置单个插槽可见性
    /// </summary>
    public void SetSlotVisible(string slotName, bool visible, float duration = 0f)
    {
        if (!slots.TryGetValue(slotName, out var slot))
        {
            Debug.LogWarning($"SlotManager: Slot '{slotName}' not found.");
            return;
        }
        
        // 尝试从管理的插槽获取配置
        var managedSlot = GetManagedSlot(slotName);
        if (managedSlot != null && duration <= 0)
        {
            duration = managedSlot.fadeDuration;
        }
        
        float targetAlpha = visible ? (managedSlot?.defaultAlpha ?? 1f) : 0f;
        
        if (duration <= 0f)
        {
            slot.A = targetAlpha;
            if (managedSlot != null)
            {
                managedSlot.SetAlpha(targetAlpha);
                managedSlot.SetVisible(visible);
            }
        }
        else
        {
            StartCoroutine(FadeSlotAlpha(slotName, slot, targetAlpha, duration, managedSlot));
        }
    }
    
    /// <summary>
    /// 设置单个插槽透明度
    /// </summary>
    public void SetSlotAlpha(string slotName, float alpha, float duration = 0f)
    {
        if (!slots.TryGetValue(slotName, out var slot))
        {
            Debug.LogWarning($"SlotManager: Slot '{slotName}' not found.");
            return;
        }
        
        // 尝试从管理的插槽获取配置
        var managedSlot = GetManagedSlot(slotName);
        if (managedSlot != null && duration <= 0)
        {
            duration = managedSlot.fadeDuration;
        }
        
        alpha = Mathf.Clamp01(alpha);
        
        if (duration <= 0f)
        {
            slot.A = alpha;
            if (managedSlot != null)
            {
                managedSlot.SetAlpha(alpha);
                managedSlot.SetVisible(alpha > 0.01f);
            }
        }
        else
        {
            StartCoroutine(FadeSlotAlpha(slotName, slot, alpha, duration, managedSlot));
        }
    }
    
    /// <summary>
    /// 切换单个插槽可见性
    /// </summary>
    public void ToggleSlotVisible(string slotName, float duration = 0f)
    {
        if (!slots.TryGetValue(slotName, out var slot))
        {
            Debug.LogWarning($"SlotManager: Slot '{slotName}' not found.");
            return;
        }
        
        bool isVisible = slot.A > 0.01f;
        SetSlotVisible(slotName, !isVisible, duration);
    }
    
    /// <summary>
    /// 设置单个插槽附件
    /// </summary>
    public void SetSlotAttachment(string slotName, string attachmentName, bool log = true)
    {
        if (!slots.TryGetValue(slotName, out var slot))
        {
            Debug.LogWarning($"SlotManager: Slot '{slotName}' not found.");
            return;
        }
        
        int slotIndex = skeleton.FindSlotIndex(slotName);
        Attachment attachment = skeleton.GetAttachment(slotIndex, attachmentName);
        
        if (attachment != null)
        {
            slot.Attachment = attachment;
            
            if (log && debugLog)
                Debug.Log($"SlotManager: Set attachment '{attachmentName}' to slot '{slotName}'.");
        }
        else
        {
            Debug.LogWarning($"SlotManager: Attachment '{attachmentName}' not found for slot '{slotName}'.");
        }
    }
    
    /// <summary>
    /// 重置插槽到默认附件
    /// </summary>
    public void ResetSlotAttachment(string slotName)
    {
        var managedSlot = GetManagedSlot(slotName);
        if (managedSlot != null && !string.IsNullOrEmpty(managedSlot.defaultAttachment))
        {
            SetSlotAttachment(slotName, managedSlot.defaultAttachment);
        }
    }
    
    #endregion
    
    #region 分组操作
    
    /// <summary>
    /// 设置分组可见性
    /// </summary>
    public void SetGroupVisible(string groupName, bool visible, float duration = -1f)
    {
        if (!groupByName.TryGetValue(groupName, out var group))
        {
            Debug.LogWarning($"SlotManager: Group '{groupName}' not found.");
            return;
        }
        
        if (duration < 0) duration = group.fadeDuration;
        float targetAlpha = visible ? group.defaultAlpha : 0f;
        
        // 更新分组状态
        group.SetVisible(visible);
        group.SetAlpha(targetAlpha);
        
        // 应用所有插槽
        foreach (var slotName in group.slotNames)
        {
            if (slots.TryGetValue(slotName, out var slot))
            {
                if (duration <= 0f)
                {
                    slot.A = targetAlpha;
                }
                else
                {
                    StartCoroutine(FadeSlotAlpha(slotName, slot, targetAlpha, duration));
                }
            }
            else
            {
                Debug.LogWarning($"SlotManager: Slot '{slotName}' in group '{groupName}' not found.");
            }
        }
        
        if (debugLog)
            Debug.Log($"SlotManager: Set group '{groupName}' visible: {visible}");
    }
    
    /// <summary>
    /// 设置分组透明度
    /// </summary>
    public void SetGroupAlpha(string groupName, float alpha, float duration = -1f)
    {
        if (!groupByName.TryGetValue(groupName, out var group))
        {
            Debug.LogWarning($"SlotManager: Group '{groupName}' not found.");
            return;
        }
        
        if (duration < 0) duration = group.fadeDuration;
        alpha = Mathf.Clamp01(alpha);
        
        // 更新分组状态
        group.SetAlpha(alpha);
        group.SetVisible(alpha > 0.01f);
        
        // 应用所有插槽
        foreach (var slotName in group.slotNames)
        {
            if (slots.TryGetValue(slotName, out var slot))
            {
                if (duration <= 0f)
                {
                    slot.A = alpha;
                }
                else
                {
                    StartCoroutine(FadeSlotAlpha(slotName, slot, alpha, duration));
                }
            }
            else
            {
                Debug.LogWarning($"SlotManager: Slot '{slotName}' in group '{groupName}' not found.");
            }
        }
        
        if (debugLog)
            Debug.Log($"SlotManager: Set group '{groupName}' alpha to {alpha:F2}");
    }
    
    /// <summary>
    /// 切换分组可见性
    /// </summary>
    public void ToggleGroupVisible(string groupName, float duration = -1f)
    {
        if (!groupByName.TryGetValue(groupName, out var group))
        {
            Debug.LogWarning($"SlotManager: Group '{groupName}' not found.");
            return;
        }
        
        SetGroupVisible(groupName, !group.IsVisible, duration);
    }
    
    /// <summary>
    /// 设置分组所有插槽的附件
    /// </summary>
    public void SetGroupAttachments(string groupName, string attachmentName)
    {
        if (!groupByName.TryGetValue(groupName, out var group))
        {
            Debug.LogWarning($"SlotManager: Group '{groupName}' not found.");
            return;
        }
        
        foreach (var slotName in group.slotNames)
        {
            SetSlotAttachment(slotName, attachmentName);
        }
    }
    
    /// <summary>
    /// 添加插槽到分组
    /// </summary>
    public void AddSlotToGroup(string groupName, string slotName)
    {
        if (!groupByName.TryGetValue(groupName, out var group))
        {
            Debug.LogWarning($"SlotManager: Group '{groupName}' not found.");
            return;
        }
        
        if (!group.slotNames.Contains(slotName))
        {
            group.slotNames.Add(slotName);
            
            if (debugLog)
                Debug.Log($"SlotManager: Added slot '{slotName}' to group '{groupName}'.");
        }
    }
    
    /// <summary>
    /// 从分组移除插槽
    /// </summary>
    public void RemoveSlotFromGroup(string groupName, string slotName)
    {
        if (!groupByName.TryGetValue(groupName, out var group))
        {
            Debug.LogWarning($"SlotManager: Group '{groupName}' not found.");
            return;
        }
        
        if (group.slotNames.Remove(slotName))
        {
            if (debugLog)
                Debug.Log($"SlotManager: Removed slot '{slotName}' from group '{groupName}'.");
        }
    }
    
    /// <summary>
    /// 创建新分组
    /// </summary>
    public void CreateGroup(string groupName, float defaultAlpha = 1f, bool visibleOnStart = true)
    {
        if (groupByName.ContainsKey(groupName))
        {
            Debug.LogWarning($"SlotManager: Group '{groupName}' already exists.");
            return;
        }
        
        var newGroup = new SlotGroup
        {
            name = groupName,
            defaultAlpha = defaultAlpha,
            visibleOnStart = visibleOnStart
        };
        
        slotGroups.Add(newGroup);
        groupByName[groupName] = newGroup;
        
        if (debugLog)
            Debug.Log($"SlotManager: Created group '{groupName}'.");
    }
    
    #endregion
    
    #region 批量操作
    
    /// <summary>
    /// 设置所有插槽可见性
    /// </summary>
    public void SetAllVisible(bool visible, float duration = 0f)
    {
        foreach (var slotName in SlotNames)
        {
            SetSlotVisible(slotName, visible, duration);
        }
    }
    
    /// <summary>
    /// 设置所有插槽透明度
    /// </summary>
    public void SetAllAlpha(float alpha, float duration = 0f)
    {
        foreach (var slotName in SlotNames)
        {
            SetSlotAlpha(slotName, alpha, duration);
        }
    }
    
    /// <summary>
    /// 根据图层配置设置插槽
    /// </summary>
    public void ApplyLayerSlots(AnimationConfig.AnimationLayer layerConfig, bool visible, float duration = 0f)
    {
        if (layerConfig == null) return;
        
        if (layerConfig.slotNames == null || layerConfig.slotNames.Count == 0)
        {
            // 如果图层未指定插槽，控制所有插槽
            SetAllVisible(visible, duration);
        }
        else
        {
            // 控制图层指定的插槽
            foreach (var slotName in layerConfig.slotNames)
            {
                SetSlotVisible(slotName, visible, duration);
            }
        }
    }
    
    #endregion
    
    #region 查询方法
    
    /// <summary>
    /// 检查插槽是否存在
    /// </summary>
    public bool HasSlot(string slotName)
    {
        return slots.ContainsKey(slotName);
    }

    public string GetAttachmentNameByIndex(string slotName, int index)
    {
        if (!slots.TryGetValue(slotName, out var slot))
            return null;

        var skinEntries = new List<string>();
        var skeletonData = skeleton.Data;
        var defaultSkin = skeletonData.DefaultSkin;
        int slotIndex = slot.Data.Index;

        if (defaultSkin != null)
        {
            foreach (var entry in defaultSkin.Attachments)
            {
                if (entry.Key.SlotIndex == slotIndex)
                    skinEntries.Add(entry.Key.Name);
            }
        }

        if (skeletonData.Skins != null)
        {
            foreach (var skin in skeletonData.Skins)
            {
                if (skin == defaultSkin) continue;
                foreach (var entry in skin.Attachments)
                {
                    if (entry.Key.SlotIndex == slotIndex)
                    {
                        if (!skinEntries.Contains(entry.Key.Name))
                            skinEntries.Add(entry.Key.Name);
                    }
                }
            }
        }

        skinEntries.Sort(string.CompareOrdinal);

        if (index >= 1 && index <= skinEntries.Count)
            return skinEntries[index - 1];

        return null;
    }
    
    /// <summary>
    /// 获取插槽透明度
    /// </summary>
    public float GetSlotAlpha(string slotName)
    {
        if (slots.TryGetValue(slotName, out var slot))
            return slot.A;
        return 0f;
    }
    
    /// <summary>
    /// 获取插槽当前附件名称
    /// </summary>
    public string GetSlotAttachmentName(string slotName)
    {
        if (slots.TryGetValue(slotName, out var slot))
            return slot.Attachment?.Name;
        return null;
    }
    
    /// <summary>
    /// 检查插槽是否可见
    /// </summary>
    public bool IsSlotVisible(string slotName)
    {
        return GetSlotAlpha(slotName) > 0.01f;
    }
    
    /// <summary>
    /// 获取管理的插槽列表
    /// </summary>
    public List<ManagedSlot> GetManagedSlots()
    {
        return new List<ManagedSlot>(managedSlots);
    }
    
    /// <summary>
    /// 获取分组
    /// </summary>
    public SlotGroup GetGroup(string groupName)
    {
        groupByName.TryGetValue(groupName, out var group);
        return group;
    }
    
    /// <summary>
    /// 获取所有分组名称
    /// </summary>
    public List<string> GetGroupNames()
    {
        return new List<string>(groupByName.Keys);
    }
    
    /// <summary>
    /// 获取指定分组的所有插槽名称
    /// </summary>
    public List<string> GetGroupSlotNames(string groupName)
    {
        if (groupByName.TryGetValue(groupName, out var group))
            return new List<string>(group.slotNames);
        return new List<string>();
    }
    
    #endregion
    
    #region 辅助方法
    
    private System.Collections.IEnumerator FadeSlotAlpha(string slotName, Slot slot, float targetAlpha, float duration, ManagedSlot managedSlot = null)
    {
        // 停止同一插槽的现有淡入淡出协程
        if (activeFadeCoroutines.TryGetValue(slotName, out var existingCoroutine))
        {
            if (existingCoroutine != null)
                StopCoroutine(existingCoroutine);
        }
        
        // 启动新协程并存储引用
        var coroutine = StartCoroutine(FadeSlotAlphaCoroutine(slotName, slot, targetAlpha, duration, managedSlot));
        activeFadeCoroutines[slotName] = coroutine;
        yield return coroutine;
    }
    
    private System.Collections.IEnumerator FadeSlotAlphaCoroutine(string slotName, Slot slot, float targetAlpha, float duration, ManagedSlot managedSlot = null)
    {
        float startAlpha = slot.A;
        float startTime = Time.time;
        float endTime = startTime + duration;
        
        while (Time.time < endTime)
        {
            float t = (Time.time - startTime) / duration;
            float currentAlpha = Mathf.Lerp(startAlpha, targetAlpha, t);
            slot.A = currentAlpha;
            yield return null;
        }
        
        slot.A = targetAlpha;
        
        // 更新管理的插槽状态
        if (managedSlot != null)
        {
            managedSlot.SetAlpha(targetAlpha);
            managedSlot.SetVisible(targetAlpha > 0.01f);
        }
        
        // 从活动协程字典中移除
        activeFadeCoroutines.Remove(slotName);
    }
    
    #endregion
    
    #region 编辑器辅助方法
    
    #if UNITY_EDITOR
    [ContextMenu("收集所有插槽")]
    private void CollectAllSlotsInEditor()
    {
        if (!Application.isPlaying)
        {
            // 在编辑器中获取骨架
            ISkeletonComponent editorSkeletonComponent = GetComponent<SkeletonAnimation>();
            if (editorSkeletonComponent == null)
                editorSkeletonComponent = GetComponent<SkeletonGraphic>();
            
            if (editorSkeletonComponent == null)
            {
                Debug.LogError("No SkeletonAnimation or SkeletonGraphic component found.");
                return;
            }
            
            var skeleton = editorSkeletonComponent.Skeleton;
            if (skeleton == null)
            {
                Debug.LogError("Skeleton is null. Make sure the skeleton component is initialized.");
                return;
            }
            
            // 收集插槽信息
            slots.Clear();
            var slotList = skeleton.Slots.Items;
            for (int i = 0; i < slotList.Length; i++)
            {
                Slot slot = slotList[i];
                if (slot != null && slot.Data != null)
                {
                    string slotName = slot.Data.Name;
                    slots[slotName] = slot;
                }
            }
            
            Debug.Log($"SlotManager: Collected {slots.Count} slots.");
            
            // 显示插槽列表
            string slotListText = "可用插槽:\n";
            foreach (var slotName in SlotNames)
            {
                slotListText += $"- {slotName}\n";
            }
            Debug.Log(slotListText);
        }
        else
        {
            CollectAllSlots();
        }
    }
    
    [ContextMenu("自动填充管理的插槽")]
    private void AutoPopulateManagedSlotsInEditor()
    {
        if (!Application.isPlaying)
        {
            // 在编辑器中获取骨架
            ISkeletonComponent editorSkeletonComponent = GetComponent<SkeletonAnimation>();
            if (editorSkeletonComponent == null)
                editorSkeletonComponent = GetComponent<SkeletonGraphic>();
            
            if (editorSkeletonComponent == null)
            {
                Debug.LogError("No SkeletonAnimation or SkeletonGraphic component found.");
                return;
            }
            
            var skeleton = editorSkeletonComponent.Skeleton;
            if (skeleton == null)
            {
                Debug.LogError("Skeleton is null. Make sure the skeleton component is initialized.");
                return;
            }
            
            // 清空现有列表
            managedSlots.Clear();
            
            // 获取所有插槽
            var slotList = skeleton.Slots.Items;
            for (int i = 0; i < slotList.Length; i++)
            {
                Slot slot = slotList[i];
                if (slot != null && slot.Data != null)
                {
                    string slotName = slot.Data.Name;
                    
                    // 创建新的管理插槽
                    var managedSlot = new ManagedSlot
                    {
                        slotName = slotName,
                        visibleOnStart = true,
                        defaultAlpha = 1.0f,
                        fadeDuration = 0.2f
                    };
                    
                    managedSlots.Add(managedSlot);
                }
            }
            
            Debug.Log($"SlotManager: Auto-populated {managedSlots.Count} managed slots.");
        }
        else
        {
            Debug.LogWarning("This function only works in Editor mode.");
        }
    }
    
    [ContextMenu("自动创建默认分组")]
    private void CreateDefaultGroupsInEditor()
    {
        // 创建一些常见分组
        CreateGroup("全部", 1f, true);
        CreateGroup("武器", 1f, true);
        CreateGroup("服装", 1f, true);
        CreateGroup("装饰", 1f, true);
        CreateGroup("特效", 1f, false);
        
        Debug.Log("SlotManager: 已创建默认分组");
    }
    
    [ContextMenu("显示所有插槽")]
    private void ShowAllSlotsInEditor()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("This function only works in Play mode.");
            return;
        }
        
        SetAllVisible(true, 0f);
    }
    
    [ContextMenu("隐藏所有插槽")]
    private void HideAllSlotsInEditor()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("This function only works in Play mode.");
            return;
        }
        
        SetAllVisible(false, 0f);
    }
    
    [ContextMenu("打印插槽状态")]
    private void PrintSlotStatusInEditor()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("This function only works in Play mode.");
            return;
        }
        
        string status = "插槽状态:\n";
        foreach (var slotName in SlotNames)
        {
            float alpha = GetSlotAlpha(slotName);
            string attachment = GetSlotAttachmentName(slotName);
            status += $"- {slotName}: 透明度={alpha:F2}, 附件={attachment ?? "无"}\n";
        }
        
        Debug.Log(status);
    }
    
    [ContextMenu("打印管理的插槽状态")]
    private void PrintManagedSlotStatusInEditor()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("This function only works in Play mode.");
            return;
        }
        
        string status = "管理的插槽状态:\n";
        foreach (var managedSlot in managedSlots)
        {
            status += $"- {managedSlot.slotName}: 可见={managedSlot.isVisible}, 透明度={managedSlot.currentAlpha:F2}, 默认附件={managedSlot.defaultAttachment ?? "无"}\n";
        }
        
        Debug.Log(status);
    }
    
    private void OnDestroy()
    {
        // 停止所有活动的淡入淡出协程
        foreach (var coroutine in activeFadeCoroutines.Values)
        {
            if (coroutine != null)
                StopCoroutine(coroutine);
        }
        activeFadeCoroutines.Clear();
    }
    #endif
    
    #endregion
}