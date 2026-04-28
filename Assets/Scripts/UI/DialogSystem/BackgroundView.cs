using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;

public sealed class BackgroundView : MonoBehaviour
{
    [SerializeField] private Image target;
    [SerializeField] private string addressableRoot = "backgrounds";

    private readonly Dictionary<string, Sprite> _cache = new Dictionary<string, Sprite>();
    private readonly Dictionary<string, AsyncOperationHandle<Sprite>> _handles = new Dictionary<string, AsyncOperationHandle<Sprite>>();

    public async void Apply(string normalizedName)
    {
        if (target == null) return;
        if (string.IsNullOrWhiteSpace(normalizedName)) return;

        var key = normalizedName.Trim();
        if (_cache.TryGetValue(key, out var sp) && sp != null)
        {
            target.sprite = sp;
            return;
        }

        var address = $"{addressableRoot}/{key}";
        if (_handles.TryGetValue(key, out var existing))
        {
            if (!existing.IsDone) return;
            sp = existing.Result;
        }
        else
        {
            var handle = Addressables.LoadAssetAsync<Sprite>(address);
            _handles[key] = handle;
            await handle.Task;
            sp = handle.Status == AsyncOperationStatus.Succeeded ? handle.Result : null;
        }

        if (sp == null)
        {
            Debug.LogWarning($"[BackgroundView] 未找到背景图：{address}");
            return;
        }

        _cache[key] = sp;
        target.sprite = sp;
    }

    private void OnDestroy()
    {
        foreach (var kvp in _handles)
        {
            if (kvp.Value.IsValid())
                Addressables.Release(kvp.Value);
        }
        _handles.Clear();
        _cache.Clear();
    }
}
