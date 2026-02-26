using System;
using UnityEngine;

namespace QuestSystem
{
    // 你可以按需扩展
    public enum ConditionType
    {
        None = 0,
        TriggerDialogue = 1,

        // 示例1：到达区域/触发点（参数1=regionId，参数2=次数或布尔用1）
        ReachRegion = 2,

        // 示例2：收集物品（参数1=itemId，参数2=需要数量）
        CollectItem = 3,

        // 示例3：击杀怪物（参数1=enemyTypeId，参数2=需要数量）
        KillEnemy = 4,
    }

    public enum GameEventType
    {
        TriggerDialogue,
        ReachRegion,
        CollectItem,
        KillEnemy
    }

    public struct GameEvent
    {
        public GameEventType Type;
        public int A;     // 通用参数槽（例如 regionId / itemId / enemyTypeId）
        public int B;     // 通用参数槽（例如 count）
    }

    /// <summary>
    /// 游戏事件总线：其他系统只需要 Raise，不需要知道任务内部细节。
    /// </summary>
    public static class TaskEventBus
    {
        public static event Action<GameEvent> OnEvent;

        public static void Raise(GameEvent e) => OnEvent?.Invoke(e);

        // 便捷封装
        
        public static void RaiseTriggerDialogue(int dialogueTaskId)
            => Raise(new GameEvent { Type = GameEventType.TriggerDialogue, A = dialogueTaskId, B = 1 });
        
        public static void RaiseReachRegion(int regionId)
            => Raise(new GameEvent { Type = GameEventType.ReachRegion, A = regionId, B = 1 });

        public static void RaiseCollectItem(int itemId, int count)
            => Raise(new GameEvent { Type = GameEventType.CollectItem, A = itemId, B = count });

        public static void RaiseKillEnemy(int enemyTypeId, int count = 1)
            => Raise(new GameEvent { Type = GameEventType.KillEnemy, A = enemyTypeId, B = count });
    }
}