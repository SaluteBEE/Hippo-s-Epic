using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

class TestableQuestConditionSystem : ConditionSystem
{
    public readonly Dictionary<int, bool> MockDialogFinished = new Dictionary<int, bool>();
    public readonly Dictionary<int, bool> MockBattleResults = new Dictionary<int, bool>();
    public readonly Dictionary<int, int> MockItems = new Dictionary<int, int>();

    protected override bool CheckDialogFinished(int dialogId)
    {
        return MockDialogFinished.TryGetValue(dialogId, out bool v) && v;
    }

    protected override bool CheckBattleResult(int battleId, int result)
    {
        return MockBattleResults.TryGetValue(battleId, out bool v) && v;
    }

    protected override int GetItemCount(int itemId)
    {
        return MockItems.TryGetValue(itemId, out int c) ? c : 0;
    }
}

class QuestSystemTests
{
    private cfg.Tables _tables;
    private TestableQuestConditionSystem _condSys;

    [SetUp]
    public void Setup()
    {
        ManagerRegistry.Clear();
        _tables = TestDataLoader.LoadTables();
        ManagerRegistry.SetTables(_tables);

        var field = typeof(ConditionSystem)
            .GetField("_instance", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
        if (field != null) field.SetValue(null, null);

        _condSys = new TestableQuestConditionSystem();
        typeof(ConditionSystem)
            .GetField("_instance", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)
            ?.SetValue(null, _condSys);
        _condSys.Initialize();
    }

    [TearDown]
    public void TearDown()
    {
        ManagerRegistry.Clear();
        typeof(ConditionSystem)
            .GetField("_instance", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)
            ?.SetValue(null, null);
    }

    [Test]
    public void Quest1_CanAccept_ConditionNotMet_ReturnsFalse()
    {
        Assert.IsFalse(QuestManager.Instance.CanAccept(1), "条件1未满足，不应能接取任务1");
    }

    [Test]
    public void Quest1_Accept_ReturnsNull_WhenConditionNotMet()
    {
        var inst = QuestManager.Instance.AcceptQuest(1);
        Assert.IsNull(inst, "条件不满足，接取应返回null");
    }

    [Test]
    public void Quest1_CanAccept_ConditionMet_ReturnsTrue()
    {
        _condSys.MockItems[1] = 99;
        _condSys.Initialize();

        Assert.IsTrue(QuestManager.Instance.CanAccept(1), "条件满足后应能接取");
    }

    [Test]
    public void Quest1_Accept_EntersNode1()
    {
        _condSys.MockItems[1] = 99;
        _condSys.Initialize();

        var inst = QuestManager.Instance.AcceptQuest(1);
        Assert.IsNotNull(inst);
        Assert.AreEqual(1, inst.CurrentNodeId, "应进入节点1");
        Assert.AreEqual(QuestState.InProgress, inst.State);
    }

    [Test]
    public void Quest1_Accept_Twice_ReturnsSameInstance()
    {
        _condSys.MockItems[1] = 99;
        _condSys.Initialize();

        var inst1 = QuestManager.Instance.AcceptQuest(1);
        var inst2 = QuestManager.Instance.AcceptQuest(1);
        Assert.AreSame(inst1, inst2, "重复接取返回同一实例");
    }

    [Test]
    public void Quest1_Node1_CheckCompletion_DialogNotFinished_NoAdvance()
    {
        _condSys.MockItems[1] = 99;
        _condSys.Initialize();

        var inst = QuestManager.Instance.AcceptQuest(1);
        QuestManager.Instance.CheckCurrentNodeCompletion(1);

        Assert.AreEqual(1, inst.CurrentNodeId, "对话未完成，不应推进");
        Assert.AreEqual(QuestState.InProgress, inst.State);
    }

    [Test]
    public void Quest1_Node1_DialogFinished_AutoAdvanceToComplete()
    {
        _condSys.MockItems[1] = 99;
        _condSys.MockDialogFinished[1001001] = true;
        _condSys.Initialize();

        var inst = QuestManager.Instance.AcceptQuest(1);
        QuestManager.Instance.CheckCurrentNodeCompletion(1);

        Assert.AreEqual(1, inst.CurrentNodeId, "节点1有分支，需玩家选择，不自动推进");
        Assert.AreEqual(QuestState.InProgress, inst.State);
    }

    [Test]
    public void Quest1_Node1_ChooseBranch2_BattleWon_Completes()
    {
        _condSys.MockItems[1] = 99;
        _condSys.MockDialogFinished[1001001] = true;
        _condSys.Initialize();

        var inst = QuestManager.Instance.AcceptQuest(1);

        QuestManager.Instance.CheckCurrentNodeCompletion(1);
        Assert.IsTrue(inst.CompletedNodeIds.Contains(1), "节点1应完成");

        _condSys.MockBattleResults[2002] = true;
        _condSys.Initialize();

        inst.ChooseBranch(2);
        Assert.AreEqual(2, inst.CurrentNodeId, "应进入节点2");

        QuestManager.Instance.CheckCurrentNodeCompletion(1);
        Assert.AreEqual(QuestState.Completed, inst.State, "战斗2002胜利，节点2条件满足→自动进入-1→完成");
    }

    [Test]
    public void Quest1_Node1_ChooseBranch3_BattleLost_CompletesWithReward()
    {
        _condSys.MockItems[1] = 99;
        _condSys.MockDialogFinished[1001001] = true;
        _condSys.Initialize();

        var inst = QuestManager.Instance.AcceptQuest(1);
        QuestManager.Instance.CheckCurrentNodeCompletion(1);

        inst.ChooseBranch(3);
        Assert.AreEqual(3, inst.CurrentNodeId, "应进入节点3");

        QuestManager.Instance.CheckCurrentNodeCompletion(1);
        Assert.AreEqual(QuestState.InProgress, inst.State, "战斗2003未结束，不应完成");
    }

    [Test]
    public void Quest1_ChooseInvalidBranch_DoesNotAdvance()
    {
        _condSys.MockItems[1] = 99;
        _condSys.Initialize();

        var inst = QuestManager.Instance.AcceptQuest(1);
        inst.ChooseBranch(999);

        Assert.AreEqual(1, inst.CurrentNodeId, "无效分支不应推进");
    }

    [Test]
    public void QuestManager_ForceComplete_SetsCompleted()
    {
        _condSys.MockItems[1] = 99;
        _condSys.Initialize();

        var inst = QuestManager.Instance.AcceptQuest(1);
        QuestManager.Instance.ForceComplete(1);

        Assert.AreEqual(QuestState.Completed, inst.State);
    }

    [Test]
    public void QuestManager_ForceFail_SetsFailed()
    {
        _condSys.MockItems[1] = 99;
        _condSys.Initialize();

        var inst = QuestManager.Instance.AcceptQuest(1);
        QuestManager.Instance.ForceFail(1);

        Assert.AreEqual(QuestState.Failed, inst.State);
    }

    [Test]
    public void QuestManager_BuildSaveData_RestoreFromSaveData()
    {
        _condSys.MockItems[1] = 99;
        _condSys.Initialize();

        var inst = QuestManager.Instance.AcceptQuest(1);

        var save = QuestManager.Instance.BuildSaveData();
        Assert.AreEqual(1, save.entries.Count);
        Assert.AreEqual(1, save.entries[0].questId);
        Assert.AreEqual((int)QuestState.InProgress, save.entries[0].state);

        QuestManager.Instance.Clear();
        Assert.IsNull(QuestManager.Instance.GetQuest(1));

        QuestManager.Instance.RestoreFromSaveData(save);
        var restored = QuestManager.Instance.GetQuest(1);
        Assert.IsNotNull(restored);
        Assert.AreEqual(QuestState.InProgress, restored.State);
        Assert.AreEqual(1, restored.CurrentNodeId);
    }

    [Test]
    public void QuestManager_IsQuestCompleted_ReturnsFalse_WhenNotAccepted()
    {
        Assert.IsFalse(QuestManager.Instance.IsQuestCompleted(1));
    }

    [Test]
    public void QuestManager_IsQuestActive_ReturnsFalse_WhenNotAccepted()
    {
        Assert.IsFalse(QuestManager.Instance.IsQuestActive(1));
    }

    [Test]
    public void QuestManager_GetQuestState_ReturnsNotAccepted()
    {
        Assert.AreEqual(QuestState.NotAccepted, QuestManager.Instance.GetQuestState(1));
    }

    [Test]
    public void QuestManager_NonExistentQuest_CanAccept_ReturnsFalse()
    {
        Assert.IsFalse(QuestManager.Instance.CanAccept(99999));
    }

    [Test]
    public void Quest1_FullFlow_DialogToBranch3ToComplete()
    {
        _condSys.MockItems[1] = 99;
        _condSys.MockDialogFinished[1001001] = true;
        _condSys.Initialize();

        var inst = QuestManager.Instance.AcceptQuest(1);

        QuestManager.Instance.CheckCurrentNodeCompletion(1);
        Assert.IsTrue(inst.CompletedNodeIds.Contains(1));

        inst.ChooseBranch(3);
        Assert.AreEqual(3, inst.CurrentNodeId);

        QuestManager.Instance.CheckCurrentNodeCompletion(1);
        Assert.AreEqual(QuestState.InProgress, inst.State, "战斗2003未结束");

        _condSys.MockBattleResults[2003] = true;
        _condSys.Initialize();

        QuestManager.Instance.CheckCurrentNodeCompletion(1);
        Assert.AreEqual(QuestState.Completed, inst.State, "战斗2003结束，进入-1→完成");
    }
}
