using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public static class IconLoader
{
    private static readonly Dictionary<int, Sprite> _cache = new Dictionary<int, Sprite>();
    private static readonly Dictionary<int, AsyncOperationHandle<Sprite>> _handles = new Dictionary<int, AsyncOperationHandle<Sprite>>();
    private static bool _initialized;

    public static IEnumerator Init()
    {
        if (_initialized) yield break;

        int[] iconIds = { 6, 7, 8, 9, 10, 11 };
        List<AsyncOperationHandle<Sprite>> loading = new List<AsyncOperationHandle<Sprite>>();

        foreach (int id in iconIds)
        {
            if (_cache.ContainsKey(id)) continue;
            var handle = Addressables.LoadAssetAsync<Sprite>($"icon/item/{id}");
            _handles[id] = handle;
            loading.Add(handle);
        }

        foreach (var handle in loading)
            yield return handle;

        foreach (var id in iconIds)
        {
            if (!_handles.TryGetValue(id, out var h)) continue;
            if (h.Status == AsyncOperationStatus.Succeeded)
                _cache[id] = h.Result;
            else
                Debug.LogWarning($"[IconLoader] 加载图标 {id} 失败");
        }

        _initialized = true;
        Debug.Log($"[IconLoader] 初始化完成，缓存 {_cache.Count} 个图标");
    }

    public static Sprite LoadItemIcon(int iconId)
    {
        if (iconId <= 0) return null;
        _cache.TryGetValue(iconId, out Sprite sp);
        return sp;
    }
}
