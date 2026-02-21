using UnityEngine;
using UnityEditor;
using Spine.Unity;
using Spine;
using System.Reflection;
using System.IO;

public class CreateAllSpineAnimationReferences : EditorWindow
{
    [MenuItem("Tools/Animation/Create All Spine Animation References")]
    public static void ShowWindow()
    {
        GetWindow<CreateAllSpineAnimationReferences>("创建所有Spine动画引用");
    }
    
    private bool createForAll = true;
    private bool overwriteExisting = false;
    private bool createAnimationConfigs = false;
    
    private void OnGUI()
    {
        GUILayout.Label("Spine动画引用批量创建工具", EditorStyles.boldLabel);
        GUILayout.Space(10);
        
        GUILayout.Label("功能:", EditorStyles.boldLabel);
        GUILayout.Label("• 为所有Spine骨骼创建ReferenceAssets文件夹");
        GUILayout.Label("• 为每个动画创建AnimationReferenceAsset");
        GUILayout.Label("• 可选: 创建AnimationConfig文件");
        GUILayout.Space(20);
        
        createForAll = EditorGUILayout.Toggle("为所有Spine骨骼创建", createForAll);
        overwriteExisting = EditorGUILayout.Toggle("覆盖现有文件", overwriteExisting);
        createAnimationConfigs = EditorGUILayout.Toggle("创建AnimationConfig", createAnimationConfigs);
        
        GUILayout.Space(20);
        
        if (GUILayout.Button("开始创建", GUILayout.Height(40)))
        {
            if (createForAll)
            {
                CreateForAllSkeletonDataAssets();
            }
            else
            {
                CreateForSelectedSkeletonDataAssets();
            }
        }
        
        GUILayout.Space(20);
        

        
        GUILayout.Space(20);
        GUILayout.Label("说明:", EditorStyles.label);
        GUILayout.Label("标准Spine项目结构:", EditorStyles.miniBoldLabel);
        GUILayout.Label("Spine骨骼目录/", EditorStyles.miniLabel);
        GUILayout.Label("├── ReferenceAssets/ (动画引用)", EditorStyles.miniLabel);
        GUILayout.Label("├── SkeletonData.asset", EditorStyles.miniLabel);
        GUILayout.Label("├── .json .atlas .png", EditorStyles.miniLabel);
        GUILayout.Label("└── Materials/", EditorStyles.miniLabel);
    }
    
    private static void CreateForAllSkeletonDataAssets()
    {
        string[] guids = AssetDatabase.FindAssets("t:SkeletonDataAsset");
        
        if (guids.Length == 0)
        {
            Debug.LogWarning("未找到任何SkeletonDataAsset资产");
            return;
        }
        
        Debug.Log($"找到 {guids.Length} 个SkeletonDataAsset资产");
        
        int createdCount = 0;
        int totalAnimations = 0;
        
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            SkeletonDataAsset skeletonData = AssetDatabase.LoadAssetAtPath<SkeletonDataAsset>(path);
            
            if (skeletonData != null)
            {
                int animCount = CreateReferenceAssetsForSkeleton(skeletonData);
                if (animCount > 0)
                {
                    createdCount++;
                    totalAnimations += animCount;
                }
            }
        }
        
