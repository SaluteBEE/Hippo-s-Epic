using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;

public enum MapRootId
{
    PunkCity,
}

public class MapPanel : UIWindow
{
    private const string AddressPrefix = "ui/map/MapRoot_";

    [Header("UI")]
    [SerializeField] private RectTransform mapRootContainer;
    [SerializeField] private RectTransform head;
    [SerializeField] private Button btnClose;

    private MapRoot _currentMapRoot;
    private GameObject _currentMapRootInstance;
    private AsyncOperationHandle<GameObject> _loadHandle;

    public override UILayer Layer => UILayer.Popup;

    public override void OnCreate(object args)
    {
        if (btnClose != null)
            btnClose.onClick.AddListener(OnCloseClicked);
    }

    public override void OnOpen(object args)
    {
        MapRootId id = MapRootId.PunkCity;
        if (args is MapRootId rootId)
            id = rootId;

        LoadMapRoot(id);
    }

    public override void OnClose()
    {
        ClearMapRoot();
    }

    private void OnCloseClicked()
    {
        ManagerRegistry.Get<UIManager>()?.Close<MapPanel>();
    }

    private string GetAddress(MapRootId id)
    {
        return AddressPrefix + id;
    }

    private void LoadMapRoot(MapRootId id)
    {
        ClearMapRoot();
        StartCoroutine(LoadMapRootCoroutine(id));
    }

    private System.Collections.IEnumerator LoadMapRootCoroutine(MapRootId id)
    {
        string address = GetAddress(id);
        _loadHandle = Addressables.LoadAssetAsync<GameObject>(address);
        yield return _loadHandle;

        if (_loadHandle.Status != AsyncOperationStatus.Succeeded)
        {
            Debug.LogError("[MapPanel] failed to load MapRoot: " + address);
            yield break;
        }

        _currentMapRootInstance = Instantiate(_loadHandle.Result, mapRootContainer, false);
        _currentMapRoot = _currentMapRootInstance.GetComponent<MapRoot>();

        if (_currentMapRoot != null)
        {
            SceneMapId currentMap = GetCurrentSceneMapId();
            _currentMapRoot.Initialize(OnNodeClicked);
            PositionHead(currentMap);
        }
    }

    private void PositionHead(SceneMapId currentMap)
    {
        if (head == null || _currentMapRoot == null) return;

        var pos = _currentMapRoot.GetHeadPosition(currentMap);
        if (pos.HasValue)
        {
            head.anchoredPosition = pos.Value;
            head.gameObject.SetActive(true);
        }
        else
        {
            head.gameObject.SetActive(false);
        }
    }

    private void OnNodeClicked(SceneMapId targetMap)
    {
        ManagerRegistry.Get<UIManager>()?.Close<MapPanel>();
        GameApp.Instance?.GoToMap(targetMap);
    }

    private SceneMapId GetCurrentSceneMapId()
    {
        string mapName = GetCurrentMapName();
        if (string.IsNullOrEmpty(mapName)) return SceneMapId.StaffLounge;

        foreach (SceneMapId id in System.Enum.GetValues(typeof(SceneMapId)))
        {
            if (id.GetMapName() == mapName)
                return id;
        }

        return SceneMapId.StaffLounge;
    }

    private string GetCurrentMapName()
    {
        var levelController = LevelController.Instance;
        if (levelController != null && levelController.MapManager != null && levelController.MapManager.CurrentMap != null)
        {
            return levelController.MapManager.CurrentMap.name;
        }
        return null;
    }

    public void SwitchToMapRoot(MapRootId id)
    {
        LoadMapRoot(id);
    }

    private void ClearMapRoot()
    {
        if (_currentMapRootInstance != null)
        {
            Destroy(_currentMapRootInstance);
            _currentMapRootInstance = null;
        }

        if (_loadHandle.IsValid())
        {
            Addressables.Release(_loadHandle);
        }

        _currentMapRoot = null;
    }
}
