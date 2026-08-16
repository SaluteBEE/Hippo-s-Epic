public enum BattlePhase
{
    None,
    Init,
    RoundStart,
    TurnQueue,
    RoundEnd,
    BattleEnd
}

public enum UnitTurnPhase
{
    None,
    TurnStart,
    StatusCheck,
    Action,
    TurnEnd
}

public enum ActionType
{
    Skill,
    Item,
    Flee
}

/// <summary>
/// 技能目标阵营（与 skill.targettype 一致）
/// </summary>
public enum TargetType
{
    None = 0,
    Self = 1,
    Ally = 2,
    Enemy = 3
}

/// <summary>
/// 技能效果类型（与 skill.skilltype 一致）
/// </summary>
public enum SkillType
{
    BuffOnly = 0,
    Heal = 1,
    PhysicalDamage = 2,
    MagicDamage = 3
}

/// <summary>
/// 伤害/治疗公式（与 skill.dmgfunc 一致）
/// 力量=毁灭 Destroy，理智=创作者意志 Creatorwilling，体质=自我 Selfmatain
/// </summary>
public enum SkillDmgFunc
{
    None = 0,
    DestroyAdd = 1,
    CreatorAdd = 2,
    SelfmatainAdd = 3,
    DestroyMul = 4,
    CreatorMul = 5,
    SelfmatainMul = 6
}

public enum BattleResult
{
    None,
    Win,
    Lose,
    Flee
}
