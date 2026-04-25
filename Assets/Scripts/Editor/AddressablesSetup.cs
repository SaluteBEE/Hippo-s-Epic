using UnityEditor;
using UnityEngine;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;

public static class AddressablesSetup
{
    [MenuItem("Tools/Setup Addressables")]
    public static void Setup()
    {
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        if (settings == null)
        {
            settings = AddressableAssetSettings.Create(
                "Assets/AddressableAssetsData",
                "AddressableAssetSettings",
                true,
                true);
            AddressableAssetSettingsDefaultObject.Settings = settings;
            Debug.Log("[AddressablesSetup] 创建 Addressables 设置");
        }

        var group = settings.FindGroup("DialogCharacters");
        if (group == null)
        {
            group = settings.CreateGroup("DialogCharacters", false, false, true, null);
            Debug.Log("[AddressablesSetup] 创建 DialogCharacters Group");
        }

        string[] prefabPaths = new[]
        {
            "Assets/Prefabs/player/homo_talk.prefab",
            "Assets/Prefabs/player/Hippo.prefab",
            "Assets/Prefabs/npc/coach.prefab",
            "Assets/Prefabs/npc/quanwang_bk.prefab",
            "Assets/Prefabs/npc/npc_01.prefab",
            "Assets/Prefabs/npc/npc_02.prefab",
        };

        string[] addressKeys = new[]
        {
            "prefabs/player/homo_talk",
            "prefabs/player/Hippo",
            "prefabs/npc/coach",
            "prefabs/npc/quanwang_bk",
            "prefabs/npc/npc_01",
            "prefabs/npc/npc_02",
        };

        for (int i = 0; i < prefabPaths.Length; i++)
        {
            var guid = AssetDatabase.AssetPathToGUID(prefabPaths[i]);
            if (string.IsNullOrEmpty(guid))
            {
                Debug.LogWarning($"[AddressablesSetup] 未找到资源: {prefabPaths[i]}");
                continue;
            }

            var entry = settings.CreateOrMoveEntry(guid, group, false, false);
            entry.address = addressKeys[i];
            entry.labels.Add("dialog_character");
            Debug.Log($"[AddressablesSetup] 标记 Addressable: {prefabPaths[i]} → {addressKeys[i]}");
        }

        settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, null, true);
        AssetDatabase.SaveAssets();
        Debug.Log("[AddressablesSetup] 完成");
    }
}
