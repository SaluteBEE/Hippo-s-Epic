using UnityEngine;
using UnityEditor;
using Spine;
using Spine.Unity;
using System.IO;

public class SpineAssetFixer : EditorWindow
{
    [MenuItem("Tools/Spine/Rebuild SkeletonDataAsset")]
    public static void RebuildSkeletonDataAsset()
    {
        string jsonPath = "Assets/Resources/spine/河马.json";
        string atlasPath = "Assets/Resources/spine/河马_Atlas.asset";
        string outputPath = "Assets/Resources/spine/河马_SkeletonData.asset";

        TextAsset jsonFile = AssetDatabase.LoadAssetAtPath<TextAsset>(jsonPath);
        SpineAtlasAsset atlasAsset = AssetDatabase.LoadAssetAtPath<SpineAtlasAsset>(atlasPath);

        if (jsonFile == null)
        {
            Debug.LogError("JSON file not found: " + jsonPath);
            return;
        }

        if (atlasAsset == null)
        {
            Debug.LogError("Atlas asset not found: " + atlasPath);
            return;
        }

        Debug.Log("JSON file size: " + jsonFile.text.Length + " chars");
        Debug.Log("JSON file first 100 chars: " + jsonFile.text.Substring(0, Mathf.Min(100, jsonFile.text.Length)));

        Atlas atlas = atlasAsset.GetAtlas();
        if (atlas == null)
        {
            Debug.LogError("Failed to get Atlas from AtlasAsset!");
            return;
        }
        Debug.Log("Atlas loaded successfully");

        AttachmentLoader attachmentLoader = new AtlasAttachmentLoader(new Atlas[] { atlas });
        
        try
        {
            var input = new StringReader(jsonFile.text);
            var skeletonJson = new SkeletonJson(attachmentLoader);
            skeletonJson.Scale = 0.01f;
            SkeletonData skeletonData = skeletonJson.ReadSkeletonData(input);
            
            if (skeletonData != null)
            {
                Debug.Log("SkeletonData parsed directly! Bones: " + skeletonData.Bones.Count + ", Animations: " + skeletonData.Animations.Count);
            }
            else
            {
                Debug.LogError("SkeletonData parsing returned null!");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError("Direct parsing exception: " + ex.GetType().Name + " - " + ex.Message + "\n" + ex.StackTrace);
        }

        SkeletonDataAsset skeletonDataAsset = ScriptableObject.CreateInstance<SkeletonDataAsset>();
        skeletonDataAsset.atlasAssets = new AtlasAssetBase[] { atlasAsset };
        skeletonDataAsset.skeletonJSON = jsonFile;
        skeletonDataAsset.scale = 0.01f;

        AssetDatabase.CreateAsset(skeletonDataAsset, outputPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("SkeletonDataAsset created at: " + outputPath);
    }
}
