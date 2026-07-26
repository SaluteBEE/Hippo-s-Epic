using System.Collections.Generic;

public class BattleUnit
{
    public int UnitId;
    public int PersonId;
    public bool IsPlayerSide;
    public int SlotIndex;
    public CharacterStats Stats;
    public BuffManager UnitBuffs;
    public bool IsAlive = true;
    public bool IsPlayerControlled;
    public int Speed;
    public List<int> AvailableSkills = new List<int>();
    public Dictionary<int, int> SkillCooldowns = new Dictionary<int, int>();
    public UnitTurnPhase CurrentPhase = UnitTurnPhase.None;
    public bool SkipAction;
    public bool IsDefending;

    public int GetRow() => SlotIndex / 3;
    public int GetColumn() => SlotIndex % 3;

    public void ResetTurnState()
    {
        CurrentPhase = UnitTurnPhase.None;
        SkipAction = false;
        IsDefending = false;
    }

    public void DecrementCooldowns()
    {
        var keys = new List<int>(SkillCooldowns.Keys);
        foreach (var skillId in keys)
        {
            SkillCooldowns[skillId]--;
            if (SkillCooldowns[skillId] <= 0)
                SkillCooldowns.Remove(skillId);
        }
    }

    public bool IsSkillOnCooldown(int skillId)
    {
        return SkillCooldowns.TryGetValue(skillId, out int cd) && cd > 0;
    }

    public void SetCooldown(int skillId, int turns)
    {
        if (turns > 0)
            SkillCooldowns[skillId] = turns;
    }
}
