using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum BuffFuncType
{
    None = 0,
    LootBonus = 1,
    DamageUp = 2,
    AoeDamage = 3,
    SelfmatainUp = 4,
    CreatorwillingUp = 5,
    SelfmatainPerUp = 6,
    CreatorwillingPerUp = 7,
    DestroyPerUp = 8,
    SpeedUp = 9,
    Heal = 10,
    Shield = 11,
    CounterDamage = 12,
    Stun = 13,
    Freeze = 14
}

public enum BuffTrigger
{
    Passive = 0,
    TurnStart = 1,
    TurnEnd = 2,
    BeforeAction = 3,
    AfterAction = 4,
    OnHit = 5,
    OnKill = 6,
    BattleStart = 7,
    BattleEnd = 8
}

public class BuffManager
{
    private static BuffManager _instance;
    public static BuffManager Instance => _instance ??= new BuffManager();

    private readonly List<ActiveBuff> _playerBuffs = new List<ActiveBuff>();
    private readonly CharacterStats _targetStats;

    public IReadOnlyList<ActiveBuff> ActiveBuffs => _playerBuffs;

    public event Action<int, bool> OnBuffChanged;

    public BuffManager()
    {
        _targetStats = null;
    }

    public BuffManager(CharacterStats targetStats)
    {
        _targetStats = targetStats;
    }

    public class ActiveBuff
    {
        public int BuffId;
        public int RemainingTurns;
        public int RemainingUses;
        public bool IsFromEquip;
        public bool IsFromSkill;
        public int ShieldAmount;

        public bool IsPermanent => RemainingTurns <= 0 && RemainingUses <= 0;
    }

    [Serializable]
    public class BuffSaveEntry
    {
        public int buffId;
        public int remainingTurns;
        public int remainingUses;
        public bool isFromEquip;
        public bool isFromSkill;
    }

    public void ApplyBuff(int buffId, bool fromEquip = false, bool fromSkill = false)
    {
        if (HasBuff(buffId))
        {
            Debug.LogWarning($"[BuffManager] Buff {buffId} 已存在，跳过施加");
            return;
        }

        var tables = GetTables();
        if (tables == null) return;

        var buffCfg = tables.TbBuff.GetOrDefault(buffId);
        if (buffCfg == null)
        {
            Debug.LogWarning($"[BuffManager] Buff配置不存在: {buffId}");
            return;
        }

        var activeBuff = new ActiveBuff
        {
            BuffId = buffId,
            RemainingTurns = buffCfg.Time,
            RemainingUses = buffCfg.Num,
            IsFromEquip = fromEquip,
            IsFromSkill = fromSkill
        };

        _playerBuffs.Add(activeBuff);
        ApplyBuffEffects(buffCfg);
        OnBuffChanged?.Invoke(buffId, true);

        Debug.Log($"[BuffManager] 施加Buff: {buffCfg.Name}(id={buffId}), fromEquip={fromEquip}, fromSkill={fromSkill}");
    }

    public void RemoveBuff(int buffId)
    {
        int idx = _playerBuffs.FindIndex(b => b.BuffId == buffId);
        if (idx < 0)
        {
            Debug.LogWarning($"[BuffManager] Buff {buffId} 不存在，跳过移除");
            return;
        }

        _playerBuffs.RemoveAt(idx);

        var tables = GetTables();
        if (tables != null)
        {
            var buffCfg = tables.TbBuff.GetOrDefault(buffId);
            if (buffCfg != null)
                RemoveBuffEffects(buffCfg);
        }

        OnBuffChanged?.Invoke(buffId, false);
        Debug.Log($"[BuffManager] 移除Buff: {buffId}");
    }

    public void RemoveAllEquipBuffs()
    {
        var equipBuffs = _playerBuffs.Where(b => b.IsFromEquip).ToList();
        foreach (var buff in equipBuffs)
        {
            RemoveBuff(buff.BuffId);
        }
    }

    public void RemoveAllSkillBuffs()
    {
        var skillBuffs = _playerBuffs.Where(b => b.IsFromSkill).ToList();
        foreach (var buff in skillBuffs)
        {
            RemoveBuff(buff.BuffId);
        }
    }

