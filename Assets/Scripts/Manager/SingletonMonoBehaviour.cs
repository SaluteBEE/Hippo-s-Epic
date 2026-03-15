using UnityEngine;

public abstract class SingletonMonoBehaviour<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T _instance;

    public static T Instance
    {
        get
        {
            if (_instance != null) return _instance;

            // 优先从场景 / DontDestroyOnLoad 子树里找
            _instance = Object.FindObjectOfType<T>(true);
            if (_instance != null) return _instance;

            // 不存在则创建在 GameRoot 下
            var root = GameApp.Instance;
            var go = new GameObject(typeof(T).Name);
            go.transform.SetParent(root.transform, false);
            _instance = go.AddComponent<T>();
            return _instance;
        }
    }

    public static T getInstance() => Instance;

    protected virtual void Awake()
    {
        if (_instance == null)
        {
            _instance = this as T;
            // 注意：根物体负责 DontDestroyOnLoad，所以子物体无需再次调用
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }
}