using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;

public static class AddressableAutoSetup
{
    struct DirConfig
    {
        public string dir;
        public string addressPrefix;
        public string label;
        public string filter;
        public bool forceSprite;
    }

    static readonly DirConfig[] _configs = new[]
    {
        new DirConfig { dir = "Assets/Art/Backgrounds", addressPrefix = "backgrounds", label = "background", filter = "t:Texture2D", forceSprite = false },
        new DirConfig { dir = "Assets/Art/DialogImages", addressPrefix = "dialog_images", label = null, filter = "t:Texture2D", forceSprite = false },
        new DirConfig { dir = "Assets/Art/Sprites/UI/Item", addressPrefix = "icon/item", label = "icon", filter = "t:Texture2D", forceSprite = true },
        new DirConfig { dir = "Assets/Art/Avatars", addressPrefix = "avatars", label = "avatar", filter = "t:Texture2D", forceSprite = true },
    };

    [MenuItem("Tools/自动设置 Addressable")]
    public static void Setup()
    {
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        if (settings == null)
        {
            Debug.LogError("[AddressableAutoSetup] Addressable Settings 未找到，请先创建");
            return;
        }

        EnsureDirectories();
        EnsureDialogCharactersSchema(settings);

        var group = settings.DefaultGroup;
        int added = 0;

        foreach (var cfg in _configs)
        {
            if (!Directory.Exists(cfg.dir))
            {
                Debug.LogWarning($"[AddressableAutoSetup] 目录不存在，跳过: {cfg.dir}");
                continue;
            }
            added += SetupDirectory(settings, group, cfg.dir, cfg.addressPrefix, cfg.filter, cfg.label, cfg.forceSprite);
        }

        AssetDatabase.Refresh();
        settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, null, true);
        AssetDatabase.SaveAssets();

        Debug.Log($"[AddressableAutoSetup] 完成，新增/更新 {added} 个 Addressable 条目");
    }

    static void EnsureDirectories()
    {
        string[] dirs = { "Assets/Art/DialogImages", "Assets/Art/Avatars" };
        foreach (var dir in dirs)
        {
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
                Debug.Log($"[AddressableAutoSetup] 创建目录: {dir}");
            }
        }
    }

    static void EnsureDialogCharactersSchema(AddressableAssetSettings settings)
    {
        var group = settings.FindGroup("DialogCharacters");
        if (group == null)
        {
            Debug.LogWarning("[AddressableAutoSetup] 未找到 DialogCharacters 组");
            return;
        }

        bool hasBundled = false;
        foreach (var schema in group.Schemas)
        {
            if (schema is BundledAssetGroupSchema)
                hasBundled = true;
        }

        if (!hasBundled)
        {
            var bundledSchema = group.AddSchema<BundledAssetGroupSchema>();
            bundledSchema.BuildPath.SetVariableByName(settings, "Local.BuildPath");
            bundledSchema.LoadPath.SetVariableByName(settings, "Local.LoadPath");
            bundledSchema.BundleMode = BundledAssetGroupSchema.BundlePackingMode.PackTogether;
            bundledSchema.Compression = BundledAssetGroupSchema.BundleCompressionMode.LZ4;

            if (!group.HasSchema<ContentUpdateGroupSchema>())
                group.AddSchema<ContentUpdateGroupSchema>();

            settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, group, true);
            Debug.Log("[AddressableAutoSetup] 已为 DialogCharacters 组添加 BundledAssetGroupSchema + ContentUpdateGroupSchema");
        }
    }

    static int SetupDirectory(AddressableAssetSettings settings, AddressableAssetGroup group,
        string dir, string addressPrefix, string filter, string label, bool forceSprite)
    {
        int count = 0;
        var guids = AssetDatabase.FindAssets(filter, new[] { dir });

        if (label != null && !settings.GetLabels().Contains(label))
            settings.AddLabel(label);

        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var assetName = Path.GetFileNameWithoutExtension(path);
            var address = $"{addressPrefix}/{assetName}";

            if (forceSprite)
                EnsureSpriteImport(path);

            var entry = settings.CreateOrMoveEntry(guid, group);
            entry.address = address;

            if (label != null)
                entry.SetLabel(label, true);

            count++;
            Debug.Log($"  设置: {address} → {path}" + (label != null ? $" [label={label}]" : ""));
        }

        return count;
    }

    static void EnsureSpriteImport(string assetPath)
    {
        var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null) return;

        if (importer.textureType == TextureImporterType.Sprite)
            return;

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.SaveAndReimport();

        Debug.Log($"  修正导入: {assetPath} → Sprite");
    }
}
