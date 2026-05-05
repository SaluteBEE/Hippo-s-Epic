using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using cfg.cfg.person;

public class DialogCharacterManager : MonoBehaviour
{
    private const int PrefabType = AnimationStateManager.PrefabType_Dialog;
    private const int RightSlotCount = 3;

    private cfg.Tables _tables;
    private AnimationStateManager _stateMgr;
    private DialogCharacterRenderer _renderer;

    private readonly Dictionary<int, GameObject> _dynamicInstances = new Dictionary<int, GameObject>();
    private readonly Dictionary<int, AsyncOperationHandle<GameObject>> _loadedHandles = new Dictionary<int, AsyncOperationHandle<GameObject>>();
    private readonly List<AsyncOperationHandle> _pendingHandles = new List<AsyncOperationHandle>();
    private readonly HashSet<int> _scenePersonIds = new HashSet<int>();
    private readonly Dictionary<int, SpeakerSide> _personSides = new Dictionary<int, SpeakerSide>();
    private readonly int[] _rightSlotPersonIds = new int[RightSlotCount];
    private Coroutine _prepareCoroutine;
    private int _prepareSeq;

    private void Awake()
    {
        ManagerRegistry.Register(this);
        TryAutoInit();
    }

    private void TryAutoInit()
    {
        if (_tables != null) return;
        var dtm = ManagerRegistry.Get<DataTableManager>();
        if (dtm != null && dtm.IsLoaded)
            Initialize(dtm.Tables);
    }

    private DialogManager _dialogManager;

    public void Initialize(cfg.Tables tables)
    {
        _tables = tables;
        _stateMgr = ManagerRegistry.Get<AnimationStateManager>();
        SubscribeDialogManager();
    }

    private void EnsureRenderer()
    {
        if (_renderer == null)
            _renderer = ManagerRegistry.Get<DialogCharacterRenderer>();
    }

    private void SubscribeDialogManager()
    {
        UnsubscribeDialogManager();
        _dialogManager = ManagerRegistry.Get<DialogManager>();
        if (_dialogManager != null)
        {
            _dialogManager.OnDialogStart += OnDialogStart;
            _dialogManager.OnContent += OnContent;
        }
    }

    private void UnsubscribeDialogManager()
    {
        if (_dialogManager != null)
        {
            _dialogManager.OnDialogStart -= OnDialogStart;
            _dialogManager.OnContent -= OnContent;
        }
    }

    private void OnEnable()
    {
        if (_dialogManager == null)
            SubscribeDialogManager();
    }

    private void OnDisable()
    {
        UnsubscribeDialogManager();
    }

    private void OnDialogStart(int speakerid1, int speakerid2)
    {
        EnsureRenderer();

        _prepareSeq++;
        int seq = _prepareSeq;

        if (_prepareCoroutine != null)
        {
            Debug.LogWarning("[DialogCharacterManager] 上一次角色准备尚未完成，强制中止");
            StopCoroutine(_prepareCoroutine);
            _prepareCoroutine = null;
        }

        for (int i = 0; i < RightSlotCount; i++)
            _rightSlotPersonIds[i] = 0;

        _prepareCoroutine = StartCoroutine(PrepareCharactersCoroutine(speakerid1, speakerid2, seq));
    }

    private void OnContent(SpeakerSide side, string text, string speakerName)
    {
        if (_renderer == null) return;
        if (side != SpeakerSide.Right) return;

        var dialog = ManagerRegistry.Get<DialogManager>();
        if (dialog == null) return;
        int personId = dialog.CurrentSpeakerId2;
        int slotIndex = GetRightSlotIndex(personId);
        if (slotIndex == 1) return;

        int oldCenterId = _rightSlotPersonIds[1];
        _rightSlotPersonIds[1] = personId;
        _rightSlotPersonIds[slotIndex] = oldCenterId;

        _renderer.SwapToCenterSlot(slotIndex);
    }

    private int GetRightSlotIndex(int personId)
    {
        for (int i = 0; i < RightSlotCount; i++)
        {
            if (_rightSlotPersonIds[i] == personId)
                return i;
        }
        return 0;
    }

