using System;
using System.Collections.Generic;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    [SerializeField] private Canvas rootCanvas;
    [SerializeField] private List<UIWindow> windows = new List<UIWindow>();

    private readonly Dictionary<Type, UIWindow> windowMap = new Dictionary<Type, UIWindow>();
    private readonly HashSet<Type> createdSet = new HashSet<Type>();

    private void Awake()
    {
        if (rootCanvas == null)
            rootCanvas = GetComponentInChildren<Canvas>(true);

        windowMap.Clear();
        createdSet.Clear();

        foreach (var window in windows)
        {
            if (window == null) continue;

            var type = window.GetType();
            if (windowMap.ContainsKey(type)) continue;

            windowMap.Add(type, window);

            PlaceWindow(window);
            window.gameObject.SetActive(false);
        }
    }

    public T Open<T>(object args = null) where T : UIWindow
    {
        if (!windowMap.TryGetValue(typeof(T), out var window))
        {
            Debug.LogError($"[UIManager] Window not found: {typeof(T).Name}");
            return null;
        }

        PlaceWindow(window);

        if (!createdSet.Contains(typeof(T)))
        {
            window.OnCreate(args);
            createdSet.Add(typeof(T));
        }

        window.gameObject.SetActive(true);
        window.OnOpen(args);
        return (T)window;
    }

    public void Close<T>() where T : UIWindow
    {
        if (!windowMap.TryGetValue(typeof(T), out var window))
            return;

        window.OnClose();
        window.gameObject.SetActive(false);
    }

    public bool IsOpen<T>() where T : UIWindow
    {
        return windowMap.TryGetValue(typeof(T), out var window) && window.gameObject.activeSelf;
    }

    public T Get<T>() where T : UIWindow
    {
        if (windowMap.TryGetValue(typeof(T), out var window))
            return (T)window;
        return null;
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