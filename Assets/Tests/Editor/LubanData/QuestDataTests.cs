using NUnit.Framework;

class QuestDataTests
{
    private cfg.Tables tables;

    [SetUp]
    public void Setup()
    {
        tables = TestDataLoader.LoadTables();
    }

    [Test]
    public void Quest_Count_IsOne()
    {
        Assert.AreEqual(1, tables.TbQuest.DataList.Count);
    }

    [Test]
    public void Quest_Id1_AllFieldsMatch()
    {
        var quest = tables.TbQuest.Get(1);
        Assert.AreEqual(1, quest.Id);
        Assert.AreEqual(0, quest.Type);
        Assert.AreEqual(4, quest.State);
        Assert.AreEqual(1, quest.Conditionid);
        Assert.AreEqual("", quest.Name);
        Assert.AreEqual("", quest.Shorttext);
        Assert.AreEqual("", quest.Longtext);
    }

    [Test]
    public void Quest_Conditionid_RefResolved()
    {
        var quest = tables.TbQuest.Get(1);
        Assert.IsNotNull(quest.Conditionid_Ref);
        Assert.AreEqual(1, quest.Conditionid_Ref.Id);
    }

    [Test]
    public void Quest_EmptyStrings_NotNull()
    {
        var quest = tables.TbQuest.Get(1);
        Assert.IsNotNull(quest.Name);
        Assert.IsNotNull(quest.Shorttext);
        Assert.IsNotNull(quest.Longtext);
    }
}