    public bool HasBuff(int buffId)
    {
        return _playerBuffs.Any(b => b.BuffId == buffId);
    }

    public void OnTurnEnd()
    {
        var toRemove = new List<ActiveBuff>();

        foreach (var buff in _playerBuffs)
        {
            if (buff.IsFromEquip || buff.IsFromSkill)
                continue;

            if (buff.RemainingTurns > 0)
            {
                buff.RemainingTurns--;
                if (buff.RemainingTurns <= 0)
                    toRemove.Add(buff);
            }
        }

        foreach (var buff in toRemove)
        {
            RemoveBuff(buff.BuffId);
        }
    }

    public void OnBattleStart()
    {
    }

    public void OnBattleEnd()
    {
    }

    public void ReapplyEquipBuffs()
    {
        RemoveAllEquipBuffs();

        var equip = EquipManager.Instance;
        foreach (var kvp in equip.Equips)
        {
            if (kvp.Value.itemId <= 0) continue;

            var itemCfg = GetItemConfig(kvp.Value.itemId);
            if (itemCfg == null) continue;

            int buffId = ParseBuffId(itemCfg.Param1);
            if (buffId > 0)
                ApplyBuff(buffId, fromEquip: true);
        }
    }

    public void ReapplySkillBuffs()
    {
        RemoveAllSkillBuffs();

        var stats = CharacterStatsManager.Instance.PlayerStats;
        if (stats == null) return;

        var tables = GetTables();
        if (tables == null) return;

        foreach (int skillId in stats.UnlockedSkills)
        {
            var skillCfg = tables.TbSkill.GetOrDefault(skillId);
            if (skillCfg == null) continue;

            int level = stats.SkillLevels.TryGetValue(skillId, out int lv) ? lv : 1;
            int buffId = skillCfg.Buffid + (level - 1);
            ApplyBuff(buffId, fromSkill: true);
        }
    }

    public List<BuffSaveEntry> BuildSaveData()
    {
        var result = new List<BuffSaveEntry>(_playerBuffs.Count);
        foreach (var buff in _playerBuffs)
        {
            if (buff.IsFromEquip || buff.IsFromSkill)
                continue;

            result.Add(new BuffSaveEntry
            {
                buffId = buff.BuffId,
                remainingTurns = buff.RemainingTurns,
                remainingUses = buff.RemainingUses,
                isFromEquip = false,
                isFromSkill = false
            });
        }
        return result;
    }

    public void RestoreFromSaveData(List<BuffSaveEntry> data)
    {
        _playerBuffs.Clear();

        if (data == null) return;

        var tables = GetTables();

        foreach (var entry in data)
        {
            if (entry.isFromEquip || entry.isFromSkill)
                continue;

            _playerBuffs.Add(new ActiveBuff
            {
                BuffId = entry.buffId,
                RemainingTurns = entry.remainingTurns,
                RemainingUses = entry.remainingUses,
                IsFromEquip = false,
                IsFromSkill = false
            });

            if (tables != null)
            {
                var buffCfg = tables.TbBuff.GetOrDefault(entry.buffId);
                if (buffCfg != null)
                    ApplyBuffEffects(buffCfg);
            }
        }
    }

    public void Clear()
    {
        _playerBuffs.Clear();
    }

