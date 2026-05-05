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
    private readonly Dictionary<string, AsyncOperationHandle<Texture2D>> _handles = new Dictionary<string, AsyncOperationHandle<Texture2D>>();

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
            sp = CreateSprite(existing.Result, key);
        }
        else
        {
            var handle = Addressables.LoadAssetAsync<Texture2D>(address);
            _handles[key] = handle;
            await handle.Task;
            if (handle.Status != AsyncOperationStatus.Succeeded || handle.Result == null)
            {
                Debug.LogWarning($"[BackgroundView] 未找到背景图：{address}");
                return;
            }
            sp = CreateSprite(handle.Result, key);
        }

        if (sp == null) return;

        _cache[key] = sp;
        target.sprite = sp;
    }

    private Sprite CreateSprite(Texture2D tex, string key)
    {
        if (tex == null) return null;
        return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f, 0u, SpriteMeshType.FullRect);
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
