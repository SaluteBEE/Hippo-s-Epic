using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using Spine.Unity;

[CustomEditor(typeof(SlotManager))]
public class SlotManagerEditor : Editor
{
    private SerializedProperty skeletonAnimationProp;
    private SerializedProperty skeletonGraphicProp;
    private SerializedProperty autoInitializeProp;
    private SerializedProperty debugLogProp;
    private SerializedProperty managedSlotsProp;
    private SerializedProperty slotGroupsProp;
    
    // 折叠面板状态
    private Dictionary<int, bool> groupFoldouts = new Dictionary<int, bool>();
    
    private void OnEnable()
    {
        skeletonAnimationProp = serializedObject.FindProperty("skeletonAnimation");
        skeletonGraphicProp = serializedObject.FindProperty("skeletonGraphic");
        autoInitializeProp = serializedObject.FindProperty("autoInitialize");
        debugLogProp = serializedObject.FindProperty("debugLog");
        managedSlotsProp = serializedObject.FindProperty("managedSlots");
        slotGroupsProp = serializedObject.FindProperty("slotGroups");
    }
    
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        
        EditorGUILayout.LabelField("插槽管理器", EditorStyles.boldLabel);
        EditorGUILayout.Space();
        
        // 骨骼组件
        EditorGUILayout.PropertyField(skeletonAnimationProp);
        EditorGUILayout.PropertyField(skeletonGraphicProp);
        
        EditorGUILayout.Space();
        
        // 基础设置
        EditorGUILayout.PropertyField(autoInitializeProp);
        EditorGUILayout.PropertyField(debugLogProp);
        
        EditorGUILayout.Space();
        
        // 管理的插槽
        EditorGUILayout.LabelField("管理的插槽", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(managedSlotsProp);
        
        EditorGUILayout.Space();
        
        // 插槽分组 - 自定义绘制
        DrawSlotGroups();
        
        serializedObject.ApplyModifiedProperties();
    }
    
    private void DrawSlotGroups()
    {
        EditorGUILayout.LabelField("插槽分组", EditorStyles.boldLabel);
        
        if (slotGroupsProp == null || !slotGroupsProp.isArray)
            return;
        
        SlotManager slotManager = target as SlotManager;
        
        // 添加分组按钮
        if (GUILayout.Button("添加新分组", GUILayout.Width(100)))
        {
            slotGroupsProp.arraySize++;
            serializedObject.ApplyModifiedProperties();
            return;
        }
        
        EditorGUILayout.Space();
        
        // 绘制每个分组
        for (int i = 0; i < slotGroupsProp.arraySize; i++)
        {
            SerializedProperty groupProp = slotGroupsProp.GetArrayElementAtIndex(i);
            
            // 初始化折叠状态
            if (!groupFoldouts.ContainsKey(i))
                groupFoldouts[i] = true;
            
            // 分组标题
            SerializedProperty nameProp = groupProp.FindPropertyRelative("name");
            string groupName = string.IsNullOrEmpty(nameProp.stringValue) ? $"分组 {i + 1}" : nameProp.stringValue;
            
            groupFoldouts[i] = EditorGUILayout.Foldout(groupFoldouts[i], $"分组: {groupName}", true);
            
            if (groupFoldouts[i])
            {
                EditorGUI.indentLevel++;
                
                // 分组名称
                EditorGUILayout.PropertyField(nameProp);
                
                // 分组设置
                EditorGUILayout.PropertyField(groupProp.FindPropertyRelative("defaultAlpha"));
                EditorGUILayout.PropertyField(groupProp.FindPropertyRelative("fadeDuration"));
                EditorGUILayout.PropertyField(groupProp.FindPropertyRelative("visibleOnStart"));
                
                // 插槽列表 - 自定义绘制
                DrawSlotNamesList(groupProp);
                
                // 删除按钮
                EditorGUILayout.Space();
                if (GUILayout.Button("删除此分组", GUILayout.Width(100)))
                {
                    slotGroupsProp.DeleteArrayElementAtIndex(i);
                    groupFoldouts.Remove(i);
                    // 重新整理折叠状态
                    var newFoldouts = new Dictionary<int, bool>();
                    foreach (var kvp in groupFoldouts)
                    {
                        if (kvp.Key > i)
                            newFoldouts[kvp.Key - 1] = kvp.Value;
                        else
                            newFoldouts[kvp.Key] = kvp.Value;
                    }
                    groupFoldouts = newFoldouts;
                    serializedObject.ApplyModifiedProperties();
                    return;
                }
                
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.Space();
        }
    }
    
    private void DrawSlotNamesList(SerializedProperty groupProp)
    {
        SerializedProperty slotNamesProp = groupProp.FindPropertyRelative("slotNames");
        
        if (slotNamesProp == null || !slotNamesProp.isArray)
            return;
        
        // 使用Unity默认的数组绘制，SpineSlot属性会自动处理下拉选择
        EditorGUILayout.PropertyField(slotNamesProp, true); // true表示显示子元素
        
        // 添加简单的控制按钮
        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        
        if (GUILayout.Button("添加插槽", GUILayout.Width(80)))
        {
            slotNamesProp.arraySize++;
            serializedObject.ApplyModifiedProperties();
        }
        
        if (slotNamesProp.arraySize > 0 && GUILayout.Button("清空列表", GUILayout.Width(80)))
        {
            slotNamesProp.arraySize = 0;
            serializedObject.ApplyModifiedProperties();
        }
        
        EditorGUILayout.EndHorizontal();
    }
    

}