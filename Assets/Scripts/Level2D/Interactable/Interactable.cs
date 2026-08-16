using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

[AddComponentMenu("Level 2D/Interactable")]
public class Interactable : MonoBehaviour
{
    private const string HintPrefabAddress = "Assets/Prefabs/UI/Interact/InteractHint";

    [Header("Phases")]
    [SerializeField] private List<InteractionPhase> phases = new List<InteractionPhase>();

    private static GameObject _hintPrefab;
    private static AsyncOperationHandle<GameObject> _hintPrefabHandle;
    private static bool _hintLoading;
    private readonly List<InteractHint> _pendingHints = new List<InteractHint>();
    private InteractHint _hint;
    private int _currentState;
    private InteractionPhase _currentPhase;
    private bool _playerInside;
    private bool _hasExecuted;
    private int _playerInsideCount;

    public string EntityId => $"{transform.parent.name}_{name}";
    public int CurrentState => _currentState;
    public InteractionPhase CurrentPhase => _currentPhase;

    public InteractionPhase GetPhase(int state)
    {
        return phases.Find(p => p.state == state);
    }

    public void ApplyState(int state)
    {
        _currentState = state;
        _hasExecuted = false;

        _currentPhase = GetPhase(state);

        if (_currentPhase == null)
        {
            Debug.LogWarning($"[Interactable] {EntityId} 未找到 state={state} 的阶段配置");
            return;
        }

        _currentPhase.MigrateIfNeeded();

        if (_currentPhase.deactivateSelf)
        {
            gameObject.SetActive(false);
            return;
        }

        gameObject.SetActive(true);

        ApplyChildNodes(_currentPhase);
        ApplyAnimation(_currentPhase);
    }

    private void ApplyAnimation(InteractionPhase phase)
    {
        if (!phase.playAnimation) return;
        if (phase.animationController == null) return;

        var ctrl = phase.animationController;
        if (!ctrl.IsInitialized) return;

        if (ctrl.Config != null && ctrl.Config.HasComposition(phase.composition))
        {
            ctrl.PlayComposition(phase.composition);
        }
    }

    private void ApplyChildNodes(InteractionPhase phase)
    {
        var styleRoot = transform.Find("Style");
        if (styleRoot == null) return;
        if (phase.activeChildNames == null || phase.activeChildNames.Count == 0) return;

        var activeSet = new HashSet<string>(phase.activeChildNames);

        for (int i = 0; i < styleRoot.childCount; i++)
        {
            Transform child = styleRoot.GetChild(i);
            bool shouldBeActive = activeSet.Contains(child.name);

            if (child.gameObject.activeSelf != shouldBeActive)
                child.gameObject.SetActive(shouldBeActive);
        }
    }

    public void InitializeHint()
    {
        if (_hint != null) return;
        if (phases == null || phases.Count == 0) return;

        Transform uiChild = transform.Find("UI");
        if (uiChild == null) return;

        if (_hintPrefab != null)
        {
            CreateHintInstance(uiChild);
            return;
        }

        if (!_hintLoading)
            StartCoroutine(LoadHintPrefabAsync(uiChild));
        else
            _pendingHints.Add(null);
    }

    private IEnumerator LoadHintPrefabAsync(Transform uiChild)
    {
        _hintLoading = true;
        _hintPrefabHandle = Addressables.LoadAssetAsync<GameObject>(HintPrefabAddress);
        yield return _hintPrefabHandle;

        if (_hintPrefabHandle.Status == AsyncOperationStatus.Succeeded)
        {
            _hintPrefab = _hintPrefabHandle.Result;
            CreateHintInstance(uiChild);
        }
        else
        {
            Debug.LogWarning($"[Interactable] 加载 Hint 预制体失败: {HintPrefabAddress}");
        }
        _hintLoading = false;
    }

    private void CreateHintInstance(Transform parent)
    {
        if (_hintPrefab == null) return;
        if (_hint != null) return;
        GameObject instance = Instantiate(_hintPrefab, parent);
        instance.name = "InteractHint";
        _hint = instance.GetComponent<InteractHint>();

        if (_playerInside)
            RefreshHint();
    }