    private IEnumerator PrepareCharactersCoroutine(int speakerid1, int speakerid2, int seq)
    {
        _scenePersonIds.Clear();

        PrepareSide(speakerid1, SpeakerSide.Left, -1, seq);
        PrepareRightSlot(0, speakerid2, seq);

        while (_pendingHandles.Count > 0)
        {
            for (int i = _pendingHandles.Count - 1; i >= 0; i--)
            {
                if (_pendingHandles[i].IsDone)
                {
                    if (_pendingHandles[i].Status == AsyncOperationStatus.Failed)
                    {
                        Debug.LogError($"[DialogCharacterManager] 加载失败: {_pendingHandles[i].DebugName}");
                    }
                    _pendingHandles.RemoveAt(i);
                }
            }
            yield return null;
        }

        Debug.Log("[DialogCharacterManager] 角色准备完成");
        _prepareCoroutine = null;

        var mgr = ManagerRegistry.Get<DialogManager>();
        if (mgr != null)
            mgr.NotifyCharactersReady();
    }

    public void ApplyRightSlots(cfg.cfg.dialogcontent.Dialogcontent content)
    {
        if (content == null) return;
        EnsureRenderer();
        if (_renderer == null) return;

        int[] newIds = new int[RightSlotCount];
        bool anyChanged = false;

        int[] fieldIds = { content.RightSlot1Personid, content.RightSlot2Personid, content.RightSlot3Personid };
        for (int i = 0; i < RightSlotCount; i++)
        {
            newIds[i] = fieldIds[i] != 0 ? fieldIds[i] : _rightSlotPersonIds[i];
            if (newIds[i] != _rightSlotPersonIds[i])
                anyChanged = true;
        }

        if (!anyChanged) return;

        for (int i = 0; i < RightSlotCount; i++)
        {
            if (newIds[i] == _rightSlotPersonIds[i]) continue;

            if (_rightSlotPersonIds[i] != 0 && _dynamicInstances.ContainsKey(_rightSlotPersonIds[i]))
            {
                RemoveFromSlot(_rightSlotPersonIds[i]);
            }

            if (newIds[i] != 0)
            {
                PrepareRightSlot(i, newIds[i], _prepareSeq);
            }

            _rightSlotPersonIds[i] = newIds[i];
        }
    }

    private void PrepareRightSlot(int slotIndex, int personId, int seq)
    {
        if (personId == 0) return;

        if (_tables == null)
        {
            Debug.LogWarning("[DialogCharacterManager] Tables 未初始化");
            return;
        }

        var person = _tables.TbPerson.GetOrDefault(personId);
        if (person == null)
        {
            Debug.LogWarning($"[DialogCharacterManager] Person id={personId} 不存在");
            return;
        }

        if (_dynamicInstances.ContainsKey(personId) && _dynamicInstances[personId] != null)
        {
            Debug.Log($"[DialogCharacterManager] 复用已加载角色: personId={personId} ({person.Name})");
            MoveToSlot(personId, slotIndex);
            _rightSlotPersonIds[slotIndex] = personId;
            return;
        }

        AnimationController existingCtrl = FindControllerInScene(personId);
        if (existingCtrl != null)
        {
            _scenePersonIds.Add(personId);
            RegisterToStateManager(personId, existingCtrl);
            _rightSlotPersonIds[slotIndex] = personId;
            Debug.Log($"[DialogCharacterManager] 使用场景角色: personId={personId} ({person.Name})");
            return;
        }

        LoadAndInstantiateToSlot(person, slotIndex, seq);
        _rightSlotPersonIds[slotIndex] = personId;
    }

