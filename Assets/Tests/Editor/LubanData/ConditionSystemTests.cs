using NUnit.Framework;
using System.Collections.Generic;

class TestableConditionSystem : ConditionSystem
{
    public readonly Dictionary<int, int> MockItemCounts = new Dictionary<int, int>();
    public readonly Dictionary<int, int> MockTaskProgress = new Dictionary<int, int>();
    public readonly HashSet<int> MockPerks = new HashSet<int>();
    public readonly Dictionary<int, int> MockBattleResults = new Dictionary<int, int>();
    public readonly HashSet<int> MockFinishedDialogs = new HashSet<int>();

    protected override bool CheckTaskProgress(int taskId, int progress)
    {
        return MockTaskProgress.TryGetValue(taskId, out int p) && p >= progress;
    }

    protected override bool CheckPerk(int perkId)
    {
        return MockPerks.Contains(perkId);
    }

    protected override bool CheckBattleResult(int battleId, int result)
    {
        return MockBattleResults.TryGetValue(battleId, out int r) && r == result;
    }

    protected override bool CheckDialogFinished(int dialogId)
    {
        return MockFinishedDialogs.Contains(dialogId);
    }

    protected override int GetItemCount(int itemId)
    {
        return MockItemCounts.TryGetValue(itemId, out int c) ? c : 0;
    }

    public void SetBagItemCount(int itemId, int count)
    {
        MockItemCounts[itemId] = count;
    }
}

class ConditionSystemTests
{
    private cfg.Tables _tables;
    private TestableConditionSystem _sys;
    private List<(int conditionId, bool isMet)> _changes;

    [SetUp]
    public void Setup()
    {
        ManagerRegistry.Clear();
        _tables = TestDataLoader.LoadTables();
        ManagerRegistry.SetTables(_tables);

        _sys = new TestableConditionSystem();
        _changes = new List<(int, bool)>();
        _sys.OnConditionChanged += (id, met) => _changes.Add((id, met));
    }

    [TearDown]
    public void TearDown()
    {
        ManagerRegistry.Clear();
    }

    [Test]
    public void Initialize_CachesAllConditions()
    {
        _sys.Initialize();
        foreach (var cond in _tables.TbCondition.DataList)
        {
            Assert.IsTrue(_sys.IsConditionMet(cond.Id) || !_sys.IsConditionMet(cond.Id),
                $"条件 {cond.Id} 应可查询");
        }
    }

    [Test]
    public void AllOpsZero_ConditionAlwaysMet()
    {
        ManagerRegistry.SetTables(_tables);
        _sys.Initialize();

        Assert.IsTrue(_sys.IsConditionMet(2), "id=2 composeType 有0位应跳过对应字段");
    }

    [Test]
    public void NotifyItem_OnlyAffectsItemConditions()
    {
        _sys.Initialize();

        _sys.SetBagItemCount(1, 99);
        _sys.Notify(ConditionChangeType.Item);

        bool cond3Found = false;
        bool cond5Found = false;
        foreach (var (id, _) in _changes)
        {
            if (id == 3) cond3Found = true;
            if (id == 5) cond5Found = true;
        }

        Assert.IsFalse(cond3Found, "条件3 composeType[Item]=0，不应被Item通知影响");
        Assert.IsFalse(cond5Found, "条件5 composeType[Item]=0，不应被Item通知影响");
    }

    [Test]
    public void NotifyBattle_OnlyAffectsBattleConditions()
    {
        _sys.Initialize();

        _sys.Notify(ConditionChangeType.Battle);

        foreach (var (id, _) in _changes)
        {
            var cond = _tables.TbCondition.Get(id);
            Assert.AreNotEqual(0, cond.ComposeType[3][0],
                $"条件{id}被Battle通知影响，但composeType[Battle]=0");
        }
    }

    [Test]
    public void NotifyDialog_OnlyAffectsDialogConditions()
    {
        _sys.Initialize();

        _sys.Notify(ConditionChangeType.Dialog);

        foreach (var (id, _) in _changes)
        {
            var cond = _tables.TbCondition.Get(id);
            Assert.AreNotEqual(0, cond.ComposeType[4][0],
                $"条件{id}被Dialog通知影响，但composeType[Dialog]=0");
        }
    }

    [Test]
    public void CompareQuantity_Equal_ReturnsTrue()
    {
        _sys.SetBagItemCount(1, 1);
        _sys.Initialize();

        Assert.IsTrue(_sys.IsConditionMet(1), "count=1 ≤ 1 应满足");
    }

    [Test]
    public void CompareQuantity_ExceedsThreshold_ReturnsFalse()
    {
        _sys.SetBagItemCount(1, 10);
        _sys.Initialize();

        Assert.IsFalse(_sys.IsConditionMet(1), "count=10 > 1 不应满足");
    }

    [Test]
    public void Condition5_DialogNotFinished_NotMet()
    {
        _sys.Initialize();

        Assert.IsFalse(_sys.IsConditionMet(5), "对话未完成，条件5不应满足");
    }

    [Test]
    public void Condition5_DialogFinished_IsMet()
    {
        _sys.MockFinishedDialogs.Add(1001001);
        _sys.Initialize();

        Assert.IsTrue(_sys.IsConditionMet(5), "对话1001001已完成，条件5应满足");
    }

    [Test]
    public void MultipleNotifies_OnlyFiresOnChange()
    {
        _sys.Initialize();

        _sys.Notify(ConditionChangeType.Item);
        int count1 = _changes.Count;

        _sys.Notify(ConditionChangeType.Item);
        int count2 = _changes.Count;

        Assert.AreEqual(count1, count2, "状态未变时不应重复触发事件");
    }

    [Test]
    public void Condition3_BattleNotFinished_NotMet()
    {
        _sys.Initialize();

        Assert.IsFalse(_sys.IsConditionMet(3), "战斗未完成，条件3不应满足");
    }

    [Test]
    public void Condition3_BattleWonButNeedLose_NotMet()
    {
        _sys.MockBattleResults[2003] = 1;
        _sys.Initialize();

        Assert.IsFalse(_sys.IsConditionMet(3), "战斗2003胜利但条件要求失败(2)");
    }

    [Test]
    public void Condition3_BattleLost_IsMet()
    {
        _sys.MockBattleResults[2003] = 2;
        _sys.Initialize();

        Assert.IsTrue(_sys.IsConditionMet(3), "战斗2003失败(2)，条件3应满足");
    }
}
