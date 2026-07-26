using System.Collections.Generic;

/// <summary>
/// 事件触发时机（对应6个队列）
/// </summary>
public enum BattleEventPhase
{
    RoundStart,
    TurnStart,
    StatusCheck,
    Action,
    TurnEnd,
    RoundEnd
}

/// <summary>
/// 事件类型
/// </summary>
public enum BattleEventType
{
    SummonUnit,     // 召唤新单位入场
    ApplyDamage,    // 施加伤害
    ApplyHeal,      // 施加治疗
    ApplyBuff,      // 施加Buff
    RemoveBuff,     // 移除Buff
    AdjustSpeed,    // 调整速度
    SkipAction,     // 跳过行动
    TriggerSkill,   // 触发技能
}

/// <summary>
/// 战斗事件数据
/// </summary>
public class BattleEvent
{
    public BattleEventType EventType;
    public BattleEventPhase TriggerPhase;
    public BattleUnit TargetUnit;       // 目标单位
    public BattleUnit SourceUnit;       // 来源单位

    // 具体参数（根据事件类型取用）
    public int Param1;                  // 伤害值/治疗值/BuffId/PersonId等
    public int Param2;                  // 辅助参数
    public bool BoolParam;              // 布尔参数
    public object Data;                 // 额外数据

    public BattleEvent(BattleEventType type, BattleEventPhase phase)
    {
        EventType = type;
        TriggerPhase = phase;
    }

    public static BattleEvent Summon(int personId, bool isPlayerSide, int slotIndex, BattleEventPhase phase)
    {
        return new BattleEvent(BattleEventType.SummonUnit, phase)
        {
            Param1 = personId,
            Param2 = slotIndex,
            BoolParam = isPlayerSide
        };
    }

    public static BattleEvent Damage(BattleUnit target, int damage, BattleEventPhase phase)
    {
        return new BattleEvent(BattleEventType.ApplyDamage, phase)
        {
            TargetUnit = target,
            Param1 = damage
        };
    }

    public static BattleEvent Heal(BattleUnit target, int amount, BattleEventPhase phase)
    {
        return new BattleEvent(BattleEventType.ApplyHeal, phase)
        {
            TargetUnit = target,
            Param1 = amount
        };
    }

    public static BattleEvent Buff(BattleUnit target, int buffId, BattleEventPhase phase)
    {
        return new BattleEvent(BattleEventType.ApplyBuff, phase)
        {
            TargetUnit = target,
            Param1 = buffId
        };
    }

    public static BattleEvent RemoveBuff(BattleUnit target, int buffId, BattleEventPhase phase)
    {
        return new BattleEvent(BattleEventType.RemoveBuff, phase)
        {
            TargetUnit = target,
            Param1 = buffId
        };
    }

    public override string ToString()
    {
        return $"{EventType}(phase={TriggerPhase}, target=P{TargetUnit?.PersonId}, p1={Param1})";
    }
}