    public static void ReleaseHintPrefab()
    {
        if (_hintPrefabHandle.IsValid())
            Addressables.Release(_hintPrefabHandle);
        _hintPrefab = null;
        _hintPrefabHandle = default;
    }

    #region Player Detection

    private float _checkInterval = 0.5f;
    private float _lastCheckTime;
    private float _triggerCheckInterval = 1f;
    private float _lastTriggerCheckTime;
    private Collider2D _triggerCollider;

    private void OnTriggerEnter2D(Collider2D other)
    {
        var player = other.GetComponentInParent<PlayerCharacter>();
        if (player == null) return;

        _playerInsideCount++;
        if (_playerInsideCount != 1) return;

        _triggerCollider = other;
        _playerInside = true;
        _lastCheckTime = Time.time;

        if (player.CurrentInteractable != null && player.CurrentInteractable != this)
            player.CurrentInteractable.OnPlayerExit();

        player.SetCurrentInteractable(this);

        if (_hint == null)
            InitializeHint();

        RefreshHint();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        var player = other.GetComponentInParent<PlayerCharacter>();
        if (player == null) return;

        _playerInsideCount--;
        if (_playerInsideCount > 0) return;
        _playerInsideCount = 0;

        _playerInside = false;
        _triggerCollider = null;
        if (_hint != null)
            _hint.Hide();

        if (player.CurrentInteractable == this)
            player.ClearCurrentInteractable();
    }

    public void OnPlayerEnter()
    {
        _playerInside = true;
        RefreshHint();
    }

    private void Update()
    {
        CheckConditionTriggers();

        if (!_playerInside || _triggerCollider == null)
            return;

        if (Time.time - _lastCheckTime < _checkInterval)
            return;

        _lastCheckTime = Time.time;

        if (!IsStillOverlapping())
        {
            _playerInside = false;
            _playerInsideCount = 0;
            _triggerCollider = null;
            if (_hint != null)
                _hint.Hide();

            var player = FindObjectOfType<PlayerCharacter>();
            if (player != null && player.CurrentInteractable == this)
                player.ClearCurrentInteractable();
        }
    }

    private void CheckConditionTriggers()
    {
        if (_currentPhase == null) return;
        if (_currentPhase.conditionTriggers == null || _currentPhase.conditionTriggers.Count == 0) return;

        if (Time.time - _lastTriggerCheckTime < _triggerCheckInterval) return;
        _lastTriggerCheckTime = Time.time;

        var condSys = ConditionSystem.HasInstance ? ConditionSystem.Instance : null;
        if (condSys == null) return;

        foreach (var trigger in _currentPhase.conditionTriggers)
        {
            if (trigger.transitionToState < 0) continue;
            if (trigger.conditionId != 0 && condSys.IsConditionMet(trigger.conditionId))
            {
                int newState = trigger.transitionToState;
                SaveManager.Instance.SetEntityState(EntityId, newState);
                ApplyState(newState);
                return;
            }
        }
    }

    private bool IsStillOverlapping()
    {
        if (_triggerCollider == null) return false;

        var myCollider = GetComponent<Collider2D>();
        if (myCollider == null) return false;

        return myCollider.OverlapPoint(_triggerCollider.bounds.center)
            || _triggerCollider.OverlapPoint(myCollider.bounds.center);
    }

    public void OnPlayerExit()
    {
        _playerInside = false;
        if (_hint != null)
            _hint.Hide();
    }

