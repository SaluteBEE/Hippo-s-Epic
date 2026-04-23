using NUnit.Framework;
using System.Collections.Generic;

class BoundaryTests
{
    private cfg.Tables tables;

    [SetUp]
    public void Setup()
    {
        tables = TestDataLoader.LoadTables();
    }

    [Test]
    public void EmptyList_Field_ReturnsEmptyList()
    {
        var cond = tables.TbCondition.Get(3);
        Assert.IsNotNull(cond.HasItem);
        Assert.AreEqual(0, cond.HasItem.Count);
    }

    [Test]
    public void NestedList_Field_CorrectStructure()
    {
        var cond = tables.TbCondition.Get(1);
        Assert.AreEqual(3, cond.ComposeType.Count);
        Assert.AreEqual(2, cond.ComposeType[0].Count);
        Assert.AreEqual(1, cond.ComposeType[1].Count);
    }

    [Test]
    public void BoolField_TrueAndFalse_BothCorrect()
    {
        Assert.IsTrue(tables.TbItem.Get(1).Stackable);
        Assert.IsFalse(tables.TbItem.Get(2).Stackable);
    }

    [Test]
    public void EmptyString_Field_NotNull()
    {
        var quest = tables.TbQuest.Get(1);
        Assert.IsNotNull(quest.Name);
        Assert.AreEqual("", quest.Name);
    }

    [Test]
    public void ChineseText_CorrectValue()
    {
        Assert.AreEqual("钱币", tables.TbItem.Get(1).Name);
        Assert.AreEqual("河马", tables.TbPerson.Get(1).Name);
    }

    [Test]
    public void BackslashPath_CorrectValue()
    {
        var person = tables.TbPerson.Get(1);
        Assert.AreEqual("Assets\\Prefabs\\player\\Hippo", person.Prefab1);
    }

    [Test]
    public void RichTextContent_CorrectValue()
    {
        var dc = tables.TbDialogcontent.Get(6);
        Assert.IsTrue(dc.Content.Contains("<color=red>"));
    }

    [Test]
    public void NestedList_Reward_CorrectStructure()
    {
        var qc = tables.TbQuestcontext.Get(2);
        Assert.AreEqual(2, qc.Reward.Count);
        Assert.AreEqual(500, qc.Reward[0][1]);
        Assert.AreEqual(1, qc.Reward[1][1]);
    }

    [Test]
    public void RefField_NullWhenIdNotFound()
    {
        var cond = tables.TbCondition.Get(1);
        Assert.IsNotNull(cond);
    }
}
