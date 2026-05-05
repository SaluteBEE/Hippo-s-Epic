using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class RightSideSlotManager : MonoBehaviour
{
    private const int SlotCount = 3;

    [System.Serializable]
    public class SlotSpriteConfig
    {
        public Vector3 localPosition;
        public Vector3 localScale = Vector3.one;
    }

    [Header("插槽精灵布局配置")]
    [SerializeField] private SlotSpriteConfig slot1Config = new SlotSpriteConfig { localPosition = new Vector3(0, 0, 0) };
    [SerializeField] private SlotSpriteConfig slot2Config = new SlotSpriteConfig { localPosition = new Vector3(0, 0, 0) };
    [SerializeField] private SlotSpriteConfig slot3Config = new SlotSpriteConfig { localPosition = new Vector3(0, 0, 0) };

    [Header("Addressables 根路径")]
    [SerializeField] private string addressableRoot = "dialog_images";

    private readonly SlotSpriteConfig[] _configs = new SlotSpriteConfig[3];
    private readonly GameObject[] _slotSpriteGOs = new GameObject[3];
    private readonly Dictionary<string, Sprite> _cache = new Dictionary<string, Sprite>();
    private readonly Dictionary<string, AsyncOperationHandle> _handles = new Dictionary<string, AsyncOperationHandle>();
    private DialogCharacterManager _charManager;
    private DialogCharacterRenderer _renderer;

    private void Awake()
    {
        _configs[0] = slot1Config;
        _configs[1] = slot2Config;
        _configs[2] = slot3Config;
    }

    public void Initialize()
    {
        _charManager = ManagerRegistry.Get<DialogCharacterManager>();
        _renderer = ManagerRegistry.Get<DialogCharacterRenderer>();

        var dialogManager = ManagerRegistry.Get<DialogManager>();
        if (dialogManager != null)
            dialogManager.OnRightSlotUpdate += OnRightSlotUpdate;
    }

    public void Cleanup()
    {
        var dialogManager = ManagerRegistry.Get<DialogManager>();
        if (dialogManager != null)
            dialogManager.OnRightSlotUpdate -= OnRightSlotUpdate;

        ClearAllSlots();
    }

    private void OnRightSlotUpdate(cfg.cfg.dialogcontent.Dialogcontent content)
    {
        if (content == null) return;

        _charManager?.ApplyRightSlots(content);

        string[] imagePaths = { content.RightSlot1Image, content.RightSlot2Image, content.RightSlot3Image };
        for (int i = 0; i < SlotCount; i++)
        {
            if (!string.IsNullOrEmpty(imagePaths[i]))
                ShowSlotSpriteAsync(i, imagePaths[i]);
            else
                HideSlotSprite(i);
        }
    }

    private async void ShowSlotSpriteAsync(int slotIndex, string imageName)
    {
        if (slotIndex < 0 || slotIndex >= SlotCount) return;

        if (_cache.TryGetValue(imageName, out var cached) && cached != null)
        {
            CreateSpriteGO(slotIndex, cached);
            return;
        }

        var address = $"{addressableRoot}/{imageName}";
        if (_handles.TryGetValue(imageName, out var existing))
        {
            if (!existing.IsDone) return;
            ResolveHandle(slotIndex, imageName, existing);
            return;
        }

        var texHandle = Addressables.LoadAssetAsync<Texture2D>(address);
        _handles[imageName] = texHandle;
        await texHandle.Task;

        if (texHandle.Status == AsyncOperationStatus.Succeeded)
        {
            var tex = texHandle.Result;
            var sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100);
            sprite.name = imageName;
            _cache[imageName] = sprite;
            CreateSpriteGO(slotIndex, sprite);
        }
        else
        {
            Debug.LogWarning($"[RightSideSlotManager] 图片加载失败: {address}");
        }
    }

    private void ResolveHandle(int slotIndex, string imageName, AsyncOperationHandle handle)
    {
        if (handle.Result is Sprite sp)
        {
            _cache[imageName] = sp;
            CreateSpriteGO(slotIndex, sp);
        }
    }

    private void CreateSpriteGO(int slotIndex, Sprite sprite)
    {
        var slot = _renderer?.GetRightSlot(slotIndex);
        if (slot == null || sprite == null) return;

        HideSlotSprite(slotIndex);

        var go = new GameObject($"RightSprite_{slotIndex + 1}");
        go.transform.SetParent(slot, false);
        go.transform.localPosition = _configs[slotIndex].localPosition;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = _configs[slotIndex].localScale;
        go.layer = LayerMask.NameToLayer("DialogCharacter");

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = 10;

        _slotSpriteGOs[slotIndex] = go;
    }

    private void HideSlotSprite(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= SlotCount) return;
        if (_slotSpriteGOs[slotIndex] != null)
        {
            Destroy(_slotSpriteGOs[slotIndex]);
            _slotSpriteGOs[slotIndex] = null;
        }
    }

    private void ClearAllSlots()
    {
        for (int i = 0; i < SlotCount; i++)
            HideSlotSprite(i);
    }

    private void OnDestroy()
    {
        Cleanup();

        foreach (var kvp in _handles)
        {
            if (kvp.Value.IsValid())
                Addressables.Release(kvp.Value);
        }
        _handles.Clear();
        _cache.Clear();
    }
}
