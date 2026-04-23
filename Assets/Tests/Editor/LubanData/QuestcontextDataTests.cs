using NUnit.Framework;
using System.Collections.Generic;

class QuestcontextDataTests
{
    private cfg.Tables tables;

    [SetUp]
    public void Setup()
    {
        tables = TestDataLoader.LoadTables();
    }

    [Test]
    public void Questcontext_Count_IsFour()
    {
        Assert.AreEqual(4, tables.TbQuestcontext.DataList.Count);
    }

    [Test]
    public void Questcontext_Id1_AllFieldsMatch()
    {
        var qc = tables.TbQuestcontext.Get(1);
        Assert.AreEqual(1, qc.Id);
        Assert.AreEqual(1, qc.Questid);
        Assert.AreEqual("去找拳王对话", qc.Des);
        CollectionAssert.AreEqual(new List<int> { 1, 2 }, qc.Conditionid);
        CollectionAssert.AreEqual(new List<int> { 2, 3 }, qc.NextState);
        Assert.AreEqual(0, qc.Reward.Count);
    }

    [Test]
    public void Questcontext_Id2_AllFieldsMatch()
    {
        var qc = tables.TbQuestcontext.Get(2);
        Assert.AreEqual(2, qc.Id);
        Assert.AreEqual(1, qc.Questid);
        Assert.AreEqual("击败拳王", qc.Des);
        CollectionAssert.AreEqual(new List<int> { 3 }, qc.Conditionid);
        Assert.AreEqual(0, qc.NextState.Count);
    }

    [Test]
    public void Questcontext_Id2_Reward_NestedList()
    {
        var qc = tables.TbQuestcontext.Get(2);
        Assert.AreEqual(2, qc.Reward.Count);
        CollectionAssert.AreEqual(new List<int> { 1, 500 }, qc.Reward[0]);
        CollectionAssert.AreEqual(new List<int> { 2, 1 }, qc.Reward[1]);
    }

    [Test]
    public void Questcontext_Id3_AllFieldsMatch()
    {
        var qc = tables.TbQuestcontext.Get(3);
        Assert.AreEqual(3, qc.Id);
        CollectionAssert.AreEqual(new List<int> { 4 }, qc.Conditionid);
        Assert.AreEqual(0, qc.Reward.Count);
    }

    [Test]
    public void Questcontext_Id4_AllFieldsMatch()
    {
        var qc = tables.TbQuestcontext.Get(4);
        Assert.AreEqual(4, qc.Id);
        CollectionAssert.AreEqual(new List<int> { 5 }, qc.Conditionid);
        Assert.AreEqual(0, qc.Reward.Count);
    }

    [Test]
    public void Questcontext_AllBelongToQuest1()
    {
        foreach (var qc in tables.TbQuestcontext.DataList)
        {
            Assert.AreEqual(1, qc.Questid);
        }
    }

    [Test]
    public void Questcontext_AllLists_NotNull()
    {
        foreach (var qc in tables.TbQuestcontext.DataList)
        {
            Assert.IsNotNull(qc.Conditionid);
            Assert.IsNotNull(qc.NextState);
            Assert.IsNotNull(qc.Reward);
        }
    }

    [Test]
    public void Questcontext_Questid_RefResolved()
    {
        foreach (var qc in tables.TbQuestcontext.DataList)
        {
            Assert.IsNotNull(qc.Questid_Ref, $"Questcontext {qc.Id} Questid_Ref 不应为 null");
            Assert.AreEqual(1, qc.Questid_Ref.Id);
        }
    }

    [Test]
    public void Questcontext_Conditionid_RefResolved()
    {
        foreach (var qc in tables.TbQuestcontext.DataList)
        {
            Assert.IsNotNull(qc.Conditionid_Ref, $"Questcontext {qc.Id} Conditionid_Ref 不应为 null");
            Assert.AreEqual(qc.Conditionid.Count, qc.Conditionid_Ref.Count,
                $"Questcontext {qc.Id} Conditionid_Ref 数量不匹配");
            for (int i = 0; i < qc.Conditionid.Count; i++)
            {
                Assert.IsNotNull(qc.Conditionid_Ref[i],
                    $"Questcontext {qc.Id} Conditionid_Ref[{i}] 不应为 null");
                Assert.AreEqual(qc.Conditionid[i], qc.Conditionid_Ref[i].Id);
            }
        }
    }
}
