using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System;

[CustomEditor(typeof(AnimationConfig))]
public class AnimationConfigEditor : Editor
{
    private SerializedProperty skeletonDataProp;
    private SerializedProperty clipsProp;
    private SerializedProperty layersProp;
    private SerializedProperty compositionsProp;
    private SerializedProperty defaultCompositionProp;
    private SerializedProperty defaultTransitionTimeProp;
    
    // 折叠面板状态
    private bool clipsFoldout = true;
    private bool layersFoldout = true;
    private bool compositionsFoldout = true;
    
    // 缓存数据（避免每帧重新计算）
    private string[] cachedLayerNames;
    private string[] cachedClipNames;
    private Dictionary<string, string[]> layerAvailableClipsCache = new Dictionary<string, string[]>();
    private bool cacheDirty = true;
    
    // 动画类型选项
    private GUIContent[] clipTypeOptions = new GUIContent[]
    {
        new GUIContent("全身动画"),
        new GUIContent("上半身动画"),
        new GUIContent("下半身动画"),
        new GUIContent("面部动画"),
        new GUIContent("特殊效果动画")
    };
    
    private int[] clipTypeValues = new int[] { 0, 1, 2, 3, 4 };
    
    private void OnEnable()
    {
        skeletonDataProp = serializedObject.FindProperty("skeletonData");
        clipsProp = serializedObject.FindProperty("clips");
        layersProp = serializedObject.FindProperty("layers");
        compositionsProp = serializedObject.FindProperty("compositions");
        defaultCompositionProp = serializedObject.FindProperty("defaultComposition");
        defaultTransitionTimeProp = serializedObject.FindProperty("defaultTransitionTime");
        cacheDirty = true;
    }
    
    private void UpdateCacheIfNeeded()
    {
        if (!cacheDirty) return;
        
        // 更新层名称缓存
        List<string> layerNames = new List<string>();
        for (int i = 0; i < layersProp.arraySize; i++)
        {
            var layerProp = layersProp.GetArrayElementAtIndex(i);
            string name = layerProp.FindPropertyRelative("name").stringValue;
            if (!string.IsNullOrEmpty(name))
                layerNames.Add(name);
        }
        cachedLayerNames = layerNames.ToArray();
        
        // 更新剪辑名称缓存
        List<string> clipNames = new List<string>();
        for (int i = 0; i < clipsProp.arraySize; i++)
        {
            var clipProp = clipsProp.GetArrayElementAtIndex(i);
            string name = clipProp.FindPropertyRelative("name").stringValue;
            if (!string.IsNullOrEmpty(name))
                clipNames.Add(name);
        }
        cachedClipNames = clipNames.ToArray();
        
        // 清空层级可用剪辑缓存
        layerAvailableClipsCache.Clear();
        
        cacheDirty = false;
    }
    
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        
        // 更新缓存（如果需要）
        UpdateCacheIfNeeded();
        
        // 配置概览
        DrawConfigOverview();
        
        // 基本信息
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("基本信息", EditorStyles.boldLabel);
        DrawBasicInfo();
        
        // 动画片段区域
        clipsFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(clipsFoldout, $"动画片段 ({clipsProp.arraySize})");
        if (clipsFoldout) DrawClipsSection();
        EditorGUILayout.EndFoldoutHeaderGroup();
        
        // 动画层级区域
        layersFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(layersFoldout, $"动画层级 ({layersProp.arraySize})");
        if (layersFoldout) DrawLayersSection();
        EditorGUILayout.EndFoldoutHeaderGroup();
        
        // 动画组合区域
        compositionsFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(compositionsFoldout, $"动画组合 ({compositionsProp.arraySize})");
        if (compositionsFoldout) DrawCompositionsSection();
        EditorGUILayout.EndFoldoutHeaderGroup();
        
        // 操作按钮
        EditorGUILayout.Space();
        EditorGUILayout.BeginHorizontal();
        
        if (GUILayout.Button("验证配置"))
        {
            ((AnimationConfig)target).ValidateConfig();
            EditorUtility.DisplayDialog("配置验证", "配置验证完成，请查看控制台获取详细信息。", "确定");
        }
        
