using UnityEngine;
using UnityEditor;
using Spine.Unity;

public class CreateHippoAnimationReferences : EditorWindow
{
    [MenuItem("Tools/Animation/Create Hippo Animation References")]
    public static void ShowWindow()
    {
        GetWindow<CreateHippoAnimationReferences>("创建河马动画引用");
    }
    
    private void OnGUI()
    {
        GUILayout.Label("河马动画引用创建工具", EditorStyles.boldLabel);
        GUILayout.Space(10);
        
        if (GUILayout.Button("创建所有动画引用"))
        {
            CreateAllAnimationReferences();
        }
        
        GUILayout.Space(20);
        GUILayout.Label("动画列表:", EditorStyles.boldLabel);
        GUILayout.Label("1. body-idle");
        GUILayout.Label("2. body-walk");
        GUILayout.Label("3. leg-idle");
        GUILayout.Label("4. leg-walk");
        
        GUILayout.Space(20);
        if (GUILayout.Button("创建单个动画引用"))
        {
            CreateSingleAnimationReference();
        }
    }
    
    private static void CreateAllAnimationReferences()
    {
        string skeletonDataPath = "Assets/Resources/spine/河马_SkeletonData.asset";
        string outputFolder = "Assets/Configs/Animations/Hippo/";
        
        // 确保目录存在
        if (!AssetDatabase.IsValidFolder(outputFolder))
        {
            System.IO.Directory.CreateDirectory(outputFolder);
            AssetDatabase.Refresh();
        }
        
        // 加载骨骼数据
        SkeletonDataAsset skeletonData = AssetDatabase.LoadAssetAtPath<SkeletonDataAsset>(skeletonDataPath);
        if (skeletonData == null)
        {
            Debug.LogError($"无法加载骨骼数据: {skeletonDataPath}");
            return;
        }
        
        Debug.Log($"加载骨骼数据成功: {skeletonData.name}");
        
        // 动画名称列表（跳过空的"animation"）
        string[] animationNames = { "body-idle", "body-walk", "leg-idle", "leg-walk" };
        
        foreach (string animName in animationNames)
        {
            CreateAnimationReference(skeletonData, animName, outputFolder);
        }
        
        Debug.Log("动画引用资产创建完成！");
        AssetDatabase.Refresh();
    }
    
    private static void CreateSingleAnimationReference()
    {
        string skeletonDataPath = "Assets/Resources/spine/河马_SkeletonData.asset";
        string outputFolder = "Assets/Configs/Animations/Hippo/";
        
        // 确保目录存在
        if (!AssetDatabase.IsValidFolder(outputFolder))
        {
            System.IO.Directory.CreateDirectory(outputFolder);
            AssetDatabase.Refresh();
        }
        
        // 加载骨骼数据
        SkeletonDataAsset skeletonData = AssetDatabase.LoadAssetAtPath<SkeletonDataAsset>(skeletonDataPath);
        if (skeletonData == null)
        {
            Debug.LogError($"无法加载骨骼数据: {skeletonDataPath}");
            return;
        }
        
        // 创建每个动画的引用
        CreateAnimationReference(skeletonData, "body-idle", outputFolder);
        CreateAnimationReference(skeletonData, "body-walk", outputFolder);
        CreateAnimationReference(skeletonData, "leg-idle", outputFolder);
        CreateAnimationReference(skeletonData, "leg-walk", outputFolder);
        
        Debug.Log("单个动画引用创建完成！");
        AssetDatabase.Refresh();
    }
    
    private static void CreateAnimationReference(SkeletonDataAsset skeletonData, string animationName, string outputFolder)
    {
        // 创建AnimationReferenceAsset
        AnimationReferenceAsset animRef = ScriptableObject.CreateInstance<AnimationReferenceAsset>();
        
        // 设置属性
        SerializedObject serializedObject = new SerializedObject(animRef);
        serializedObject.FindProperty("skeletonDataAsset").objectReferenceValue = skeletonData;
        serializedObject.FindProperty("animationName").stringValue = animationName;
        serializedObject.ApplyModifiedProperties();
        
        // 保存资产
        string assetPath = $"{outputFolder}{animationName}.asset";
        AssetDatabase.CreateAsset(animRef, assetPath);
        
        Debug.Log($"创建动画引用: {assetPath}");
    }
}