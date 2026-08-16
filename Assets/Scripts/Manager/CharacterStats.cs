using System;
using System.Collections.Generic;
using UnityEngine;

public enum AttrType
{
    Destroy = 0,
    Selfmatain = 1,
    Creatorwilling = 2,
    Speed = 3
}

public class CharacterStats
{
    public int Destroy;
    public int Selfmatain;
    public int Creatorwilling;

    public int DestroyPlus;
    public int SelfmatainPlus;
    public int CreatorwillingPlus;

    public int DestroyPer;
    public int SelfmatainPer;
    public int CreatorwillingPer;

    public int SpeedBase;
    public int SpeedPlus;

    public int Hp;

    public int Exp;
    public int Level;
    public int SkillPoint;

    /// <summary>
    /// 行动点（每回合可用行动点，玩家基础属性；技能按 cost 消耗，用完结束回合）。
    /// 默认 2，后续从配表读取
    /// </summary>
    public int ActionPoint = 2;

    public float ExpBonus = 1f;
    public float CurrencyBonus = 1f;

    public List<int> UnlockedSkills = new List<int>();
    public Dictionary<int, int> SkillLevels = new Dictionary<int, int>();

    public int FinalDestroy => (Destroy + DestroyPlus) * (10000 + DestroyPer) / 10000;
    public int FinalSelfmatain => (Selfmatain + SelfmatainPlus) * (10000 + SelfmatainPer) / 10000;
    public int FinalCreatorwilling => (Creatorwilling + CreatorwillingPlus) * (10000 + CreatorwillingPer) / 10000;
    public int FinalHpMax => 10 + 2 * FinalCreatorwilling;
    public int FinalSpeed => SpeedBase + SpeedPlus;

    public int CalcDamageTo(CharacterStats defender)
    {
        int raw = FinalDestroy - defender.FinalDestroy / 2;
        return Mathf.Max(0, raw);
    }

    public void ClampHp()
    {
        int maxHp = FinalHpMax;
        if (Hp > maxHp)
            Hp = maxHp;
        if (Hp < 0)
            Hp = 0;
    }

    public event Action OnStatsChanged;

    public void NotifyChanged()
    {
        ClampHp();
        OnStatsChanged?.Invoke();
    }

    public int GetBase(AttrType type)
    {
        switch (type)
        {
            case AttrType.Destroy: return Destroy;
            case AttrType.Selfmatain: return Selfmatain;
            case AttrType.Creatorwilling: return Creatorwilling;
            case AttrType.Speed: return SpeedBase;
            default: return 0;
        }
    }

    public void SetBase(AttrType type, int value)
    {
        switch (type)
        {
            case AttrType.Destroy: Destroy = value; break;
            case AttrType.Selfmatain: Selfmatain = value; break;
            case AttrType.Creatorwilling: Creatorwilling = value; break;
            case AttrType.Speed: SpeedBase = value; break;
        }
    }

    public int GetPlus(AttrType type)
    {
        switch (type)
        {
            case AttrType.Destroy: return DestroyPlus;
            case AttrType.Selfmatain: return SelfmatainPlus;
            case AttrType.Creatorwilling: return CreatorwillingPlus;
            case AttrType.Speed: return SpeedPlus;
            default: return 0;
        }
    }

    public void SetPlus(AttrType type, int value)
    {
        switch (type)
        {
            case AttrType.Destroy: DestroyPlus = value; break;
            case AttrType.Selfmatain: SelfmatainPlus = value; break;
            case AttrType.Creatorwilling: CreatorwillingPlus = value; break;
            case AttrType.Speed: SpeedPlus = value; break;
        }
    }

    public int GetPer(AttrType type)
    {
        switch (type)
        {
            case AttrType.Destroy: return DestroyPer;
            case AttrType.Selfmatain: return SelfmatainPer;
            case AttrType.Creatorwilling: return CreatorwillingPer;
            case AttrType.Speed: return 0;
            default: return 0;
        }
    }

    public void SetPer(AttrType type, int value)
    {
        switch (type)
        {
            case AttrType.Destroy: DestroyPer = value; break;
            case AttrType.Selfmatain: SelfmatainPer = value; break;
            case AttrType.Creatorwilling: CreatorwillingPer = value; break;
            case AttrType.Speed: break;
        }
    }

    [Serializable]
    public class StatsSaveData
    {
        public int destroy;
        public int selfmatain;
        public int creatorwilling;
        public int destroyPlus;
        public int selfmatainPlus;
        public int creatorwillingPlus;
        public int destroyPer;
        public int selfmatainPer;
        public int creatorwillingPer;
        public int speedBase;
        public int speedPlus;
        public int hp;
        public int exp;
        public int level;
        public int skillPoint;
        public int actionPoint;
        public float expBonus;
        public float currencyBonus;
        public List<int> unlockedSkills;
        public List<SkillLevelEntry> skillLevels;
    }

    [Serializable]
    public class SkillLevelEntry
    {
        public int skillId;
        public int level;
    }

    public StatsSaveData ToSaveData()
    {
        var data = new StatsSaveData
        {
            destroy = Destroy,
            selfmatain = Selfmatain,
            creatorwilling = Creatorwilling,
            destroyPlus = DestroyPlus,
            selfmatainPlus = SelfmatainPlus,
            creatorwillingPlus = CreatorwillingPlus,
            destroyPer = DestroyPer,
            selfmatainPer = SelfmatainPer,
            creatorwillingPer = CreatorwillingPer,
            speedBase = SpeedBase,
            speedPlus = SpeedPlus,
            hp = Hp,
            exp = Exp,
            level = Level,
            skillPoint = SkillPoint,
            actionPoint = ActionPoint,
            expBonus = ExpBonus,
            currencyBonus = CurrencyBonus,
            unlockedSkills = new List<int>(UnlockedSkills),
            skillLevels = new List<SkillLevelEntry>()
        };

        foreach (var kvp in SkillLevels)
        {
            data.skillLevels.Add(new SkillLevelEntry { skillId = kvp.Key, level = kvp.Value });
        }

        return data;
    }

    public void FromSaveData(StatsSaveData data)
    {
        if (data == null) return;

        Destroy = data.destroy;
        Selfmatain = data.selfmatain;
        Creatorwilling = data.creatorwilling;
        DestroyPlus = data.destroyPlus;
        SelfmatainPlus = data.selfmatainPlus;
        CreatorwillingPlus = data.creatorwillingPlus;
        DestroyPer = data.destroyPer;
        SelfmatainPer = data.selfmatainPer;
        CreatorwillingPer = data.creatorwillingPer;
        SpeedBase = data.speedBase;
        SpeedPlus = data.speedPlus;
        Hp = data.hp;
        Exp = data.exp;
        Level = data.level;
        SkillPoint = data.skillPoint;
        // 存档兼容：旧存档无 actionPoint 字段 → 保持默认 2
        ActionPoint = data.actionPoint > 0 ? data.actionPoint : 2;
        ExpBonus = data.expBonus > 0 ? data.expBonus : 1f;
        CurrencyBonus = data.currencyBonus > 0 ? data.currencyBonus : 1f;

        UnlockedSkills = data.unlockedSkills != null ? new List<int>(data.unlockedSkills) : new List<int>();
        SkillLevels = new Dictionary<int, int>();
        if (data.skillLevels != null)
        {
            foreach (var entry in data.skillLevels)
            {
                SkillLevels[entry.skillId] = entry.level;
            }
        }
    }
}
