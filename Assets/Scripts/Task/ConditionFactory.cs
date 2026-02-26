using System;

namespace QuestSystem
{
    public class ConditionFactory
    {
        public ITaskCondition Create(TaskConditionConfig cfg, int conditionIndex, TaskInstance owner)
        {
            return cfg.Type switch
            {
                ConditionType.TriggerDialogue => new TriggerDialogueCondition(cfg, owner),
                ConditionType.ReachRegion => new ReachRegionCondition(cfg, owner),
                ConditionType.CollectItem => new CollectItemCondition(cfg, owner),
                ConditionType.KillEnemy => new KillEnemyCondition(cfg, owner),
                _ => throw new NotSupportedException($"Unsupported condition type: {cfg.Type}")
            };
        }
    }
}