    public void OnPlayerExecute(int buttonIndex = 0)
    {
        if (!_playerInside)
            return;
        if (_currentPhase == null)
            return;

        var buttons = _currentPhase.buttons;
        if (buttons == null || buttonIndex < 0 || buttonIndex >= buttons.Count)
        {
            Debug.LogWarning($"[Interactable] {EntityId} OnPlayerExecute({buttonIndex}) 无效，buttons.Count={buttons?.Count ?? 0}");
            return;
        }

        var button = buttons[buttonIndex];

        if (button.conditionId != 0)
        {
            var condSys = ConditionSystem.HasInstance ? ConditionSystem.Instance : null;
            if (condSys != null && !condSys.IsConditionMet(button.conditionId))
                return;
        }

        if (_hasExecuted && !_currentPhase.canRepeat)
            return;

        ExecuteButtonAction(button);

        _hasExecuted = true;

        bool hideAfterExecute = _currentPhase.hideAfterExecute;
        bool destroySelf = _currentPhase.destroySelf;

        if (button.transitionToState >= 0)
        {
            int newState = button.transitionToState;
            SaveManager.Instance.SetEntityState(EntityId, newState);
            ApplyState(newState);
        }

        if (destroySelf)
        {
            if (_hint != null)
                _hint.Hide();
            Destroy(gameObject);
        }
        else if (hideAfterExecute)
        {
            if (_hint != null)
                _hint.Hide();
        }
        else
        {
            RefreshHint();
        }
    }

    #endregion

    private void ExecuteButtonAction(ButtonOption button)
    {
        Debug.Log($"[Interactable] {EntityId} 执行 {button.type} (buttonText={button.buttonText}, param1={button.param1}, param2={button.param2})");

        switch (button.type)
        {
            case InteractionType.HintOnly:
                break;

            case InteractionType.Dialogue:
                ExecuteDialogue(button);
                break;

            case InteractionType.Pickup:
                ExecutePickup(button);
                break;

            case InteractionType.Consume:
                ExecuteConsume(button);
                break;

            case InteractionType.Teleport:
                ExecuteTeleport(button);
                break;
        }
    }

    private void ExecuteDialogue(ButtonOption button)
    {
        if (_hint != null)
            _hint.Hide();

        var uiManager = ManagerRegistry.Get<UIManager>();
        if (uiManager != null)
        {
            uiManager.Open<DialogWindow>(button.dataId);
        }
    }

    private void ExecutePickup(ButtonOption button)
    {
        int itemId = button.dataId;
        int count = 1;
        if (!string.IsNullOrEmpty(button.param1) && int.TryParse(button.param1, out int parsed))
            count = parsed;
        int added = BagManager.Instance.AddItem(itemId, count);
        if (added > 0)
        {
            var itemCfg = BagManager.Instance.GetItemConfig(itemId);
            Debug.Log($"获得了道具: {itemCfg?.Name ?? itemId.ToString()} x{added}");
        }
    }

    private void ExecuteConsume(ButtonOption button)
    {
        int itemId = button.dataId;
        int count = 1;
        if (!string.IsNullOrEmpty(button.param1) && int.TryParse(button.param1, out int parsed))
            count = parsed;

        if (!BagManager.Instance.HasItem(itemId, count))
        {
            var itemCfg = BagManager.Instance.GetItemConfig(itemId);
            Debug.LogWarning($"[Interactable] {EntityId} 物品不足: {itemCfg?.Name ?? itemId.ToString()} 需要{count}个");
            return;
        }

        bool removed = BagManager.Instance.RemoveItem(itemId, count);
        if (removed)
        {
            var itemCfg = BagManager.Instance.GetItemConfig(itemId);
            Debug.Log($"消耗了道具: {itemCfg?.Name ?? itemId.ToString()} x{count}");
        }
    }

    private void ExecuteTeleport(ButtonOption button)
    {
        if (button.teleportMode == 1)
        {
            string targetScene = button.param2;
            if (!string.IsNullOrEmpty(targetScene) && GameApp.Instance != null)
            {
                SaveManager.Instance.SetEntityState(EntityId, _currentState);
                GameApp.Instance.StartGame(targetScene);
            }
            return;
        }

        string target = button.param1;
        if (string.IsNullOrEmpty(target)) return;

        var all = FindObjectsOfType<Interactable>(true);
        foreach (var ia in all)
        {
            if (ia.EntityId != target) continue;

            var mapManager = LevelController.Instance?.MapManager;
            Map targetMap = FindParentMap(ia.transform);
            if (mapManager != null && targetMap != null)
                mapManager.SwitchMap(targetMap.name, false);

            var player = FindObjectOfType<PlayerCharacter>();
            if (player != null)
            {
                Vector3 pos = ia.transform.position;
                player.transform.position = new Vector3(pos.x, pos.y, pos.y);
                Debug.Log($"[Interactable] {EntityId} 传送到交互点 {target}");
            }
            return;
        }

        Debug.LogWarning($"[Interactable] {EntityId} 未找到目标交互点: {target}");
    }

