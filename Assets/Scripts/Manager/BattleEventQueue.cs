using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 战斗事件队列管理器
/// 6个队列对应 BattleEventPhase 的6个时机
/// 非当前阶段触发的事件加入对应队列，到对应阶段时统一处理
/// </summary>
public class BattleEventQueue
{
    private readonly Dictionary<BattleEventPhase, Queue<BattleEvent>> _queues
        = new Dictionary<BattleEventPhase, Queue<BattleEvent>>();

    private readonly BattleManager _battle;

    public BattleEventQueue(BattleManager battle)
    {
        _battle = battle;
        foreach (BattleEventPhase phase in System.Enum.GetValues(typeof(BattleEventPhase)))
        {
            _queues[phase] = new Queue<BattleEvent>();
        }
    }

    /// <summary>
    /// 将事件加入指定阶段的队列
    /// </summary>
    public void Enqueue(BattleEvent evt)
    {
        _queues[evt.TriggerPhase].Enqueue(evt);
        BattleLogger.Log($" 入队: {evt} → {evt.TriggerPhase}队列 (待处理数:{_queues[evt.TriggerPhase].Count})");
    }

    /// <summary>
    /// 处理指定阶段的所有排队事件
    /// </summary>
    public void ProcessPhase(BattleEventPhase phase)
    {
        var queue = _queues[phase];
        if (queue.Count == 0) return;

        BattleLogger.Log($" ── 处理 {phase} 事件 (共{queue.Count}个) ──");
        while (queue.Count > 0)
        {
            var evt = queue.Dequeue();
            ExecuteEvent(evt);
        }
    }

    /// <summary>
    /// 处理指定阶段中，目标为指定单位的事件
    /// </summary>
    public void ProcessUnitPhase(BattleEventPhase phase, BattleUnit unit)
    {
        var queue = _queues[phase];
        if (queue.Count == 0) return;

        var unitEvents = new List<BattleEvent>();
        var remaining = new Queue<BattleEvent>();

        while (queue.Count > 0)
        {
            var evt = queue.Dequeue();
            if (evt.TargetUnit == unit)
                unitEvents.Add(evt);
            else
                remaining.Enqueue(evt);
        }

        // 放回不匹配的事件
        while (remaining.Count > 0)
            queue.Enqueue(remaining.Dequeue());

        if (unitEvents.Count > 0)
        {
            BattleLogger.Log($" ── 处理 P{unit.PersonId} {phase} 事件 (共{unitEvents.Count}个) ──");
            foreach (var evt in unitEvents)
                ExecuteEvent(evt);
        }
    }

    /// <summary>
    /// 清除所有队列
    /// </summary>
    public void Clear()
    {
        foreach (var kv in _queues)
            kv.Value.Clear();
    }

    /// <summary>
    /// 获取队列中事件数量
    /// </summary>
    public int GetQueueCount(BattleEventPhase phase)
    {
        return _queues.TryGetValue(phase, out var queue) ? queue.Count : 0;
    }

    /// <summary>
    /// 处理待入场召唤事件（RoundStart阶段的Summon事件）
    /// 返回新增的单位列表
    /// </summary>
    public List<BattleUnit> ProcessPendingSummons()
    {
        var queue = _queues[BattleEventPhase.RoundStart];
        var newUnits = new List<BattleUnit>();

        // 从 RoundStart 队列中提取所有 SummonUnit 事件
        var remaining = new Queue<BattleEvent>();
        while (queue.Count > 0)
        {
            var evt = queue.Dequeue();
            if (evt.EventType == BattleEventType.SummonUnit)
            {
                var unit = _battle.CreateUnitFromEvent(evt);
                if (unit != null)
                {
                    newUnits.Add(unit);
                    BattleLogger.Log($" 召唤单位入场: P{unit.PersonId} Slot={unit.SlotIndex}");
                }
            }
            else
            {
                remaining.Enqueue(evt);
            }
        }

        // 放回非召唤事件
        while (remaining.Count > 0)
            queue.Enqueue(remaining.Dequeue());

        return newUnits;
    }

    private void ExecuteEvent(BattleEvent evt)
    {
        switch (evt.EventType)
        {
            case BattleEventType.SummonUnit:
                BattleLogger.Log($" 执行召唤: PersonId={evt.Param1}");
                break;

            case BattleEventType.ApplyDamage:
                if (evt.TargetUnit != null && evt.TargetUnit.IsAlive)
                    _battle.ApplyDamage(null, evt.TargetUnit, evt.Param1);
                break;

            case BattleEventType.ApplyHeal:
                if (evt.TargetUnit != null && evt.TargetUnit.IsAlive)
                    _battle.ApplyHeal(null, evt.TargetUnit, evt.Param1);
                break;

            case BattleEventType.ApplyBuff:
                if (evt.TargetUnit != null)
                {
                    evt.TargetUnit.UnitBuffs.ApplyBuff(evt.Param1);
                    _battle.NotifyBuffApplied(evt.TargetUnit, evt.Param1, 0);
                }
                break;

            case BattleEventType.RemoveBuff:
                if (evt.TargetUnit != null)
                {
                    evt.TargetUnit.UnitBuffs.RemoveBuff(evt.Param1);
                    _battle.NotifyBuffRemoved(evt.TargetUnit, evt.Param1);
                }
                break;

            case BattleEventType.AdjustSpeed:
                if (evt.TargetUnit != null)
                {
                    evt.TargetUnit.Speed += evt.Param1;
                    BattleLogger.Log($" P{evt.TargetUnit.PersonId} 速度调整 {evt.Param1:+0;-#}");
                }
                break;

            case BattleEventType.SkipAction:
                if (evt.TargetUnit != null)
                {
                    evt.TargetUnit.SkipAction = evt.BoolParam;
                }
                break;

            case BattleEventType.TriggerSkill:
                if (evt.SourceUnit != null)
                {
                    var targets = _battle.GetEnemyAliveUnits();
                    _battle.TriggerSkill(evt.SourceUnit, evt.Param1, targets);
                }
                break;
        }
    }
}
