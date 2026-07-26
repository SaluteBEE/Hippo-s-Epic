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
    public BattleUnit CurrentUnit;

    public bool IsWaitingForPlayerAction;

    public event Action OnBattleStart;
    public event Action<int> OnRoundStart;
    public event Action<BattleUnit> OnUnitTurnStart;
    public event Action<BattleUnit, UnitTurnPhase> OnUnitPhaseChange;
    public event Action<BattleUnit, BattleAction> OnUnitActionExecute;
    public event Action<BattleUnit> OnUnitTurnEnd;
    public event Action<int> OnRoundEnd;
    public event Action<BattleResult> OnBattleEnd;
    public event Action<BattleUnit, int> OnDamageTaken;
    public event Action<BattleUnit, int> OnHealed;
    public event Action<BattleUnit, int, int> OnBuffApplied;
    public event Action<BattleUnit, int> OnBuffRemoved;
    public event Action<BattleUnit> OnUnitDeath;
    public event Action<BattleUnit> OnUnitDefend;
    public event Action OnPlayerFlee;

    private cfg.cfg.battle.Battle _battleConfig;
    private int _unitIdCounter;

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
        TriggerGlobalBuffs(BuffTrigger.BattleStart);

        Phase = BattlePhase.RoundStart;
        OnBattleStart?.Invoke();

        Debug.Log($"[BattleManager] 战斗初始化完成: {_battleConfig.Name}, 友方{PlayerUnits.Count}人, 敌方{EnemyUnits.Count}人");
        StartRound();
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

        stats.Hp = stats.FinalHpMax;

        return unit;
    }

    #endregion

    #region 回合循环

    public void StartRound()
    {
        RoundCount++;
        Phase = BattlePhase.RoundStart;

        TriggerGlobalBuffs(BuffTrigger.TurnStart);

        var sortedUnits = AllUnits
            .Where(u => u.IsAlive)
            .OrderByDescending(u => u.Stats.FinalSpeed)
            .ThenBy(u => u.IsPlayerSide ? 0 : 1)
            .ThenBy(u => u.SlotIndex)
            .ToList();

        TurnQueue = new Queue<BattleUnit>(sortedUnits);

        OnRoundStart?.Invoke(RoundCount);
        Debug.Log($"[BattleManager] 第{RoundCount}轮开始, 行动顺序: {string.Join(",", sortedUnits.Select(u => $"P{u.PersonId}(S{u.Stats.FinalSpeed})"))}");

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

        unit.CurrentPhase = UnitTurnPhase.TurnStart;
        OnUnitPhaseChange?.Invoke(unit, UnitTurnPhase.TurnStart);
        OnUnitTurnStart?.Invoke(unit);

        TriggerBuffs(unit, BuffTrigger.TurnStart);

        ProcessStatusCheck(unit);
    }

    private void ProcessStatusCheck(BattleUnit unit)
    {
        unit.CurrentPhase = UnitTurnPhase.StatusCheck;
        OnUnitPhaseChange?.Invoke(unit, UnitTurnPhase.StatusCheck);

        if (unit.UnitBuffs.HasStatus(BuffFuncType.Stun) || unit.UnitBuffs.HasStatus(BuffFuncType.Freeze))
        {
            unit.SkipAction = true;
            Debug.Log($"[BattleManager] {unit.PersonId} 被眩晕/冻结，跳过行动");
        }

        var turnStartBuffs = unit.UnitBuffs.GetBuffsByTrigger(BuffTrigger.TurnStart);
        foreach (var buff in turnStartBuffs)
        {
            var tables = GetTables();
            if (tables == null) continue;
            var cfg = tables.TbBuff.GetOrDefault(buff.BuffId);
            if (cfg == null) continue;

            var funcType = (BuffFuncType)cfg.Func;
            if (funcType == BuffFuncType.Heal)
            {
                ApplyHeal(unit, cfg.Param1);
            }
        }

        if (unit.SkipAction)
        {
            EndUnitTurn(unit);
            return;
        }

        EnterActionPhase(unit);
    }

    private void EnterActionPhase(BattleUnit unit)
    {
        unit.CurrentPhase = UnitTurnPhase.Action;
        OnUnitPhaseChange?.Invoke(unit, UnitTurnPhase.Action);

        TriggerBuffs(unit, BuffTrigger.BeforeAction);

        if (unit.IsPlayerControlled)
        {
            IsWaitingForPlayerAction = true;
        }
        else
        {
            var action = BattleAI.Decide(unit, this);
            ExecuteAction(action);
        }
    }

    public void ExecuteAction(BattleAction action)
    {
        if (action == null || action.Actor == null || !action.Actor.IsAlive)
        {
            AdvanceTurnQueue();
            return;
        }

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

        switch (action.Type)
        {
            case ActionType.Skill:
                ExecuteSkill(action.Actor, action.ActionId, targets);
                break;
            case ActionType.Item:
                ExecuteItem(action.Actor, action.ActionId, targets);
                break;
            case ActionType.Defend:
                ExecuteDefend(action.Actor);
                break;
            case ActionType.Flee:
                ExecuteFlee(action.Actor);
                break;
        }

        OnUnitActionExecute?.Invoke(action.Actor, action);

        TriggerBuffs(action.Actor, BuffTrigger.AfterAction);

        CheckDeaths();

        if (Phase == BattlePhase.BattleEnd) return;

        EndUnitTurn(action.Actor);
    }

    private void EndUnitTurn(BattleUnit unit)
    {
        unit.CurrentPhase = UnitTurnPhase.TurnEnd;
        OnUnitPhaseChange?.Invoke(unit, UnitTurnPhase.TurnEnd);

        TriggerBuffs(unit, BuffTrigger.TurnEnd);

        unit.UnitBuffs.OnTurnEnd();

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

        foreach (var unit in AllUnits)
        {
            if (unit.IsAlive)
                unit.DecrementCooldowns();
        }

        TriggerGlobalBuffs(BuffTrigger.TurnEnd);

        OnRoundEnd?.Invoke(RoundCount);
        Debug.Log($"[BattleManager] 第{RoundCount}轮结束");

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
            var normalTargets = GetTargets(TargetType.EnemySingle, CurrentUnit, targetSlotIndex);
            var normalAction = new BattleAction(CurrentUnit, ActionType.Skill, 0,
                normalTargets.Select(t => t.SlotIndex).ToList(), TargetType.EnemySingle);
            ExecuteAction(normalAction);
            return;
        }

        var tables = GetTables();
        if (tables == null) return;

        var skillCfg = tables.TbSkill.GetOrDefault(skillId);
        if (skillCfg == null) return;

        if (CurrentUnit.IsSkillOnCooldown(skillId))
        {
            Debug.LogWarning($"[BattleManager] 技能{skillId}冷却中");
            return;
        }

        var targetType = (TargetType)skillCfg.Targettype;
        var targets = GetTargets(targetType, CurrentUnit, targetSlotIndex);

        if (skillCfg.Cooldown > 0)
            CurrentUnit.SetCooldown(skillId, skillCfg.Cooldown);

        var action = new BattleAction(CurrentUnit, ActionType.Skill, skillId,
            targets.Select(t => t.SlotIndex).ToList(), targetType);
        ExecuteAction(action);
    }

    public void PlayerUseItem(int itemId, int targetSlotIndex)
    {
        if (!IsWaitingForPlayerAction || CurrentUnit == null) return;

        var targets = GetTargets(TargetType.AllySingle, CurrentUnit, targetSlotIndex);
        var action = new BattleAction(CurrentUnit, ActionType.Item, itemId,
            targets.Select(t => t.SlotIndex).ToList(), TargetType.AllySingle);
        ExecuteAction(action);
    }

    public void PlayerDefend()
    {
        if (!IsWaitingForPlayerAction || CurrentUnit == null) return;

        var action = new BattleAction(CurrentUnit, ActionType.Defend, 0, new List<int>(), TargetType.Self);
        ExecuteAction(action);
    }

    public void PlayerFlee()
    {
        if (!IsWaitingForPlayerAction || CurrentUnit == null) return;

        if (_battleConfig != null && !_battleConfig.Canflee)
        {
            Debug.LogWarning("[BattleManager] 此战斗不可逃跑");
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
            foreach (var target in targets)
            {
                if (!target.IsAlive) continue;

                int damage = CalcDamage(actor, target);
                if (target.IsDefending)
                    damage = damage / 2;

                ApplyDamage(target, damage);
                TriggerBuffs(target, BuffTrigger.OnHit);
            }

            Debug.Log($"[BattleManager] {actor.PersonId} 普通攻击");
            return;
        }

        if (tables == null) return;

        var skillCfg = tables.TbSkill.GetOrDefault(skillId);
        if (skillCfg == null) return;

        int level = actor.Stats.SkillLevels.TryGetValue(skillId, out int lv) ? lv : 1;
        int buffId = skillCfg.Buffid + (level - 1);

        foreach (var target in targets)
        {
            if (!target.IsAlive) continue;

            int damage = CalcDamage(actor, target);
            if (target.IsDefending)
                damage = damage / 2;

            ApplyDamage(target, damage);

            if (buffId > 0)
            {
                target.UnitBuffs.ApplyBuff(buffId);
                OnBuffApplied?.Invoke(target, buffId, 0);
            }

            TriggerBuffs(target, BuffTrigger.OnHit);
        }

        Debug.Log($"[BattleManager] {actor.PersonId} 使用技能{skillCfg.Name}({skillId})");
    }

    private void ExecuteItem(BattleUnit actor, int itemId, List<BattleUnit> targets)
    {
        var tables = GetTables();
        if (tables == null) return;

        var itemCfg = tables.TbItem.GetOrDefault(itemId);
        if (itemCfg == null) return;

        int buffId = BuffManager.ParseBuffId(itemCfg.Param1);

        foreach (var target in targets)
        {
            if (!target.IsAlive) continue;

            if (buffId > 0)
            {
                target.UnitBuffs.ApplyBuff(buffId);
                OnBuffApplied?.Invoke(target, buffId, 0);
            }
        }

        BagManager.Instance.RemoveItem(itemId, 1);
        Debug.Log($"[BattleManager] {actor.PersonId} 使用物品{itemCfg.Name}({itemId})");
    }

    private void ExecuteDefend(BattleUnit unit)
    {
        unit.IsDefending = true;
        OnUnitDefend?.Invoke(unit);
        Debug.Log($"[BattleManager] {unit.PersonId} 防御");
    }

    private void ExecuteFlee(BattleUnit unit)
    {
        OnPlayerFlee?.Invoke();
        EndBattle(BattleResult.Flee);
        Debug.Log($"[BattleManager] 玩家逃跑");
    }

    #endregion

    #region 目标选择

    public List<BattleUnit> GetTargets(TargetType type, BattleUnit actor, int selectedSlot)
    {
        bool actorIsPlayer = actor.IsPlayerSide;

        switch (type)
        {
            case TargetType.Self:
                return new List<BattleUnit> { actor };

            case TargetType.EnemySingle:
                return GetAllAliveUnits(!actorIsPlayer)
                    .Where(u => u.SlotIndex == selectedSlot)
                    .ToList();

            case TargetType.EnemyRow:
                return GetRowUnits(!actorIsPlayer, selectedSlot / 3);

            case TargetType.EnemyColumn:
                return GetColumnUnits(!actorIsPlayer, selectedSlot % 3);

            case TargetType.EnemyAll:
                return GetAllAliveUnits(!actorIsPlayer);

            case TargetType.AllySingle:
                return GetAllyAliveUnits(actorIsPlayer)
                    .Where(u => u.SlotIndex == selectedSlot)
                    .ToList();

            case TargetType.AllyAll:
                return GetAllyAliveUnits(actor.IsPlayerSide);

            default:
                return new List<BattleUnit>();
        }
    }

    private List<BattleUnit> ResolveTargets(BattleAction action)
    {
        if (action.Type == ActionType.Defend || action.Type == ActionType.Flee)
            return new List<BattleUnit> { action.Actor };

        return GetTargets(action.TargetType, action.Actor,
            action.TargetSlotIndices.Count > 0 ? action.TargetSlotIndices[0] : 0);
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

    public void ApplyDamage(BattleUnit unit, int damage)
    {
        if (damage <= 0 || !unit.IsAlive) return;

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

        OnDamageTaken?.Invoke(unit, damage);
        Debug.Log($"[BattleManager] {unit.PersonId} 受到伤害{damage}(护盾吸收{shieldAbsorbed}), HP={unit.Stats.Hp}/{unit.Stats.FinalHpMax}");
    }

    public void ApplyHeal(BattleUnit unit, int amount)
    {
        if (amount <= 0 || !unit.IsAlive) return;

        unit.Stats.Hp += amount;
        unit.Stats.ClampHp();

        OnHealed?.Invoke(unit, amount);
        Debug.Log($"[BattleManager] {unit.PersonId} 治疗{amount}, HP={unit.Stats.Hp}/{unit.Stats.FinalHpMax}");
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

            var funcType = (BuffFuncType)cfg.Func;
            switch (funcType)
            {
                case BuffFuncType.Heal:
                    ApplyHeal(unit, cfg.Param1);
                    break;
                case BuffFuncType.CounterDamage:
                    if (trigger == BuffTrigger.OnHit && CurrentUnit != null && CurrentUnit != unit)
                    {
                        int counterDmg = cfg.Param1;
                        ApplyDamage(CurrentUnit, counterDmg);

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
            OnUnitDeath?.Invoke(unit);
            Debug.Log($"[BattleManager] {unit.PersonId} 阵亡");
        }

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
        Debug.Log($"[BattleManager] 战斗结束: {result}");
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
            Debug.Log($"[BattleManager] 获得货币: {_battleConfig.Currencyreward}");
        }

        if (_battleConfig.Lootitems != null)
        {
            foreach (int itemId in _battleConfig.Lootitems)
            {
                BagManager.Instance.AddItem(itemId, 1);
                Debug.Log($"[BattleManager] 获得物品: {itemId}");
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

    public List<BattleUnit> GetAvailableTargetsForSkill(BattleUnit actor, int skillId)
    {
        var tables = GetTables();
        if (tables == null) return new List<BattleUnit>();

        var skillCfg = tables.TbSkill.GetOrDefault(skillId);
        if (skillCfg == null) return new List<BattleUnit>();

        var targetType = (TargetType)skillCfg.Targettype;

        switch (targetType)
        {
            case TargetType.Self:
                return new List<BattleUnit> { actor };
            case TargetType.EnemySingle:
            case TargetType.EnemyRow:
            case TargetType.EnemyColumn:
            case TargetType.EnemyAll:
                return GetAllAliveUnits(!actor.IsPlayerSide);
            case TargetType.AllySingle:
            case TargetType.AllyAll:
                return GetAllyAliveUnits(actor.IsPlayerSide);
            default:
                return new List<BattleUnit>();
        }
    }

    public cfg.Tables GetTables()
    {
        if (ManagerRegistry.TryGet<DataTableManager>(out var dtm) && dtm.Tables != null)
            return dtm.Tables;
        return null;
    }

    #endregion
}
