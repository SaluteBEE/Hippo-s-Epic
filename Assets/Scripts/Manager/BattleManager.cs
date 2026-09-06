using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BattleManager
{
    public BattlePhase Phase = BattlePhase.None;
    public int RoundCount;
    public int BattleId;
    public BattleResult Result = BattleResult.None;

    public List<BattleUnit> AllUnits = new List<BattleUnit>();
    public List<BattleUnit> PlayerUnits = new List<BattleUnit>();
    public List<BattleUnit> EnemyUnits = new List<BattleUnit>();
    public Queue<BattleUnit> TurnQueue = new Queue<BattleUnit>();

    private BattleUnit _currentUnit;
    /// <summary>
    /// 当前行动单位（属性赋值自动推送 OnCurrentUnitChanged）
    /// </summary>
    public BattleUnit CurrentUnit
    {
        get => _currentUnit;
        set
        {
            if (ReferenceEquals(_currentUnit, value)) return;
            _currentUnit = value;
            OnCurrentUnitChanged?.Invoke(value);
        }
    }

    private bool _isWaitingForPlayerAction;
    /// <summary>
    /// 是否等待玩家操作（属性赋值自动推送 OnPlayerActionWaitChanged）
    /// </summary>
    public bool IsWaitingForPlayerAction
    {
        get => _isWaitingForPlayerAction;
        set
        {
            if (_isWaitingForPlayerAction == value) return;
            _isWaitingForPlayerAction = value;
            OnPlayerActionWaitChanged?.Invoke(value);
        }
    }

    public event Action OnBattleStart;
    public event Action<int> OnRoundStart;
    public event Action<BattleUnit> OnUnitTurnStart;
    public event Action<BattleUnit, UnitTurnPhase> OnUnitPhaseChange;
    public event Action<BattleUnit, BattleAction> OnUnitActionExecute;
    public event Action<BattleUnit> OnUnitTurnEnd;
    public event Action<int> OnRoundEnd;
    public event Action<BattleResult> OnBattleEnd;
    public event Action<BattleUnit, BattleUnit, int> OnDamageTaken;   // 攻击方, 受击方, 伤害
    public event Action<BattleUnit, BattleUnit, int> OnHealed;       // 治疗者, 受治方, 治疗量
    public event Action<BattleUnit, int, int> OnBuffApplied;
    public event Action<BattleUnit, int> OnBuffRemoved;
    public event Action<BattleUnit, int, List<BattleUnit>> OnSkillExecuted;
    public event Action<BattleUnit, BattleUnit> OnUnitDeath;   // 击杀方, 阵亡方
    public event Action OnPlayerFlee;
    public event Action<List<BattleUnit>> OnNewUnitsReady;
    public event Action<BattleUnit> OnCurrentUnitChanged;
    public event Action<bool> OnPlayerActionWaitChanged;
    public event Action<List<BattleUnit>, List<BattleUnit>> OnUnitListsChanged;

    public BattleEventQueue EventQueue { get; private set; }

    /// <summary>
    /// 向订阅者推送当前战斗状态快照（窗口打开时请求初始同步用，UI 不主动拉取）
    /// </summary>
    public void PushStateToUI()
    {
        OnCurrentUnitChanged?.Invoke(CurrentUnit);
        OnPlayerActionWaitChanged?.Invoke(IsWaitingForPlayerAction);
        OnUnitListsChanged?.Invoke(PlayerUnits, EnemyUnits);
    }

    private cfg.cfg.battle.Battle _battleConfig;
    private int _unitIdCounter;

    /// <summary> 各单位的最近一次伤害来源（用于阵亡时确定击杀方） </summary>
    private readonly Dictionary<BattleUnit, BattleUnit> _lastKillers = new Dictionary<BattleUnit, BattleUnit>();

    /// <summary>
    /// 测试模式：为玩家单位解锁技能表中所有技能
    /// </summary>
    public bool UnlockAllSkillsForTest;

    /// <summary>
    /// 测试模式：AI 只用普通攻击（不释放技能），便于观察普攻走位/攻击演出
    /// </summary>
    public bool ForceNormalAttackOnly;

    #region 待结算命中（受击表现与伤害逻辑分离）

    /// <summary>
    /// 待结算命中：技能执行时只计算并暂存，由演出在打击时刻统一结算（延迟出伤）
    /// </summary>
    public struct PendingHit
    {
        public BattleUnit Actor;
        public BattleUnit Target;
        public int Amount;
        public bool IsHeal;
        public int BuffId;

        public PendingHit(BattleUnit actor, BattleUnit target, int amount, bool isHeal, int buffId)
        {
            Actor = actor;
            Target = target;
            Amount = amount;
            IsHeal = isHeal;
            BuffId = buffId;
        }
    }

    private readonly List<PendingHit> _pendingHits = new List<PendingHit>();

    /// <summary>
    /// 暂存一次命中（伤害/治疗），不立即扣血；命中buff也一并延迟到打击时刻
    /// </summary>
    public void QueuePendingHit(BattleUnit actor, BattleUnit target, int amount, bool isHeal, int buffId)
    {
        if (target == null || !target.IsAlive) return;
        _pendingHits.Add(new PendingHit(actor, target, amount, isHeal, buffId));
    }

    /// <summary>
    /// 结算所有待结算命中：真正扣血/回血 + 触发OnHit buff（打击时刻调用）
    /// </summary>
    public void ResolvePendingHits()
    {
        if (_pendingHits.Count == 0) return;

        foreach (var hit in _pendingHits)
        {
            if (hit.Target == null || !hit.Target.IsAlive) continue;

            if (hit.IsHeal)
            {
                ApplyHeal(hit.Actor, hit.Target, hit.Amount);
            }
            else
            {
                ApplyDamage(hit.Actor, hit.Target, hit.Amount);
                TriggerBuffs(hit.Target, BuffTrigger.OnHit);
            }

            if (hit.BuffId > 0)
            {
                hit.Target.UnitBuffs.ApplyBuff(hit.BuffId);
                OnBuffApplied?.Invoke(hit.Target, hit.BuffId, 0);
            }
        }

        _pendingHits.Clear();
    }

    #endregion

    #region 初始化

    public void InitBattle(int battleId)
    {
        Phase = BattlePhase.Init;
        BattleId = battleId;
        RoundCount = 0;
        Result = BattleResult.None;
        _unitIdCounter = 0;

        AllUnits.Clear();
        PlayerUnits.Clear();
        EnemyUnits.Clear();
        TurnQueue.Clear();
        CurrentUnit = null;
        IsWaitingForPlayerAction = false;
        _lastKillers.Clear();

        EventQueue = new BattleEventQueue(this);

        var tables = GetTables();
        if (tables == null)
        {
            Debug.LogError("[BattleManager] Tables 未加载");
            return;
        }

        _battleConfig = tables.TbBattle.GetOrDefault(battleId);
        if (_battleConfig == null)
        {
            Debug.LogError($"[BattleManager] 战斗配置不存在: {battleId}");
            return;
        }

        CreateUnits(_battleConfig, tables);
        BattleLogger.Log($" 战斗初始化完成: {_battleConfig.Name}, 友方{PlayerUnits.Count}人, 敌方{EnemyUnits.Count}人");

        OnUnitListsChanged?.Invoke(PlayerUnits, EnemyUnits);
    }

    /// <summary>
    /// 开始战斗：触发BattleStart事件 + 触发Buff + 开始第一轮
    /// 在视觉角色生成完成后调用
    /// </summary>
    public void StartBattle()
    {
        if (Phase != BattlePhase.Init)
        {
            BattleLogger.LogWarning("战斗未初始化，无法开始");
            return;
        }

        TriggerGlobalBuffs(BuffTrigger.BattleStart);
        OnBattleStart?.Invoke();
        BattleLogger.Log(" 战斗开始！");

        StartRound();
    }

    /// <summary>
    /// 添加待入场单位（下一轮开始时才出现，通过事件队列机制）
    /// </summary>
    public void AddPendingUnit(int personId, bool isPlayerSide, int slotIndex)
    {
        EventQueue.Enqueue(BattleEvent.Summon(personId, isPlayerSide, slotIndex, BattleEventPhase.RoundStart));
    }

    /// <summary>
    /// 从事件创建 BattleUnit（供 EventQueue 调用）
    /// </summary>
    public BattleUnit CreateUnitFromEvent(BattleEvent evt)
    {
        var tables = GetTables();
        if (tables == null) return null;

        if (evt.BoolParam && evt.Param1 == 1)
            return CreatePlayerBattleUnit(evt.Param2, tables);
        else
            return CreateBattleUnit(evt.Param1, evt.BoolParam, evt.Param2, tables);
    }

    /// <summary>
    /// 触发技能（供 EventQueue 调用）
    /// </summary>
    public void TriggerSkill(BattleUnit actor, int skillId, List<BattleUnit> targets)
    {
        ExecuteSkill(actor, skillId, targets);
    }

    public void NotifyBuffApplied(BattleUnit unit, int buffId, int param)
    {
        OnBuffApplied?.Invoke(unit, buffId, param);
    }

    public void NotifyBuffRemoved(BattleUnit unit, int buffId)
    {
        OnBuffRemoved?.Invoke(unit, buffId);
    }

    private void CreateUnits(cfg.cfg.battle.Battle config, cfg.Tables tables)
    {
        for (int i = 0; i < config.Enemypersonids.Count && i < 9; i++)
        {
            int personId = config.Enemypersonids[i];
            if (personId <= 0) continue;

            var unit = CreateBattleUnit(personId, false, i, tables);
            if (unit != null)
            {
                EnemyUnits.Add(unit);
                AllUnits.Add(unit);
            }
        }

        for (int i = 0; i < config.Playerpersonids.Count && i < 9; i++)
        {
            int personId = config.Playerpersonids[i];
            if (personId <= 0) continue;

            BattleUnit unit;
            if (personId == 1)
            {
                unit = CreatePlayerBattleUnit(i, tables);
            }
            else
            {
                unit = CreateBattleUnit(personId, true, i, tables);
            }

            if (unit != null)
            {
                PlayerUnits.Add(unit);
                AllUnits.Add(unit);
            }
        }
    }

    private BattleUnit CreateBattleUnit(int personId, bool isPlayerSide, int slotIndex, cfg.Tables tables)
    {
        var stats = CharacterStatsManager.Instance.CreateTempStats(personId);
        if (stats == null) return null;

        var person = tables.TbPerson.GetOrDefault(personId);

        var unit = new BattleUnit
        {
            UnitId = ++_unitIdCounter,
            PersonId = personId,
            IsPlayerSide = isPlayerSide,
            SlotIndex = slotIndex,
            Stats = stats,
            UnitBuffs = new BuffManager(stats),
            IsPlayerControlled = isPlayerSide,
            Speed = stats.FinalSpeed
        };

        if (person != null && person.BuffIds != null)
        {
            foreach (int buffId in person.BuffIds)
            {
                if (buffId > 0)
                    unit.UnitBuffs.ApplyBuff(buffId);
            }
        }

        // 从 person 表读默认技能
        if (person != null && person.Skills != null)
        {
            foreach (int skillId in person.Skills)
            {
                if (skillId > 0 && !unit.AvailableSkills.Contains(skillId))
                    unit.AvailableSkills.Add(skillId);
            }
        }

        return unit;
    }

    private BattleUnit CreatePlayerBattleUnit(int slotIndex, cfg.Tables tables)
    {
        var playerStats = CharacterStatsManager.Instance.PlayerStats;
        if (playerStats == null) return null;

        var stats = new CharacterStats
        {
            Destroy = playerStats.Destroy,
            Selfmatain = playerStats.Selfmatain,
            Creatorwilling = playerStats.Creatorwilling,
            DestroyPlus = playerStats.DestroyPlus,
            SelfmatainPlus = playerStats.SelfmatainPlus,
            CreatorwillingPlus = playerStats.CreatorwillingPlus,
            DestroyPer = playerStats.DestroyPer,
            SelfmatainPer = playerStats.SelfmatainPer,
            CreatorwillingPer = playerStats.CreatorwillingPer,
            SpeedBase = playerStats.SpeedBase,
            SpeedPlus = playerStats.SpeedPlus,
            Hp = playerStats.Hp,
            Level = playerStats.Level,
            Exp = playerStats.Exp,
            SkillPoint = playerStats.SkillPoint,
            ActionPoint = playerStats.ActionPoint,
            ExpBonus = playerStats.ExpBonus,
            CurrencyBonus = playerStats.CurrencyBonus
        };

        stats.UnlockedSkills = new List<int>(playerStats.UnlockedSkills);
        stats.SkillLevels = new Dictionary<int, int>(playerStats.SkillLevels);

        var unit = new BattleUnit
        {
            UnitId = ++_unitIdCounter,
            PersonId = 1,
            IsPlayerSide = true,
            SlotIndex = slotIndex,
            Stats = stats,
            UnitBuffs = new BuffManager(stats),
            IsPlayerControlled = true,
            Speed = stats.FinalSpeed
        };

        foreach (int skillId in stats.UnlockedSkills)
        {
            unit.AvailableSkills.Add(skillId);
        }

        // 测试模式：解锁全部技能
        if (UnlockAllSkillsForTest && tables != null)
        {
            foreach (var skill in tables.TbSkill.DataList)
            {
                if (skill.Id > 0 && !unit.AvailableSkills.Contains(skill.Id))
                    unit.AvailableSkills.Add(skill.Id);
            }
        }

        stats.Hp = stats.FinalHpMax;

        return unit;
    }

    #endregion

    #region 回合循环

    public void StartRound()
    {
        RoundCount++;
        Phase = BattlePhase.RoundStart;
        BattleLogger.Log($" ══════════ 第{RoundCount}轮开始 ══════════");

        // ① 事件队列 — 提取 RoundStart 召唤事件
        var newUnits = EventQueue.ProcessPendingSummons();

        // ② 增援入列（在排序前加入，确保参与本轮排序）
        if (newUnits.Count > 0)
        {
            BattleLogger.Log($" 增援入场: {newUnits.Count}人");
            foreach (var unit in newUnits)
            {
                AllUnits.Add(unit);
                if (unit.IsPlayerSide)
                    PlayerUnits.Add(unit);
                else
                    EnemyUnits.Add(unit);
            }

            OnUnitListsChanged?.Invoke(PlayerUnits, EnemyUnits);
        }

        // ③ 事件队列 — 处理 RoundStart 其他延迟事件
        EventQueue.ProcessPhase(BattleEventPhase.RoundStart);

        // ④ Buff系统 — 全局TurnStart Buff
        TriggerGlobalBuffs(BuffTrigger.TurnStart);

        // ⑤ 排序
        var sortedUnits = AllUnits
            .Where(u => u.IsAlive)
            .OrderByDescending(u => u.Stats.FinalSpeed)
            .ThenBy(u => u.IsPlayerSide ? 0 : 1)
            .ThenBy(u => u.SlotIndex)
            .ToList();

        TurnQueue = new Queue<BattleUnit>(sortedUnits);

        // ⑥ 通知外部（增援视觉 + 回合开始）
        if (newUnits.Count > 0)
            OnNewUnitsReady?.Invoke(newUnits);

        OnRoundStart?.Invoke(RoundCount);
        foreach (var u in sortedUnits)
            BattleLogger.Log($"   行动顺序: P{u.PersonId} Speed={u.Stats.FinalSpeed} Slot={u.SlotIndex} {(u.IsPlayerSide ? "友方" : "敌方")}");

        Phase = BattlePhase.TurnQueue;
        ProcessTurnQueue();
    }

    public void ProcessTurnQueue()
    {
        while (TurnQueue.Count > 0)
        {
            var unit = TurnQueue.Peek();

            if (!unit.IsAlive)
            {
                TurnQueue.Dequeue();
                continue;
            }

            StartUnitTurn(unit);
            return;
        }

        EndRound();
    }

    private void StartUnitTurn(BattleUnit unit)
    {
        CurrentUnit = unit;
        unit.ResetTurnState();
        BattleLogger.Log($" >>> P{unit.PersonId} 开始行动 (Slot={unit.SlotIndex}, HP={unit.Stats.Hp}/{unit.Stats.FinalHpMax})");

        // ① 事件队列 — 该单位TurnStart阶段的延迟事件
        EventQueue.ProcessUnitPhase(BattleEventPhase.TurnStart, unit);

        // ② Buff系统 — TurnStart触发的Buff效果
        TriggerBuffs(unit, BuffTrigger.TurnStart);

        // ③ 状态通知
        unit.CurrentPhase = UnitTurnPhase.TurnStart;
        OnUnitPhaseChange?.Invoke(unit, UnitTurnPhase.TurnStart);
        OnUnitTurnStart?.Invoke(unit);

        ProcessStatusCheck(unit);
    }

    private void ProcessStatusCheck(BattleUnit unit)
    {
        // ① 事件队列 — 该单位StatusCheck阶段的延迟事件
        EventQueue.ProcessUnitPhase(BattleEventPhase.StatusCheck, unit);

        if (unit.UnitBuffs.HasStatus(BuffFuncType.Stun) || unit.UnitBuffs.HasStatus(BuffFuncType.Freeze))
        {
            unit.SkipAction = true;
            BattleLogger.Log($"   P{unit.PersonId} 被眩晕/冻结，跳过行动");
        }

        var turnStartBuffs = unit.UnitBuffs.GetBuffsByTrigger(BuffTrigger.TurnStart);
        foreach (var buff in turnStartBuffs)
        {
            var tables = GetTables();
            if (tables == null) continue;
            var cfg = tables.TbBuff.GetOrDefault(buff.BuffId);
            if (cfg == null) continue;

            var funcType = BuffManager.MapBuffFunc(cfg.Func);
            if (funcType == BuffFuncType.Heal)
            {
                BattleLogger.Log($"   P{unit.PersonId} TurnStart Buff恢复HP+{cfg.Param1}");
                ApplyHeal(unit, unit, cfg.Param1);
            }
        }

        // ② 状态通知
        unit.CurrentPhase = UnitTurnPhase.StatusCheck;
        OnUnitPhaseChange?.Invoke(unit, UnitTurnPhase.StatusCheck);

        if (unit.SkipAction)
        {
            BattleLogger.Log($" <<< P{unit.PersonId} 跳过回合");
            EndUnitTurn(unit);
            return;
        }

        EnterActionPhase(unit);
    }

    private void EnterActionPhase(BattleUnit unit)
    {
        // ① 事件队列 — 该单位Action阶段的延迟事件
        EventQueue.ProcessUnitPhase(BattleEventPhase.Action, unit);

        // ② Buff系统 — Action前的Buff效果
        TriggerBuffs(unit, BuffTrigger.BeforeAction);

        // ③ 状态通知
        unit.CurrentPhase = UnitTurnPhase.Action;
        OnUnitPhaseChange?.Invoke(unit, UnitTurnPhase.Action);

        if (unit.IsPlayerControlled)
        {
            BattleLogger.Log($"   P{unit.PersonId} 等待玩家操作...");
            IsWaitingForPlayerAction = true;
        }
        else
        {
            BattleLogger.Log($"   P{unit.PersonId} AI决策中...");
            var action = BattleAI.Decide(unit, this);
            ExecuteAction(action);
        }
    }

    public void ExecuteAction(BattleAction action)
    {
        if (action == null || action.Actor == null || !action.Actor.IsAlive)
        {
            BattleLogger.Log($"   行动无效(Actor死亡或为空)，跳过");
            AdvanceTurnQueue();
            return;
        }

        BattleLogger.Log($"   P{action.Actor.PersonId} 执行 {action.Type} (id={action.ActionId})");

        IsWaitingForPlayerAction = false;

        var targets = ResolveTargets(action);
        if (targets == null || targets.Count == 0)
        {
            if (action.Type == ActionType.Flee)
            {
                ExecuteFlee(action.Actor);
                return;
            }

            AdvanceTurnQueue();
            return;
        }

        // 行动前 HP 快照（供表现层血条延迟动画使用）
        var beforeHpSnapshot = new Dictionary<BattleUnit, int>();
        foreach (var t in targets)
            beforeHpSnapshot[t] = t.Stats.Hp;

        switch (action.Type)
        {
            case ActionType.Skill:
                ExecuteSkill(action.Actor, action.ActionId, targets);
                break;
            case ActionType.Item:
                ExecuteItem(action.Actor, action.ActionId, targets);
                break;
            case ActionType.Flee:
                ExecuteFlee(action.Actor);
                break;
        }

        // ② Buff系统 — AfterAction Buff
        OnUnitActionExecute?.Invoke(action.Actor, action);
        TriggerBuffs(action.Actor, BuffTrigger.AfterAction);

        // ③ 表现层 — 技能释放演出（演出结束后再推进回合）
        var presenter = BattlePresenter.Instance;
        if (action.Type == ActionType.Skill && presenter != null)
        {
            presenter.PlaySkillSequence(action.Actor, action.ActionId, targets, beforeHpSnapshot, () =>
            {
                CheckDeaths();
                if (Phase == BattlePhase.BattleEnd) return;
                if (!ContinuePlayerTurnIfApLeft(action.Actor))
                    EndUnitTurn(action.Actor);
            });
            return;
        }

        // 无演出（Presenter 不可用）时的兜底：立即结算待结算命中
        ResolvePendingHits();

        CheckDeaths();

        if (Phase == BattlePhase.BattleEnd) return;

        if (!ContinuePlayerTurnIfApLeft(action.Actor))
            EndUnitTurn(action.Actor);
    }

    /// <summary>
    /// 玩家单位行动后仍有剩余行动点 → 不结束回合，重新等待玩家操作（物品/低费技能可连续行动）
    /// </summary>
    private bool ContinuePlayerTurnIfApLeft(BattleUnit unit)
    {
        if (unit == null || !unit.IsPlayerControlled || !unit.IsAlive) return false;
        if (unit.CurrentActionPoints <= 0) return false;

        BattleLogger.Log($"    剩余行动点 {unit.CurrentActionPoints}，继续行动");
        IsWaitingForPlayerAction = true;
        return true;
    }

    private void EndUnitTurn(BattleUnit unit)
    {
        // ① 事件队列 — 该单位TurnEnd阶段的延迟事件
        EventQueue.ProcessUnitPhase(BattleEventPhase.TurnEnd, unit);

        // ② Buff系统 — TurnEnd触发的Buff效果
        TriggerBuffs(unit, BuffTrigger.TurnEnd);
        unit.UnitBuffs.OnTurnEnd();

        // ③ 状态通知
        BattleLogger.Log($" <<< P{unit.PersonId} 回合结束");
        unit.CurrentPhase = UnitTurnPhase.TurnEnd;
        OnUnitPhaseChange?.Invoke(unit, UnitTurnPhase.TurnEnd);
        OnUnitTurnEnd?.Invoke(unit);

        AdvanceTurnQueue();
    }

    private void AdvanceTurnQueue()
    {
        if (Phase == BattlePhase.BattleEnd) return;

        if (TurnQueue.Count > 0)
            TurnQueue.Dequeue();

        ProcessTurnQueue();
    }

    private void EndRound()
    {
        Phase = BattlePhase.RoundEnd;

        // ① 事件队列 — RoundEnd 延迟事件
        EventQueue.ProcessPhase(BattleEventPhase.RoundEnd);

        // ② 日志
        BattleLogger.Log($" ══════════ 第{RoundCount}轮结束 ══════════");

        // ③ 冷却递减
        foreach (var unit in AllUnits)
        {
            if (unit.IsAlive)
                unit.DecrementCooldowns();
        }

        // ④ Buff系统 — 全局TurnEnd Buff
        TriggerGlobalBuffs(BuffTrigger.TurnEnd);

        // ⑤ 通知外部
        OnRoundEnd?.Invoke(RoundCount);

        if (CheckBattleEnd()) return;

        StartRound();
    }

    #endregion

    #region 玩家操作

    public void PlayerUseSkill(int skillId, int targetSlotIndex)
    {
        if (!IsWaitingForPlayerAction || CurrentUnit == null) return;

        if (skillId == 0)
        {
            if (CurrentUnit.CurrentActionPoints < 1)
            {
                BattleLogger.LogWarning(" 行动点不足，无法普通攻击");
                return;
            }
            CurrentUnit.CurrentActionPoints -= 1;

            var normalAction = new BattleAction(CurrentUnit, ActionType.Skill, 0,
                new List<int> { targetSlotIndex }, TargetType.Enemy);
            ExecuteAction(normalAction);
            return;
        }

        var tables = GetTables();
        if (tables == null) return;

        var skillCfg = tables.TbSkill.GetOrDefault(skillId);
        if (skillCfg == null) return;

        if (CurrentUnit.IsSkillOnCooldown(skillId))
        {
            BattleLogger.LogWarning($" 技能{skillId}冷却中");
            return;
        }

        if (CurrentUnit.CurrentActionPoints < skillCfg.Cost)
        {
            BattleLogger.LogWarning($" 行动点不足: 需要{skillCfg.Cost}, 当前{CurrentUnit.CurrentActionPoints}");
            return;
        }

        if (!CanSelectSkillSlot(skillCfg, targetSlotIndex))
        {
            BattleLogger.LogWarning($" 技能{skillId}不可从槽位{targetSlotIndex}释放");
            return;
        }

        var targetType = (TargetType)skillCfg.Targettype;
        var preview = GetSkillTargets(CurrentUnit, skillCfg, targetSlotIndex);
        if (preview.Count == 0)
        {
            BattleLogger.LogWarning($" 技能{skillId}无有效目标");
            return;
        }

        if (skillCfg.Cooldown > 0)
            CurrentUnit.SetCooldown(skillId, skillCfg.Cooldown);

        CurrentUnit.CurrentActionPoints -= skillCfg.Cost;

        // TargetSlotIndices 只存落点中心，range 在 ResolveTargets 时展开
        int centerSlot = targetType == TargetType.Self ? CurrentUnit.SlotIndex : targetSlotIndex;
        var action = new BattleAction(CurrentUnit, ActionType.Skill, skillId,
            new List<int> { centerSlot }, targetType);
        ExecuteAction(action);
    }

    public void PlayerUseItem(int itemId, int targetSlotIndex)
    {
        if (!IsWaitingForPlayerAction || CurrentUnit == null) return;

        var tables = GetTables();
        var itemCfg = tables?.TbItem.GetOrDefault(itemId);
        if (itemCfg == null) return;

        // func：1=战斗内对己方使用 → Ally，2=战斗内对敌方使用 → Enemy
        int func = itemCfg.Func != null && itemCfg.Func.Count > 0 ? itemCfg.Func[0] : 0;
        if (func != 1 && func != 2)
        {
            BattleLogger.LogWarning($"[BattleManager] 物品 {itemId} 无战斗功能(func={func})，无法使用");
            return;
        }
        var targetType = func == 1 ? TargetType.Ally : TargetType.Enemy;

        // param2：小键盘范围（键5=本体中心），以选中槽位为落点中心展开命中槽位
        var hitSlots = SkillCombatUtil.ExpandRangeSlots(targetSlotIndex, itemCfg.Param2);
        if (hitSlots.Count == 0)
            hitSlots.Add(targetSlotIndex);

        var targets = GetTargetsBySide(targetType, CurrentUnit, hitSlots);
        if (targets.Count == 0)
        {
            BattleLogger.Log($"   P{CurrentUnit.PersonId} 物品{itemCfg.Name}范围内无有效目标");
            return;
        }

        if (CurrentUnit.CurrentActionPoints < 1)
        {
            BattleLogger.Log($"   P{CurrentUnit.PersonId} 行动点不足，无法使用物品");
            return;
        }
        CurrentUnit.CurrentActionPoints -= 1;  // 物品使用固定消耗 1 行动点

        var action = new BattleAction(CurrentUnit, ActionType.Item, itemId, hitSlots, targetType);
        ExecuteAction(action);
    }

    /// <summary>
    /// 物品可作落点的有效单位列表（func 决定阵营：1=对己方/2=对敌方；全部存活单位均可作范围中心）
    /// </summary>
    public List<BattleUnit> GetAvailableTargetsForItem(BattleUnit actor, int itemId)
    {
        var result = new List<BattleUnit>();
        var tables = GetTables();
        var itemCfg = tables?.TbItem.GetOrDefault(itemId);
        if (itemCfg == null || actor == null) return result;

        int func = itemCfg.Func != null && itemCfg.Func.Count > 0 ? itemCfg.Func[0] : 0;
        if (func != 1 && func != 2) return result;

        bool targetSide = func == 1 ? actor.IsPlayerSide : !actor.IsPlayerSide;
        var list = targetSide ? PlayerUnits : EnemyUnits;
        foreach (var unit in list)
        {
            if (unit.IsAlive)
                result.Add(unit);
        }
        return result;
    }

    /// <summary>
    /// 背包中是否存在可用战斗道具（func 含 1=战斗内对己方 / 2=战斗内对敌方）
    /// </summary>
    public bool HasUsableBattleItems()
    {
        var bagManager = BagManager.Instance;
        if (bagManager == null) return false;

        var tables = GetTables();
        if (tables == null) return false;

        foreach (var entry in bagManager.AllItems)
        {
            if (entry.count <= 0) continue;

            var itemCfg = tables.TbItem.GetOrDefault(entry.itemId);
            if (itemCfg == null) continue;

            if (itemCfg.Func != null && (itemCfg.Func.Contains(1) || itemCfg.Func.Contains(2)))
                return true;
        }

        return false;
    }

    /// <summary>
    /// 后移回合：当前角色排到队尾，让后一位角色先行
    /// </summary>
    public void PlayerDeferTurn()
    {
        if (!IsWaitingForPlayerAction || CurrentUnit == null) return;
        IsWaitingForPlayerAction = false;

        TurnQueue.Dequeue();           // 移除当前单位
        TurnQueue.Enqueue(CurrentUnit); // 放到队尾

        BattleLogger.Log($"   P{CurrentUnit.PersonId} 后移回合，排到队尾");
        OnUnitTurnEnd?.Invoke(CurrentUnit);

        ProcessTurnQueue();
    }

    public void PlayerFlee()
    {
        if (!IsWaitingForPlayerAction || CurrentUnit == null) return;

        if (_battleConfig != null && !_battleConfig.Canflee)
        {
            BattleLogger.LogWarning("此战斗不可逃跑");
            return;
        }

        var action = new BattleAction(CurrentUnit, ActionType.Flee, 0, new List<int>(), TargetType.Self);
        ExecuteAction(action);
    }

    #endregion

    #region 行动执行

    private void ExecuteSkill(BattleUnit actor, int skillId, List<BattleUnit> targets)
    {
        var tables = GetTables();

        if (skillId <= 0)
        {
            BattleLogger.Log($"    P{actor.PersonId} 普通攻击: {targets.Count}目标");
            foreach (var target in targets)
            {
                if (!target.IsAlive) continue;

                int damage = CalcDamage(actor, target);
                QueuePendingHit(actor, target, damage, false, 0);
            }

            OnSkillExecuted?.Invoke(actor, skillId, targets);
            return;
        }

        if (tables == null) return;

        var skillCfg = tables.TbSkill.GetOrDefault(skillId);
        if (skillCfg == null) return;

        var skillType = (SkillType)skillCfg.Skilltype;
        int effectValue = SkillCombatUtil.CalcSkillValue(actor.Stats, skillCfg.Dmgfunc, skillCfg.Effectparam);
        int buffId = skillCfg.Buffid;

        BattleLogger.Log($"    P{actor.PersonId} 使用技能 {skillCfg.Name}({skillId}) type={skillType} value={effectValue} targets={targets.Count}");

        foreach (var target in targets)
        {
            if (!target.IsAlive) continue;

            switch (skillType)
            {
                case SkillType.Heal:
                    QueuePendingHit(actor, target, effectValue, true, buffId);
                    break;

                case SkillType.PhysicalDamage:
                case SkillType.MagicDamage:
                {
                    int damage = effectValue;
                    QueuePendingHit(actor, target, damage, false, buffId);
                    break;
                }

                case SkillType.BuffOnly:
                    BattleLogger.Log($"    P{actor.PersonId} 使用 {skillCfg.Name}: 附加Buff id={buffId} → P{target.PersonId}");
                    if (buffId > 0)
                    {
                        target.UnitBuffs.ApplyBuff(buffId);
                        OnBuffApplied?.Invoke(target, buffId, 0);
                    }
                    break;
                default:
                    break;
            }

        }

        OnSkillExecuted?.Invoke(actor, skillId, targets);
    }

    private void ExecuteItem(BattleUnit actor, int itemId, List<BattleUnit> targets)
    {
        var tables = GetTables();
        if (tables == null) return;

        var itemCfg = tables.TbItem.GetOrDefault(itemId);
        if (itemCfg == null) return;

        // 物品战斗效果：param1=效果类型列表，param3=对应数值/buff id（一一对应）
        //   1=一次性治疗（param3=治疗量）  2=一次性物理伤害（param3=伤害量）  3=给目标加BUFF（param3=buff id）
        var effects = itemCfg.Param1;
        var values = itemCfg.Param3;

        foreach (var target in targets)
        {
            if (!target.IsAlive) continue;

            for (int i = 0; i < effects.Count; i++)
            {
                int value = (values != null && i < values.Count) ? values[i] : 0;
                switch (effects[i])
                {
                    case 1: // 治疗
                        ApplyHeal(actor, target, value);
                        break;
                    case 2: // 物理伤害
                        ApplyDamage(actor, target, value);
                        break;
                    case 3: // 加BUFF
                        if (value > 0)
                        {
                            target.UnitBuffs.ApplyBuff(value);
                            OnBuffApplied?.Invoke(target, value, 0);
                            BattleLogger.Log($"   P{target.PersonId} 获得Buff id={value}");
                        }
                        break;
                    default:
                        BattleLogger.LogWarning($"   未知物品效果类型 {effects[i]}");
                        break;
                }
            }
        }

        BagManager.Instance.RemoveItem(itemId, 1);
        BattleLogger.Log($" {actor.PersonId} 使用物品{itemCfg.Name}({itemId})");
    }

    private void ExecuteFlee(BattleUnit unit)
    {
        OnPlayerFlee?.Invoke();
        EndBattle(BattleResult.Flee);
        BattleLogger.Log($" 玩家逃跑");
    }

    #endregion

    #region 目标选择

    /// <summary>
    /// 按技能配置解析命中单位（selectedSlot 为落点中心，对应 range 中的键5）
    /// </summary>
    public List<BattleUnit> GetSkillTargets(BattleUnit actor, cfg.cfg.skill.Skill skillCfg, int selectedSlot)
    {
        if (actor == null || skillCfg == null)
            return new List<BattleUnit>();

        var targetType = (TargetType)skillCfg.Targettype;

        if (targetType == TargetType.Self)
            return actor.IsAlive ? new List<BattleUnit> { actor } : new List<BattleUnit>();

        if (!CanSelectSkillSlot(skillCfg, selectedSlot))
            return new List<BattleUnit>();

        var hitSlots = SkillCombatUtil.ExpandRangeSlots(selectedSlot, skillCfg.Range);
        return GetTargetsBySide(targetType, actor, hitSlots);
    }

    public bool CanSelectSkillSlot(cfg.cfg.skill.Skill skillCfg, int selectedSlot)
    {
        if (skillCfg == null) return false;
        if ((TargetType)skillCfg.Targettype == TargetType.Self)
            return true;
        return SkillCombatUtil.IsSlotSelectable(selectedSlot, skillCfg.Selectable);
    }

    public List<BattleUnit> GetTargetsBySide(TargetType type, BattleUnit actor, IList<int> slotIndices)
    {
        var result = new List<BattleUnit>();
        if (actor == null || slotIndices == null) return result;

        switch (type)
        {
            case TargetType.Self:
                if (actor.IsAlive)
                    result.Add(actor);
                return result;

            case TargetType.Ally:
            {
                foreach (int slot in slotIndices)
                {
                    var unit = GetUnitAtSlot(actor.IsPlayerSide, slot);
                    if (unit != null && unit.IsAlive)
                        result.Add(unit);
                }
                return result;
            }

            case TargetType.Enemy:
            {
                foreach (int slot in slotIndices)
                {
                    var unit = GetUnitAtSlot(!actor.IsPlayerSide, slot);
                    if (unit != null && unit.IsAlive)
                        result.Add(unit);
                }
                return result;
            }

            default:
                return result;
        }
    }

    /// <summary>兼容旧调用：单槽目标</summary>
    public List<BattleUnit> GetTargets(TargetType type, BattleUnit actor, int selectedSlot)
    {
        if (type == TargetType.Self)
            return actor != null && actor.IsAlive ? new List<BattleUnit> { actor } : new List<BattleUnit>();
        return GetTargetsBySide(type, actor, new List<int> { selectedSlot });
    }

    private List<BattleUnit> ResolveTargets(BattleAction action)
    {
        if (action.Type == ActionType.Flee)
            return new List<BattleUnit> { action.Actor };

        // 技能：TargetSlotIndices[0] 为落点中心；配置技能按 range 展开，普通攻击打单格
        if (action.Type == ActionType.Skill)
        {
            int center = action.TargetSlotIndices != null && action.TargetSlotIndices.Count > 0
                ? action.TargetSlotIndices[0]
                : action.Actor.SlotIndex;

            if (action.ActionId > 0)
            {
                var tables = GetTables();
                var skillCfg = tables?.TbSkill.GetOrDefault(action.ActionId);
                if (skillCfg != null)
                    return GetSkillTargets(action.Actor, skillCfg, center);
            }

            return GetTargetsBySide(action.TargetType, action.Actor, new List<int> { center });
        }

        if (action.TargetSlotIndices == null || action.TargetSlotIndices.Count == 0)
        {
            if (action.TargetType == TargetType.Self && action.Actor != null)
                return new List<BattleUnit> { action.Actor };
            return new List<BattleUnit>();
        }

        return GetTargetsBySide(action.TargetType, action.Actor, action.TargetSlotIndices);
    }

    public List<BattleUnit> GetRowUnits(bool playerSide, int row)
    {
        var units = playerSide ? PlayerUnits : EnemyUnits;
        return units.Where(u => u.IsAlive && u.GetRow() == row).ToList();
    }

    public List<BattleUnit> GetColumnUnits(bool playerSide, int column)
    {
        var units = playerSide ? PlayerUnits : EnemyUnits;
        return units.Where(u => u.IsAlive && u.GetColumn() == column).ToList();
    }

    public List<BattleUnit> GetAllAliveUnits(bool playerSide)
    {
        var units = playerSide ? PlayerUnits : EnemyUnits;
        return units.Where(u => u.IsAlive).ToList();
    }

    public List<BattleUnit> GetAllyAliveUnits(bool playerSide)
    {
        return GetAllAliveUnits(playerSide);
    }

    public List<BattleUnit> GetEnemyAliveUnits()
    {
        return EnemyUnits.Where(u => u.IsAlive).ToList();
    }

    #endregion

    #region 伤害计算

    public int CalcDamage(BattleUnit attacker, BattleUnit defender)
    {
        int raw = attacker.Stats.FinalDestroy - defender.Stats.FinalDestroy / 2;
        return Mathf.Max(0, raw);
    }

    public void ApplyDamage(BattleUnit actor, BattleUnit unit, int damage)
    {
        if (damage <= 0 || !unit.IsAlive) return;

        // 记录伤害来源（阵亡时用于确定击杀方）
        _lastKillers[unit] = actor;

        int shieldAbsorbed = unit.UnitBuffs.AbsorbDamage(damage);
        int remaining = damage - shieldAbsorbed;

        if (remaining > 0)
        {
            unit.Stats.Hp -= remaining;
            if (unit.Stats.Hp <= 0)
            {
                unit.Stats.Hp = 0;
                unit.IsAlive = false;
            }
        }

        OnDamageTaken?.Invoke(actor, unit, damage);
        var absorbMsg = shieldAbsorbed > 0 ? string.Format(" [护盾吸收{0}]", shieldAbsorbed) : "";
        var beforeHp = unit.Stats.Hp + remaining + shieldAbsorbed;
        BattleLogger.Log(string.Format("    P{0} HP: {1} -> {2} (-{3}){4}", unit.PersonId, beforeHp, unit.Stats.Hp, damage, absorbMsg));
    }

    public void ApplyHeal(BattleUnit actor, BattleUnit unit, int amount)
    {
        if (amount <= 0 || !unit.IsAlive) return;

        unit.Stats.Hp += amount;
        unit.Stats.ClampHp();

        OnHealed?.Invoke(actor, unit, amount);
        BattleLogger.Log($"    P{unit.PersonId} 治疗+{amount}, HP={unit.Stats.Hp}/{unit.Stats.FinalHpMax}");
    }

    #endregion

    #region Buff触发

    public void TriggerBuffs(BattleUnit unit, BuffTrigger trigger)
    {
        var buffs = unit.UnitBuffs.GetBuffsByTrigger(trigger);
        var tables = GetTables();
        if (tables == null) return;

        foreach (var buff in buffs)
        {
            var cfg = tables.TbBuff.GetOrDefault(buff.BuffId);
            if (cfg == null) continue;

            var funcType = BuffManager.MapBuffFunc(cfg.Func);
            switch (funcType)
            {
                case BuffFuncType.Heal:
                    ApplyHeal(unit, unit, cfg.Param1);
                    break;
                case BuffFuncType.AoeDamage:
                    // 回合结束随机对一个存活敌人造成伤害（如「尿了」buff）
                    if (trigger == BuffTrigger.TurnEnd)
                    {
                        var enemies = unit.IsPlayerSide ? EnemyUnits : PlayerUnits;
                        var aliveEnemies = enemies.FindAll(u => u.IsAlive);
                        if (aliveEnemies.Count > 0)
                        {
                            var victim = aliveEnemies[UnityEngine.Random.Range(0, aliveEnemies.Count)];
                            BattleLogger.Log($"   P{unit.PersonId} 回合结束AOE 对 P{victim.PersonId} 造成 {cfg.Param1} 伤害");
                            ApplyDamage(unit, victim, cfg.Param1);
                        }
                    }
                    break;
                case BuffFuncType.CounterDamage:
                    if (trigger == BuffTrigger.OnHit && CurrentUnit != null && CurrentUnit != unit)
                    {
                        int counterDmg = cfg.Param1;
                        ApplyDamage(unit, CurrentUnit, counterDmg);

                        if (cfg.Param2 > 0)
                        {
                            CurrentUnit.UnitBuffs.ApplyBuff(cfg.Param2);
                        }
                    }
                    break;
            }
        }
    }

    public void TriggerGlobalBuffs(BuffTrigger trigger)
    {
        foreach (var unit in AllUnits.Where(u => u.IsAlive))
        {
            TriggerBuffs(unit, trigger);
        }
    }

    #endregion

    #region 胜负判定

    public bool CheckBattleEnd()
    {
        if (!IsPlayerSideAlive())
        {
            EndBattle(BattleResult.Lose);
            return true;
        }

        if (!IsEnemySideAlive())
        {
            EndBattle(BattleResult.Win);
            return true;
        }

        return false;
    }

    public bool IsPlayerSideAlive()
    {
        return PlayerUnits.Any(u => u.IsAlive);
    }

    public bool IsEnemySideAlive()
    {
        return EnemyUnits.Any(u => u.IsAlive);
    }

    private void CheckDeaths()
    {
        foreach (var unit in AllUnits.Where(u => !u.IsAlive && u.Stats.Hp <= 0))
        {
            _lastKillers.TryGetValue(unit, out var killer);
            OnUnitDeath?.Invoke(killer, unit);
            BattleLogger.Log($"    P{unit.PersonId} 阵亡！");
        }

        OnUnitListsChanged?.Invoke(PlayerUnits, EnemyUnits);
        CheckBattleEnd();
    }

    #endregion

    #region 战斗结算

    public void EndBattle(BattleResult result)
    {
        Phase = BattlePhase.BattleEnd;
        Result = result;

        TriggerGlobalBuffs(BuffTrigger.BattleEnd);

        if (result == BattleResult.Win)
        {
            ApplyBattleRewards();
        }

        OnBattleEnd?.Invoke(result);
        BattleLogger.Log($" ══════════ 战斗结束: {result} ══════════");
    }

    private void ApplyBattleRewards()
    {
        if (_battleConfig == null) return;

        if (_battleConfig.Expreward > 0)
        {
            CharacterStatsManager.Instance.AddExp(_battleConfig.Expreward);
        }

        if (_battleConfig.Currencyreward > 0)
        {
            BattleLogger.Log($" 获得货币: {_battleConfig.Currencyreward}");
        }

        if (_battleConfig.Lootitems != null)
        {
            foreach (int itemId in _battleConfig.Lootitems)
            {
                BagManager.Instance.AddItem(itemId, 1);
                BattleLogger.Log($" 获得物品: {itemId}");
            }
        }
    }

    #endregion

    #region 工具方法

    public BattleUnit GetUnitAtSlot(bool playerSide, int slotIndex)
    {
        var units = playerSide ? PlayerUnits : EnemyUnits;
        return units.FirstOrDefault(u => u.SlotIndex == slotIndex && u.IsAlive);
    }

    /// <summary>
    /// 返回可作为技能落点中心的存活单位（受 selectable 与 targettype 约束）
    /// </summary>
    public List<BattleUnit> GetAvailableTargetsForSkill(BattleUnit actor, int skillId)
    {
        var tables = GetTables();
        if (tables == null) return new List<BattleUnit>();

        var skillCfg = tables.TbSkill.GetOrDefault(skillId);
        if (skillCfg == null) return new List<BattleUnit>();

        var targetType = (TargetType)skillCfg.Targettype;
        if (targetType == TargetType.Self)
            return actor != null && actor.IsAlive ? new List<BattleUnit> { actor } : new List<BattleUnit>();

        bool playerSide = targetType == TargetType.Ally ? actor.IsPlayerSide : !actor.IsPlayerSide;
        var candidates = GetAllAliveUnits(playerSide);
        var result = new List<BattleUnit>();

        foreach (var unit in candidates)
        {
            if (!CanSelectSkillSlot(skillCfg, unit.SlotIndex))
                continue;

            // 落点必须至少能打到一个存活单位
            if (GetSkillTargets(actor, skillCfg, unit.SlotIndex).Count > 0)
                result.Add(unit);
        }

        return result;
    }

    public cfg.Tables GetTables()
    {
        if (ManagerRegistry.TryGet<DataTableManager>(out var dtm) && dtm.Tables != null)
            return dtm.Tables;
        return null;
    }

    #endregion
}
