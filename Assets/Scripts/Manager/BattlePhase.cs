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
    Defend,
    Flee
}

public enum TargetType
{
    Self = 0,
    EnemySingle = 1,
    EnemyRow = 2,
    EnemyColumn = 3,
    EnemyAll = 4,
    AllySingle = 5,
    AllyAll = 6
}

public enum BattleResult
{
    None,
    Win,
    Lose,
    Flee
}
