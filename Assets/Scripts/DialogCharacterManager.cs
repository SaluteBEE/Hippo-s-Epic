using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using cfg.cfg.person;

public class DialogCharacterManager : MonoBehaviour
{
    private static DialogCharacterManager _instance;
    public static DialogCharacterManager Instance
    {
        get
        {
            if (_instance == null)
                _instance = FindObjectOfType<DialogCharacterManager>();
            return _instance;
        }
    }

    private const int PrefabType = AnimationStateManager.PrefabType_Dialog;

    private cfg.Tables _tables;
    private AnimationStateManager _stateMgr;

    private readonly Dictionary<int, GameObject> _dynamicInstances = new Dictionary<int, GameObject>();
    private readonly Dictionary<int, AsyncOperationHandle<GameObject>> _loadedHandles = new Dictionary<int, AsyncOperationHandle<GameObject>>();
    private readonly List<AsyncOperationHandle> _pendingHandles = new List<AsyncOperationHandle>();
    private readonly HashSet<int> _scenePersonIds = new HashSet<int>();
    private Coroutine _prepareCoroutine;
    private int _prepareSeq;

    public void Initialize(cfg.Tables tables)
    {
        _tables = tables;
        _stateMgr = AnimationStateManager.Instance;
    }

    private void OnEnable()
    {
        var mgr = DialogManager.Instance;
        if (mgr != null)
            mgr.OnDialogStart += OnDialogStart;
    }

    private void OnDisable()
    {
        var mgr = DialogManager.Instance;
        if (mgr != null)
            mgr.OnDialogStart -= OnDialogStart;
    }

    private void OnDialogStart(int speakerid1, int speakerid2)
    {
        _prepareSeq++;
        int seq = _prepareSeq;

        if (_prepareCoroutine != null)
        {
            Debug.LogWarning("[DialogCharacterManager] 上一次角色准备尚未完成，强制中止");
            StopCoroutine(_prepareCoroutine);
            _prepareCoroutine = null;
        }

        _prepareCoroutine = StartCoroutine(PrepareCharactersCoroutine(speakerid1, speakerid2, seq));
    }

    private IEnumerator PrepareCharactersCoroutine(int speakerid1, int speakerid2, int seq)
    {
        _scenePersonIds.Clear();

        PrepareSide(speakerid1, SpeakerSide.Left, seq);
        PrepareSide(speakerid2, SpeakerSide.Right, seq);

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

        var mgr = DialogManager.Instance;
        if (mgr != null)
            mgr.NotifyCharactersReady();
    }

    private void PrepareSide(int personId, SpeakerSide side, int seq)
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

        LoadAndInstantiate(person, side, seq);
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

    private void LoadAndInstantiate(Person person, SpeakerSide side, int seq)
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
            GameObject instance = Instantiate(prefab);
            instance.name = $"Dialog_{person.Name}_{side}";
            _dynamicInstances[person.Id] = instance;

            var ctrl = instance.GetComponent<AnimationController>();
            if (ctrl != null)
            {
                RegisterToStateManager(person.Id, ctrl);
            }
            else
            {
                Debug.LogWarning($"[DialogCharacterManager] 预制体 {addressableKey} 没有 AnimationController");
            }

            Debug.Log($"[DialogCharacterManager] 动态加载角色: personId={person.Id} ({person.Name}) → {side}");
        };
    }

    private void RegisterToStateManager(int personId, AnimationController ctrl)
    {
        if (_stateMgr == null) return;

        var slotMgr = ctrl.GetComponent<SlotManager>();
        _stateMgr.RegisterCharacter(personId, PrefabType, ctrl, slotMgr);
    }

    public void CleanupDynamicCharacters()
    {
        foreach (var kvp in _dynamicInstances)
        {
            int personId = kvp.Key;
            GameObject instance = kvp.Value;

            if (instance != null)
            {
                _stateMgr?.UnregisterCharacter(personId, PrefabType);
                Destroy(instance);
                Debug.Log($"[DialogCharacterManager] 清理动态角色: personId={personId}");
            }
        }

        _dynamicInstances.Clear();

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
    }

    private void OnDestroy()
    {
        if (_instance == this)
            _instance = null;

        if (_prepareCoroutine != null)
        {
            StopCoroutine(_prepareCoroutine);
            _prepareCoroutine = null;
        }
        CleanupDynamicCharacters();
    }
}
