using System.Collections.Generic;
using System.Reflection;
using cfg.cfg.animationstate;
using cfg.cfg.slotstate;
using UnityEngine;

public class AnimationStateManager : MonoBehaviour
{
    private static AnimationStateManager _instance;
    public static AnimationStateManager Instance
    {
        get
        {
            if (_instance == null)
                _instance = FindObjectOfType<AnimationStateManager>();
            return _instance;
        }
    }

    public const int PrefabType_Dialog = 1;
    public const int PrefabType_Battle = 2;

    private cfg.Tables _tables;
    private Dictionary<(int personId, int prefabType), AnimationController> _animControllers = new Dictionary<(int, int), AnimationController>();
    private Dictionary<(int personId, int prefabType), SlotManager> _slotManagers = new Dictionary<(int, int), SlotManager>();

    private Dictionary<(int, string, int), Animationstate> _animStateIndex = new Dictionary<(int, string, int), Animationstate>();
    private Dictionary<int, Slotstate> _slotStateByIdIndex = new Dictionary<int, Slotstate>();

    private static readonly HashSet<string> _slotFixedFieldNames = new HashSet<string>
    {
        "Id", "Personid", "Personid_Ref", "Prefabtype", "__ID__"
    };

    private static FieldInfo[] _slotDataFields;

    public void SetTables(cfg.Tables tables)
    {
        _tables = tables;
        BuildIndices();
        SubscribeEvents();
    }

    private void BuildIndices()
    {
        _animStateIndex.Clear();
        _slotStateByIdIndex.Clear();

        if (_tables == null) return;

        foreach (var item in _tables.TbAnimationstate.DataList)
            _animStateIndex[(item.Personid, item.Statename, item.Prefabtype)] = item;

        foreach (var item in _tables.TbSlotstate.DataList)
            _slotStateByIdIndex[item.Id] = item;
    }

    private void OnEnable()
    {
        SubscribeEvents();
    }

    private void OnDisable()
    {
        UnsubscribeEvents();
    }

    private void SubscribeEvents()
    {
        var dialogMgr = DialogManager.Instance;
        if (dialogMgr == null) return;
        dialogMgr.OnEmotion -= ApplyState;
        dialogMgr.OnSlotState -= ApplySlotStateById;
        dialogMgr.OnEmotion += ApplyState;
        dialogMgr.OnSlotState += ApplySlotStateById;
    }

    private void UnsubscribeEvents()
    {
        var dialogMgr = DialogManager.Instance;
        if (dialogMgr == null) return;
        dialogMgr.OnEmotion -= ApplyState;
        dialogMgr.OnSlotState -= ApplySlotStateById;
    }

    public void RegisterCharacter(int personId, int prefabType, AnimationController ctrl, SlotManager slotMgr)
    {
        if (ctrl != null)
            _animControllers[(personId, prefabType)] = ctrl;

        if (slotMgr != null)
            _slotManagers[(personId, prefabType)] = slotMgr;
    }

    public void UnregisterCharacter(int personId, int prefabType)
    {
        _animControllers.Remove((personId, prefabType));
        _slotManagers.Remove((personId, prefabType));
    }

    public void UnregisterCharacterAll(int personId)
    {
        var keysToRemove = new List<(int, int)>();
        foreach (var key in _animControllers.Keys)
        {
            if (key.Item1 == personId)
                keysToRemove.Add(key);
        }
        foreach (var key in keysToRemove)
        {
            _animControllers.Remove(key);
            _slotManagers.Remove(key);
        }
    }

    public bool IsRegistered(int personId, int prefabType)
    {
        return _animControllers.ContainsKey((personId, prefabType));
    }

    public AnimationController GetController(int personId, int prefabType)
    {
        _animControllers.TryGetValue((personId, prefabType), out var ctrl);
        return ctrl;
    }

    public SlotManager GetSlotManager(int personId, int prefabType)
    {
        _slotManagers.TryGetValue((personId, prefabType), out var mgr);
        return mgr;
    }

    public List<int> GetRegisteredPrefabTypes(int personId)
    {
        var result = new List<int>();
        foreach (var key in _animControllers.Keys)
        {
            if (key.Item1 == personId)
                result.Add(key.Item2);
        }
        return result;
    }

