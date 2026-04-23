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
    public void Condition_Id1_SimpleListFields()
    {
        var cond = tables.TbCondition.Get(1);
        Assert.AreEqual(1, cond.Id);
        Assert.AreEqual(new List<int> { 1, 2, 3 }, cond.ConditionType);
        Assert.AreEqual(new List<int> { 1, 1 }, cond.HasItem);
        Assert.AreEqual(new List<int> { 1, 1 }, cond.TaskState);
        Assert.AreEqual(new List<int> { 1, 2 }, cond.HasPerkId);
    }

    [Test]
    public void Condition_Id1_NestedList_ComposeType()
    {
        var cond = tables.TbCondition.Get(1);
        Assert.AreEqual(3, cond.ComposeType.Count);
        CollectionAssert.AreEqual(new List<int> { 2, 3 }, cond.ComposeType[0]);
        CollectionAssert.AreEqual(new List<int> { 2 }, cond.ComposeType[1]);
        CollectionAssert.AreEqual(new List<int> { 4 }, cond.ComposeType[2]);
    }

    [Test]
    public void Condition_Id2_AllFieldsMatch()
    {
        var cond = tables.TbCondition.Get(2);
        Assert.AreEqual(2, cond.Id);
        Assert.AreEqual(new List<int> { 1 }, cond.ConditionType);
        Assert.AreEqual(1, cond.ComposeType.Count);
        CollectionAssert.AreEqual(new List<int> { 1 }, cond.ComposeType[0]);
        Assert.AreEqual(new List<int> { 1, 1 }, cond.HasItem);
        Assert.AreEqual(0, cond.TaskState.Count);
        Assert.AreEqual(0, cond.HasPerkId.Count);
    }

    [Test]
    public void Condition_Id3_AllFieldsMatch()
    {
        var cond = tables.TbCondition.Get(3);
        Assert.AreEqual(3, cond.Id);
        Assert.AreEqual(new List<int> { 1 }, cond.ConditionType);
        Assert.AreEqual(0, cond.HasItem.Count);
        Assert.AreEqual(0, cond.TaskState.Count);
    }

    [Test]
    public void Condition_Id4_AllFieldsMatch()
    {
        var cond = tables.TbCondition.Get(4);
        Assert.AreEqual(4, cond.Id);
        Assert.AreEqual(new List<int> { 1 }, cond.ConditionType);
    }

    [Test]
    public void Condition_Id5_AllFieldsMatch()
    {
        var cond = tables.TbCondition.Get(5);
        Assert.AreEqual(5, cond.Id);
        Assert.AreEqual(new List<int> { 1 }, cond.ConditionType);
        Assert.AreEqual(new List<int> { 1, 1 }, cond.HasItem);
    }

    [Test]
    public void Condition_AllLists_NotNull()
    {
        foreach (var cond in tables.TbCondition.DataList)
        {
            Assert.IsNotNull(cond.ConditionType, $"Condition {cond.Id} ConditionType 不应为 null");
            Assert.IsNotNull(cond.ComposeType, $"Condition {cond.Id} ComposeType 不应为 null");
            Assert.IsNotNull(cond.HasItem, $"Condition {cond.Id} HasItem 不应为 null");
            Assert.IsNotNull(cond.TaskState, $"Condition {cond.Id} TaskState 不应为 null");
            Assert.IsNotNull(cond.HasPerkId, $"Condition {cond.Id} HasPerkId 不应为 null");
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
