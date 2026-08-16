using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.AsyncOperations;

public static class IconLoader
{
    static readonly Dictionary<int, Sprite> _cache = new Dictionary<int, Sprite>();
    static readonly Dictionary<int, AsyncOperationHandle<Sprite>> _handles = new Dictionary<int, AsyncOperationHandle<Sprite>>();
    static readonly HashSet<int> _loading = new HashSet<int>();
    static readonly HashSet<int> _missing = new HashSet<int>();
    static Sprite _placeholder;
    static bool _initialized;

    static Sprite GetPlaceholder()
    {
        if (_placeholder != null) return _placeholder;
        var tex = new Texture2D(64, 64);
        var pixels = new Color32[64 * 64];
        for (int i = 0; i < pixels.Length; i++)
        {
            int x = i % 64;
            int y = i / 64;
            bool checker = (x / 8 + y / 8) % 2 == 0;
            pixels[i] = checker ? new Color32(180, 180, 180, 255) : new Color32(120, 120, 120, 255);
        }
        tex.SetPixels32(pixels);
        tex.Apply();
        tex.name = "[IconPlaceholder]";
        _placeholder = Sprite.Create(tex, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f), 64f);
        _placeholder.name = "[IconPlaceholder]";
        return _placeholder;
    }

    static bool KeyExists(object key)
    {
        foreach (IResourceLocator locator in Addressables.ResourceLocators)
        {
            if (locator.Locate(key, typeof(Object), out _))
                return true;
        }
        return false;
    }

    public static IEnumerator Init()
    {
        if (_initialized) yield break;

        yield return Addressables.InitializeAsync();

        var tables = ManagerRegistry.GetTables<cfg.Tables>();
        if (tables == null)
        {
            Debug.LogWarning("[IconLoader] Tables 未就绪，跳过预加载");
            _initialized = true;
            yield break;
        }

        var iconIds = new HashSet<int>();
        foreach (var item in tables.TbItem.DataList)
        {
            if (item.Icon > 0)
                iconIds.Add(item.Icon);
        }

        var toLoad = new List<int>();
        foreach (var id in iconIds)
        {
            if (_cache.ContainsKey(id)) continue;

            string address = $"icon/item/{id}";
            if (!KeyExists(address))
            {
                _missing.Add(id);
                _cache[id] = GetPlaceholder();
                continue;
            }

            toLoad.Add(id);
        }

        if (toLoad.Count > 0)
        {
            var loading = new List<AsyncOperationHandle<Sprite>>();
            foreach (var id in toLoad)
            {
                var handle = Addressables.LoadAssetAsync<Sprite>($"Assets/Art/Sprites/UI/Item/{id}");
                _handles[id] = handle;
                _loading.Add(id);
                loading.Add(handle);
            }

            foreach (var handle in loading)
                yield return handle;

            foreach (var id in toLoad)
            {
                _loading.Remove(id);
                if (!_handles.TryGetValue(id, out var h)) continue;
                if (h.Status == AsyncOperationStatus.Succeeded)
                {
                    _cache[id] = h.Result;
                }
                else
                {
                    _missing.Add(id);
                    _cache[id] = GetPlaceholder();
                    if (h.IsValid())
                        Addressables.Release(h);
                    _handles.Remove(id);
                }
            }
        }

        _initialized = true;
        Debug.Log($"[IconLoader] 初始化完成，缓存 {_cache.Count} 个图标（{_missing.Count} 个占位）");
    }

    public static Sprite LoadItemIcon(int iconId)
    {
        if (iconId <= 0) return null;
        if (_cache.TryGetValue(iconId, out Sprite sp))
            return sp;
        return GetPlaceholder();
    }

    public static void PreloadIcon(MonoBehaviour host, int iconId)
    {
        if (iconId <= 0 || _cache.ContainsKey(iconId) || _loading.Contains(iconId) || _missing.Contains(iconId)) return;
        string address = $"icon/item/{iconId}";
        if (!KeyExists(address))
        {
            _missing.Add(iconId);
            _cache[iconId] = GetPlaceholder();
            return;
        }
        _loading.Add(iconId);
        host.StartCoroutine(DoLoad(iconId));
    }

    static IEnumerator DoLoad(int id)
    {
        var handle = Addressables.LoadAssetAsync<Sprite>($"Assets/Art/Sprites/UI/Item/{id}");
        _handles[id] = handle;
        yield return handle;
        _loading.Remove(id);
        if (handle.Status == AsyncOperationStatus.Succeeded)
        {
            _cache[id] = handle.Result;
        }
        else
        {
            _missing.Add(id);
            _cache[id] = GetPlaceholder();
            if (handle.IsValid())
                Addressables.Release(handle);
            _handles.Remove(id);
        }
    }

    public static void ReleaseAll()
    {
        foreach (var kv in _handles)
        {
            if (kv.Value.IsValid())
                Addressables.Release(kv.Value);
        }
        _handles.Clear();
        _cache.Clear();
        _loading.Clear();
        _missing.Clear();
        if (_placeholder != null)
        {
            if (_placeholder.texture != null)
                Object.Destroy(_placeholder.texture);
            Object.Destroy(_placeholder);
            _placeholder = null;
        }
        _initialized = false;
    }
}
