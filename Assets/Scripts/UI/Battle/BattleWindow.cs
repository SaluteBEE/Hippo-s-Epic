using System.Collections.Generic;
using UnityEngine;
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

    #endregion

    #region 生命周期

    public override void OnCreate(object args)
    {
        if (btnViolence != null) btnViolence.onClick.AddListener(OnViolenceClicked);
        if (btnTalk != null) btnTalk.onClick.AddListener(OnTalkClicked);
        if (btnItem != null) btnItem.onClick.AddListener(OnItemClicked);
        if (btnIdle != null) btnIdle.onClick.AddListener(OnIdleClicked);

        CloseAllSubPanels();

        _mainCamera = Camera.main;
    }

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

        // 如果已经在等待玩家操作
        if (_battleManager.IsWaitingForPlayerAction && _battleManager.CurrentUnit != null)
        {
            SetActionButtonsVisible(true);
            // 全局选中中心点兜底：窗口打开时若已进入玩家回合，立即设默认选中目标
            _selectedSkillId = 0;
            _currentCursorTarget = FindDefaultTarget();
            if (_currentCursorTarget != null && _stageManager != null)
                _stageManager.ShowTargetSelected(_currentCursorTarget);
        }
    }

    public override void OnClose()
    {
        UnsubscribeEvents();
        _battleManager = null;
        _stageManager = null;
    }

    private void Update()
    {
        // 目标选择模式（旧交互保留：点击角色释放/回车确认）
        if (_isSelectingTarget)
        {
            // 鼠标点击选择（保留）
            if (Input.GetMouseButtonDown(0))
            {
                TrySelectTargetByClick();
            }
            // 键盘游标切换目标（方向键/WASD：左右循环切换，上下切换行）
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
            }
            return;
        }

        // 全局选中中心点：玩家回合等待行动时，方向键随时切换选中目标（无需先进目标选择）
        if (_battleManager != null && _battleManager.IsWaitingForPlayerAction
            && _battleManager.CurrentUnit != null && _battleManager.CurrentUnit.IsPlayerControlled)
        {
            if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
                CycleTarget(1, 0);
            else if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
                CycleTarget(-1, 0);
            else if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W))
                CycleTarget(0, -1);
            else if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S))
                CycleTarget(0, 1);
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
        _battleManager.OnUnitDefend += OnUnitDefend;
        _battleManager.OnUnitTurnEnd += OnUnitTurnEnd;
        _battleManager.OnRoundEnd += OnRoundEnd;
        _battleManager.OnBattleEnd += OnBattleEnd;
        _battleManager.OnBuffApplied += OnBuffApplied;

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
        _battleManager.OnUnitDefend -= OnUnitDefend;
        _battleManager.OnUnitTurnEnd -= OnUnitTurnEnd;
        _battleManager.OnRoundEnd -= OnRoundEnd;
        _battleManager.OnBattleEnd -= OnBattleEnd;
        _battleManager.OnBuffApplied -= OnBuffApplied;

        var presenter = BattlePresenter.Instance;
        if (presenter != null)
        {
            presenter.OnSequenceBegin -= OnSequenceBegin;
            presenter.OnSequenceEnd -= OnSequenceEnd;
        }
    }

    #endregion

    #region 事件回调

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
            // 全局选中中心点：进入玩家回合即默认选中（普攻→第一个存活敌人）
            _selectedSkillId = 0;
            _currentCursorTarget = FindDefaultTarget();
            if (_currentCursorTarget != null && _stageManager != null)
                _stageManager.ShowTargetSelected(_currentCursorTarget);
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

    private void OnUnitDefend(BattleUnit unit)
    {
        if (logPanel != null) logPanel.AddDefendLog(GetUnitName(unit));
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
            skillPanel.Open(_battleManager, OnSkillSelected,
                canUseOnTarget: skillId => IsTargetValidForSkill(_currentCursorTarget, skillId));
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
        if (_battleManager == null || !_battleManager.IsWaitingForPlayerAction) return false;
        var presenter = BattlePresenter.Instance;
        return presenter == null || !presenter.IsPlaying;
    }

    #endregion

    #region 子面板回调

    private void OnSkillSelected(int skillId)
    {
        _selectedSkillId = skillId;

        // targettype=Self 无需选目标，直接对自身释放
        if (skillId > 0 && _battleManager != null)
        {
            var skillCfg = _battleManager.GetTables()?.TbSkill.GetOrDefault(skillId);
            if (skillCfg != null && (TargetType)skillCfg.Targettype == TargetType.Self
                && _battleManager.CurrentUnit != null)
            {
                CloseAllSubPanels();
                _battleManager.PlayerUseSkill(skillId, _battleManager.CurrentUnit.SlotIndex);
                return;
            }
        }

        // 全局选中中心点：直接对当前选中目标释放（无需二次选择）
        var target = _currentCursorTarget;
        if (target == null || !IsTargetValidForSkill(target, skillId))
        {
            if (logPanel != null)
                logPanel.AddLog("当前选中目标无法使用该技能，请先切换目标（方向键/点击角色）");
            return; // 面板保持打开
        }

        CloseAllSubPanels();
        _battleManager.PlayerUseSkill(skillId, target.SlotIndex);
        if (_stageManager != null)
            _stageManager.ShowTargetSelected(target);
        if (logPanel != null && _battleManager.CurrentUnit != null)
        {
            string skillName = skillId == 0 ? "普通攻击" : $"技能{skillId}";
            logPanel.AddDamageLog(GetUnitName(_battleManager.CurrentUnit), GetUnitName(target), 0, skillName);
        }
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
        _isSelectingTarget = true;
        if (logPanel != null)
            logPanel.AddLog("请选择目标：点击 / 方向键切换 / 回车确认（默认选中敌人）");

        // 默认选中一个目标并显示选中标签
        var defaultTarget = FindDefaultTarget();
        _currentCursorTarget = defaultTarget;
        if (defaultTarget != null && _stageManager != null)
            _stageManager.ShowTargetSelected(defaultTarget);
    }

    /// <summary>
    /// 目标选择模式的默认目标：按技能 targettype 决定阵营，取第一个存活单位
    /// </summary>
    private BattleUnit FindDefaultTarget()
    {
        if (_battleManager == null || _battleManager.CurrentUnit == null) return null;

        if (_selectedSkillId > 0)
        {
            var skillCfg = _battleManager.GetTables()?.TbSkill.GetOrDefault(_selectedSkillId);
            if (skillCfg != null)
            {
                var tt = (TargetType)skillCfg.Targettype;
                if (tt == TargetType.Ally)
                    // 治疗/增益默认选中自己（自身必然存活在场，点技能直接对自己生效）
                    return _battleManager.CurrentUnit;
                if (tt == TargetType.Enemy)
                    return _battleManager.EnemyUnits.Find(u => u.IsAlive);
            }
        }

        // 普通攻击/未知类型 → 默认第一个存活敌人
        return _battleManager.EnemyUnits.Find(u => u.IsAlive);
    }

    private void CancelTargetSelection()
    {
        _isSelectingTarget = false;
        _currentCursorTarget = null;
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

        // 执行技能
        _battleManager.PlayerUseSkill(_selectedSkillId, unit.SlotIndex);

        // 显示目标选中标签
        if (_stageManager != null)
            _stageManager.ShowTargetSelected(unit);

        // 记录日志
        if (logPanel != null && _battleManager.CurrentUnit != null)
        {
            string skillName = _selectedSkillId == 0 ? "普通攻击" : $"技能{_selectedSkillId}";
            logPanel.AddDamageLog(GetUnitName(_battleManager.CurrentUnit), GetUnitName(unit), 0, skillName);
        }

        CancelTargetSelection();
    }

    private BattleUnit FindUnitByGameObject(GameObject go)
    {
        if (_battleManager == null) return null;

        // 向上查找父物体名
        Transform current = go.transform;
        while (current != null)
        {
            string name = current.name;
            if (name.StartsWith("Unit_"))
            {
                // 解析 "Unit_{personId}_{name}"
                string[] parts = name.Split('_');
                if (parts.Length >= 2 && int.TryParse(parts[1], out int personId))
                {
                    foreach (var unit in _battleManager.PlayerUnits)
                    {
                        if (unit.PersonId == personId && unit.IsAlive) return unit;
                    }
                    foreach (var unit in _battleManager.EnemyUnits)
                    {
                        if (unit.PersonId == personId && unit.IsAlive) return unit;
                    }
                }
            }
            current = current.parent;
        }
        return null;
    }

    #region 键盘游标切换目标（3.2 动态选择轻量版）

    /// <summary>
    /// 获取当前技能可选目标的存活单位列表（按槽位排序）
    /// </summary>
    private List<BattleUnit> GetValidTargets()
    {
        return GetValidTargetsForSkill(_selectedSkillId);
    }

    /// <summary>
    /// 所有存活单位（玩家+敌人合并），供键盘游标全场循环切换——游标可移动到任意单位（含自己），
    /// 技能的可用性由 IsTargetValidForSkill 置灰表达
    /// </summary>
    private List<BattleUnit> GetAllAliveUnits()
    {
        var result = new List<BattleUnit>();
        if (_battleManager == null) return result;
        foreach (var u in _battleManager.PlayerUnits)
            if (u.IsAlive) result.Add(u);
        foreach (var u in _battleManager.EnemyUnits)
            if (u.IsAlive) result.Add(u);
        return result;
    }

    /// <summary>
    /// 指定技能的可选目标列表（skillId=0 普通攻击→敌人；按 targettype 分阵营；Self→自身）
    /// </summary>
    private List<BattleUnit> GetValidTargetsForSkill(int skillId)
    {
        var result = new List<BattleUnit>();
        if (_battleManager == null || _battleManager.CurrentUnit == null) return result;

        if (skillId > 0)
        {
            var skillCfg = _battleManager.GetTables()?.TbSkill.GetOrDefault(skillId);
            if (skillCfg != null)
            {
                var tt = (TargetType)skillCfg.Targettype;
                if (tt == TargetType.Self)
                {
                    result.Add(_battleManager.CurrentUnit);
                    return result;
                }
                var pool = tt == TargetType.Ally ? _battleManager.PlayerUnits : _battleManager.EnemyUnits;
                foreach (var u in pool)
                {
                    // 治疗/增益类(Ally)目标池包含自己——可对自己释放（引用比较，敌我 SlotIndex 可能相同不能按索引排除）
                    if (!u.IsAlive) continue;
                    result.Add(u);
                }
                result.Sort((a, b) => a.SlotIndex.CompareTo(b.SlotIndex));
                return result;
            }
        }

        // 普通攻击：默认敌人
        foreach (var u in _battleManager.EnemyUnits)
            if (u.IsAlive) result.Add(u);
        result.Sort((a, b) => a.SlotIndex.CompareTo(b.SlotIndex));
        return result;
    }

    /// <summary> 当前选中目标对该技能是否有效（存活 + 在可选列表内） </summary>
    private bool IsTargetValidForSkill(BattleUnit target, int skillId)
    {
        if (target == null || !target.IsAlive) return false;
        return GetValidTargetsForSkill(skillId).Contains(target);
    }

    /// <summary> 键盘游标在当前目标基础上按行列偏移切换（左右循环，上下换行） </summary>
    private void CycleTarget(int colDelta, int rowDelta)
    {
        var targets = GetAllAliveUnits();
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

        _battleManager.PlayerUseSkill(_selectedSkillId, unit.SlotIndex);

        if (_stageManager != null)
            _stageManager.ShowTargetSelected(unit);

        if (logPanel != null && _battleManager.CurrentUnit != null)
        {
            string skillName = _selectedSkillId == 0 ? "普通攻击" : $"技能{_selectedSkillId}";
            logPanel.AddDamageLog(GetUnitName(_battleManager.CurrentUnit), GetUnitName(unit), 0, skillName);
        }

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

    #endregion
}
