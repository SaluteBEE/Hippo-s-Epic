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
            SetActionButtonsVisible(true);
    }

    public override void OnClose()
    {
        UnsubscribeEvents();
        _battleManager = null;
        _stageManager = null;
    }

    private void Update()
    {
        // 目标选择模式：检测点击场景中的角色
        if (_isSelectingTarget && Input.GetMouseButtonDown(0))
        {
            TrySelectTargetByClick();
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
            _stageManager.HighlightCurrentUnit(unit);
    }

    private void OnUnitPhaseChange(BattleUnit unit, UnitTurnPhase phase)
    {
        if (phase == UnitTurnPhase.Action && unit.IsPlayerControlled)
        {
            SetActionButtonsVisible(true);
        }
    }

    private void OnDamageTaken(BattleUnit unit, int damage)
    {
        // 更新场景血条
        if (_stageManager != null)
            _stageManager.UpdateHealthBar(unit);
    }

    private void OnHealed(BattleUnit unit, int amount)
    {
        if (_stageManager != null)
            _stageManager.UpdateHealthBar(unit);
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

    private void OnBattleEnd(BattleResult result)
    {
        SetActionButtonsVisible(false);
        CloseAllSubPanels();
        CancelTargetSelection();

        if (_stageManager != null)
            _stageManager.ClearHighlight();

        if (logPanel != null)
        {
            switch (result)
            {
                case BattleResult.Win: logPanel.AddLog("战斗胜利！"); break;
                case BattleResult.Lose: logPanel.AddLog("战斗失败..."); break;
                case BattleResult.Flee: logPanel.AddLog("成功逃跑！"); break;
            }
        }

        // 战斗结束自动返回
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
            skillPanel.Open(_battleManager, OnSkillSelected);
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

    /// <summary> 正在发呆 → 跳过回合（防御） </summary>
    private void OnIdleClicked()
    {
        if (!CanOperate()) return;
        _battleManager.PlayerDefend();
    }

    private bool CanOperate()
    {
        return _battleManager != null && _battleManager.IsWaitingForPlayerAction;
    }

    #endregion

    #region 子面板回调

    private void OnSkillSelected(int skillId)
    {
        CloseAllSubPanels();
        _selectedSkillId = skillId;
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
        _isSelectingTarget = true;
    }

    private void CancelTargetSelection()
    {
        _isSelectingTarget = false;
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
