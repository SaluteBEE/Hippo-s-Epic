using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public sealed class BackgroundView : MonoBehaviour
{
    [SerializeField] private Image target;
    [SerializeField] private string resourcesRoot = "Backgrounds";

    private readonly Dictionary<string, Sprite> _cache = new Dictionary<string, Sprite>();

    public void Apply(string normalizedName)
    {
        Debug.Log(normalizedName);
        if (target == null) return;
        if (string.IsNullOrWhiteSpace(normalizedName)) return;

        var key = normalizedName.Trim();
        if (!_cache.TryGetValue(key, out var sp) || sp == null)
        {
            sp = Resources.Load<Sprite>($"{resourcesRoot}/{key}");
            _cache[key] = sp;
        }

        if (sp == null)
        {
            Debug.LogWarning($"[BackgroundView] 未找到背景图：Resources/{resourcesRoot}/{key}");
            return;
        }

        target.sprite = sp;
        //target.SetNativeSize(); // 可选：如你不希望改变尺寸可删除这一行
    }
}