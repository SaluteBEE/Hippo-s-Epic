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
            var healSkill = FindBestHealSkill(unit, battle);
            if (healSkill != null)
            {
                var healTargets = battle.GetAvailableTargetsForSkill(unit, healSkill.Id);
                if (healTargets.Count > 0)
                {
                    var healTarget = FindLowestHpAlly(unit, battle) ?? healTargets[0];
                    if (!healTargets.Contains(healTarget))
                        healTarget = healTargets[0];

                    return new BattleAction(unit, ActionType.Skill, healSkill.Id,
                        new List<int> { healTarget.SlotIndex }, (TargetType)healSkill.Targettype);
                }
            }
        }

        var availableSkill = FindBestAttackSkill(unit, battle);
        if (availableSkill != null)
        {
            var targets = battle.GetAvailableTargetsForSkill(unit, availableSkill.Id);
            if (targets.Count > 0)
            {
                var primaryTarget = FindBestTarget(targets);
                return new BattleAction(unit, ActionType.Skill, availableSkill.Id,
                    new List<int> { primaryTarget.SlotIndex }, (TargetType)availableSkill.Targettype);
            }
        }

        if (enemyUnits.Count > 0)
        {
            var target = FindBestTarget(enemyUnits);
            return new BattleAction(unit, ActionType.Skill, 0,
                new List<int> { target.SlotIndex }, TargetType.Enemy);
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

    private static cfg.cfg.skill.Skill FindBestHealSkill(BattleUnit unit, BattleManager battle)
    {
        var tables = battle.GetTables();
        if (tables == null) return null;

        cfg.cfg.skill.Skill best = null;
        int bestValue = 0;

        foreach (int skillId in unit.AvailableSkills)
        {
            if (unit.IsSkillOnCooldown(skillId)) continue;

            var skillCfg = tables.TbSkill.GetOrDefault(skillId);
            if (skillCfg == null) continue;
            if ((SkillType)skillCfg.Skilltype != SkillType.Heal) continue;

            int value = SkillCombatUtil.CalcSkillValue(unit.Stats, skillCfg.Dmgfunc, skillCfg.Effectparam);
            if (value >= bestValue)
            {
                bestValue = value;
                best = skillCfg;
            }
        }

        return best;
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

            var skillType = (SkillType)skillCfg.Skilltype;
            if (skillType != SkillType.PhysicalDamage && skillType != SkillType.MagicDamage)
                continue;

            if ((TargetType)skillCfg.Targettype != TargetType.Enemy)
                continue;

            int damage = SkillCombatUtil.CalcSkillValue(unit.Stats, skillCfg.Dmgfunc, skillCfg.Effectparam);
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
