using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class BattleAI
{
    /// <summary>
    /// 敌方AI决策主入口：按优先级生成行动——HP 低于 30%且有治疗技能→治疗最低血友军；否则最强伤害技能打最优目标；再否则普攻；无目标则防御
    /// </summary>
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

    /// <summary>
    /// 判断单位是否需要治疗（当前HP比例低于 30%）
    /// </summary>
    private static bool ShouldHeal(BattleUnit unit, BattleManager battle)
    {
        if (unit.Stats.FinalHpMax <= 0) return false;
        float hpRatio = (float)unit.Stats.Hp / unit.Stats.FinalHpMax;
        return hpRatio < 0.3f;
    }

    /// <summary>
    /// 在友军存活单位中寻找HP最低者（治疗优先目标）
    /// </summary>
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

    /// <summary>
    /// 遍历可用技能，挑选治疗量最大的非冷却治疗技能；无则返回null
    /// </summary>
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

    /// <summary>
    /// 遍历可用技能，挑选伤害最高的非冷却伤害技能（限敌方目标类型）；无则返回null
    /// </summary>
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

    /// <summary>
    /// 目标择优：优先前排（row=2）中HP最低的单位，无前排则取目标列表第一个
    /// </summary>
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
