using System;
using System.Collections.Generic;
using UnityEngine;

public enum UILayer
{
    Background = 0,
    Normal = 100,
    Popup = 200,
    Top = 300
}
public abstract class UIWindow : MonoBehaviour
{
    /// <summary>此窗口所属层级</summary>
    public virtual UILayer Layer => UILayer.Normal;

    /// <summary>打开时是否入栈（用于 Back 返回）</summary>
    public virtual bool PushToStack => true;

    /// <summary>关闭后是否保留缓存（不销毁 GameObject）</summary>
    public virtual bool CacheOnClose => false;

    /// <summary>首次创建后调用一次</summary>
    public virtual void OnCreate(object args) { }

    /// <summary>每次打开都会调用</summary>
    public virtual void OnOpen(object args) { }

    /// <summary>每次关闭都会调用</summary>
    public virtual void OnClose() { }

    /// <summary>真正销毁前调用一次</summary>
    public virtual void OnDestroyWindow() { }
}
public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("Root Canvas (UGUI)")]
    [SerializeField] private Canvas rootCanvas;

    [Header("Optional: Create layers automatically if missing")]
    [SerializeField] private bool autoCreateLayers = true;

    // 每个层对应一个父节点（RectTransform）
    private readonly Dictionary<UILayer, RectTransform> _layerRoots = new();

    // 已创建窗口：Type -> 实例
    private readonly Dictionary<Type, UIWindow> _windows = new();

    // 栈：用于 Back()
    private readonly Stack<Type> _stack = new();

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (rootCanvas == null)
        {
            rootCanvas = FindFirstObjectByType<Canvas>();
        }

        if (rootCanvas == null)
        {
            Debug.LogError("[UIManager] Root Canvas not found. Please assign a Canvas.");
            return;
        }

        BuildLayerRoots();
    }

    private void BuildLayerRoots()
    {
        foreach (UILayer layer in Enum.GetValues(typeof(UILayer)))
        {
            var name = $"Layer_{layer}";
            var existing = rootCanvas.transform.Find(name) as RectTransform;
            if (existing == null && autoCreateLayers)
            {
                var go = new GameObject(name, typeof(RectTransform));
                existing = go.GetComponent<RectTransform>();
                existing.SetParent(rootCanvas.transform, false);

                // 让 Layer 节点铺满 Canvas
                StretchFull(existing);
            }

            if (existing != null)
                _layerRoots[layer] = existing;
        }
    }

    /// <summary>
    /// 打开（或创建并打开）一个窗口。prefabPath 为 Resources 路径（不含扩展名）
    /// </summary>
    public T Open<T>(string prefabPath, object args = null) where T : UIWindow
    {
        var type = typeof(T);

        // 已存在实例：直接打开
        if (_windows.TryGetValue(type, out var existing))
        {
            existing.gameObject.SetActive(true);
            EnsureLayerAndSorting(existing);
            existing.OnOpen(args);

            if (existing.PushToStack) Push(type);
            return (T)existing;
        }

        // 从 Resources 加载
        var prefab = Resources.Load<GameObject>(prefabPath);
        if (prefab == null)
        {
            Debug.LogError($"[UIManager] Prefab not found at Resources/{prefabPath}");
            return null;
        }

        var go = Instantiate(prefab);
        var window = go.GetComponent<T>();
        if (window == null)
        {
            Debug.LogError($"[UIManager] Prefab has no component: {typeof(T).Name}");
            Destroy(go);
            return null;
        }

        EnsureLayerAndSorting(window);
        StretchFull(window.transform as RectTransform);

        _windows[type] = window;

        window.OnCreate(args);
        window.OnOpen(args);

        if (window.PushToStack) Push(type);
        return window;
    }

    /// <summary>关闭窗口（若 CacheOnClose=true 则隐藏，否则销毁）</summary>
    public void Close<T>() where T : UIWindow
    {
        Close(typeof(T));
    }

    public void Close(Type type)
    {
        if (!_windows.TryGetValue(type, out var window)) return;

        window.OnClose();

        if (window.CacheOnClose)
        {
            window.gameObject.SetActive(false);
        }
        else
        {
            window.OnDestroyWindow();
            Destroy(window.gameObject);
            _windows.Remove(type);
        }

        // 栈中可能存在多个同类型（比如重复打开同一界面），这里做一次清理即可
        RemoveOneFromStack(type);
    }

    /// <summary>切换：关闭其他 Normal 层窗口，仅打开指定窗口（常用于主界面切页）</summary>
    public T SwitchExclusiveNormal<T>(string prefabPath, object args = null) where T : UIWindow
    {
        // 关闭所有 Normal 层（且不在 Popup/Top 的窗口）
        var toClose = new List<Type>();
        foreach (var kv in _windows)
        {
            if (kv.Value != null && kv.Value.Layer == UILayer.Normal)
                toClose.Add(kv.Key);
        }
        foreach (var t in toClose) Close(t);

        return Open<T>(prefabPath, args);
    }

    /// <summary>返回上一个入栈的窗口（Back）</summary>
    public void Back()
    {
        // 弹出当前
        while (_stack.Count > 0)
        {
            var current = _stack.Pop();
            if (_windows.ContainsKey(current))
            {
                Close(current);
                break;
            }
        }

        // 找到下一个仍存在的
        while (_stack.Count > 0)
        {
            var next = _stack.Peek();
            if (_windows.TryGetValue(next, out var w) && w != null)
            {
                w.gameObject.SetActive(true);
                EnsureLayerAndSorting(w);
                w.OnOpen(null);
                break;
            }
            _stack.Pop();
        }
    }

    public bool IsOpen<T>() where T : UIWindow
    {
        if (_windows.TryGetValue(typeof(T), out var w) && w != null)
            return w.gameObject.activeSelf;
        return false;
    }

    private void Push(Type type)
    {
        // 避免连续重复压栈同一个类型（常见于重复点按钮）
        if (_stack.Count > 0 && _stack.Peek() == type) return;
        _stack.Push(type);
    }

    private void RemoveOneFromStack(Type type)
    {
        if (_stack.Count == 0) return;

        // Stack 没有直接删除中间元素的 API，用临时栈处理一次
        var tmp = new Stack<Type>();
        bool removed = false;

        while (_stack.Count > 0)
        {
            var t = _stack.Pop();
            if (!removed && t == type)
            {
                removed = true;
                continue;
            }
            tmp.Push(t);
        }
        while (tmp.Count > 0) _stack.Push(tmp.Pop());
    }

    private void EnsureLayerAndSorting(UIWindow window)
    {
        if (!_layerRoots.TryGetValue(window.Layer, out var parent) || parent == null)
        {
            Debug.LogWarning($"[UIManager] Layer root not found for {window.Layer}. Put it under rootCanvas.");
            window.transform.SetParent(rootCanvas.transform, false);
        }
        else
        {
            window.transform.SetParent(parent, false);
        }

        // 确保窗口 Canvas 排序正确（如果窗口 prefab 自带 Canvas）
        var c = window.GetComponentInChildren<Canvas>(true);
        if (c != null)
        {
            c.overrideSorting = true;
            c.sortingOrder = (int)window.Layer;
        }
    }

    private static void StretchFull(RectTransform rt)
    {
        if (rt == null) return;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.localScale = Vector3.one;
    }
}
