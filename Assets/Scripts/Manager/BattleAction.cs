using System.Collections.Generic;

public class BattleAction
{
    public BattleUnit Actor;
    public ActionType Type;
    public int ActionId;
    public List<int> TargetSlotIndices;
    public TargetType TargetType;

    public BattleAction(BattleUnit actor, ActionType type, int actionId, List<int> targets, TargetType targetType)
    {
        Actor = actor;
        Type = type;
        ActionId = actionId;
        TargetSlotIndices = targets ?? new List<int>();
        TargetType = targetType;
    }
}
