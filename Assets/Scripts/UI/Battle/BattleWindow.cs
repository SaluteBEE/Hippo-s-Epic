using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 战斗窗口：下半部分UI面板
/// 左侧4个操作按钮 + 中间画框 + 右侧战斗记录/子面板 + 右上EXIT
/// </summary>
public class BattleWindow : UIWindow
{
    public override UILayer Layer => UILayer.Top;

    #region 序列化字段

    [Header("底部面板")]
    [SerializeField] private RectTransform bottomPanel;

    [Header("左侧操作按钮")]
    [SerializeField] private Button btnViolence;    // 暴力行为 → 技能列表
    [SerializeField] private Button btnTalk;        // 咄咄逼人 → 对话选择
    [SerializeField] private Button btnItem;        // 翻找工具 → 物品列表
    [SerializeField] private Button btnIdle;        // 正在发呆 → 跳过回合

    [Header("右侧内容区")]
    [SerializeField] private BattleLogPanel logPanel;          // 战斗记录
    [SerializeField] private GameObject subPanelContainer;      // 子面板容器（覆盖在记录上）

    [Header("子面板")]
    [SerializeField] private BattleSkillPanel skillPanel;
    [SerializeField] private BattleItemPanel itemPanel;
    [SerializeField] private BattleTalkPanel talkPanel;

    #endregion

    #region 运行时

    private BattleManager _battleManager;
    private BattleStageManager _stageManager;
    private bool _isSelectingTarget;
    private int _selectedSkillId;
    private Camera _mainCamera;
    private BattleUnit _currentCursorTarget;

    // 提示弹窗（技能无法使用时居中弹出，自动消失）
    private GameObject _toastGo;
    private TextMeshProUGUI _toastText;
    private Coroutine _toastCoroutine;

    // ---- 事件推送缓存（严格 MVC：只由事件更新，不主动读取战斗状态）----
    private BattleUnit _currentUnit;
    private bool _isWaitingForPlayerAction;
    private List<BattleUnit> _playerUnits = new List<BattleUnit>();
    private List<BattleUnit> _enemyUnits = new List<BattleUnit>();

    #endregion

    #region 生命周期

    /// <summary>
    /// 窗口创建：绑定四个操作按钮点击事件（暴力/咄咄逼人/翻找/发呆）
    /// </summary>
    public override void OnCreate(object args)
    {
        if (btnViolence != null) btnViolence.onClick.AddListener(OnViolenceClicked);
        if (btnTalk != null) btnTalk.onClick.AddListener(OnTalkClicked);
        if (btnItem != null) btnItem.onClick.AddListener(OnItemClicked);
        if (btnIdle != null) btnIdle.onClick.AddListener(OnIdleClicked);

        CloseAllSubPanels();

        _mainCamera = Camera.main;
    }

    /// <summary>
    /// 窗口打开：获取 BattleManager/StageManager、订阅事件、刷新UI；若已在等待玩家操作则立即设置默认选中目标
    /// </summary>
    public override void OnOpen(object args)
    {
        _battleManager = GameApp.Instance.BattleManager;
        _stageManager = BattleStageManager.Instance;

        if (_battleManager == null)
        {
            Debug.LogError("[BattleWindow] BattleManager 为空");
            return;
        }

        SubscribeEvents();
        SetActionButtonsVisible(false);
        CloseAllSubPanels();

        if (logPanel != null) logPanel.ClearLogs();

        // 严格 MVC：请求管理器推送当前状态快照（由事件回调更新缓存，不主动读取）
        // 若战斗已处于玩家等待，OnPlayerActionWaitChanged(true) 回调会自动显示按钮并选中目标
        _battleManager.PushStateToUI();
    }

    /// <summary>
    /// 窗口关闭：退订事件、清空管理器引用
    /// </summary>
    public override void OnClose()
    {
        UnsubscribeEvents();
        _battleManager = null;
        _stageManager = null;
    }

