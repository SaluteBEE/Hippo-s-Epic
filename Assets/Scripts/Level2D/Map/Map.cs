using System.Collections.Generic;
using UnityEngine;

public sealed class Map : MonoBehaviour
{
    [Header("Spawn Point")]
    [Tooltip("游戏初始出生点")]
    [SerializeField] private Transform spawnPoint;

    [Header("Teleport")]
    [Tooltip("传送到达点，从其他地图传送到此地图时的出生位置。为空时使用 SpawnPoint")]
    [SerializeField] private Transform teleportArrival;

    [Header("Camera Bounds")]
    [Tooltip("用碰撞体定义摄像机可显示的地图范围，Scene 视图中可直接拖拽编辑")]
    [SerializeField] private Collider2D cameraBounds;

    [Header("Interactables")]
    [Tooltip("此地图下的所有可交互节点，编辑器自动收集")]
    [SerializeField] private List<Interactable> interactables = new List<Interactable>();

    public IReadOnlyList<Interactable> InteractableList => interactables;

    public Vector3 SpawnPointWorld
    {
        get
        {
            if (spawnPoint != null)
            {
                Vector3 pos = spawnPoint.position;
                return new Vector3(pos.x, pos.y, pos.y);
            }
            return transform.position;
        }
    }

    public Vector3 TeleportArrivalWorld
    {
        get
        {
            Transform point = teleportArrival != null ? teleportArrival : spawnPoint;
            if (point != null)
            {
                Vector3 pos = point.position;
                return new Vector3(pos.x, pos.y, pos.y);
            }
            return transform.position;
        }
    }

    public Vector2 CameraClampXWorld
    {
        get
        {
            if (cameraBounds != null)
            {
                Camera cam = Camera.main;
                if (cam != null && cam.orthographic)
                {
                    float halfW = cam.orthographicSize * cam.aspect;
                    Bounds b = cameraBounds.bounds;
                    float min = b.min.x + halfW;
                    float max = b.max.x - halfW;
                    if (min > max) return new Vector2((min + max) * 0.5f, (min + max) * 0.5f);
                    return new Vector2(min, max);
                }
            }
            return new Vector2(transform.position.x, transform.position.x);
        }
    }

    public Vector2 CameraClampYWorld
    {
        get
        {
            if (cameraBounds != null)
            {
                Camera cam = Camera.main;
                if (cam != null && cam.orthographic)
                {
                    float halfH = cam.orthographicSize;
                    Bounds b = cameraBounds.bounds;
                    float min = b.min.y + halfH;
                    float max = b.max.y - halfH;
                    if (min > max) return new Vector2((min + max) * 0.5f, (min + max) * 0.5f);
                    return new Vector2(min, max);
                }
            }
            return new Vector2(transform.position.y, transform.position.y);
        }
    }

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

    public void SaveInteractableStates()
    {
        foreach (var interactable in interactables)
        {
            if (interactable == null) continue;
            SaveManager.Instance.SetEntityState(interactable.EntityId, interactable.CurrentState);
        }
    }

#if UNITY_EDITOR
    public void CollectInteractables()
    {
        interactables.Clear();
        var found = GetComponentsInChildren<Interactable>(true);
        if (found == null || found.Length == 0)
            return;

        var sorted = new List<Interactable>(found);
        sorted.Sort((a, b) =>
        {
            string pathA = GetHierarchyPath(a.transform, transform);
            string pathB = GetHierarchyPath(b.transform, transform);
            return string.Compare(pathA, pathB, System.StringComparison.Ordinal);
        });
        interactables.AddRange(sorted);
        UnityEditor.EditorUtility.SetDirty(this);
    }

    private static string GetHierarchyPath(Transform t, Transform root)
    {
        var parts = new List<string>();
        Transform current = t;
        while (current != null && current != root)
        {
            parts.Add(current.name);
            current = current.parent;
        }
        parts.Reverse();
        return string.Join("/", parts);
    }
#endif

    public void Dispose()
    {
        Destroy(gameObject);
    }
}