    private void PrepareSide(int personId, SpeakerSide side, int slotIndex, int seq)
    {
        if (personId == 0) return;

        if (_tables == null)
        {
            Debug.LogWarning("[DialogCharacterManager] Tables 未初始化");
            return;
        }

        var person = _tables.TbPerson.GetOrDefault(personId);
        if (person == null)
        {
            Debug.LogWarning($"[DialogCharacterManager] Person id={personId} 不存在");
            return;
        }

        if (_dynamicInstances.ContainsKey(personId) && _dynamicInstances[personId] != null)
        {
            Debug.Log($"[DialogCharacterManager] 复用已加载角色: personId={personId} ({person.Name})");
            return;
        }

        AnimationController existingCtrl = FindControllerInScene(personId);
        if (existingCtrl != null)
        {
            _scenePersonIds.Add(personId);
            RegisterToStateManager(personId, existingCtrl);
            Debug.Log($"[DialogCharacterManager] 使用场景角色: personId={personId} ({person.Name})");
            return;
        }

        LoadAndInstantiate(person, side, slotIndex, seq);
    }

    private AnimationController FindControllerInScene(int personId)
    {
        if (_stateMgr == null) return null;

        if (_stateMgr.IsRegistered(personId, PrefabType))
            return _stateMgr.GetController(personId, PrefabType);

        var person = _tables.TbPerson.GetOrDefault(personId);
        if (person == null) return null;

        var allControllers = FindObjectsOfType<AnimationController>();
        foreach (var ctrl in allControllers)
        {
            if (ctrl.Config == null) continue;

            string configName = ctrl.Config.name.Replace("_AnimationConfig", "");
            if (person.Animconfigs != null && person.Animconfigs.Contains(configName))
            {
                return ctrl;
            }
        }

        return null;
    }

    private void LoadAndInstantiate(Person person, SpeakerSide side, int slotIndex, int seq)
    {
        string addressableKey = person.Prefab1;
        if (string.IsNullOrEmpty(addressableKey))
        {
            Debug.LogWarning($"[DialogCharacterManager] Person id={person.Id} 的 Prefab1 为空");
            return;
        }

        var handle = Addressables.LoadAssetAsync<GameObject>(addressableKey);
        _pendingHandles.Add(handle);
        handle.Completed += op =>
        {
            _pendingHandles.Remove(op);

            if (seq != _prepareSeq)
            {
                if (op.IsValid())
                    Addressables.Release(op);
                return;
            }

            if (op.Status != AsyncOperationStatus.Succeeded)
            {
                Debug.LogError($"[DialogCharacterManager] 无法加载预制体: {addressableKey}");
                return;
            }

            _loadedHandles[person.Id] = op;

            GameObject prefab = op.Result;

            Transform anchor = null;
            if (_renderer != null)
            {
                if (side == SpeakerSide.Left)
                    anchor = _renderer.LeftAnchor;
                else if (slotIndex >= 0)
                    anchor = _renderer.GetRightSlot(slotIndex);
                else
                    anchor = _renderer.GetAnchor(side);
            }

            GameObject instance = anchor != null
                ? Instantiate(prefab, anchor)
                : Instantiate(prefab);
            instance.name = $"Dialog_{person.Name}_{side}";
            SetLayerRecursively(instance, LayerMask.NameToLayer("DialogCharacter"));
            _dynamicInstances[person.Id] = instance;
            _personSides[person.Id] = side;

            var ctrl = instance.GetComponent<AnimationController>();
            if (ctrl != null)
                RegisterToStateManager(person.Id, ctrl);
            else
                Debug.LogWarning($"[DialogCharacterManager] 预制体 {addressableKey} 没有 AnimationController");

            Debug.Log($"[DialogCharacterManager] 动态加载角色: personId={person.Id} ({person.Name}) → {side}");
        };
    }

