using UnityEngine;

public enum UILayer
{
    Normal = 0,
    Popup = 100,
    Top = 200
}

public abstract class UIWindow : MonoBehaviour
{
    public virtual UILayer Layer => UILayer.Normal;

    public virtual void OnCreate(object args) { }
    public virtual void OnOpen(object args) { }
    public virtual void OnClose() { }
}