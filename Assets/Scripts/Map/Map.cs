using System;
using Map;
using UnityEngine;

public class MapBehaviour : MonoBehaviour
{
    private bool isMapEntered;
    public MapObject[] MapObjects;

    public event Action<MapTeleportContext> MapTeleported;

    public void Teleport(MapTeleportContext context)
    {
        if (MapTeleported != null)
        {
            MapTeleported.Invoke(context);
        }
    }

    public void Initialize()
    {
        // Initialize the map and its objects here

        // 遍历所有子物体，找到所有 MapObject 组件并添加到 MapObjects 数组中
        MapObjects = GetComponentsInChildren<MapObject>();
    }

    public void EnterMap()
    {
        if (!isMapEntered)
        {
            isMapEntered = true;

            OnMapEntered();
            return;
        }
        throw new Exception("Map has already been entered. Multiple entries are not allowed.");
    }

    public void ExitMap()
    {
        if (isMapEntered)
        {
            isMapEntered = false;
            // Perform any necessary cleanup or state reset here
            OnMapExited();
            return;
        }
        throw new Exception("Map has not been entered yet. Cannot exit.");
    }

    private void OnMapEntered()
    {
        foreach (var mapObject in MapObjects)
        {
            mapObject.OnMapEntered();
        }
    }

    private void OnMapExited()
    {
        foreach (var mapObject in MapObjects)
        {
            mapObject.OnMapExited();
        }
    }
}