using System.Collections.Generic;
using UnityEngine;

public sealed class Map : MonoBehaviour
{
    public Vector2 MainEntrance;
    public Vector2 CameraClampX;
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

    public Vector2 MapCenterWorld =>
        new Vector2(
            (CameraClampXWorld.x + CameraClampXWorld.y) * 0.5f,
            (CameraClampYWorld.x + CameraClampYWorld.y) * 0.5f
        );

    [Header("Visual Root")]
    [SerializeField] public GameObject VisualRoot;

    private ParallaxLayer[] parallaxLayers;

    // 缓存每个视差层的原始局部位置
    private readonly Dictionary<Transform, Vector3> originalLayerLocalPositions = new Dictionary<Transform, Vector3>();

    // 防止重复采集“原始位置”
    private bool originalPositionsCached;

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

    public void Initialize()
    {
        if (VisualRoot == null)
        {
            Debug.LogError($"[Map] VisualRoot is null on map: {name}");
            parallaxLayers = System.Array.Empty<ParallaxLayer>();
            return;
        }

        CacheOriginalLayerPositionsIfNeeded();
        RestoreLayerPositions();

        int childCount = VisualRoot.transform.childCount;
        var layerList = new List<ParallaxLayer>();

        for (int i = 0; i < childCount; i++)
        {
            Transform child = VisualRoot.transform.GetChild(i);

            MapObject[] mapObjects = child.GetComponentsInChildren<MapObject>();
            for (int j = mapObjects.Length - 1; j >= 0; j--)
            {
                mapObjects[j].Initialize();
            }

            if (child.name != "Main")
            {
                float factor = child.localPosition.z;
                layerList.Add(new ParallaxLayer(child, factor));
            }
        }

        parallaxLayers = layerList.ToArray();
    }

    private void CacheOriginalLayerPositionsIfNeeded()
    {
        if (originalPositionsCached)
            return;

        originalLayerLocalPositions.Clear();

        int childCount = VisualRoot.transform.childCount;
        for (int i = 0; i < childCount; i++)
        {
            Transform child = VisualRoot.transform.GetChild(i);
            originalLayerLocalPositions[child] = child.localPosition;
        }

        originalPositionsCached = true;
    }

    private void RestoreLayerPositions()
    {
        foreach (var pair in originalLayerLocalPositions)
        {
            if (pair.Key != null)
            {
                pair.Key.localPosition = pair.Value;
            }
        }
    }

    public void OnFocusMoved(Vector2 cameraWorldPosition)
    {
        if (parallaxLayers == null || parallaxLayers.Length == 0)
            return;

        Vector2 cameraDelta = cameraWorldPosition - MapCenterWorld;

        foreach (var item in parallaxLayers)
        {
            item?.OnCameraOffsetChanged(cameraDelta);
        }
    }

    public void Dispose()
    {
        Destroy(gameObject);
    }
}