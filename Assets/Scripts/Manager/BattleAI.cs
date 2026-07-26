using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class BattleAI
{
    public static BattleAction Decide(BattleUnit unit, BattleManager battle)
    {
        var enemyUnits = battle.GetAllAliveUnits(!unit.IsPlayerSide);

        if (ShouldHeal(unit, battle))
        {
            var healTarget = FindLowestHpAlly(unit, battle);
            if (healTarget != null)
            {
                return new BattleAction(unit, ActionType.Defend, 0, new List<int>(), TargetType.Self);
            }
        }

        var availableSkill = FindBestAttackSkill(unit, battle);
        if (availableSkill != null)
        {
            var targetType = (TargetType)availableSkill.Targettype;
            var targets = battle.GetAvailableTargetsForSkill(unit, availableSkill.Id);
            if (targets.Count > 0)
            {
                var primaryTarget = FindBestTarget(targets);
                return new BattleAction(unit, ActionType.Skill, availableSkill.Id,
                    new List<int> { primaryTarget.SlotIndex }, targetType);
            }
        }

        if (enemyUnits.Count > 0)
        {
            var target = FindBestTarget(enemyUnits);
            return new BattleAction(unit, ActionType.Skill, 0,
                new List<int> { target.SlotIndex }, TargetType.EnemySingle);
        }

        return new BattleAction(unit, ActionType.Defend, 0, new List<int>(), TargetType.Self);
    }

    private static bool ShouldHeal(BattleUnit unit, BattleManager battle)
    {
        if (unit.Stats.FinalHpMax <= 0) return false;
        float hpRatio = (float)unit.Stats.Hp / unit.Stats.FinalHpMax;
        return hpRatio < 0.3f;
    }

    private static BattleUnit FindLowestHpAlly(BattleUnit unit, BattleManager battle)
    {
        var allies = battle.GetAllAliveUnits(unit.IsPlayerSide);
        BattleUnit lowest = null;
        int lowestHp = int.MaxValue;

        foreach (var ally in allies)
        {
            if (ally.Stats.Hp < lowestHp)
            {
                lowestHp = ally.Stats.Hp;
                lowest = ally;
            }
        }

        return lowest;
    }

    private static cfg.cfg.skill.Skill FindBestAttackSkill(BattleUnit unit, BattleManager battle)
    {
        var tables = battle.GetTables();
        if (tables == null) return null;

        cfg.cfg.skill.Skill bestSkill = null;
        int bestDamage = 0;

        foreach (int skillId in unit.AvailableSkills)
        {
            if (unit.IsSkillOnCooldown(skillId)) continue;

            var skillCfg = tables.TbSkill.GetOrDefault(skillId);
            if (skillCfg == null) continue;

            var targetType = (TargetType)skillCfg.Targettype;
            if (targetType == TargetType.Self || targetType == TargetType.AllySingle || targetType == TargetType.AllyAll)
                continue;

            int level = unit.Stats.SkillLevels.TryGetValue(skillId, out int lv) ? lv : 1;
            int buffId = skillCfg.Buffid + (level - 1);

            var buffCfg = tables.TbBuff.GetOrDefault(buffId);
            int damage = buffCfg != null && (BuffFuncType)buffCfg.Func == BuffFuncType.DamageUp
                ? unit.Stats.FinalDestroy + buffCfg.Param1
                : unit.Stats.FinalDestroy;

            if (damage > bestDamage)
            {
                bestDamage = damage;
                bestSkill = skillCfg;
            }
        }

        return bestSkill;
    }

    private static BattleUnit FindBestTarget(List<BattleUnit> targets)
    {
        BattleUnit best = null;
        int lowestHp = int.MaxValue;

        var frontRow = targets.Where(u => u.GetRow() == 2).ToList();
        var searchList = frontRow.Count > 0 ? frontRow : targets;

        foreach (var target in searchList)
        {
            if (target.Stats.Hp < lowestHp)
            {
                lowestHp = target.Stats.Hp;
                best = target;
            }
        }

        return best ?? targets[0];
    }
}