    private static Map FindParentMap(Transform t)
    {
        Transform cur = t.parent;
        while (cur != null)
        {
            var map = cur.GetComponent<Map>();
            if (map != null) return map;
            cur = cur.parent;
        }
        return null;
    }

    private void OnEnable()
    {
        if (ConditionSystem.HasInstance)
            ConditionSystem.Instance.OnConditionChanged += OnConditionChanged;
    }

    private void OnDisable()
    {
        if (ConditionSystem.HasInstance)
            ConditionSystem.Instance.OnConditionChanged -= OnConditionChanged;
    }

    private void OnConditionChanged(int conditionId, bool isMet)
    {
        if (_currentPhase == null) return;

        if (isMet && _currentPhase.conditionTriggers != null)
        {
            foreach (var trigger in _currentPhase.conditionTriggers)
            {
                if (trigger.conditionId == conditionId && trigger.transitionToState >= 0)
                {
                    SaveManager.Instance.SetEntityState(EntityId, trigger.transitionToState);
                    ApplyState(trigger.transitionToState);
                    if (_playerInside)
                        RefreshHint();
                    return;
                }
            }
        }

        if (!_playerInside || _hint == null)
            return;

        var buttons = _currentPhase.buttons;
        if (buttons == null) return;

        for (int i = 0; i < buttons.Count; i++)
        {
            if (buttons[i].conditionId == conditionId)
            {
                RefreshHint();
                return;
            }
        }

        if (_currentPhase.conditionTriggers != null)
        {
            foreach (var trigger in _currentPhase.conditionTriggers)
            {
                if (trigger.conditionId == conditionId)
                {
                    RefreshHint();
                    return;
                }
            }
        }
    }

    private void RefreshHint()
    {
        if (_hint == null || _currentPhase == null)
            return;

        if (_hasExecuted && !_currentPhase.canRepeat)
        {
            _hint.Hide();
            return;
        }

        _currentPhase.MigrateIfNeeded();

        var buttons = _currentPhase.buttons;
        if (buttons == null || buttons.Count == 0)
        {
            _hint.Show(_currentPhase.hintText, Array.Empty<string>(), Array.Empty<bool>(), null);
            return;
        }

        var condSys = ConditionSystem.HasInstance ? ConditionSystem.Instance : null;
        var visibleTexts = new List<string>();
        var visibleInteractables = new List<bool>();
        var visibleIndices = new List<int>();

        for (int i = 0; i < buttons.Count; i++)
        {
            var btn = buttons[i];
            visibleTexts.Add(btn.buttonText);
            visibleIndices.Add(i);

            bool conditionMet = true;
            if (btn.conditionId != 0 && condSys != null && !condSys.IsConditionMet(btn.conditionId))
                conditionMet = false;
            visibleInteractables.Add(conditionMet);
        }

        _hint.Show(_currentPhase.hintText, visibleTexts.ToArray(), visibleInteractables.ToArray(), OnHintButtonClicked);
        _visibleButtonIndices = visibleIndices;
        _visibleButtonInteractables = visibleInteractables;
    }

    private List<int> _visibleButtonIndices = new List<int>();
    private List<bool> _visibleButtonInteractables = new List<bool>();

    private void OnHintButtonClicked(int visibleIndex)
    {
        if (visibleIndex < 0 || visibleIndex >= _visibleButtonIndices.Count)
            return;
        if (!_visibleButtonInteractables[visibleIndex])
            return;
        int actualIndex = _visibleButtonIndices[visibleIndex];
        OnPlayerExecute(actualIndex);
    }
}
