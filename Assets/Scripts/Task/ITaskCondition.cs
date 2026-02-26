using System;

namespace QuestSystem
{
    public interface ITaskCondition
    {
        ConditionRuntimeState State { get; }
        void Activate();
        void Deactivate();
    }
    
    /// <summary>
    /// TriggerDialogue：参数1=dialogueId(对应事件A)，参数2=需要触发次数(目标)
    /// </summary>
    public class TriggerDialogueCondition : ITaskCondition
    {
        public ConditionRuntimeState State { get; } = new();
        private readonly int _dialogueId;
        private readonly TaskInstance _owner;

        public TriggerDialogueCondition(TaskConditionConfig cfg, TaskInstance owner)
        {
            _owner = owner;
            _dialogueId = cfg.P1;
            State.Target = cfg.P2 > 0 ? cfg.P2 : 1;
            State.Current = 0;
        }

        public void Activate() => TaskEventBus.OnEvent += OnEvent;
        public void Deactivate() => TaskEventBus.OnEvent -= OnEvent;

        private void OnEvent(GameEvent e)
        {
            if (e.Type != GameEventType.TriggerDialogue) return;
            if (e.A != _dialogueId) return;

            State.Current = Math.Min(State.Current + Math.Max(1, e.B), State.Target);
            _owner.NotifyUpdated();
        }
    }

    /// <summary>
    /// ReachRegion：参数1=regionId，参数2=需要触发次数（未填则默认为1）
    /// </summary>
    public class ReachRegionCondition : ITaskCondition
    {
        public ConditionRuntimeState State { get; } = new();
        private readonly int _regionId;
        private readonly TaskInstance _owner;

        public ReachRegionCondition(TaskConditionConfig cfg, TaskInstance owner)
        {
            _owner = owner;
            _regionId = cfg.P1;
            State.Target = cfg.P2 > 0 ? cfg.P2 : 1;
            State.Current = 0;
        }

        public void Activate() => TaskEventBus.OnEvent += OnEvent;
        public void Deactivate() => TaskEventBus.OnEvent -= OnEvent;

        private void OnEvent(GameEvent e)
        {
            if (e.Type != GameEventType.ReachRegion) return;
            if (e.A != _regionId) return;

            State.Current = Math.Min(State.Current + 1, State.Target);
            _owner.NotifyUpdated();
        }
    }

    /// <summary>
    /// CollectItem：参数1=itemId，参数2=需要数量
    /// </summary>
    public class CollectItemCondition : ITaskCondition
    {
        public ConditionRuntimeState State { get; } = new();
        private readonly int _itemId;
        private readonly TaskInstance _owner;

        public CollectItemCondition(TaskConditionConfig cfg, TaskInstance owner)
        {
            _owner = owner;
            _itemId = cfg.P1;
            State.Target = cfg.P2 > 0 ? cfg.P2 : 1;
            State.Current = 0;
        }

        public void Activate() => TaskEventBus.OnEvent += OnEvent;
        public void Deactivate() => TaskEventBus.OnEvent -= OnEvent;

        private void OnEvent(GameEvent e)
        {
            if (e.Type != GameEventType.CollectItem) return;
            if (e.A != _itemId) return;

            State.Current = Math.Min(State.Current + Math.Max(1, e.B), State.Target);
            _owner.NotifyUpdated();
        }
    }

    /// <summary>
    /// KillEnemy：参数1=enemyTypeId，参数2=需要数量
    /// </summary>
    public class KillEnemyCondition : ITaskCondition
    {
        public ConditionRuntimeState State { get; } = new();
        private readonly int _enemyTypeId;
        private readonly TaskInstance _owner;

        public KillEnemyCondition(TaskConditionConfig cfg, TaskInstance owner)
        {
            _owner = owner;
            _enemyTypeId = cfg.P1;
            State.Target = cfg.P2 > 0 ? cfg.P2 : 1;
            State.Current = 0;
        }

        public void Activate() => TaskEventBus.OnEvent += OnEvent;
        public void Deactivate() => TaskEventBus.OnEvent -= OnEvent;

        private void OnEvent(GameEvent e)
        {
            if (e.Type != GameEventType.KillEnemy) return;
            if (e.A != _enemyTypeId) return;

            State.Current = Math.Min(State.Current + Math.Max(1, e.B), State.Target);
            _owner.NotifyUpdated();
        }
    }
}