    private void ApplyBuffEffects(cfg.cfg.buff.Buff buffCfg)
    {
        var funcType = (BuffFuncType)buffCfg.Func;
        int param = buffCfg.Param1;

        if (_targetStats != null)
        {
            ApplyBuffEffectsToStats(_targetStats, funcType, param);
            return;
        }

        var statsMgr = CharacterStatsManager.Instance;

        switch (funcType)
        {
            case BuffFuncType.LootBonus:
                statsMgr.ModifyPer(AttrType.Destroy, param * 10000);
                break;
            case BuffFuncType.DamageUp:
                statsMgr.ModifyPlus(AttrType.Destroy, param);
                break;
            case BuffFuncType.SelfmatainUp:
                statsMgr.ModifyPlus(AttrType.Selfmatain, param);
                break;
            case BuffFuncType.CreatorwillingUp:
                statsMgr.ModifyPlus(AttrType.Creatorwilling, param);
                break;
            case BuffFuncType.SelfmatainPerUp:
                statsMgr.ModifyPer(AttrType.Selfmatain, param * 10000);
                break;
            case BuffFuncType.CreatorwillingPerUp:
                statsMgr.ModifyPer(AttrType.Creatorwilling, param * 10000);
                break;
            case BuffFuncType.DestroyPerUp:
                statsMgr.ModifyPer(AttrType.Destroy, param * 10000);
                break;
            case BuffFuncType.SpeedUp:
                statsMgr.ModifyPlus(AttrType.Speed, param);
                break;
            case BuffFuncType.Heal:
                statsMgr.Heal(param);
                break;
            case BuffFuncType.Shield:
                ApplyShield(buffCfg.Id, param);
                break;
            case BuffFuncType.AoeDamage:
            case BuffFuncType.CounterDamage:
            case BuffFuncType.Stun:
            case BuffFuncType.Freeze:
                break;
            default:
                Debug.LogWarning($"[BuffManager] 未处理的Buff功能类型: {funcType}");
                break;
        }
    }

    private void RemoveBuffEffects(cfg.cfg.buff.Buff buffCfg)
    {
        var funcType = (BuffFuncType)buffCfg.Func;
        int param = buffCfg.Param1;

        if (_targetStats != null)
        {
            RemoveBuffEffectsFromStats(_targetStats, funcType, param);
            return;
        }

        var statsMgr = CharacterStatsManager.Instance;

        switch (funcType)
        {
            case BuffFuncType.LootBonus:
                statsMgr.ModifyPer(AttrType.Destroy, -param * 10000);
                break;
            case BuffFuncType.DamageUp:
                statsMgr.ModifyPlus(AttrType.Destroy, -param);
                break;
            case BuffFuncType.SelfmatainUp:
                statsMgr.ModifyPlus(AttrType.Selfmatain, -param);
                break;
            case BuffFuncType.CreatorwillingUp:
                statsMgr.ModifyPlus(AttrType.Creatorwilling, -param);
                break;
            case BuffFuncType.SelfmatainPerUp:
                statsMgr.ModifyPer(AttrType.Selfmatain, -param * 10000);
                break;
            case BuffFuncType.CreatorwillingPerUp:
                statsMgr.ModifyPer(AttrType.Creatorwilling, -param * 10000);
                break;
            case BuffFuncType.DestroyPerUp:
                statsMgr.ModifyPer(AttrType.Destroy, -param * 10000);
                break;
            case BuffFuncType.SpeedUp:
                statsMgr.ModifyPlus(AttrType.Speed, -param);
                break;
            case BuffFuncType.Heal:
                break;
            case BuffFuncType.Shield:
                RemoveShield(buffCfg.Id);
                break;
            case BuffFuncType.AoeDamage:
            case BuffFuncType.CounterDamage:
            case BuffFuncType.Stun:
            case BuffFuncType.Freeze:
                break;
            default:
                break;
        }
    }

    public static void ApplyBuffEffectsToStats(CharacterStats stats, BuffFuncType funcType, int param)
    {
        switch (funcType)
        {
            case BuffFuncType.DamageUp:
                stats.DestroyPlus += param;
                break;
            case BuffFuncType.SelfmatainUp:
                stats.SelfmatainPlus += param;
                break;
            case BuffFuncType.CreatorwillingUp:
                stats.CreatorwillingPlus += param;
                break;
            case BuffFuncType.SelfmatainPerUp:
                stats.SelfmatainPer += param * 10000;
                break;
            case BuffFuncType.CreatorwillingPerUp:
                stats.CreatorwillingPer += param * 10000;
                break;
            case BuffFuncType.DestroyPerUp:
                stats.DestroyPer += param * 10000;
                break;
            case BuffFuncType.SpeedUp:
                stats.SpeedPlus += param;
                break;
            case BuffFuncType.LootBonus:
                stats.DestroyPer += param * 10000;
                break;
            case BuffFuncType.Heal:
                stats.Hp += param;
                stats.ClampHp();
                break;
            case BuffFuncType.AoeDamage:
            case BuffFuncType.Shield:
            case BuffFuncType.CounterDamage:
            case BuffFuncType.Stun:
            case BuffFuncType.Freeze:
                break;
        }
    }