    private void Update()
    {
        // 目标选择模式：先选技能后，点击/方向键选目标，回车/空格确认，Esc 取消
        if (_isSelectingTarget)
        {
            // 鼠标点击选择（点击有效目标直接释放）
            if (Input.GetMouseButtonDown(0) && !IsPointerOverUI())
            {
                TrySelectTargetByClick();
            }
            // 键盘游标切换目标（方向键/WASD：在有效目标间循环切换）
            else if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
            {
                CycleTarget(1, 0);
            }
            else if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
            {
                CycleTarget(-1, 0);
            }
            else if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W))
            {
                CycleTarget(0, -1);
            }
            else if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S))
            {
                CycleTarget(0, 1);
            }
            // 回车/空格：确认当前选中目标
            else if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))
            {
                ConfirmCurrentTarget();
            }
            // Esc/右键：取消，返回技能面板
            else if (Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1))
            {
                CancelTargetSelection();
                if (logPanel != null)
                    logPanel.AddLog("已取消目标选择");
                OnViolenceClicked();
            }
            return;
        }
    }

    #endregion

    #region 事件订阅

    private void SubscribeEvents()
    {
        if (_battleManager == null) return;
        _battleManager.OnBattleStart += OnBattleStart;
        _battleManager.OnRoundStart += OnRoundStart;
        _battleManager.OnUnitTurnStart += OnUnitTurnStart;
        _battleManager.OnUnitPhaseChange += OnUnitPhaseChange;
        _battleManager.OnDamageTaken += OnDamageTaken;
        _battleManager.OnHealed += OnHealed;
        _battleManager.OnUnitDeath += OnUnitDeath;
        _battleManager.OnUnitTurnEnd += OnUnitTurnEnd;
        _battleManager.OnRoundEnd += OnRoundEnd;
        _battleManager.OnBattleEnd += OnBattleEnd;
        _battleManager.OnBuffApplied += OnBuffApplied;
        _battleManager.OnCurrentUnitChanged += OnCurrentUnitChanged;
        _battleManager.OnPlayerActionWaitChanged += OnPlayerActionWaitChanged;
        _battleManager.OnUnitListsChanged += OnUnitListsChanged;

        var presenter = BattlePresenter.Instance;
        if (presenter != null)
        {
            presenter.OnSequenceBegin += OnSequenceBegin;
            presenter.OnSequenceEnd += OnSequenceEnd;
        }
    }

    private void UnsubscribeEvents()
    {
        if (_battleManager == null) return;
        _battleManager.OnBattleStart -= OnBattleStart;
        _battleManager.OnRoundStart -= OnRoundStart;
        _battleManager.OnUnitTurnStart -= OnUnitTurnStart;
        _battleManager.OnUnitPhaseChange -= OnUnitPhaseChange;
        _battleManager.OnDamageTaken -= OnDamageTaken;
        _battleManager.OnHealed -= OnHealed;
        _battleManager.OnUnitDeath -= OnUnitDeath;
        _battleManager.OnUnitTurnEnd -= OnUnitTurnEnd;
        _battleManager.OnRoundEnd -= OnRoundEnd;
        _battleManager.OnBattleEnd -= OnBattleEnd;
        _battleManager.OnBuffApplied -= OnBuffApplied;
        _battleManager.OnCurrentUnitChanged -= OnCurrentUnitChanged;
        _battleManager.OnPlayerActionWaitChanged -= OnPlayerActionWaitChanged;
        _battleManager.OnUnitListsChanged -= OnUnitListsChanged;

        var presenter = BattlePresenter.Instance;
        if (presenter != null)
        {
            presenter.OnSequenceBegin -= OnSequenceBegin;
            presenter.OnSequenceEnd -= OnSequenceEnd;
        }
    }

    #endregion

    #region 事件回调

    /// <summary> 当前行动单位变化：仅缓存（由管理器推送，UI 不主动读取） </summary>
    private void OnCurrentUnitChanged(BattleUnit unit)
    {
        _currentUnit = unit;
    }

    /// <summary>
    /// 玩家操作等待状态变化：进入等待 → 显示操作按钮并默认选中目标；结束 → 隐藏按钮并清理
    /// </summary>
    private void OnPlayerActionWaitChanged(bool waiting)
    {
        // 缓存等待状态（严格 MVC：CanOperate/Update 门控读取此字段）
        _isWaitingForPlayerAction = waiting;
        if (waiting)
        {
            if (_currentUnit == null) return;
            SetActionButtonsVisible(true);
            // 新流程：进入玩家回合不预选目标，等玩家先选技能再选目标
            _selectedSkillId = 0;
            _currentCursorTarget = null;
            _isSelectingTarget = false;
            if (_stageManager != null)
                _stageManager.ClearTargetSelection();
        }
        else
        {
            SetActionButtonsVisible(false);
            CloseAllSubPanels();
            CancelTargetSelection();
        }
    }

    /// <summary> 单位列表变化（增援/死亡）：缓存快照 </summary>
    private void OnUnitListsChanged(List<BattleUnit> players, List<BattleUnit> enemies)
    {
        _playerUnits = new List<BattleUnit>(players);
        _enemyUnits = new List<BattleUnit>(enemies);
    }

    private void OnBattleStart()
    {
        if (logPanel != null) logPanel.AddLog("战斗开始！");
    }

    private void OnRoundStart(int round)
    {
        if (logPanel != null) logPanel.AddRoundLog(round);
    }

    private void OnUnitTurnStart(BattleUnit unit)
    {
        if (_stageManager != null)
        {
            // 清除上一位被选中的目标标签
            _stageManager.ClearTargetSelection();
            _stageManager.HighlightCurrentUnit(unit);
        }
    }

    private void OnUnitPhaseChange(BattleUnit unit, UnitTurnPhase phase)
    {
        if (phase == UnitTurnPhase.Action && unit.IsPlayerControlled)
        {
            SetActionButtonsVisible(true);
            // 新流程：进入玩家回合不预选目标，等玩家先选技能再选目标
            _selectedSkillId = 0;
            _currentCursorTarget = null;
            _isSelectingTarget = false;
            if (_stageManager != null)
                _stageManager.ClearTargetSelection();
        }
    }

    private void OnDamageTaken(BattleUnit unit, int damage)
    {
        // 更新场景血条
        if (_stageManager != null)
            _stageManager.UpdateHealthBar(unit);
        if (logPanel != null)
            logPanel.AddLog($"{GetUnitName(unit)} 受到 {damage} 点伤害");
    }

    private void OnHealed(BattleUnit unit, int amount)
    {
        if (_stageManager != null)
            _stageManager.UpdateHealthBar(unit);
        if (logPanel != null)
            logPanel.AddLog($"{GetUnitName(unit)} 恢复 {amount} 点生命");
    }

    private void OnBuffApplied(BattleUnit unit, int buffId, int param)
    {
        if (logPanel != null)
            logPanel.AddLog($"{GetUnitName(unit)} 获得Buff({buffId})");
    }

    private void OnUnitDeath(BattleUnit unit)
    {
        if (_stageManager != null)
            _stageManager.UpdateHealthBar(unit);
        if (logPanel != null) logPanel.AddDeathLog(GetUnitName(unit));
    }

    private void OnUnitTurnEnd(BattleUnit unit)
    {
        if (_stageManager != null)
            _stageManager.ClearHighlight();
        SetActionButtonsVisible(false);
        CloseAllSubPanels();
        CancelTargetSelection();
    }

    private void OnRoundEnd(int round) { }

    /// <summary> 演出序列开始：抑制输入（隐藏操作按钮/关闭子面板/取消目标选择） </summary>
    private void OnSequenceBegin()
    {
        SetActionButtonsVisible(false);
        CloseAllSubPanels();
        CancelTargetSelection();
    }

    /// <summary> 演出序列结束（回合推进由 BattleManager 负责） </summary>
    private void OnSequenceEnd() { }

    private void OnBattleEnd(BattleResult result)
    {
        SetActionButtonsVisible(false);
        CloseAllSubPanels();
        CancelTargetSelection();

        if (_stageManager != null)
            _stageManager.ClearHighlight();
        if (_stageManager != null)
            _stageManager.ClearTargetSelection();

        if (logPanel != null)
        {
            switch (result)
            {
                case BattleResult.Win: logPanel.AddLog("战斗胜利！"); break;
                case BattleResult.Lose: logPanel.AddLog("战斗失败..."); break;
                case BattleResult.Flee: logPanel.AddLog("成功逃跑！"); break;
            }
        }

        // 战斗结束：等演出全部结束（技能序列+死亡倒地）再返回原场景，避免截断死亡演出
        StartCoroutine(WaitPerformancesThenReturn());
    }

    /// <summary>
    /// 等待演出收尾：技能序列结束 → 死亡倒地播完 → 飘字淡出，再返回原场景
    /// </summary>
    private System.Collections.IEnumerator WaitPerformancesThenReturn()
    {
        var presenter = BattlePresenter.Instance;
        if (presenter != null)
        {
            while (presenter.IsPlaying)
                yield return null;
            while (presenter.IsDeathPlaying)
                yield return null;
        }
        // 再等半拍让飘字自然淡出
        yield return new WaitForSeconds(0.5f);

        var gameApp = GameApp.Instance;
        if (gameApp != null)
            gameApp.OnBattleResultConfirmed();
    }

    #endregion

    #region 画框立绘

    /// <summary>
    /// 通过 Person 表获取单位名称
    /// </summary>
    private string GetUnitName(BattleUnit unit)
    {
        if (unit == null) return "?";
        var tables = _battleManager?.GetTables();
        var person = tables?.TbPerson.GetOrDefault(unit.PersonId);
        return person != null ? person.Name : $"P{unit.PersonId}";
    }

    #endregion

    #region 玩家操作

    /// <summary> 暴力行为 → 打开技能列表 </summary>
    private void OnViolenceClicked()
    {
        if (!CanOperate()) return;
        CloseAllSubPanels();
        if (skillPanel != null)
        {
            skillPanel.Open(_battleManager, _currentUnit, OnSkillSelected);
            if (subPanelContainer != null) subPanelContainer.SetActive(true);
        }
    }

    /// <summary> 咄咄逼人 → 打开对话选择 </summary>
    private void OnTalkClicked()
    {
        if (!CanOperate()) return;
        CloseAllSubPanels();
        if (talkPanel != null)
        {
            talkPanel.Open(_battleManager, OnTalkSelected);
            if (subPanelContainer != null) subPanelContainer.SetActive(true);
        }
    }

    /// <summary> 翻找工具 → 打开物品列表 </summary>
    private void OnItemClicked()
    {
        if (!CanOperate()) return;
        CloseAllSubPanels();
        if (itemPanel != null)
        {
            itemPanel.Open(_battleManager, OnItemSelected);
            if (subPanelContainer != null) subPanelContainer.SetActive(true);
        }
    }

    /// <summary> 正在发呆 → 后移回合（排到队尾，下一位先行） </summary>
    private void OnIdleClicked()
    {
        if (!CanOperate()) return;
        _battleManager.PlayerDeferTurn();
    }

    private bool CanOperate()
    {
        if (_battleManager == null || !_isWaitingForPlayerAction) return false;
        var presenter = BattlePresenter.Instance;
        return presenter == null || !presenter.IsPlaying;
    }

    #endregion

    #region 子面板回调

    private void OnSkillSelected(int skillId)
    {
        _selectedSkillId = skillId;

        // 冷却中：给出提示，面板保持打开
        if (skillId > 0 && _currentUnit != null && _currentUnit.IsSkillOnCooldown(skillId))
        {
            _currentUnit.SkillCooldowns.TryGetValue(skillId, out int cd);
            if (logPanel != null)
                logPanel.AddLog($"技能冷却中，还需{cd}回合");
            ShowToast($"技能冷却中，还需{cd}回合");
            return;
        }

        // 行动点不足：给出提示，面板保持打开
        if (_currentUnit != null)
        {
            int apCost = 1;
            if (skillId > 0)
            {
                var costCfg = _battleManager?.GetTables()?.TbSkill.GetOrDefault(skillId);
                if (costCfg != null) apCost = costCfg.Cost;
            }
            if (_currentUnit.CurrentActionPoints < apCost)
            {
                if (logPanel != null)
                    logPanel.AddLog($"行动点不足，无法使用（需要{apCost}点）");
                ShowToast($"行动点不足，无法使用（需要{apCost}点）");
                return; // 面板保持打开
            }
        }

        // targettype=Self 无需选目标，直接对自身释放
        if (skillId > 0 && _battleManager != null)
        {
            var skillCfg = _battleManager.GetTables()?.TbSkill.GetOrDefault(skillId);
            if (skillCfg != null && (TargetType)skillCfg.Targettype == TargetType.Self
                && _currentUnit != null)
            {
                CloseAllSubPanels();
                _battleManager.PlayerUseSkill(skillId, _currentUnit.SlotIndex);
                return;
            }
        }

        // 先选技能 → 进入目标选择模式（再选目标）
        StartTargetSelection();
    }

    private void OnTalkSelected(int choiceIndex)
    {
        CloseAllSubPanels();
        // TODO: 咄咄逼人对话效果
        // 目前先作为普通攻击处理
        _selectedSkillId = 0;
        StartTargetSelection();
    }

    private void OnItemSelected(int itemId)
    {
        CloseAllSubPanels();
        // TODO: 物品使用
        Debug.Log($"[BattleWindow] 使用物品: {itemId}");
    }

    #endregion

    #region 目标选择

    private void StartTargetSelection()
    {
        // 关闭技能面板（已选技能，现在进入选目标）
        if (skillPanel != null) skillPanel.Close();
        if (subPanelContainer != null) subPanelContainer.SetActive(false);

        // 默认选中第一个有效目标
        var defaultTarget = FindDefaultTarget();
        if (defaultTarget == null)
        {
            if (logPanel != null)
                logPanel.AddLog("无有效目标，无法释放该技能");
            ShowToast("无有效目标，无法释放该技能");
            return;
        }

        _isSelectingTarget = true;
        _currentCursorTarget = defaultTarget;
        if (logPanel != null)
            logPanel.AddLog("请选择目标：点击 / 方向键切换 / 回车确认（Esc 取消）");
        if (_stageManager != null)
            _stageManager.ShowTargetSelected(defaultTarget);
    }

    /// <summary>
    /// 目标选择模式的默认目标：当前技能的第一个有效目标
    /// </summary>
    private BattleUnit FindDefaultTarget()
    {
        var validTargets = GetValidTargets();
        return validTargets.Count > 0 ? validTargets[0] : null;
    }

    /// <summary>
    /// 当前技能的有效目标列表（普通攻击→存活敌人；技能→按 selectable + range 筛选的可落点单位）
    /// </summary>
    private List<BattleUnit> GetValidTargets()
    {
        if (_battleManager == null || _currentUnit == null) return new List<BattleUnit>();

        if (_selectedSkillId == 0)
            return _enemyUnits.FindAll(u => u.IsAlive);

        return _battleManager.GetAvailableTargetsForSkill(_currentUnit, _selectedSkillId);
    }

    private void CancelTargetSelection()
    {
        _isSelectingTarget = false;
        _currentCursorTarget = null;
    }

    /// <summary> 鼠标是否悬停在 UI 上（避免点击角色时误触 UI 按钮） </summary>
    private bool IsPointerOverUI()
    {
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }

    /// <summary>
    /// 点击场景中的角色来选择目标
    /// </summary>
    private void TrySelectTargetByClick()
    {
        if (_mainCamera == null) _mainCamera = Camera.main;
        if (_mainCamera == null) return;

        Vector2 worldPos = _mainCamera.ScreenToWorldPoint(Input.mousePosition);
        var hit = Physics2D.OverlapPoint(worldPos);
        if (hit == null) return;

        // 查找被点击的单位
        var unit = FindUnitByGameObject(hit.gameObject);
        if (unit == null || !unit.IsAlive) return;

        // 只接受有效目标（落在技能九宫格有效范围内）
        if (!IsTargetValidForSkill(unit, _selectedSkillId)) return;

        // 执行技能
        _battleManager.PlayerUseSkill(_selectedSkillId, unit.SlotIndex);

        // 显示目标选中标签
        if (_stageManager != null)
            _stageManager.ShowTargetSelected(unit);

        CancelTargetSelection();
    }

    private BattleUnit FindUnitByGameObject(GameObject go)
    {
        if (_battleManager == null || _stageManager == null) return null;

        // 通过槽位唯一定位被点击的单位（支持同一 personId 的多个单位）
        if (!_stageManager.TryResolveUnitSlot(go, out bool isPlayerSide, out int slotIndex))
            return null;

        var list = isPlayerSide ? _playerUnits : _enemyUnits;
        foreach (var unit in list)
        {
            if (unit.SlotIndex == slotIndex && unit.IsAlive) return unit;
        }
        return null;
    }

    #region 键盘游标切换目标（3.2 动态选择轻量版）

    /// <summary>
    /// 当前选中目标对该技能是否为有效目标：由技能九宫格配置（阵营 targettype + 可选落点 selectable + 范围 range）决定。
    /// 选中目标与有效目标分离——选中目标无限制（鼠标所指任意角色），只有落在技能九宫格有效范围内才算有效目标，
    /// 技能仅在有有效目标时才允许释放。
    /// </summary>
    private bool IsTargetValidForSkill(BattleUnit target, int skillId)
    {
        if (target == null || !target.IsAlive) return false;
        if (_battleManager == null || _currentUnit == null) return false;

        // 普通攻击：只对敌方单位有效（单格命中）
        if (skillId == 0)
            return target.IsPlayerSide != _currentUnit.IsPlayerSide;

        var skillCfg = _battleManager.GetTables()?.TbSkill.GetOrDefault(skillId);
        if (skillCfg == null) return false;

        var tt = (TargetType)skillCfg.Targettype;
        // Self 技能无需选目标，始终有效（可用性由行动点/冷却决定，不受选中目标影响）
        if (tt == TargetType.Self)
            return true;

        // 阵营校验：targettype 决定有效阵营（Ally→友方，Enemy→敌方）
        bool expectedSide = tt == TargetType.Ally ? _currentUnit.IsPlayerSide : !_currentUnit.IsPlayerSide;
        if (target.IsPlayerSide != expectedSide) return false;

        // 九宫格校验：GetSkillTargets 内部统一校验 selectable（落点可选）与 range（范围内命中），
        // 以选中目标所在槽位为落点中心，范围展开后至少命中一个存活单位才算有效
        return _battleManager.GetSkillTargets(_currentUnit, skillCfg, target.SlotIndex).Count > 0;
    }

    /// <summary> 键盘游标在当前有效目标基础上按行列偏移切换（左右循环，上下换行） </summary>
    private void CycleTarget(int colDelta, int rowDelta)
    {
        var targets = GetValidTargets();
        if (targets.Count == 0) return;

        var current = _currentCursorTarget;
        if (current == null || !current.IsAlive)
        {
            current = targets[0];
        }
        else
        {
            int idx = targets.IndexOf(current);
            if (idx < 0)
            {
                current = targets[0];
            }
            else
            {
                int next = idx + colDelta + rowDelta * 3;
                // 循环（含上下换行越界回绕）
                next = (next % targets.Count + targets.Count) % targets.Count;
                current = targets[next];
            }
        }

        _currentCursorTarget = current;
        if (_stageManager != null)
            _stageManager.ShowTargetSelected(current);
    }

    /// <summary> 回车/空格：确认当前游标目标并释放技能 </summary>
    private void ConfirmCurrentTarget()
    {
        if (_currentCursorTarget == null) return;
        var unit = _currentCursorTarget;
        if (!unit.IsAlive) return;
        if (!IsTargetValidForSkill(unit, _selectedSkillId)) return;

        _battleManager.PlayerUseSkill(_selectedSkillId, unit.SlotIndex);

        if (_stageManager != null)
            _stageManager.ShowTargetSelected(unit);

        CancelTargetSelection();
    }

    #endregion

    #endregion

    #region UI控制

    private void SetActionButtonsVisible(bool visible)
    {
        if (btnViolence != null) btnViolence.gameObject.SetActive(visible);
        if (btnTalk != null) btnTalk.gameObject.SetActive(visible);
        if (btnItem != null) btnItem.gameObject.SetActive(visible);
        if (btnIdle != null) btnIdle.gameObject.SetActive(visible);
    }

    private void CloseAllSubPanels()
    {
        if (skillPanel != null) skillPanel.Close();
        if (itemPanel != null) itemPanel.Close();
        if (talkPanel != null) talkPanel.Close();
        if (subPanelContainer != null) subPanelContainer.SetActive(false);
    }

    /// <summary>
    /// 弹出提示弹窗（技能无法使用等），居中显示，约 1.5 秒后自动消失
    /// </summary>
    private void ShowToast(string message)
    {
        if (_toastGo == null)
            CreateToast();
        if (_toastGo == null) return;

        _toastGo.SetActive(true);
        if (_toastText != null)
            _toastText.text = message;

        if (_toastCoroutine != null)
            StopCoroutine(_toastCoroutine);
        _toastCoroutine = StartCoroutine(HideToastRoutine());
    }

    /// <summary>
    /// 动态创建提示弹窗（挂到 Canvas 下，屏幕居中偏上）
    /// </summary>
    private void CreateToast()
    {
        var canvas = GetComponentInParent<Canvas>();
        var parent = canvas != null ? canvas.transform : transform;

        var go = new GameObject("BattleToast", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(460f, 130f);
        rect.anchoredPosition = new Vector2(0f, 240f);

        var bg = go.GetComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.82f);

        var textGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        textGo.transform.SetParent(go.transform, false);
        var textRect = textGo.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(16f, 12f);
        textRect.offsetMax = new Vector2(-16f, -12f);

        _toastText = textGo.GetComponent<TextMeshProUGUI>();
        _toastText.font = TMP_Settings.defaultFontAsset;
        _toastText.fontSize = 26f;
        _toastText.alignment = TextAlignmentOptions.Center;
        _toastText.color = Color.white;

        _toastGo = go;
        _toastGo.SetActive(false);
    }

    private System.Collections.IEnumerator HideToastRoutine()
    {
        yield return new WaitForSeconds(1.5f);
        if (_toastGo != null)
            _toastGo.SetActive(false);
    }

    #endregion
}
