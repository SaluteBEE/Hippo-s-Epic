using NUnit.Framework;
using System.Collections.Generic;

class ConditionDataTests
{
    private cfg.Tables tables;

    [SetUp]
    public void Setup()
    {
        tables = TestDataLoader.LoadTables();
    }

    [Test]
    public void Condition_Count_IsFive()
    {
        Assert.AreEqual(5, tables.TbCondition.DataList.Count);
    }

    [Test]
    public void Condition_Id1_AllFieldsMatch()
    {
        var cond = tables.TbCondition.Get(1);
        Assert.AreEqual(1, cond.Id);
        Assert.AreEqual(5, cond.ComposeType.Count);
        CollectionAssert.AreEqual(new List<int> { 2, 3 }, cond.ComposeType[0]);
        CollectionAssert.AreEqual(new List<int> { 4 }, cond.ComposeType[1]);
        CollectionAssert.AreEqual(new List<int> { 1, 1 }, cond.HasItem[0]);
        CollectionAssert.AreEqual(new List<int> { 1, 1 }, cond.TaskState[0]);
        Assert.AreEqual(new List<int> { 1, 2 }, cond.HasPerkId);
        CollectionAssert.AreEqual(new List<int> { 2002, 1 }, cond.BattleId[0]);
        Assert.AreEqual(0, cond.DialogFinishId.Count);
    }

    [Test]
    public void Condition_Id2_AllFieldsMatch()
    {
        var cond = tables.TbCondition.Get(2);
        Assert.AreEqual(2, cond.Id);
        Assert.AreEqual(5, cond.ComposeType.Count);
        CollectionAssert.AreEqual(new List<int> { 0 }, cond.ComposeType[0]);
        CollectionAssert.AreEqual(new List<int> { 1, 1 }, cond.HasItem[0]);
        Assert.AreEqual(0, cond.TaskState.Count);
        Assert.AreEqual(0, cond.HasPerkId.Count);
        CollectionAssert.AreEqual(new List<int> { 2002, 1 }, cond.BattleId[0]);
        Assert.AreEqual(0, cond.DialogFinishId.Count);
    }

    [Test]
    public void Condition_Id3_AllFieldsMatch()
    {
        var cond = tables.TbCondition.Get(3);
        Assert.AreEqual(3, cond.Id);
        Assert.AreEqual(5, cond.ComposeType.Count);
        Assert.AreEqual(0, cond.HasItem.Count);
        Assert.AreEqual(0, cond.TaskState.Count);
        Assert.AreEqual(0, cond.HasPerkId.Count);
        CollectionAssert.AreEqual(new List<int> { 2003, 2 }, cond.BattleId[0]);
        Assert.AreEqual(0, cond.DialogFinishId.Count);
    }

    [Test]
    public void Condition_Id4_AllFieldsMatch()
    {
        var cond = tables.TbCondition.Get(4);
        Assert.AreEqual(4, cond.Id);
        Assert.AreEqual(5, cond.ComposeType.Count);
        CollectionAssert.AreEqual(new List<int> { 2004, 2 }, cond.BattleId[0]);
        Assert.AreEqual(0, cond.DialogFinishId.Count);
    }

    [Test]
    public void Condition_Id5_HasDialogFinishId()
    {
        var cond = tables.TbCondition.Get(5);
        Assert.AreEqual(5, cond.Id);
        Assert.AreEqual(0, cond.HasItem.Count);
        Assert.AreEqual(0, cond.TaskState.Count);
        Assert.AreEqual(0, cond.HasPerkId.Count);
        Assert.AreEqual(0, cond.BattleId.Count);
        Assert.AreEqual(new List<int> { 1001001 }, cond.DialogFinishId);
    }

    [Test]
    public void Condition_AllLists_NotNull()
    {
        foreach (var cond in tables.TbCondition.DataList)
        {
            Assert.IsNotNull(cond.ComposeType, $"Condition {cond.Id} ComposeType 不应为 null");
            Assert.IsNotNull(cond.HasItem, $"Condition {cond.Id} HasItem 不应为 null");
            Assert.IsNotNull(cond.TaskState, $"Condition {cond.Id} TaskState 不应为 null");
            Assert.IsNotNull(cond.HasPerkId, $"Condition {cond.Id} HasPerkId 不应为 null");
            Assert.IsNotNull(cond.BattleId, $"Condition {cond.Id} BattleId 不应为 null");
            Assert.IsNotNull(cond.DialogFinishId, $"Condition {cond.Id} DialogFinishId 不应为 null");
        }
    }

    [Test]
    public void Condition_ComposeType_InnerListsNotNull()
    {
        foreach (var cond in tables.TbCondition.DataList)
        {
            foreach (var inner in cond.ComposeType)
            {
                Assert.IsNotNull(inner, $"Condition {cond.Id} ComposeType 内层列表不应为 null");
            }
        }
    }
}
