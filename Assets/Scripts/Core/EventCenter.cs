using System;
using System.Collections.Generic;

public static class EventCenter
{
    // 使用 Object 作为 Value，通过强转来支持不同参数类型的委托
    private static readonly Dictionary<string, Delegate> eventTable = new Dictionary<string, Delegate>();

    #region 监听与移除

    // 添加无参监听
    public static void AddListener(string eventName, Action handler)
    {
        OnListenerAdding(eventName, handler);
        eventTable[eventName] = (Action)eventTable[eventName] + handler;
    }

    // 添加一个参数监听
    public static void AddListener<T>(string eventName, Action<T> handler)
    {
        OnListenerAdding(eventName, handler);
        eventTable[eventName] = (Action<T>)eventTable[eventName] + handler;
    }

    // 移除无参监听
    public static void RemoveListener(string eventName, Action handler)
    {
        if (OnListenerRemoving(eventName, handler))
        {
            eventTable[eventName] = (Action)eventTable[eventName] - handler;
            OnListenerRemoved(eventName);
        }
    }

    // 移除一个参数监听
    public static void RemoveListener<T>(string eventName, Action<T> handler)
    {
        if (OnListenerRemoving(eventName, handler))
        {
            eventTable[eventName] = (Action<T>)eventTable[eventName] - handler;
            OnListenerRemoved(eventName);
        }
    }

    #endregion

    #region 广播/触发

    // 触发无参事件
    public static void Broadcast(string eventName)
    {
        if (eventTable.TryGetValue(eventName, out Delegate d))
        {
            if (d is Action callback) callback.Invoke();
            else throw new Exception($"广播事件 {eventName} 错误：委托类型不匹配。");
        }
    }

    // 触发带参事件
    public static void Broadcast<T>(string eventName, T arg)
    {
        if (eventTable.TryGetValue(eventName, out Delegate d))
        {
            if (d is Action<T> callback) callback.Invoke(arg);
            else throw new Exception($"广播事件 {eventName} 错误：委托类型不匹配。");
        }
    }

    #endregion

    #region 内部校验逻辑

    private static void OnListenerAdding(string eventName, Delegate listener)
    {
        if (!eventTable.ContainsKey(eventName)) eventTable.Add(eventName, null);
        
        Delegate d = eventTable[eventName];
        if (d != null && d.GetType() != listener.GetType())
        {
            throw new Exception($"尝试为事件 {eventName} 添加不同类型的委托。");
        }
    }

    private static bool OnListenerRemoving(string eventName, Delegate listener)
    {
        if (eventTable.ContainsKey(eventName))
        {
            Delegate d = eventTable[eventName];
            if (d == null) return false;
            if (d.GetType() != listener.GetType()) throw new Exception($"移除事件 {eventName} 错误：类型不匹配。");
            return true;
        }
        return false;
    }

    private static void OnListenerRemoved(string eventName)
    {
        if (eventTable[eventName] == null) eventTable.Remove(eventName);
    }

    #endregion
}