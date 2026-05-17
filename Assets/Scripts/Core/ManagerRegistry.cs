using System;
using System.Collections.Generic;
using UnityEngine;

public static class ManagerRegistry
{
    private static readonly Dictionary<Type, object> _managers = new Dictionary<Type, object>();

    public static void Register<T>(T manager) where T : class
    {
        if (manager == null)
        {
            Debug.LogWarning($"[ManagerRegistry] 注册空引用: {typeof(T).Name}");
            return;
        }
        _managers[typeof(T)] = manager;
    }

    public static void Unregister<T>() where T : class
    {
        _managers.Remove(typeof(T));
    }

    public static T Get<T>() where T : class
    {
        _managers.TryGetValue(typeof(T), out var mgr);
        return mgr as T;
    }

    public static bool TryGet<T>(out T manager) where T : class
    {
        if (_managers.TryGetValue(typeof(T), out var obj))
        {
            manager = obj as T;
            return manager != null;
        }
        manager = null;
        return false;
    }

    private static object _tables;

    public static void SetTables(object tables)
    {
        _tables = tables;
    }

    public static T GetTables<T>() where T : class
    {
        return _tables as T;
    }

    public static void Clear()
    {
        _managers.Clear();
        _tables = null;
    }

    public static T GetOrCreate<T>() where T : MonoBehaviour
    {
        var mgr = Get<T>();
        if (mgr != null) return mgr;

        var go = new GameObject($"[{typeof(T).Name}]");
        UnityEngine.Object.DontDestroyOnLoad(go);
        mgr = go.AddComponent<T>();
        _managers[typeof(T)] = mgr;
        Debug.Log($"[ManagerRegistry] 自动创建: {typeof(T).Name}");
        return mgr;
    }
}
