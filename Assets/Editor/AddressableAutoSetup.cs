using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

public static class AddressableAutoSetup
{
    [MenuItem("Tools/自动设置 Addressable")]
    public static void Setup()
    {
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        if (settings == null)
        {
            Debug.LogError("[AddressableAutoSetup] Addressable Settings 未找到，请先创建");
            return;
        }

        var group = settings.DefaultGroup;

        int added = 0;

        added += SetupDirectory(settings, group, "Assets/Backgrounds", "backgrounds");
        added += SetupDirectory(settings, group, "Assets/DialogImages", "dialog_images");

        settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, null, true);
        AssetDatabase.SaveAssets();

        Debug.Log($"[AddressableAutoSetup] 完成，新增/更新 {added} 个 Addressable 条目");
    }

    private static int SetupDirectory(AddressableAssetSettings settings, AddressableAssetGroup group, string dir, string addressPrefix)
    {
        int count = 0;
        var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { dir });

        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var assetName = System.IO.Path.GetFileNameWithoutExtension(path);
            var address = $"{addressPrefix}/{assetName}";

            var entry = settings.CreateOrMoveEntry(guid, group);
            entry.address = address;
            count++;
            Debug.Log($"  设置: {address} → {path}");
        }

        return count;
    }
}