    private void LoadAndInstantiateToSlot(Person person, int slotIndex, int seq)
    {
        string addressableKey = person.Prefab1;
        if (string.IsNullOrEmpty(addressableKey))
        {
            Debug.LogWarning($"[DialogCharacterManager] Person id={person.Id} 的 Prefab1 为空");
            return;
        }

        var handle = Addressables.LoadAssetAsync<GameObject>(addressableKey);
        _pendingHandles.Add(handle);
        handle.Completed += op =>
        {
            _pendingHandles.Remove(op);

            if (seq != _prepareSeq)
            {
                if (op.IsValid())
                    Addressables.Release(op);
                return;
            }

            if (op.Status != AsyncOperationStatus.Succeeded)
            {
                Debug.LogError($"[DialogCharacterManager] 无法加载预制体: {addressableKey}");
                return;
            }

            _loadedHandles[person.Id] = op;

            GameObject prefab = op.Result;

            Transform anchor = _renderer?.GetRightSlot(slotIndex);

            GameObject instance = anchor != null
                ? Instantiate(prefab, anchor)
                : Instantiate(prefab);
            instance.name = $"Dialog_{person.Name}_RightSlot{slotIndex + 1}";
            SetLayerRecursively(instance, LayerMask.NameToLayer("DialogCharacter"));
            _dynamicInstances[person.Id] = instance;
            _personSides[person.Id] = SpeakerSide.Right;

            var ctrl = instance.GetComponent<AnimationController>();
            if (ctrl != null)
                RegisterToStateManager(person.Id, ctrl);

            Debug.Log($"[DialogCharacterManager] 动态加载角色到插槽{slotIndex + 1}: personId={person.Id} ({person.Name})");
        };
    }

    private void MoveToSlot(int personId, int slotIndex)
    {
        if (!_dynamicInstances.TryGetValue(personId, out var instance) || instance == null) return;
        if (_renderer == null) return;

        var targetAnchor = _renderer.GetRightSlot(slotIndex);
        if (targetAnchor != null)
            instance.transform.SetParent(targetAnchor, false);
    }

    private void RemoveFromSlot(int personId)
    {
        if (!_dynamicInstances.TryGetValue(personId, out var instance) || instance == null) return;

        _stateMgr?.UnregisterCharacter(personId, PrefabType);
        Destroy(instance);
        _dynamicInstances.Remove(personId);
        _personSides.Remove(personId);

        if (_loadedHandles.TryGetValue(personId, out var handle) && handle.IsValid())
        {
            Addressables.Release(handle);
            _loadedHandles.Remove(personId);
        }

        Debug.Log($"[DialogCharacterManager] 移除插槽角色: personId={personId}");
    }

    private void RegisterToStateManager(int personId, AnimationController ctrl)
    {
        if (_stateMgr == null) return;

        var slotMgr = ctrl.GetComponent<SlotManager>();
        _stateMgr.RegisterCharacter(personId, PrefabType, ctrl, slotMgr);
    }

    public void CleanupDynamicCharacters()
    {
        EnsureRenderer();

        foreach (var kvp in _dynamicInstances)
        {
            int personId = kvp.Key;
            GameObject instance = kvp.Value;

            if (instance != null)
            {
                _stateMgr?.UnregisterCharacter(personId, PrefabType);
                DestroyImmediate(instance);
                Debug.Log($"[DialogCharacterManager] 清理动态角色: personId={personId}");
            }
        }

        _dynamicInstances.Clear();
        _personSides.Clear();

        for (int i = 0; i < RightSlotCount; i++)
            _rightSlotPersonIds[i] = 0;

        foreach (var kvp in _loadedHandles)
        {
            if (kvp.Value.IsValid())
                Addressables.Release(kvp.Value);
        }
        _loadedHandles.Clear();

        foreach (var handle in _pendingHandles)
        {
            if (handle.IsValid())
                Addressables.Release(handle);
        }
        _pendingHandles.Clear();

        _scenePersonIds.Clear();

        if (_renderer != null)
            _renderer.SetVisible(false);

        _renderer = null;
    }

    private static void SetLayerRecursively(GameObject go, int layer)
    {
        go.layer = layer;
        for (int i = 0; i < go.transform.childCount; i++)
            SetLayerRecursively(go.transform.GetChild(i).gameObject, layer);
    }

    private void OnDestroy()
    {
        UnsubscribeDialogManager();
        ManagerRegistry.Unregister<DialogCharacterManager>();

        if (_prepareCoroutine != null)
        {
            StopCoroutine(_prepareCoroutine);
            _prepareCoroutine = null;
        }
        CleanupDynamicCharacters();
    }
}