    #region 动画状态

    public void ApplyState(int personId, string stateName)
    {
        if (_tables == null)
        {
            Debug.LogWarning("[AnimationStateManager] Tables 未初始化");
            return;
        }

        if (string.IsNullOrEmpty(stateName)) return;

        var keys = new List<(int, int)>(_animControllers.Keys);
        foreach (var key in keys)
        {
            if (key.Item1 == personId)
                ApplyAnimationState(personId, key.Item2, stateName);
        }
    }

    public void ApplyState(int personId, int prefabType, string stateName)
    {
        if (string.IsNullOrEmpty(stateName)) return;
        ApplyAnimationState(personId, prefabType, stateName);
    }

    private void ApplyAnimationState(int personId, int prefabType, string stateName)
    {
        if (!_animControllers.TryGetValue((personId, prefabType), out var ctrl))
            return;

        var animState = FindAnimState(personId, stateName, prefabType);
        if (animState == null)
            animState = FindAnimState(personId, stateName, 0);

        if (animState == null)
        {
            Debug.LogWarning($"[AnimationStateManager] 未找到动画状态 personId={personId} stateName={stateName} prefabType={prefabType}");
            return;
        }

        if (ctrl.IsInitialized)
        {
            ctrl.PlayComposition(animState.Statename);
            Debug.Log($"[AnimationStateManager] 角色{personId} prefabType={prefabType} 播放组合: {animState.Statename}");
        }

        if (animState.Slotstateid != 0)
            ApplySlotStateById(personId, animState.Slotstateid);
    }

    private Animationstate FindAnimState(int personId, string stateName, int prefabType)
    {
        _animStateIndex.TryGetValue((personId, stateName, prefabType), out var result);
        return result;
    }

    #endregion

    #region 插槽状态

    public void ApplySlotStateById(int personId, int slotstateId)
    {
        if (!_slotStateByIdIndex.TryGetValue(slotstateId, out var slotState))
        {
            if (_tables != null)
                slotState = _tables.TbSlotstate.GetOrDefault(slotstateId);
        }

        if (slotState == null)
        {
            Debug.LogWarning($"[AnimationStateManager] 未找到 Slotstate id={slotstateId}");
            return;
        }

        int targetPrefabType = slotState.Prefabtype;
        var keys = new List<(int, int)>(_slotManagers.Keys);
        foreach (var key in keys)
        {
            if (key.Item1 == personId && (targetPrefabType == 0 || key.Item2 == targetPrefabType))
            {
                if (_slotManagers.TryGetValue(key, out var slotMgr))
                {
                    int applied = ApplySlotStateValues(slotMgr, slotState);
                    Debug.Log($"[AnimationStateManager] 角色{personId} prefabType={key.Item2} 通过SlotStateId={slotstateId} 应用{applied}个插槽状态");
                }
            }
        }
    }

    private static FieldInfo[] GetSlotDataFields()
    {
        if (_slotDataFields != null)
            return _slotDataFields;
        var allFields = typeof(Slotstate).GetFields();
        var list = new List<FieldInfo>(allFields.Length);
        foreach (var f in allFields)
        {
            if (!_slotFixedFieldNames.Contains(f.Name))
                list.Add(f);
        }
        _slotDataFields = list.ToArray();
        return _slotDataFields;
    }

    private static int ApplySlotStateValues(SlotManager slotMgr, Slotstate slotState)
    {
        int applied = 0;
        var fields = GetSlotDataFields();
        foreach (var field in fields)
        {
            string slotName = field.Name;
            int val = (int)field.GetValue(slotState);

            if (!slotMgr.HasSlot(slotName))
                continue;

            if (val == -1)
            {
                slotMgr.SetSlotVisible(slotName, false);
            }
            else if (val >= 1)
            {
                string attachmentName = slotMgr.GetAttachmentNameByIndex(slotName, val);
                if (!string.IsNullOrEmpty(attachmentName))
                    slotMgr.SetSlotAttachment(slotName, attachmentName, false);
                slotMgr.SetSlotVisible(slotName, true);
            }
            else
            {
                slotMgr.SetSlotVisible(slotName, true);
            }
            applied++;
        }
        return applied;
    }

    #endregion
}