        AssetDatabase.Refresh();
        Debug.Log($"创建完成！为 {createdCount} 个Spine骨骼创建了 {totalAnimations} 个动画引用");
    }
    
    private static void CreateForSelectedSkeletonDataAssets()
    {
        Object[] selectedObjects = Selection.objects;
        
        if (selectedObjects == null || selectedObjects.Length == 0)
        {
            Debug.LogWarning("请先选择一个或多个SkeletonDataAsset资产");
            return;
        }
        
        int createdCount = 0;
        int totalAnimations = 0;
        
        foreach (Object obj in selectedObjects)
        {
            SkeletonDataAsset skeletonData = obj as SkeletonDataAsset;
            if (skeletonData != null)
            {
                int animCount = CreateReferenceAssetsForSkeleton(skeletonData);
                if (animCount > 0)
                {
                    createdCount++;
                    totalAnimations += animCount;
                }
            }
        }
        
        AssetDatabase.Refresh();
        Debug.Log($"创建完成！为 {createdCount} 个Spine骨骼创建了 {totalAnimations} 个动画引用");
    }
    

    
    private static int CreateReferenceAssetsForSkeleton(SkeletonDataAsset skeletonData)
    {
        string skeletonPath = AssetDatabase.GetAssetPath(skeletonData);
        string parentFolder = Path.GetDirectoryName(skeletonPath);
        string skeletonName = Path.GetFileNameWithoutExtension(skeletonPath);
        
        // 创建ReferenceAssets文件夹
        const string assetFolderName = "ReferenceAssets";
        string referenceAssetsPath = parentFolder + "/" + assetFolderName;
        
        if (!AssetDatabase.IsValidFolder(referenceAssetsPath))
        {
            AssetDatabase.CreateFolder(parentFolder, assetFolderName);
            Debug.Log($"创建文件夹: {referenceAssetsPath}");
        }
        
        // 获取动画数据
        var skeletonDataObject = skeletonData.GetSkeletonData(true);
        if (skeletonDataObject == null)
        {
            Debug.LogError($"无法获取SkeletonData对象: {skeletonData.name}");
            return 0;
        }
        
        int animationCount = 0;
        
        // 使用反射获取私有字段（与Spine标准方法一致）
        FieldInfo nameField = typeof(AnimationReferenceAsset).GetField("animationName", 
            BindingFlags.NonPublic | BindingFlags.Instance);
        FieldInfo skeletonDataAssetField = typeof(AnimationReferenceAsset).GetField("skeletonDataAsset", 
            BindingFlags.NonPublic | BindingFlags.Instance);
        
        if (nameField == null || skeletonDataAssetField == null)
        {
            Debug.LogError("无法访问AnimationReferenceAsset的私有字段");
            return 0;
        }
        
        // 为每个动画创建引用
        foreach (var animation in skeletonDataObject.Animations)
        {
            if (string.IsNullOrEmpty(animation.Name) || animation.Name == "animation")
            {
                Debug.Log($"跳过空动画: {animation.Name} (骨骼: {skeletonName})");
                continue;
            }
            
            // 创建安全的文件名
            string safeName = GetSafeFileName(animation.Name);
            string assetPath = $"{referenceAssetsPath}/{safeName}.asset";
            
            AnimationReferenceAsset existingAsset = AssetDatabase.LoadAssetAtPath<AnimationReferenceAsset>(assetPath);
            
            if (existingAsset == null)
            {
                // 创建新资产
                AnimationReferenceAsset newAsset = ScriptableObject.CreateInstance<AnimationReferenceAsset>();
                skeletonDataAssetField.SetValue(newAsset, skeletonData);
                nameField.SetValue(newAsset, animation.Name);
                AssetDatabase.CreateAsset(newAsset, assetPath);
                
                animationCount++;
                Debug.Log($"创建动画引用: {assetPath}");
            }
            else
            {
                // 更新现有资产
                skeletonDataAssetField.SetValue(existingAsset, skeletonData);
                nameField.SetValue(existingAsset, animation.Name);
                EditorUtility.SetDirty(existingAsset);
                
                animationCount++;
                Debug.Log($"更新动画引用: {assetPath}");
            }
        }
        
        // 创建AnimationConfig（如果需要）
        // 注意：这里需要根据项目需求调整
        if (animationCount > 0)
        {
            Debug.Log($"为 {skeletonName} 创建了 {animationCount} 个动画引用");
        }
        
        return animationCount;
    }
    
    private static string GetSafeFileName(string name)
    {
        // 简单的文件名安全处理
        string safeName = name.Replace(" ", "_");
        safeName = safeName.Replace("/", "_");
        safeName = safeName.Replace("\\", "_");
        safeName = safeName.Replace(":", "_");
        safeName = safeName.Replace("*", "_");
        safeName = safeName.Replace("?", "_");
        safeName = safeName.Replace("\"", "_");
        safeName = safeName.Replace("<", "_");
        safeName = safeName.Replace(">", "_");
        safeName = safeName.Replace("|", "_");
        return safeName;
    }
}