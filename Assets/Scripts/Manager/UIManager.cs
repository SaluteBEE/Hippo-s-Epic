using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class UIManager : MonoBehaviour
{
    [SerializeField] private Canvas rootCanvas;

    private static readonly Dictionary<string, string> addressMap = new Dictionary<string, string>();

    private readonly Dictionary<string, UIWindow> windowMap = new Dictionary<string, UIWindow>();
    private readonly HashSet<string> createdSet = new HashSet<string>();
    private readonly Dictionary<string, AsyncOperationHandle<GameObject>> loadHandles = new Dictionary<string, AsyncOperationHandle<GameObject>>();
    private readonly HashSet<string> pendingClose = new HashSet<string>();

    public static void RegisterAddress(Type windowType, string address)
    {
        addressMap[windowType.FullName] = address;
    }

    private void Awake()
    {
        ManagerRegistry.Register(this);

        if (rootCanvas == null)
            rootCanvas = GetComponentInChildren<Canvas>(true);
    }

    public T Open<T>(object args = null, Action<T> onReady = null) where T : UIWindow
    {
        string key = typeof(T).FullName;
        pendingClose.Remove(key);

        if (windowMap.TryGetValue(key, out var window))
        {
            PlaceWindow(window);
            window.gameObject.SetActive(true);

            if (!createdSet.Contains(key))
            {
                window.OnCreate(args);
                createdSet.Add(key);
            }

            window.OnOpen(args);
            onReady?.Invoke((T)window);
            return (T)window;
        }

        StartCoroutine(LoadAndOpenCoroutine<T>(args, onReady));
        return null;
    }

    private IEnumerator LoadAndOpenCoroutine<T>(object args, Action<T> onReady) where T : UIWindow
    {
        string key = typeof(T).FullName;

        if (!addressMap.TryGetValue(key, out var address))
        {
            Debug.LogError($"[UIManager] 未注册 Addressable 地址: {typeof(T).Name}");
            yield break;
        }

        if (loadHandles.ContainsKey(key))
        {
            yield return loadHandles[key];
        }
        else
        {
            var handle = Addressables.LoadAssetAsync<GameObject>(address);
            loadHandles[key] = handle;
            yield return handle;
        }

        if (loadHandles[key].Status != AsyncOperationStatus.Succeeded)
        {
            Debug.LogError($"[UIManager] 加载失败: {address}");
            loadHandles.Remove(key);
            yield break;
        }

        var prefab = loadHandles[key].Result;
        var instance = Instantiate(prefab, rootCanvas.transform, false);
        var window = instance.GetComponent<T>();

        if (window == null)
        {
            Debug.LogError($"[UIManager] 预制体上找不到 {typeof(T).Name} 组件: {address}");
            Destroy(instance);
            yield break;
        }

        windowMap[key] = window;
        PlaceWindow(window);

        if (pendingClose.Contains(key))
        {
            pendingClose.Remove(key);
            window.gameObject.SetActive(false);
            yield break;
        }

        window.gameObject.SetActive(true);

        if (!createdSet.Contains(key))
        {
            window.OnCreate(args);
            createdSet.Add(key);
        }

        window.OnOpen(args);
        onReady?.Invoke(window);
    }

    public void Close<T>() where T : UIWindow
    {
        string key = typeof(T).FullName;

        if (!windowMap.TryGetValue(key, out var window))
        {
            pendingClose.Add(key);
            return;
        }

        window.OnClose();
        window.gameObject.SetActive(false);
    }

    public bool IsOpen<T>() where T : UIWindow
    {
        string key = typeof(T).FullName;
        return windowMap.TryGetValue(key, out var window) && window.gameObject.activeSelf;
    }

    public bool HasPopupOpen()
    {
        foreach (var kvp in windowMap)
        {
            var w = kvp.Value;
            if (w != null && w.gameObject.activeSelf && (int)w.Layer >= (int)UILayer.Popup)
                return true;
        }
        return false;
    }

    public void CloseTopmost()
    {
        UIWindow topmost = null;
        int topLayer = -1;

        foreach (var kvp in windowMap)
        {
            var w = kvp.Value;
            if (w != null && w.gameObject.activeSelf && (int)w.Layer > topLayer)
            {
                topmost = w;
                topLayer = (int)w.Layer;
            }
        }

        if (topmost != null)
        {
            topmost.OnClose();
            topmost.gameObject.SetActive(false);
        }
    }

    public T Get<T>() where T : UIWindow
    {
        string key = typeof(T).FullName;

        if (windowMap.TryGetValue(key, out var window))
            return (T)window;
        return null;
    }

    private void OnDestroy()
    {
        foreach (var kvp in windowMap)
        {
            if (kvp.Value != null)
            {
                kvp.Value.OnClose();
                Destroy(kvp.Value.gameObject);
            }
        }
        windowMap.Clear();
        createdSet.Clear();

        foreach (var kvp in loadHandles)
        {
            if (kvp.Value.IsValid())
                Addressables.Release(kvp.Value);
        }
        loadHandles.Clear();
        pendingClose.Clear();
        ManagerRegistry.Unregister<UIManager>();
    }

    private void PlaceWindow(UIWindow window)
    {
        if (rootCanvas == null) return;

        window.transform.SetParent(rootCanvas.transform, false);

        var canvas = window.GetComponent<Canvas>();
        if (canvas != null)
        {
            canvas.overrideSorting = true;
            canvas.sortingOrder = (int)window.Layer;
        }

        var rect = window.transform as RectTransform;
        if (rect != null)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;
        }
    }
}