    public static void RemoveBuffEffectsFromStats(CharacterStats stats, BuffFuncType funcType, int param)
    {
        switch (funcType)
        {
            case BuffFuncType.DamageUp:
                stats.DestroyPlus -= param;
                break;
            case BuffFuncType.SelfmatainUp:
                stats.SelfmatainPlus -= param;
                break;
            case BuffFuncType.CreatorwillingUp:
                stats.CreatorwillingPlus -= param;
                break;
            case BuffFuncType.SelfmatainPerUp:
                stats.SelfmatainPer -= param * 10000;
                break;
            case BuffFuncType.CreatorwillingPerUp:
                stats.CreatorwillingPer -= param * 10000;
                break;
            case BuffFuncType.DestroyPerUp:
                stats.DestroyPer -= param * 10000;
                break;
            case BuffFuncType.SpeedUp:
                stats.SpeedPlus -= param;
                break;
            case BuffFuncType.LootBonus:
                stats.DestroyPer -= param * 10000;
                break;
            case BuffFuncType.Heal:
                break;
            case BuffFuncType.AoeDamage:
            case BuffFuncType.Shield:
            case BuffFuncType.CounterDamage:
            case BuffFuncType.Stun:
            case BuffFuncType.Freeze:
                break;
        }
    }

    private void ApplyShield(int buffId, int amount)
    {
        var buff = _playerBuffs.Find(b => b.BuffId == buffId);
        if (buff != null)
            buff.ShieldAmount = amount;
    }

    private void RemoveShield(int buffId)
    {
        var buff = _playerBuffs.Find(b => b.BuffId == buffId);
        if (buff != null)
            buff.ShieldAmount = 0;
    }

    public int GetShieldAmount()
    {
        int total = 0;
        foreach (var buff in _playerBuffs)
            total += buff.ShieldAmount;
        return total;
    }

    public int AbsorbDamage(int damage)
    {
        if (damage <= 0) return 0;

        int remaining = damage;
        var shieldBuffs = _playerBuffs.Where(b => b.ShieldAmount > 0).ToList();

        foreach (var buff in shieldBuffs)
        {
            if (remaining <= 0) break;

            int absorbed = Mathf.Min(buff.ShieldAmount, remaining);
            buff.ShieldAmount -= absorbed;
            remaining -= absorbed;

            if (buff.ShieldAmount <= 0)
            {
                var tables = GetTables();
                if (tables != null)
                {
                    var buffCfg = tables.TbBuff.GetOrDefault(buff.BuffId);
                    if (buffCfg != null)
                        RemoveBuffEffects(buffCfg);
                }
                _playerBuffs.Remove(buff);
                OnBuffChanged?.Invoke(buff.BuffId, false);
            }
        }

        return damage - remaining;
    }

    public bool HasStatus(BuffFuncType statusType)
    {
        return _playerBuffs.Any(b =>
        {
            var tables = GetTables();
            if (tables == null) return false;
            var cfg = tables.TbBuff.GetOrDefault(b.BuffId);
            return cfg != null && cfg.Func == (int)statusType;
        });
    }

    public List<ActiveBuff> GetBuffsByTrigger(BuffTrigger trigger)
    {
        var result = new List<ActiveBuff>();
        var tables = GetTables();
        if (tables == null) return result;

        foreach (var buff in _playerBuffs)
        {
            var cfg = tables.TbBuff.GetOrDefault(buff.BuffId);
            if (cfg != null && cfg.Trigger == (int)trigger)
                result.Add(buff);
        }

        return result;
    }

    public static int ParseBuffId(string param1)
    {
        if (string.IsNullOrEmpty(param1))
            return 0;
        if (int.TryParse(param1, out int id) && id > 0)
            return id;
        return 0;
    }

    private cfg.Tables GetTables()
    {
        if (ManagerRegistry.TryGet<DataTableManager>(out var dtm) && dtm.Tables != null)
            return dtm.Tables;
        return null;
    }

    private cfg.cfg.item.Item GetItemConfig(int itemId)
    {
        var tables = GetTables();
        return tables?.TbItem.GetOrDefault(itemId);
    }
}