        if (GUILayout.Button("重置配置"))
        {
            if (EditorUtility.DisplayDialog("重置配置", "确定要重置所有动画片段和组合配置吗？此操作不可撤销。", "重置", "取消"))
            {
                ResetConfig();
            }
        }
        
        EditorGUILayout.EndHorizontal();
        
        serializedObject.ApplyModifiedProperties();
    }
    
    private void DrawConfigOverview()
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("配置概览", EditorStyles.boldLabel);
        
        var config = (AnimationConfig)target;
        string skeletonInfo = config.skeletonData != null ? config.skeletonData.name : "未设置";
        
        EditorGUILayout.BeginHorizontal();
        
        // 统计卡片
        DrawStatCard("动画片段", clipsProp.arraySize.ToString(), clipsProp.arraySize > 0 ? Color.green : Color.gray);
        DrawStatCard("动画层级", layersProp.arraySize.ToString(), layersProp.arraySize > 0 ? Color.green : Color.gray);
        DrawStatCard("动画组合", compositionsProp.arraySize.ToString(), compositionsProp.arraySize > 0 ? Color.green : Color.gray);
        
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.HelpBox($"骨骼数据: {skeletonInfo}\n默认组合: {defaultCompositionProp.stringValue}", MessageType.Info);
        EditorGUILayout.Space();
        
        // 测试按钮（开发用）
        #if false
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("测试：从Hippo文件夹加载示例"))
        {
            LoadExampleAnimationsFromHippoFolder();
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space();
        #endif
    }
    
    private void DrawStatCard(string label, string value, Color color)
    {
        EditorGUILayout.BeginVertical("box", GUILayout.MinWidth(80));
        
        var originalColor = GUI.color;
        GUI.color = color;
        EditorGUILayout.LabelField(label, EditorStyles.centeredGreyMiniLabel);
        GUI.color = originalColor;
        
        EditorGUILayout.LabelField(value, EditorStyles.boldLabel, GUILayout.Height(20));
        
        EditorGUILayout.EndVertical();
    }
    
    private void DrawBasicInfo()
    {
        EditorGUILayout.BeginVertical("box");
        
        // 骨骼数据字段
        EditorGUILayout.PropertyField(skeletonDataProp, new GUIContent("骨骼数据", "Spine骨骼数据资产"));
        
        // 默认设置
        EditorGUILayout.PropertyField(defaultCompositionProp, new GUIContent("默认动画组合"));
        EditorGUILayout.PropertyField(defaultTransitionTimeProp, new GUIContent("默认过渡时间"));
        
        EditorGUILayout.EndVertical();
    }
    
    private void DrawClipsSection()
    {
        EditorGUILayout.BeginVertical("box");
        
        // 拖拽区域
        Rect dropArea = GUILayoutUtility.GetRect(0, 40, GUILayout.ExpandWidth(true));
        GUI.Box(dropArea, "拖拽 AnimationReferenceAsset 到这里添加动画片段", EditorStyles.helpBox);
        
        // 处理拖拽事件
        Event evt = Event.current;
        switch (evt.type)
        {
            case EventType.DragUpdated:
            case EventType.DragPerform:
                if (!dropArea.Contains(evt.mousePosition)) break;
                
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                
                if (evt.type == EventType.DragPerform)
                {
                    DragAndDrop.AcceptDrag();
                    
                    bool anyAdded = false;
                    foreach (var obj in DragAndDrop.objectReferences)
                    {
                        var animRef = obj as Spine.Unity.AnimationReferenceAsset;
                        if (animRef != null)
                        {
                            if (AddClipFromReference(animRef))
                            {
                                anyAdded = true;
                            }
                        }
                    }
                    
                    if (anyAdded)
                    {
                        EditorUtility.SetDirty(target);
                        AssetDatabase.SaveAssets();
                        Debug.Log("动画片段已添加并保存");
                    }
                }
                evt.Use();
                break;
        }
        
        // 动画片段列表
        if (clipsProp.arraySize == 0)
        {
            EditorGUILayout.HelpBox("暂无动画片段。请从 Project 窗口拖拽 AnimationReferenceAsset 到上方区域。", MessageType.Info);
        }
        else
        {
            EditorGUILayout.LabelField("动画片段列表:", EditorStyles.boldLabel);
            
            for (int i = 0; i < clipsProp.arraySize; i++)
            {
                DrawClipElement(i);
            }
        }
        
        EditorGUILayout.EndVertical();
    }
    
    private bool AddClipFromReference(Spine.Unity.AnimationReferenceAsset animRef)
    {
        // 检查是否已存在同名 clip
        for (int i = 0; i < clipsProp.arraySize; i++)
        {
            var clipProp = clipsProp.GetArrayElementAtIndex(i);
            var nameProp = clipProp.FindPropertyRelative("name");
            var animProp = clipProp.FindPropertyRelative("animation");
            
            var existingAnimRef = animProp.objectReferenceValue as Spine.Unity.AnimationReferenceAsset;
            if (existingAnimRef == animRef || nameProp.stringValue == animRef.name)
            {
                Debug.LogWarning($"动画片段已存在: {animRef.name}");
                return false;
            }
        }
        
        // 创建新 clip
        clipsProp.arraySize++;
        var newClipProp = clipsProp.GetArrayElementAtIndex(clipsProp.arraySize - 1);
        
        // 设置属性
        newClipProp.FindPropertyRelative("name").stringValue = animRef.name;
        newClipProp.FindPropertyRelative("animation").objectReferenceValue = animRef;
        newClipProp.FindPropertyRelative("animationName").stringValue = GetAnimationNameFromReference(animRef);
        newClipProp.FindPropertyRelative("speed").floatValue = 1.0f;
        newClipProp.FindPropertyRelative("loop").boolValue = true;
        
        // 根据命名智能猜测动画类型
        var typeProp = newClipProp.FindPropertyRelative("type");
        int typeValue = GuessClipTypeFromName(animRef.name);
        typeProp.enumValueIndex = typeValue;
        
        Debug.Log($"添加动画片段: {animRef.name} (类型: {((AnimationConfig.AnimationClipType)typeValue)})");
        cacheDirty = true;
        return true;
    }
    
    private string GetAnimationNameFromReference(Spine.Unity.AnimationReferenceAsset animRef)
    {
        if (animRef == null)
            return string.Empty;
            
        try
        {
            // 尝试使用反射获取私有字段 animationName
            var field = typeof(Spine.Unity.AnimationReferenceAsset).GetField("animationName", 
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
        catch (Exception ex)
        {
            Debug.LogWarning($"获取动画名称失败: {ex.Message}");
        }
        
        return animRef.name; // 降级：使用资产名称
    }
    
    private int GuessClipTypeFromName(string name)
    {
        string lowerName = name.ToLower();
        
        if (lowerName.Contains("body-"))
        {
            return (int)AnimationConfig.AnimationClipType.UpperBody;
        }
        else if (lowerName.Contains("leg-"))
        {
            return (int)AnimationConfig.AnimationClipType.LowerBody;
        }
        else if (lowerName.Contains("face-") || lowerName.Contains("表情") || lowerName.Contains("mouth"))
        {
            return (int)AnimationConfig.AnimationClipType.Face;
        }
        else if (lowerName.Contains("special-") || lowerName.Contains("特效") || lowerName.Contains("技能"))
        {
            return (int)AnimationConfig.AnimationClipType.Special;
        }
        
        return (int)AnimationConfig.AnimationClipType.FullBody;
    }
    
    private void DrawClipElement(int index)
    {
        var clipProp = clipsProp.GetArrayElementAtIndex(index);
        var nameProp = clipProp.FindPropertyRelative("name");
        var animationProp = clipProp.FindPropertyRelative("animation");
        var animationNameProp = clipProp.FindPropertyRelative("animationName");
        var speedProp = clipProp.FindPropertyRelative("speed");
        var loopProp = clipProp.FindPropertyRelative("loop");
        var typeProp = clipProp.FindPropertyRelative("type");
        
        EditorGUILayout.BeginVertical("box");
        
        EditorGUILayout.BeginHorizontal();
        
        // 动画引用预览
        EditorGUILayout.LabelField($"片段 {index + 1}:", GUILayout.Width(60));
        EditorGUILayout.ObjectField(animationProp.objectReferenceValue, typeof(Spine.Unity.AnimationReferenceAsset), false, GUILayout.Width(150));
        
        // 名称和类型
        EditorGUILayout.BeginVertical();
        EditorGUILayout.PropertyField(nameProp, GUIContent.none, GUILayout.Width(120));
        
        // 类型下拉菜单
        int currentType = typeProp.enumValueIndex;
        int newType = EditorGUILayout.IntPopup(currentType, clipTypeOptions, clipTypeValues, GUILayout.Width(100));
        if (newType != currentType)
        {
            typeProp.enumValueIndex = newType;
        }
        EditorGUILayout.EndVertical();
        
        // 参数控制
        EditorGUILayout.BeginVertical();
        EditorGUILayout.PropertyField(speedProp, GUIContent.none, GUILayout.Width(60));
        EditorGUILayout.PropertyField(loopProp, GUIContent.none, GUILayout.Width(40));
        EditorGUILayout.EndVertical();
        
        // 删除按钮
        if (GUILayout.Button("×", GUILayout.Width(20)))
        {
            if (EditorUtility.DisplayDialog("删除动画片段", $"确定要删除动画片段 '{nameProp.stringValue}' 吗？", "删除", "取消"))
            {
                RemoveClipAtIndex(index);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                return;
            }
        }
        
        EditorGUILayout.EndHorizontal();
        
        // 详细信息
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Spine动画名:", GUILayout.Width(80));
        EditorGUILayout.LabelField(animationNameProp.stringValue, EditorStyles.wordWrappedLabel);
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.EndVertical();
    }
    
    private void RemoveClipAtIndex(int index)
    {
        if (index < 0 || index >= clipsProp.arraySize) return;
        
        var clipProp = clipsProp.GetArrayElementAtIndex(index);
        var nameProp = clipProp.FindPropertyRelative("name");
        string clipName = nameProp.stringValue;
        
        // 从所有层的 availableClipNames 中移除
        for (int i = 0; i < layersProp.arraySize; i++)
        {
            var layerProp = layersProp.GetArrayElementAtIndex(i);
            var availableClipsProp = layerProp.FindPropertyRelative("availableClipNames");
            
            for (int j = availableClipsProp.arraySize - 1; j >= 0; j--)
            {
                var clipNameProp = availableClipsProp.GetArrayElementAtIndex(j);
                if (clipNameProp.stringValue == clipName)
                {
                    availableClipsProp.DeleteArrayElementAtIndex(j);
                }
            }
        }
        
        // 从所有组合中移除引用
        for (int i = 0; i < compositionsProp.arraySize; i++)
        {
            var compProp = compositionsProp.GetArrayElementAtIndex(i);
            var layersPropInComp = compProp.FindPropertyRelative("layers");
            
            for (int j = layersPropInComp.arraySize - 1; j >= 0; j--)
            {
                var layerInCompProp = layersPropInComp.GetArrayElementAtIndex(j);
                var clipNameProp = layerInCompProp.FindPropertyRelative("clipName");
                
                if (clipNameProp.stringValue == clipName)
                {
                    layersPropInComp.DeleteArrayElementAtIndex(j);
                }
            }
        }
        
        // 删除 clip 本身
        clipsProp.DeleteArrayElementAtIndex(index);
        
        Debug.Log($"删除动画片段: {clipName}");
        cacheDirty = true;
    }
    
    private void DrawLayersSection()
    {
        EditorGUILayout.BeginVertical("box");
        
        if (layersProp.arraySize == 0)
        {
            EditorGUILayout.HelpBox("暂无动画层级。", MessageType.Info);
        }
        else
        {
            // 表头
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("层级名称", GUILayout.Width(80));
            EditorGUILayout.LabelField("轨道", GUILayout.Width(40));
            EditorGUILayout.LabelField("默认权重", GUILayout.Width(70));
            EditorGUILayout.LabelField("叠加模式", GUILayout.Width(70));
            EditorGUILayout.LabelField("可用动画片段");
            EditorGUILayout.EndHorizontal();
            
            // 每个 layer
            for (int i = 0; i < layersProp.arraySize; i++)
            {
                DrawLayerElement(i);
            }
        }
        
        EditorGUILayout.EndVertical();
    }
    
    private void DrawLayerElement(int index)
    {
        var layerProp = layersProp.GetArrayElementAtIndex(index);
        var nameProp = layerProp.FindPropertyRelative("name");
        var trackIndexProp = layerProp.FindPropertyRelative("trackIndex");
        var defaultWeightProp = layerProp.FindPropertyRelative("defaultWeight");
        var additiveProp = layerProp.FindPropertyRelative("additive");
        var availableClipsProp = layerProp.FindPropertyRelative("availableClipNames");
        
        EditorGUILayout.BeginVertical("box");
        
        EditorGUILayout.BeginHorizontal();
        
        // 基本信息
        EditorGUILayout.PropertyField(nameProp, GUIContent.none, GUILayout.Width(80));
        EditorGUILayout.PropertyField(trackIndexProp, GUIContent.none, GUILayout.Width(40));
        EditorGUILayout.PropertyField(defaultWeightProp, GUIContent.none, GUILayout.Width(70));
        EditorGUILayout.PropertyField(additiveProp, GUIContent.none, GUILayout.Width(70));
        
        // 可用动画片段
        EditorGUILayout.BeginVertical();
        
        if (clipsProp.arraySize == 0)
        {
            EditorGUILayout.LabelField("无可用动画片段", EditorStyles.miniLabel);
        }
        else
        {
            for (int i = 0; i < clipsProp.arraySize; i++)
            {
                var clipProp = clipsProp.GetArrayElementAtIndex(i);
                var clipNameProp = clipProp.FindPropertyRelative("name");
                string clipName = clipNameProp.stringValue;
                
                bool isAvailable = IsClipAvailableForLayer(availableClipsProp, clipName);
                bool newValue = EditorGUILayout.ToggleLeft(clipName, isAvailable);
                
                if (newValue != isAvailable)
                {
                    UpdateClipAvailability(availableClipsProp, clipName, newValue);
                }
            }
        }
        
        EditorGUILayout.EndVertical();
        
        EditorGUILayout.EndHorizontal();
        
        // 骨骼槽位配置（如果需要）
        var slotNamesProp = layerProp.FindPropertyRelative("slotNames");
        if (slotNamesProp.arraySize > 0 || GUILayout.Button("配置骨骼槽位", GUILayout.Width(120)))
        {
            EditorGUILayout.PropertyField(slotNamesProp, new GUIContent("控制的骨骼槽位"), true);
        }
        
        EditorGUILayout.EndVertical();
    }
    
    private bool IsClipAvailableForLayer(SerializedProperty availableClipsProp, string clipName)
    {
        for (int i = 0; i < availableClipsProp.arraySize; i++)
        {
            var clipNameProp = availableClipsProp.GetArrayElementAtIndex(i);
            if (clipNameProp.stringValue == clipName)
            {
                return true;
            }
        }
        return false;
    }
    
    private void UpdateClipAvailability(SerializedProperty availableClipsProp, string clipName, bool isAvailable)
    {
        if (isAvailable)
        {
            // 添加到可用列表
            bool alreadyExists = false;
            for (int i = 0; i < availableClipsProp.arraySize; i++)
            {
                var clipNameProp = availableClipsProp.GetArrayElementAtIndex(i);
                if (clipNameProp.stringValue == clipName)
                {
                    alreadyExists = true;
                    break;
                }
            }
            
            if (!alreadyExists)
            {
                availableClipsProp.arraySize++;
                availableClipsProp.GetArrayElementAtIndex(availableClipsProp.arraySize - 1).stringValue = clipName;
            }
        }
        else
        {
            // 从可用列表中移除
            for (int i = availableClipsProp.arraySize - 1; i >= 0; i--)
            {
                var clipNameProp = availableClipsProp.GetArrayElementAtIndex(i);
                if (clipNameProp.stringValue == clipName)
                {
                    availableClipsProp.DeleteArrayElementAtIndex(i);
                    break;
                }
            }
        }
    }
    
    private void DrawCompositionsSection()
    {
        EditorGUILayout.BeginVertical("box");
        
        // 添加组合按钮
        if (GUILayout.Button("+ 添加新组合", GUILayout.Width(100)))
        {
            AddNewComposition();
        }
        
        if (compositionsProp.arraySize == 0)
        {
            EditorGUILayout.HelpBox("暂无动画组合。", MessageType.Info);
        }
        else
        {
            for (int i = 0; i < compositionsProp.arraySize; i++)
            {
                DrawCompositionElement(i);
            }
        }
        
        EditorGUILayout.EndVertical();
    }
    
    private void DrawCompositionElement(int index)
    {
        var compProp = compositionsProp.GetArrayElementAtIndex(index);
        var nameProp = compProp.FindPropertyRelative("name");
        var descriptionProp = compProp.FindPropertyRelative("description");
        var layersPropInComp = compProp.FindPropertyRelative("layers");
        
        EditorGUILayout.BeginVertical("box");
        
        EditorGUILayout.BeginHorizontal();
        
        // 组合名称和描述
        EditorGUILayout.BeginVertical();
        EditorGUILayout.PropertyField(nameProp, new GUIContent("组合名称"));
        EditorGUILayout.PropertyField(descriptionProp, new GUIContent("组合描述"));
        EditorGUILayout.EndVertical();
        
        // 删除按钮
        if (GUILayout.Button("删除", GUILayout.Width(60)))
        {
            if (EditorUtility.DisplayDialog("删除动画组合", $"确定要删除动画组合 '{nameProp.stringValue}' 吗？", "删除", "取消"))
            {
                compositionsProp.DeleteArrayElementAtIndex(index);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                return;
            }
        }
        
        EditorGUILayout.EndHorizontal();
        
        // 组合层级配置
        EditorGUILayout.LabelField("层级配置:", EditorStyles.boldLabel);
        
        if (layersPropInComp.arraySize == 0)
        {
            EditorGUILayout.HelpBox("此组合无层级配置。", MessageType.Warning);
        }
        else
        {
            // 表头
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("层级", GUILayout.Width(80));
            EditorGUILayout.LabelField("动画片段", GUILayout.Width(100));
            EditorGUILayout.LabelField("权重", GUILayout.Width(50));
            EditorGUILayout.LabelField("过渡时间", GUILayout.Width(70));
            EditorGUILayout.LabelField("延迟", GUILayout.Width(50));
            EditorGUILayout.EndHorizontal();
            
            // 每个 CompositionLayer
            for (int i = 0; i < layersPropInComp.arraySize; i++)
            {
                DrawCompositionLayerElement(layersPropInComp, i);
            }
        }
        
        // 添加 CompositionLayer 按钮
        if (GUILayout.Button("+ 添加层级到此组合", GUILayout.Width(150)))
        {
            AddCompositionLayer(layersPropInComp);
        }
        
        EditorGUILayout.EndVertical();
    }
    
    private void DrawCompositionLayerElement(SerializedProperty layersPropInComp, int index)
    {
        var layerProp = layersPropInComp.GetArrayElementAtIndex(index);
        
        EditorGUILayout.BeginHorizontal();
        
        // 层级名称下拉菜单
        var layerNameProp = layerProp.FindPropertyRelative("layerName");
        var clipNameProp = layerProp.FindPropertyRelative("clipName");
        string[] layerNames = GetLayerNames();
        int currentLayerIndex = GetLayerIndex(layerNameProp.stringValue);
        
        int newLayerIndex = EditorGUILayout.Popup(currentLayerIndex, layerNames, GUILayout.Width(80));
        if (newLayerIndex >= 0 && newLayerIndex < layerNames.Length && newLayerIndex != currentLayerIndex)
        {
            // 层级改变时，更新层级名称
            string newLayerName = layerNames[newLayerIndex];
            layerNameProp.stringValue = newLayerName;
            
            // 检查当前动画片段是否在新层级的可用列表中
            string[] newAvailableClipNames = GetAvailableClipNamesForLayer(newLayerName);
            
            if (newAvailableClipNames.Length > 0)
            {
                // 如果当前片段不在可用列表中，选择第一个可用片段
                if (Array.IndexOf(newAvailableClipNames, clipNameProp.stringValue) < 0)
                {
                    clipNameProp.stringValue = newAvailableClipNames[0];
                }
            }
            else if (!string.IsNullOrEmpty(clipNameProp.stringValue))
            {
                // 如果新层级没有可用动画片段，清空选择
                clipNameProp.stringValue = "";
            }
        }
        
        // 动画片段下拉菜单（根据层级筛选）
        string[] availableClipNames = GetAvailableClipNamesForLayer(layerNameProp.stringValue);
        int currentClipIndex = GetClipIndexInArray(clipNameProp.stringValue, availableClipNames);
        
        int newClipIndex = EditorGUILayout.Popup(currentClipIndex, availableClipNames, GUILayout.Width(100));
        if (newClipIndex >= 0 && newClipIndex < availableClipNames.Length)
        {
            clipNameProp.stringValue = availableClipNames[newClipIndex];
        }
        
        // 参数
        EditorGUILayout.PropertyField(layerProp.FindPropertyRelative("weight"), GUIContent.none, GUILayout.Width(50));
        EditorGUILayout.PropertyField(layerProp.FindPropertyRelative("transitionTime"), GUIContent.none, GUILayout.Width(70));
        EditorGUILayout.PropertyField(layerProp.FindPropertyRelative("delay"), GUIContent.none, GUILayout.Width(50));
        
        // 删除按钮
        if (GUILayout.Button("×", GUILayout.Width(20)))
        {
            layersPropInComp.DeleteArrayElementAtIndex(index);
        }
        
        EditorGUILayout.EndHorizontal();
    }
    
    private string[] GetLayerNames()
    {
        if (cachedLayerNames == null || cacheDirty)
            UpdateCacheIfNeeded();
        return cachedLayerNames ?? new string[0];
    }
    
    private int GetLayerIndex(string layerName)
    {
        string[] names = GetLayerNames();
        for (int i = 0; i < names.Length; i++)
        {
            if (names[i] == layerName) return i;
        }
        return 0;
    }
    
    private string[] GetClipNames()
    {
        if (cachedClipNames == null || cacheDirty)
            UpdateCacheIfNeeded();
        return cachedClipNames ?? new string[0];
    }
    
    private string[] GetAvailableClipNamesForLayer(string layerName)
    {
        // 如果未指定层级，返回所有动画片段
        if (string.IsNullOrEmpty(layerName))
            return GetClipNames();
        
        // 检查缓存
        if (layerAvailableClipsCache.TryGetValue(layerName, out string[] cachedResult))
            return cachedResult;
        
        List<string> names = new List<string>();
        
        // 查找指定的层级
        for (int i = 0; i < layersProp.arraySize; i++)
        {
            var layerProp = layersProp.GetArrayElementAtIndex(i);
            var nameProp = layerProp.FindPropertyRelative("name");
            
            if (nameProp.stringValue == layerName)
            {
                // 获取该层级可用的动画片段列表
                var availableClipsProp = layerProp.FindPropertyRelative("availableClipNames");
                
                if (availableClipsProp.arraySize == 0)
                {
                    // 如果层级未配置任何可用动画，返回所有动画片段
                    string[] allClips = GetClipNames();
                    layerAvailableClipsCache[layerName] = allClips;
                    return allClips;
                }
                else
                {
                    // 只返回该层级可用的动画片段
                    for (int j = 0; j < availableClipsProp.arraySize; j++)
                    {
                        var clipNameProp = availableClipsProp.GetArrayElementAtIndex(j);
                        names.Add(clipNameProp.stringValue);
                    }
                    string[] result = names.ToArray();
                    layerAvailableClipsCache[layerName] = result;
                    return result;
                }
            }
        }
        
        // 未找到指定层级，返回所有动画片段
        string[] fallbackResult = GetClipNames();
        layerAvailableClipsCache[layerName] = fallbackResult;
        return fallbackResult;
    }
    
    private int GetClipIndex(string clipName)
    {
        string[] names = GetClipNames();
        for (int i = 0; i < names.Length; i++)
        {
            if (names[i] == clipName) return i;
        }
        return 0;
    }
    
    private int GetClipIndexInArray(string clipName, string[] clipNames)
    {
        for (int i = 0; i < clipNames.Length; i++)
        {
            if (clipNames[i] == clipName) return i;
        }
        return 0;
    }
    
    private void AddNewComposition()
    {
        compositionsProp.arraySize++;
        var newCompProp = compositionsProp.GetArrayElementAtIndex(compositionsProp.arraySize - 1);
        
        newCompProp.FindPropertyRelative("name").stringValue = "新组合";
        newCompProp.FindPropertyRelative("description").stringValue = "描述";
        newCompProp.FindPropertyRelative("layers").ClearArray();
    }
    
    private void AddCompositionLayer(SerializedProperty layersPropInComp)
    {
        if (layersProp.arraySize == 0)
        {
            Debug.LogWarning("无法添加组合层级：没有可用的动画层级");
            return;
        }
        
        if (clipsProp.arraySize == 0)
        {
            Debug.LogWarning("无法添加组合层级：没有可用的动画片段");
            return;
        }
        
        layersPropInComp.arraySize++;
        var newLayerProp = layersPropInComp.GetArrayElementAtIndex(layersPropInComp.arraySize - 1);
        
        // 设置默认值
        newLayerProp.FindPropertyRelative("layerName").stringValue = GetLayerNames()[0];
        newLayerProp.FindPropertyRelative("clipName").stringValue = GetClipNames()[0];
        newLayerProp.FindPropertyRelative("weight").floatValue = 1.0f;
        newLayerProp.FindPropertyRelative("transitionTime").floatValue = defaultTransitionTimeProp.floatValue;
        newLayerProp.FindPropertyRelative("delay").floatValue = 0f;
    }
    
    private void ResetConfig()
    {
        // 清空 clips
        clipsProp.ClearArray();
        
        // 清空 compositions
        compositionsProp.ClearArray();
        
        // 重置默认组合
        defaultCompositionProp.stringValue = "Idle";
        
        // 清空所有层的 availableClipNames
        for (int i = 0; i < layersProp.arraySize; i++)
        {
            var layerProp = layersProp.GetArrayElementAtIndex(i);
            var availableClipsProp = layerProp.FindPropertyRelative("availableClipNames");
            availableClipsProp.ClearArray();
        }
        
        Debug.Log("动画配置已重置");
    }
    
    private void LoadExampleAnimationsFromHippoFolder()
    {
        string hippoFolderPath = "Assets/Configs/Animations/Hippo/";
        
        if (!AssetDatabase.IsValidFolder(hippoFolderPath))
        {
            Debug.LogError($"文件夹不存在: {hippoFolderPath}");
            return;
        }
        
        // 查找所有 AnimationReferenceAsset
        string[] guids = AssetDatabase.FindAssets("t:AnimationReferenceAsset", new[] { hippoFolderPath });
        
        if (guids.Length == 0)
        {
            Debug.LogWarning($"在 {hippoFolderPath} 中未找到 AnimationReferenceAsset");
            return;
        }
        
        int addedCount = 0;
        foreach (string guid in guids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            var animRef = AssetDatabase.LoadAssetAtPath<Spine.Unity.AnimationReferenceAsset>(assetPath);
            
            if (animRef != null)
            {
                if (AddClipFromReference(animRef))
                {
                    addedCount++;
                }
            }
        }
        
        if (addedCount > 0)
        {
            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(target);
            AssetDatabase.SaveAssets();
            Debug.Log($"从 {hippoFolderPath} 加载了 {addedCount} 个动画片段");
        }
        else
        {
            Debug.Log("未添加任何新动画片段（可能已全部存在）");
        }
    }
}