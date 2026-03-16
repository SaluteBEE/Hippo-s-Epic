using System.Collections.Generic;
using UnityEngine;

public sealed class Map : MonoBehaviour
{
    /// <summary>
    /// 默认入口（局部坐标）
    /// </summary>
    public Vector2 MainEntrance;

    /// <summary>
    /// 摄像机 X 轴位移限制（局部坐标）
    /// </summary>
    public Vector2 CameraClampX;

    /// <summary>
    /// 摄像机 Y 轴位移限制（局部坐标）
    /// </summary>
    public Vector2 CameraClampY;

    public Vector2 CameraClampXWorld =>
        new Vector2(
            CameraClampX.x + transform.position.x,
            CameraClampX.y + transform.position.x
        );

    public Vector2 CameraClampYWorld =>
        new Vector2(
            CameraClampY.x + transform.position.y,
            CameraClampY.y + transform.position.y
        );

    [Header("Visual Root")]
    [SerializeField] public GameObject VisualRoot;

    private ParallaxLayer[] parallaxLayers;

    // 当前地图建立视差时记录的相机基准点
    private Vector2 cameraBasePosition;

    private void Reset()
    {
        if (VisualRoot == null)
        {
            Transform visualTransform = transform.Find("Visual");
            if (visualTransform != null)
            {
                VisualRoot = visualTransform.gameObject;
            }
            else
            {
                VisualRoot = new GameObject("Visual");
                VisualRoot.transform.SetParent(transform, false);
            }
        }
    }

    /// <summary>
    /// 初始化地图，并记录视差的相机基准点
    /// </summary>
    public void Initialize(Vector2 cameraStartPosition)
    {
        cameraBasePosition = cameraStartPosition;

        if (VisualRoot == null)
        {
            Debug.LogError($"[Map] VisualRoot is null on map: {name}");
            parallaxLayers = System.Array.Empty<ParallaxLayer>();
            return;
        }

        int childCount = VisualRoot.transform.childCount;
        var layerList = new List<ParallaxLayer>();

        for (int i = 0; i < childCount; i++)
        {
            Transform child = VisualRoot.transform.GetChild(i);

            // 初始化该层下所有 MapObject
            MapObject[] mapObjects = child.GetComponentsInChildren<MapObject>();
            for (int j = mapObjects.Length - 1; j >= 0; j--)
            {
                mapObjects[j].Initialize();
            }

            // Main 层不参与视差
            if (child.name != "Main")
            {
                float factor = child.localPosition.z;
                layerList.Add(new ParallaxLayer(child, factor));
            }
        }

        parallaxLayers = layerList.ToArray();
    }

    /// <summary>
    /// 相机移动时调用。传入的是相机世界坐标。
    /// </summary>
    public void OnFocusMoved(Vector2 cameraWorldPosition)
    {
        if (parallaxLayers == null || parallaxLayers.Length == 0)
            return;

        Vector2 cameraDelta = cameraWorldPosition - cameraBasePosition;

        foreach (var item in parallaxLayers)
        {
            item?.OnCameraOffsetChanged(cameraDelta);
        }
    }

    /// <summary>
    /// 如果切图后玩家/相机重新定位，需要刷新视差基准点
    /// </summary>
    public void ResetParallaxBase(Vector2 cameraWorldPosition)
    {
        cameraBasePosition = cameraWorldPosition;
    }

    public void Dispose()
    {
        Destroy(gameObject);
    }
}